namespace WorkReady.Domain.Jobs.Events;

/// <summary>Site access evidence matching the Job's Technician and Site was recorded.</summary>
public sealed record SiteAccessEvidenceRecorded(Guid JobId, SiteAccessEvidence Evidence);
