namespace WorkReady.Domain.Jobs;

public sealed class JobNotPlannedException(Guid jobId, JobState state, string action)
    : InvalidOperationException($"Cannot {action}: job '{jobId}' is {state}, not Planned.");
