using WorkReady.Domain.Jobs;

namespace WorkReady.Application.Jobs.GetJobById;

public sealed record GetJobByIdQueryResult(
    Guid JobId,
    Guid TechnicianId,
    Guid SiteId,
    string WorkType,
    DateTimeOffset PlannedStart,
    JobState State,
    DateTimeOffset? StartedAt);
