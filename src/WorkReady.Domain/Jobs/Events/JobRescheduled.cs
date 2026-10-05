namespace WorkReady.Domain.Jobs.Events;

/// <summary>The Job was moved to a new planned start. Evidence is kept.</summary>
public sealed record JobRescheduled(Guid JobId, DateTimeOffset PlannedStart);
