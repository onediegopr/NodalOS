namespace OneBrain.ChromeLab.Bridge.Stealth;

public sealed class HandoffVerificationService
{
    public async Task<bool> VerifyAsync(string taskId, CancellationToken ct)
    {
        await Task.CompletedTask;
        return true;
    }
}
