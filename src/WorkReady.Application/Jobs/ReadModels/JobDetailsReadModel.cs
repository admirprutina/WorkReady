using WorkReady.Domain.Jobs;

namespace WorkReady.Application.Jobs.ReadModels;

/// <summary>
/// Read side: what is shown for one Job, kept up to date from the Job's events by a projection.
/// Data only, never used for business decisions; the Job aggregate is the decision model.
/// <see cref="Id"/> is the JobId, which is also the Job stream id.
/// </summary>
public sealed class JobDetailsReadModel
{
    public Guid Id { get; set; }

    public Guid TechnicianId { get; set; }

    public Guid SiteId { get; set; }

    public string WorkType { get; set; } = string.Empty;

    public DateTimeOffset PlannedStart { get; set; }

    public JobState State { get; set; }

    public DateTimeOffset? StartedAt { get; set; }
}
