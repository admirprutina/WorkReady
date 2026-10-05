namespace WorkReady.Domain.Jobs.Events;

/// <summary>The Job now requires another WorkType. Qualification evidence no longer applies.</summary>
public sealed record WorkTypeChanged(Guid JobId, WorkType WorkType);
