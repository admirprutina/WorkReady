namespace WorkReady.Domain.Jobs;

public sealed class JobNotReadyToStartException(Guid jobId, DateTimeOffset at)
    : InvalidOperationException(
        $"Job '{jobId}' cannot start at {at:O}: valid qualification, safety training and fresh site access evidence are required.");
