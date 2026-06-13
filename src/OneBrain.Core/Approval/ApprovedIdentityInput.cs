using OneBrain.Core.Models;
using OneBrain.Core.Selectors.Web;

namespace OneBrain.Core.Approval;

public sealed record ApprovedIdentityInput(
    ElementIdentity? Identity,
    string? Source,
    WebSelectorParity? Parity);
