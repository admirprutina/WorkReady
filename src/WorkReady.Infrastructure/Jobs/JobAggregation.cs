using Marten.Events.Aggregation;
using WorkReady.Domain.Jobs;
using WorkReady.Domain.Jobs.Events;

namespace WorkReady.Infrastructure.Jobs;

/// <summary>
/// Write side: tells Marten how to rebuild the Job aggregate from its stream, registered as a Live projection.
/// It only forwards each event to Job.Create / Job.Apply, so the Job still owns how its state evolves.
/// </summary>
/// <remarks>
/// Marten 9 dispatches conventional Create/Apply methods through a compile-time source generator, which only runs
/// in projects that reference Marten. Self-aggregating <c>LiveStreamAggregation&lt;Job&gt;()</c> would need the
/// generator in WorkReady.Domain; declaring the aggregation here keeps the Domain free of Marten.
/// </remarks>
public sealed partial class JobAggregation : SingleStreamProjection<Job, Guid>
{
    public Job Create(JobCreated @event) => Job.Create(@event);

    public void Apply(TechnicianReassigned @event, Job job) => job.Apply(@event);

    public void Apply(WorkTypeChanged @event, Job job) => job.Apply(@event);

    public void Apply(JobRescheduled @event, Job job) => job.Apply(@event);

    public void Apply(QualificationEvidenceRecorded @event, Job job) => job.Apply(@event);

    public void Apply(SafetyTrainingEvidenceRecorded @event, Job job) => job.Apply(@event);

    public void Apply(SiteAccessEvidenceRecorded @event, Job job) => job.Apply(@event);

    public void Apply(JobStarted @event, Job job) => job.Apply(@event);
}
