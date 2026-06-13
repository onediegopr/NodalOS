namespace OneBrain.Core.Execution;

public enum StepState
{
    Created = 0,
    Validated = 1,
    Bound = 2,
    Executing = 3,
    Verifying = 4,
    Succeeded = 5,
    Blocked = 6,
    Paused = 7,
    Failed = 8,
    Aborted = 9
}
