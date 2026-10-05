namespace WorkReady.Domain.Jobs.Events;

/// <summary>Work on the Job started at StartedAt, with all required evidence valid at that moment.</summary>
public sealed record JobStarted(Guid JobId, DateTimeOffset StartedAt);
