namespace WorkReady.Domain.Jobs.Events;

/// <summary>A Job was planned: a Technician will do a WorkType at a Site, starting at PlannedStart.</summary>
public sealed record JobCreated(
    Guid JobId,
    Guid TechnicianId,
    Guid SiteId,
    WorkType WorkType,
    DateTimeOffset PlannedStart);
