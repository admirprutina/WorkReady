namespace WorkReady.Application.Jobs;

public sealed class JobNotFoundException(Guid jobId)
    : Exception($"Job '{jobId}' was not found.")
{
    public Guid JobId { get; } = jobId;
}
