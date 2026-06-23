const NEXA_RECIPE_SCHEMA_VERSION = 1;
const NEXA_RECIPE_STEP_TYPES = new Set([
  'navigate',
  'observe',
  'resolveTarget',
  'click',
  'input',
  'select',
  'wait',
  'verify',
  'humanCheckpoint'
]);
const NEXA_RECIPE_PARAMETER_TYPES = new Set(['text', 'number', 'money', 'date', 'select', 'boolean']);

function createRecipeV1(input) {
  const now = new Date().toISOString();
  const recipeId = input.recipeId || `recipe-${Date.now().toString(36)}`;
  return {
    schemaVersion: NEXA_RECIPE_SCHEMA_VERSION,
    recipeId,
    name: input.name || 'Nueva receta',
    description: input.description || '',
    createdAt: input.createdAt || now,
    updatedAt: input.updatedAt || now,
    startUrl: input.startUrl || '',
    parameters: normalizeRecipeParameters(input.parameters || []),
    steps: normalizeRecipeSteps(input.steps || []),
    humanCheckpoints: input.humanCheckpoints || [],
    safety: input.safety || {
      blockCredentialAutomation: true,
      redactSensitiveValues: true
    },
    metadata: input.metadata || {},
    status: input.status || 'draft-v1'
  };
}

function normalizeRecipeParameters(parameters) {
  return parameters
    .filter(Boolean)
    .map((parameter) => {
      const type = NEXA_RECIPE_PARAMETER_TYPES.has(parameter.type) ? parameter.type : 'text';
      return {
        name: parameter.name || '',
        label: parameter.label || parameter.name || '',
        type,
        required: Boolean(parameter.required),
        sensitive: Boolean(parameter.sensitive),
        defaultValue: parameter.defaultValue ?? null,
        validation: parameter.validation || {}
      };
    })
    .filter((parameter) => parameter.name);
}

function normalizeRecipeSteps(steps) {
  return steps
    .filter(Boolean)
    .map((step, index) => {
      const type = NEXA_RECIPE_STEP_TYPES.has(step.type) ? step.type : mapLegacyActionType(step.actionType || step.type);
      return {
        stepId: step.stepId || `step_${String(index + 1).padStart(3, '0')}`,
        type,
        label: step.label || defaultStepLabel(type, step),
        target: step.target || {},
        value: step.value ?? null,
        verify: step.verify || step.verificationHint || {},
        onFailure: step.onFailure || { strategy: 'fail' },
        timeoutMs: Number(step.timeoutMs || 5000)
      };
    });
}

function mapLegacyActionType(actionType) {
  switch (actionType) {
    case 'navigate':
      return 'navigate';
    case 'click':
      return 'click';
    case 'input':
    case 'change':
      return 'input';
    case 'select':
      return 'select';
    case 'submit':
      return 'humanCheckpoint';
    default:
      return 'observe';
  }
}

function defaultStepLabel(type, step) {
  const target = step.target || {};
  const label = target.accessibleName || target.visibleText || target.semantic || target.name || '';
  switch (type) {
    case 'navigate':
      return `Navegar a ${step.url || step.startUrl || ''}`.trim();
    case 'click':
      return `Click en ${label || 'target'}`;
    case 'input':
      return `Completar ${label || 'campo'}`;
    case 'select':
      return `Seleccionar ${label || 'opcion'}`;
    case 'humanCheckpoint':
      return step.reason || 'Intervencion humana';
    default:
      return type;
  }
}

function recipeFromLearningDraft(draft, payload) {
  const steps = [];
  const parameters = [];
  const sensitiveFields = new Set(draft.sensitiveFields || []);
  const humanCheckpoints = [...(draft.humanCheckpoints || [])];
  const sourceSteps = draft.steps || [];

  for (const sourceStep of sourceSteps) {
    const converted = learningStepToRecipeStep(sourceStep, parameters, sensitiveFields, humanCheckpoints);
    if (converted) {
      steps.push(converted);
    }
  }

  return createRecipeV1({
    recipeId: draft.recipeId,
    name: payload.name || draft.name,
    description: payload.description || draft.description,
    createdAt: draft.createdAt,
    startUrl: draft.startUrl,
    steps,
    parameters,
    sensitiveFields: Array.from(sensitiveFields),
    humanCheckpoints,
    metadata: {
      source: 'learning-v0',
      capturedStepCount: sourceSteps.length
    }
  });
}

function learningStepToRecipeStep(step, parameters, sensitiveFields, humanCheckpoints) {
  const target = step.target || {};
  if (step.valueRedacted || target.isPassword || target.isCredentialLike) {
    const checkpoint = {
      timestamp: step.timestamp,
      reason: 'Campo sensible detectado',
      url: step.url
    };
    humanCheckpoints.push(checkpoint);
    if (target.accessibleName || target.visibleText) {
      sensitiveFields.add(target.accessibleName || target.visibleText);
    }
    return {
      type: 'humanCheckpoint',
      label: 'Completar credenciales',
      reason: 'Campo sensible detectado',
      target: buildRecipeTarget(target),
      verify: {},
      timeoutMs: 0
    };
  }

  if (step.actionType === 'navigate') {
    return {
      type: 'navigate',
      label: `Navegar a ${step.url}`,
      url: step.url,
      target: {},
      verify: { expectUrlChange: true },
      timeoutMs: 10000
    };
  }

  if (step.actionType === 'click' || step.actionType === 'submit') {
    return {
      type: step.actionType === 'submit' ? 'humanCheckpoint' : 'click',
      label: step.actionType === 'submit' ? 'Confirmacion humana' : `Click en ${target.accessibleName || target.visibleText || 'target'}`,
      target: buildRecipeTarget(target),
      verify: step.verificationHint || { expectDomChange: true },
      timeoutMs: 5000
    };
  }

  if (step.actionType === 'input' || step.actionType === 'change') {
    const parameterName = parameterNameForTarget(target, parameters.length);
    if (!parameters.some((parameter) => parameter.name === parameterName)) {
      parameters.push({
        name: parameterName,
        label: target.accessibleName || target.visibleText || parameterName,
        type: 'text',
        required: true,
        sensitive: false,
        defaultValue: step.value || null,
        validation: {}
      });
    }
    return {
      type: 'input',
      label: `Completar ${target.accessibleName || target.visibleText || parameterName}`,
      target: buildRecipeTarget(target),
      value: `{{${parameterName}}}`,
      verify: { expectDomChange: false },
      timeoutMs: 5000
    };
  }

  if (step.actionType === 'select') {
    return {
      type: 'select',
      label: `Seleccionar ${target.accessibleName || target.visibleText || 'opcion'}`,
      target: buildRecipeTarget(target),
      value: step.value || null,
      verify: { expectDomChange: false },
      timeoutMs: 5000
    };
  }

  return null;
}

function buildRecipeTarget(target) {
  return {
    semantic: target.accessibleName || target.visibleText || '',
    role: target.elementKind ? [target.elementKind] : [],
    observedText: target.visibleText || '',
    stableSelectors: target.stableSelectors || [],
    bestSelector: target.bestSelector || null,
    nearbyText: target.nearbyText || '',
    formContext: target.formContext || '',
    riskFlags: target.riskFlags || []
  };
}

function parameterNameForTarget(target, index) {
  const raw = target.name || target.accessibleName || target.visibleText || `param_${index + 1}`;
  const normalized = String(raw)
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '');
  return normalized || `param_${index + 1}`;
}

function validateRecipeV1(recipe) {
  const errors = [];
  if (!recipe || typeof recipe !== 'object') {
    return { ok: false, errors: ['RecipeMissing'] };
  }
  if (recipe.schemaVersion !== NEXA_RECIPE_SCHEMA_VERSION) {
    errors.push('SchemaVersionInvalid');
  }
  if (!recipe.recipeId) {
    errors.push('RecipeIdMissing');
  }
  if (!recipe.name) {
    errors.push('NameMissing');
  }
  if (!Array.isArray(recipe.steps)) {
    errors.push('StepsMissing');
  } else {
    recipe.steps.forEach((step, index) => {
      if (!NEXA_RECIPE_STEP_TYPES.has(step.type)) {
        errors.push(`StepTypeInvalid:${index}`);
      }
      if (!step.stepId) {
        errors.push(`StepIdMissing:${index}`);
      }
    });
  }
  return { ok: errors.length === 0, errors };
}

function redactParameterValue(parameter, value) {
  if (parameter && parameter.sensitive) {
    return '[redacted]';
  }
  return value;
}

if (typeof module !== 'undefined') {
  module.exports = {
    NEXA_RECIPE_SCHEMA_VERSION,
    NEXA_RECIPE_STEP_TYPES,
    NEXA_RECIPE_PARAMETER_TYPES,
    createRecipeV1,
    normalizeRecipeParameters,
    normalizeRecipeSteps,
    recipeFromLearningDraft,
    learningStepToRecipeStep,
    validateRecipeV1,
    redactParameterValue
  };
}
