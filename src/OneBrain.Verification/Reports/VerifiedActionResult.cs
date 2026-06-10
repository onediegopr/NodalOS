using OneBrain.Core.Actions;

namespace OneBrain.Verification.Reports;

public sealed record VerifiedActionResult(
    bool Success,
    string Message,
    ActionResult Action,
    ActionVerificationReport Verification);
