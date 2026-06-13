namespace OneBrain.Core.Execution;

public enum StepTransition
{
    ContractValid = 0,
    ContractInvalid = 1,
    BindingSame = 2,
    BindingDifferentOrStale = 3,
    BindingAmbiguous = 4,
    BindingNotFound = 5,
    HumanInputBeforeDispatch = 6,
    DispatchStarted = 7,
    HumanInputDetected = 8,
    ExecutorError = 9,
    ExecutorReturned = 10,
    Verified = 11,
    NotVerified = 12,
    CancellationRequested = 13
}
