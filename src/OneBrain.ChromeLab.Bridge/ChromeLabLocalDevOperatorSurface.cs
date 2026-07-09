namespace OneBrain.ChromeLab.Bridge;

public enum ChromeLabLocalDevOperatorSurfaceDecision
{
    Rejected,
    RenderedPreview
}

public enum ChromeLabLocalDevOperatorSurfaceSeverity
{
    Info,
    Warning,
    Blocker
}

public enum ChromeLabLocalDevOperatorSurfaceBlocker
{
    MissingRequest,
    MissingExplicitLocalDevOperatorSurfacePrepScope,
    LiveBrowserExecutionRequested,
    ChromeLaunchRequested,
    CdpConnectionRequested,
    ExternalBrowserAutomationRequested,
    NetworkProviderRequested,
    UserCustomerDataRequested,
    ProductionRuntimeRequested,
    PublicProductPromotionRequested,
    ProductAuthorityRequested,
    ApprovalOrCommandExecutionRequested,
    ReleaseCommercialRequested
}

public sealed record ChromeLabLocalDevOperatorSurfaceRequest(
    bool ExplicitLocalDevOperatorSurfacePrepScope,
    bool RequestsLiveBrowserExecution,
    bool RequestsChromeLaunch,
    bool RequestsCdpConnection,
    bool RequestsExternalBrowserAutomation,
    bool RequestsNetworkProvider,
    bool RequestsUserCustomerData,
    bool RequestsProductionRuntime,
    bool RequestsPublicProductPromotion,
    bool RequestsProductAuthority,
    bool RequestsApprovalOrCommandExecution,
    bool RequestsReleaseCommercial);

public sealed record ChromeLabLocalDevOperatorSurfaceHeader(
    string Title,
    string Status,
    int ReadinessPercentage,
    IReadOnlyList<string> Notices);

public sealed record ChromeLabLocalDevOperatorSurfaceSection(
    string SectionId,
    string Title,
    string Status,
    IReadOnlyList<string> Lines,
    ChromeLabLocalDevOperatorSurfaceSeverity Severity);

public sealed record ChromeLabLocalDevOperatorSurfaceActionPreview(
    string ActionId,
    string Label,
    string RiskLabel,
    string BlockedReason,
    string BlockedFrontier,
    string RequiredOperatorSignal,
    IReadOnlyList<string> RequiredEvidence,
    bool Disabled,
    bool Executable,
    string? ProductiveCommandId,
    string? HandlerId,
    string? CallbackName);

public sealed record ChromeLabLocalDevOperatorSurfaceViewModel(
    string ViewModelId,
    ChromeLabLocalDevOperatorSurfaceHeader Header,
    IReadOnlyList<ChromeLabLocalDevOperatorSurfaceSection> Sections,
    IReadOnlyList<ChromeLabLocalDevOperatorSurfaceActionPreview> ActionPreviews,
    IReadOnlyList<string> Blockers,
    IReadOnlyList<string> Warnings,
    string SafeNextStep,
    bool LocalDevOnly,
    bool ReadOnly,
    bool FailClosed,
    bool LiveBrowserExecutionAvailable,
    bool ChromeLaunchAvailable,
    bool CdpConnectionAvailable,
    bool ExternalBrowserAutomationAvailable,
    bool NetworkProviderAvailable,
    bool UserCustomerDataAvailable,
    bool ProductionRuntimeAvailable,
    bool PublicProductPromotionAvailable,
    bool ProductAuthorityAvailable,
    bool ApprovalOrCommandExecutionAvailable,
    bool ReleaseCommercialReady,
    string StatusText);

public sealed record ChromeLabLocalDevOperatorSurfaceResult(
    ChromeLabLocalDevOperatorSurfaceDecision Decision,
    IReadOnlyList<ChromeLabLocalDevOperatorSurfaceBlocker> Blockers,
    ChromeLabLocalDevOperatorSurfaceViewModel ViewModel);

public sealed class ChromeLabLocalDevOperatorSurfacePresenter
{
    public const string ReadyStatus =
        "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_PREP_READY LOCAL_DEV_ONLY READ_ONLY FAIL_CLOSED ACTIONS_DISABLED NO_LIVE_BROWSER_EXECUTION NO_CHROME_LAUNCH NO_CDP_CONNECTION NO_EXTERNAL_BROWSER_AUTOMATION NO_NETWORK_PROVIDER NO_USER_CUSTOMER_DATA NO_PRODUCTION_RUNTIME NO_PUBLIC_PRODUCT_PROMOTION NO_PRODUCT_AUTHORITY NO_APPROVAL_OR_COMMAND_EXECUTION NO_RELEASE_COMMERCIAL";

    public const string RejectedStatus =
        "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_PREP_REJECTED FAIL_CLOSED ACTIONS_DISABLED NO_LIVE_BROWSER_EXECUTION NO_CHROME_LAUNCH NO_CDP_CONNECTION NO_EXTERNAL_BROWSER_AUTOMATION NO_NETWORK_PROVIDER NO_USER_CUSTOMER_DATA NO_PRODUCTION_RUNTIME NO_PUBLIC_PRODUCT_PROMOTION NO_PRODUCT_AUTHORITY NO_APPROVAL_OR_COMMAND_EXECUTION NO_RELEASE_COMMERCIAL";

    public ChromeLabLocalDevOperatorSurfaceResult Render(ChromeLabLocalDevOperatorSurfaceRequest? request)
    {
        var blockers = new List<ChromeLabLocalDevOperatorSurfaceBlocker>();
        if (request is null)
        {
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.MissingRequest);
            return Result(blockers);
        }

        if (!request.ExplicitLocalDevOperatorSurfacePrepScope)
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.MissingExplicitLocalDevOperatorSurfacePrepScope);
        if (request.RequestsLiveBrowserExecution)
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.LiveBrowserExecutionRequested);
        if (request.RequestsChromeLaunch)
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.ChromeLaunchRequested);
        if (request.RequestsCdpConnection)
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.CdpConnectionRequested);
        if (request.RequestsExternalBrowserAutomation)
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.ExternalBrowserAutomationRequested);
        if (request.RequestsNetworkProvider)
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.NetworkProviderRequested);
        if (request.RequestsUserCustomerData)
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.UserCustomerDataRequested);
        if (request.RequestsProductionRuntime)
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.ProductionRuntimeRequested);
        if (request.RequestsPublicProductPromotion)
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.PublicProductPromotionRequested);
        if (request.RequestsProductAuthority)
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.ProductAuthorityRequested);
        if (request.RequestsApprovalOrCommandExecution)
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.ApprovalOrCommandExecutionRequested);
        if (request.RequestsReleaseCommercial)
            blockers.Add(ChromeLabLocalDevOperatorSurfaceBlocker.ReleaseCommercialRequested);

        return Result(blockers);
    }

    private static ChromeLabLocalDevOperatorSurfaceResult Result(
        IReadOnlyList<ChromeLabLocalDevOperatorSurfaceBlocker> blockers)
    {
        var distinct = blockers.Distinct().OrderBy(blocker => blocker.ToString(), StringComparer.Ordinal).ToArray();
        var rendered = distinct.Length == 0;

        return new ChromeLabLocalDevOperatorSurfaceResult(
            Decision: rendered
                ? ChromeLabLocalDevOperatorSurfaceDecision.RenderedPreview
                : ChromeLabLocalDevOperatorSurfaceDecision.Rejected,
            Blockers: distinct,
            ViewModel: rendered ? ReadyViewModel() : BlockedViewModel(distinct));
    }

    private static ChromeLabLocalDevOperatorSurfaceViewModel ReadyViewModel() =>
        ViewModel(
            rendered: true,
            headerStatus: "LOCAL_DEV_OPERATOR_SURFACE_PREP",
            readinessPercentage: 27,
            sections:
            [
                new(
                    "status",
                    "ChromeLab Local/Dev Operator Surface Prep",
                    "READY_LOCAL_DEV_PREP",
                    [
                        "ChromeLab local/dev operator prep is visible.",
                        "The surface is read-only and fail-closed.",
                        "Live browser execution remains unavailable.",
                        "Production runtime readiness remains 0."
                    ],
                    ChromeLabLocalDevOperatorSurfaceSeverity.Info),
                new(
                    "limits",
                    "Limits",
                    "BOUNDARIES_VISIBLE",
                    [
                        "No Chrome launch.",
                        "No live CDP connection.",
                        "No external browser automation.",
                        "No network provider.",
                        "No user/customer data.",
                        "No public/product promotion."
                    ],
                    ChromeLabLocalDevOperatorSurfaceSeverity.Warning),
                new(
                    "blockers",
                    "Blocked Live Browser Actions",
                    "BLOCKED",
                    [
                        "Browser/CDP/WCU/OCR/Recipes live authority requires a separate operator GO.",
                        "Approval or command execution is not available from this surface.",
                        "Release/commercial readiness remains NO-GO."
                    ],
                    ChromeLabLocalDevOperatorSurfaceSeverity.Blocker),
                new(
                    "operator-signal",
                    "Required Operator Signal",
                    "EXPLICIT_OPERATOR_FRONTIER_REQUIRED",
                    [
                        "Next work requires an explicit ChromeLab local/dev frontier.",
                        "Any live browser authority requires a separate GO and fresh safety evidence."
                    ],
                    ChromeLabLocalDevOperatorSurfaceSeverity.Warning),
                new(
                    "safe-next-step",
                    "Safe Next Step",
                    "FOLLOW_UP_OR_CLOSE",
                    [
                        "Add acceptance evidence for this local/dev surface or close the ChromeLab prep line.",
                        "Keep all actions disabled until a new explicit operator frontier is selected."
                    ],
                    ChromeLabLocalDevOperatorSurfaceSeverity.Info)
            ],
            blockers: [],
            warnings:
            [
                "ChromeLab bridge runtime exists elsewhere in the repo, but this surface does not start or connect it.",
                "This preview is not product authority and not release readiness."
            ]);

    private static ChromeLabLocalDevOperatorSurfaceViewModel BlockedViewModel(
        IReadOnlyList<ChromeLabLocalDevOperatorSurfaceBlocker> blockers) =>
        ViewModel(
            rendered: false,
            headerStatus: "FAIL_CLOSED",
            readinessPercentage: 0,
            sections:
            [
                new(
                    "status",
                    "ChromeLab Local/Dev Operator Surface Prep",
                    "REJECTED_FAIL_CLOSED",
                    ["ChromeLab local/dev operator prep was rejected before rendering."],
                    ChromeLabLocalDevOperatorSurfaceSeverity.Blocker),
                new(
                    "blockers",
                    "Blockers",
                    "BLOCKED",
                    blockers.Select(blocker => blocker.ToString()).ToArray(),
                    ChromeLabLocalDevOperatorSurfaceSeverity.Blocker),
                new(
                    "safe-next-step",
                    "Safe Next Step",
                    "FIX_BLOCKERS_BEFORE_PREVIEW",
                    ["Remove unsafe requests and provide the explicit local/dev operator surface prep scope."],
                    ChromeLabLocalDevOperatorSurfaceSeverity.Blocker)
            ],
            blockers: blockers.Select(blocker => blocker.ToString()).ToArray(),
            warnings: ["Fail-closed preview exposes no execution authority."]);

    private static ChromeLabLocalDevOperatorSurfaceViewModel ViewModel(
        bool rendered,
        string headerStatus,
        int readinessPercentage,
        IReadOnlyList<ChromeLabLocalDevOperatorSurfaceSection> sections,
        IReadOnlyList<string> blockers,
        IReadOnlyList<string> warnings) =>
        new(
            ViewModelId: "chromelab.local-dev.operator-surface-prep.v1",
            Header: new ChromeLabLocalDevOperatorSurfaceHeader(
                Title: "ChromeLab Local/Dev",
                Status: headerStatus,
                ReadinessPercentage: readinessPercentage,
                Notices:
                [
                    "local/dev only",
                    "read-only preview",
                    "actions disabled",
                    "no live browser execution",
                    "no public/product promotion",
                    "no release/commercial"
                ]),
            Sections: sections,
            ActionPreviews:
            [
                new(
                    ActionId: "prepare-chromelab-local-dev-operator-review",
                    Label: "Prepare ChromeLab local/dev operator review",
                    RiskLabel: "disabled-preview-only",
                    BlockedReason: "ChromeLab live browser execution, Chrome launch and CDP connection are not authorized by this surface.",
                    BlockedFrontier: "CHROMELAB_LIVE_BROWSER_EXECUTION_AUTHORITY",
                    RequiredOperatorSignal: "explicit-chromelab-local-dev-frontier",
                    RequiredEvidence:
                    [
                        "operator-selected ChromeLab frontier",
                        "focal local/dev surface tests",
                        "forbidden activation scan",
                        "no release/commercial GO"
                    ],
                    Disabled: true,
                    Executable: false,
                    ProductiveCommandId: null,
                    HandlerId: null,
                    CallbackName: null)
            ],
            Blockers: blockers,
            Warnings: warnings,
            SafeNextStep: rendered
                ? "CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_FOLLOW_UP_OR_CLOSE"
                : "FIX_BLOCKERS_BEFORE_CHROMELAB_LOCAL_DEV_OPERATOR_SURFACE_PREP",
            LocalDevOnly: true,
            ReadOnly: true,
            FailClosed: true,
            LiveBrowserExecutionAvailable: false,
            ChromeLaunchAvailable: false,
            CdpConnectionAvailable: false,
            ExternalBrowserAutomationAvailable: false,
            NetworkProviderAvailable: false,
            UserCustomerDataAvailable: false,
            ProductionRuntimeAvailable: false,
            PublicProductPromotionAvailable: false,
            ProductAuthorityAvailable: false,
            ApprovalOrCommandExecutionAvailable: false,
            ReleaseCommercialReady: false,
            StatusText: rendered ? ReadyStatus : RejectedStatus);
}
