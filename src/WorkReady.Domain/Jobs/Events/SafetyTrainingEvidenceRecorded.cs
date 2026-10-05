namespace WorkReady.Domain.Jobs.Events;

/// <summary>Safety training evidence matching the Job's Technician and Site was recorded.</summary>
public sealed record SafetyTrainingEvidenceRecorded(Guid JobId, SafetyTrainingEvidence Evidence);
