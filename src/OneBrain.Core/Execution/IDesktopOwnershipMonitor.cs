namespace OneBrain.Core.Execution;

public interface IDesktopOwnershipMonitor
{
    OwnershipSnapshot Capture();
    bool HumanInputSince(OwnershipSnapshot baseline);
    bool ForegroundChanged(OwnershipSnapshot baseline);
}
