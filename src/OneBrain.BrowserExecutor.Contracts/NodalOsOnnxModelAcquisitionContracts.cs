namespace OneBrain.BrowserExecutor.Contracts;

// M200/M201 — ONNX model acquisition source decision and controlled acquisition.

public enum NodalOsOnnxModelAcquisitionDecision
{
    VerifiedOnnxDownload,
    ControlledConversion,
    CppPaddleInference,
    PythonPaddleOcrLegacy,
    HumanReview,
    SourceBlocked,
    ConversionToolchainBlocked,
    NotDecided
}

public enum NodalOsOnnxModelSupplyChainRisk
{
    OfficialPublisher,
    CommunityVerified,
    CommunityUnverified,
    SelfConverted,
    Unknown,
    BlockedByPolicy
}

public sealed record NodalOsOnnxModelAcquisitionSource(
    string SourceId,
    string Origin,
    string Url,
    string LicenseIdentifier,
    string LicenseUrl,
    bool RequiresAttribution,
    NodalOsOnnxModelSupplyChainRisk SupplyChainRisk,
    bool PinnedVersion,
    bool ChecksumPublished,
    bool NoAuthority);

public sealed record NodalOsOnnxModelConversionPlan(
    string PlanId,
    string Toolchain,
    IReadOnlyList<string> Steps,
    string PythonVersion,
    bool RequiresVirtualEnvironment,
    bool RequiresWindows,
    long EstimatedToolchainSizeBytes,
    bool RequiresLicenseReview,
    bool NoAuthority);

public sealed record NodalOsOnnxModelRollbackPlan(
    string PlanId,
    string TargetDirectory,
    IReadOnlyList<string> FilesToRemove,
    bool DeletesOnlyOnnx,
    bool NoAuthority);

public sealed record NodalOsOnnxModelSourceDecision(
    string DecisionId,
    NodalOsOnnxModelAcquisitionDecision Primary,
    NodalOsOnnxModelAcquisitionDecision Secondary,
    NodalOsOnnxModelAcquisitionDecision Tertiary,
    NodalOsOnnxModelAcquisitionDecision Fallback,
    NodalOsOnnxModelAcquisitionSource RecommendedSource,
    NodalOsOnnxModelConversionPlan ConversionPlan,
    NodalOsOnnxModelRollbackPlan RollbackPlan,
    string Reason,
    bool OnnxRuntimeDotNetRemainsPrimary,
    bool PythonNotPrimary,
    bool SaasDisabled,
    bool ProductionPublicOcrBlocked,
    bool NoAuthority);
