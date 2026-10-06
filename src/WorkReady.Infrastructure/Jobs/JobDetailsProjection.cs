using Marten.Events.Aggregation;
using WorkReady.Application.Jobs.ReadModels;
using WorkReady.Domain.Jobs;
using WorkReady.Domain.Jobs.Events;

namespace WorkReady.Infrastructure.Jobs;

/// <summary>
/// Builds the persisted <see cref="JobDetailsReadModel"/> from a Job stream: one document per stream, id = JobId.
/// Only copies facts from the events; every decision was already made when the events were recorded.
/// Evidence events are not handled because this read model does not show evidence.
/// </summary>
public sealed partial class JobDetailsProjection : SingleStreamProjection<JobDetailsReadModel, Guid>
{
    public JobDetailsReadModel Create(JobCreated @event) => new()
    {
        Id = @event.JobId,
        TechnicianId = @event.TechnicianId,
        SiteId = @event.SiteId,
        WorkType = @event.WorkType.Value,
        PlannedStart = @event.PlannedStart,
        State = JobState.Planned,
        StartedAt = null
    };

    public void Apply(TechnicianReassigned @event, JobDetailsReadModel job) => job.TechnicianId = @event.TechnicianId;

    public void Apply(WorkTypeChanged @event, JobDetailsReadModel job) => job.WorkType = @event.WorkType.Value;

    public void Apply(JobRescheduled @event, JobDetailsReadModel job) => job.PlannedStart = @event.PlannedStart;

    public void Apply(JobStarted @event, JobDetailsReadModel job)
    {
        job.State = JobState.Started;
        job.StartedAt = @event.StartedAt;
    }
}
