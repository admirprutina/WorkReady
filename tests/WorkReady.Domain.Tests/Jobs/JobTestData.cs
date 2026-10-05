using WorkReady.Domain.Jobs;
using WorkReady.Domain.Jobs.Events;

namespace WorkReady.Domain.Tests.Jobs;

/// <summary>Builds Job state the same way an event store would: Create from the first event, then Apply the rest.</summary>
internal static class JobTestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);
    public static readonly WorkType HighVoltage = new("HighVoltage");
    public static readonly WorkType Plumbing = new("Plumbing");

    public static readonly Guid JobId = Guid.NewGuid();
    public static readonly Guid TechnicianId = Guid.NewGuid();
    public static readonly Guid SiteId = Guid.NewGuid();

    public static JobCreated Created() => new(JobId, TechnicianId, SiteId, HighVoltage, Now.AddHours(2));

    public static QualificationEvidence Qualification(
        Guid? technicianId = null, WorkType? workType = null, DateTimeOffset? validUntil = null) =>
        new(Guid.NewGuid(), technicianId ?? TechnicianId, workType ?? HighVoltage, validUntil ?? Now.AddDays(365));

    public static SafetyTrainingEvidence SafetyTraining(
        Guid? technicianId = null, Guid? siteId = null, DateTimeOffset? validUntil = null) =>
        new(Guid.NewGuid(), technicianId ?? TechnicianId, siteId ?? SiteId, validUntil ?? Now.AddDays(90));

    public static SiteAccessEvidence SiteAccess(
        Guid? technicianId = null, Guid? siteId = null, DateTimeOffset? freshUntil = null) =>
        new(Guid.NewGuid(), technicianId ?? TechnicianId, siteId ?? SiteId, freshUntil ?? Now.AddHours(4));

    public static object[] AllEvidenceRecorded() =>
    [
        new QualificationEvidenceRecorded(JobId, Qualification()),
        new SafetyTrainingEvidenceRecorded(JobId, SafetyTraining()),
        new SiteAccessEvidenceRecorded(JobId, SiteAccess())
    ];

    public static Job Given(JobCreated created, params object[] events)
    {
        var job = Job.Create(created);

        foreach (var @event in events)
        {
            Evolve(job, @event);
        }

        return job;
    }

    public static Job Given(params object[] events) => Given(Created(), events);

    public static Job GivenAllEvidence(params object[] events) => Given([.. AllEvidenceRecorded(), .. events]);

    public static Job GivenStarted() => GivenAllEvidence(new JobStarted(JobId, Now));

    private static void Evolve(Job job, object @event)
    {
        switch (@event)
        {
            case TechnicianReassigned e: job.Apply(e); break;
            case WorkTypeChanged e: job.Apply(e); break;
            case JobRescheduled e: job.Apply(e); break;
            case QualificationEvidenceRecorded e: job.Apply(e); break;
            case SafetyTrainingEvidenceRecorded e: job.Apply(e); break;
            case SiteAccessEvidenceRecorded e: job.Apply(e); break;
            case JobStarted e: job.Apply(e); break;
            default: throw new ArgumentException($"Unknown Job event {@event.GetType().Name}.", nameof(@event));
        }
    }
}
