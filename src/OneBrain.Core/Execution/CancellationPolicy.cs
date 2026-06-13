namespace OneBrain.Core.Execution;

public static class CancellationPolicy
{
    public static bool CanCancel(StepState state) =>
        state is StepState.Created or StepState.Validated or StepState.Bound or StepState.Verifying;
}
