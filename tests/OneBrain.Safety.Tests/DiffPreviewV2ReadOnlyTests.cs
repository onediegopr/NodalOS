using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OneBrain.Safety.Tests;

[TestClass]
[TestCategory("DiffPreviewV2ReadOnly")]
public sealed class DiffPreviewV2ReadOnlyTests
{
    private const string SidepanelHtmlPath = "browser-extension/onebrain-chrome-lab/sidepanel.html";
    private const string SidepanelJsPath = "browser-extension/onebrain-chrome-lab/sidepanel.js";
    private const string SidepanelCssPath = "browser-extension/onebrain-chrome-lab/sidepanel.css";
    private const string InstalledHarnessPath = "scripts/verify-installed-sidepanel.mjs";

    [TestMethod]
    public void DiffPreviewV2_SurfaceIsVisibleAndReadOnly()
    {
        var section = ExtractChangeCandidateSection(ReadRepoText(SidepanelHtmlPath));

        StringAssert.Contains(section, "Cambios candidatos");
        StringAssert.Contains(section, "Diff Preview V2");
        StringAssert.Contains(section, "Detalle de candidato");
        StringAssert.Contains(section, "Notas humanas de revisión");
        StringAssert.Contains(section, "No genera diff real, patch, comandos ni modificaciones.");
        StringAssert.Contains(section, "Copiar detalle");
        StringAssert.Contains(section, "Marcar este revisado");
        StringAssert.Contains(section, "Guardar nota");
        StringAssert.Contains(section, "Limpiar nota");
    }

    [TestMethod]
    public void DiffPreviewV2_ContractForcesNoDiffNoPatchNoExecution()
    {
        var js = ReadRepoText(SidepanelJsPath);

        foreach (var expected in new[]
        {
            "DIFF_PREVIEW_V2_FLAGS",
            "readOnly: true",
            "diffGenerated: false",
            "patchGenerated: false",
            "commandsExecuted: false",
            "filesModified: false",
            "executionReady: false",
            "productFilesModified: false",
            "realDiffAvailable: false",
            "normalizeCandidateDiffPreview",
            "normalizeCandidateReviewNotes",
            "buildSelectedCandidateDetailSummary",
            "sanitizeCandidateReviewNote"
        })
        {
            StringAssert.Contains(js, expected);
        }
    }

    [TestMethod]
    public void DiffPreviewV2_CopySummaryIsMetadataOnly()
    {
        var js = ReadRepoText(SidepanelJsPath);
        var start = js.IndexOf("function buildSelectedCandidateDetailSummary", StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "Candidate detail copy summary is missing.");
        var end = js.IndexOf("function renderWorkspaceUnderstanding", start, StringComparison.Ordinal);
        Assert.IsTrue(end > start, "Candidate detail copy summary end marker is missing.");
        var summaryFunction = js[start..end];

        foreach (var expected in new[]
        {
            "NODAL OS — Diff Preview V2 read-only",
            "candidate_id:",
            "source_task_id:",
            "suggested_change:",
            "likely_target:",
            "evidence_refs:",
            "workspace_signals:",
            "human_review_note_present:",
            "read_only:",
            "diff_generated:",
            "patch_generated:",
            "commands_executed:",
            "files_modified:",
            "product_files_modified:",
            "real_diff_available:",
            "not_done: no diff real, no patch, no shell, no provider, no filesystem write, no execution"
        })
        {
            StringAssert.Contains(summaryFunction, expected);
        }

        foreach (var forbidden in new[] { "document.cookie", "fetch(", "post(", "chrome.runtime", "WebSocket", "eval(", "new Function" })
        {
            Assert.IsFalse(summaryFunction.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    [TestMethod]
    public void DiffPreviewV2_DoesNotExposeApplyPatchOrExecuteButtons()
    {
        var section = ExtractChangeCandidateSection(ReadRepoText(SidepanelHtmlPath));

        foreach (var forbidden in new[]
        {
            "Aplicar",
            "Ejecutar",
            "Crear patch",
            "Crear diff",
            "Abrir URL",
            "Login",
            "Resolver captcha"
        })
        {
            Assert.IsFalse(section.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    [TestMethod]
    public void DiffPreviewV2_NotesUseExistingLocalStoreOnly()
    {
        var js = ReadRepoText(SidepanelJsPath);
        var saveStart = js.IndexOf("function saveSelectedCandidateReviewNote", StringComparison.Ordinal);
        Assert.IsTrue(saveStart >= 0, "Candidate note save function is missing.");
        var saveEnd = js.IndexOf("function clearSelectedCandidateReviewNote", saveStart, StringComparison.Ordinal);
        Assert.IsTrue(saveEnd > saveStart, "Candidate note save function end marker is missing.");
        var saveFunction = js[saveStart..saveEnd];

        StringAssert.Contains(saveFunction, "saveDemoStore()");
        StringAssert.Contains(saveFunction, "readOnly: true");
        StringAssert.Contains(saveFunction, "enablesExecution: false");
        StringAssert.Contains(saveFunction, "filesModified: false");

        foreach (var forbidden in new[] { "fetch(", "post(", "chrome.runtime", "WebSocket", "showSaveFilePicker", "FileSystemWritableFileStream" })
        {
            Assert.IsFalse(saveFunction.Contains(forbidden, StringComparison.OrdinalIgnoreCase), forbidden);
        }
    }

    [TestMethod]
    public void DiffPreviewV2_HarnessChecksDetailAndNotes()
    {
        var harness = ReadRepoText(InstalledHarnessPath);

        StringAssert.Contains(harness, "candidateDetailTextLower.includes('diff preview v2')");
        StringAssert.Contains(harness, "candidateDetailTextLower.includes('notas humanas de revisión')");
        StringAssert.Contains(harness, "candidateDangerousButtons.length === 0");
        StringAssert.Contains(harness, "candidatesDiffPreviewV2");
        StringAssert.Contains(harness, "candidateHumanReviewNotes");
        StringAssert.Contains(harness, "legacy-installed-sidepanel-compat-only");
    }

    [TestMethod]
    public void DiffPreviewV2_HasProductStyles()
    {
        var css = ReadRepoText(SidepanelCssPath);

        foreach (var expected in new[]
        {
            ".candidate-detail-review",
            ".candidate-detail-grid",
            ".candidate-detail-evidence",
            ".candidate-review-notes",
            ".candidate-detail-flags",
            ".change-candidate-item.selected"
        })
        {
            StringAssert.Contains(css, expected);
        }
    }

    private static string ExtractChangeCandidateSection(string html)
    {
        const string startMarker = "id=\"changeCandidateCard\"";
        const string endMarker = "<section class=\"mission-progress-card\"";
        var start = html.IndexOf(startMarker, StringComparison.Ordinal);
        var end = html.IndexOf(endMarker, start >= 0 ? start : 0, StringComparison.Ordinal);
        Assert.IsTrue(start >= 0, "Change Candidate section is missing.");
        Assert.IsTrue(end > start, "Change Candidate section end marker is missing.");
        return html[start..end];
    }

    private static string ReadRepoText(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "OneBrain.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? Environment.CurrentDirectory;
    }
}
