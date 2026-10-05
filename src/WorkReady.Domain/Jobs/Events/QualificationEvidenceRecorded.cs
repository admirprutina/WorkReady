namespace WorkReady.Domain.Jobs.Events;

/// <summary>Qualification evidence matching the Job's Technician and WorkType was recorded.</summary>
public sealed record QualificationEvidenceRecorded(Guid JobId, QualificationEvidence Evidence);
