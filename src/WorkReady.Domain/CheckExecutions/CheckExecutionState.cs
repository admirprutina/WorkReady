namespace WorkReady.Domain.CheckExecutions;

public enum CheckExecutionState
{
    Requested,
    Running,
    NeedsManualReview,
    Passed,
    Failed,
    Approved,
    Rejected
}
