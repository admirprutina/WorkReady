using WorkReady.Domain.Jobs.Events;

namespace WorkReady.Domain.Jobs;

/// <summary>
/// One concrete planned piece of work at one Site. Aggregate root, event-sourced.
/// Decision methods validate invariants against the current state and return the resulting event without changing state;
/// <see cref="Create"/> and the <c>Apply</c> methods rebuild state from events.
/// Readiness is not stored: it is derived from the check evidence that is valid at a given moment.
/// </summary>
public sealed class Job
{
    private Job()
    {
    }

    public Guid Id { get; private set; }

    public Guid TechnicianId { get; private set; }

    public Guid SiteId { get; private set; }

    public WorkType WorkType { get; private set; } = null!;

    public DateTimeOffset PlannedStart { get; private set; }

    public JobState State { get; private set; }

    public QualificationEvidence? QualificationEvidence { get; private set; }

    public SafetyTrainingEvidence? SafetyTrainingEvidence { get; private set; }

    public SiteAccessEvidence? SiteAccessEvidence { get; private set; }

    // Decisions

    public static JobCreated Plan(Guid jobId, Guid technicianId, Guid siteId, WorkType workType, DateTimeOffset plannedStart)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("Job id is required.", nameof(jobId));
        }

        if (technicianId == Guid.Empty)
        {
            throw new ArgumentException("Technician is required.", nameof(technicianId));
        }

        if (siteId == Guid.Empty)
        {
            throw new ArgumentException("Site is required.", nameof(siteId));
        }

        ArgumentNullException.ThrowIfNull(workType);

        return new JobCreated(jobId, technicianId, siteId, workType, plannedStart);
    }

    /// <returns>The event, or <c>null</c> when the technician is already assigned and nothing changes.</returns>
    public TechnicianReassigned? ReassignTechnician(Guid technicianId)
    {
        EnsurePlanned("reassign technician");

        if (technicianId == Guid.Empty)
        {
            throw new ArgumentException("Technician is required.", nameof(technicianId));
        }

        return technicianId == TechnicianId ? null : new TechnicianReassigned(Id, technicianId);
    }

    /// <returns>The event, or <c>null</c> when the job already has this work type and nothing changes.</returns>
    public WorkTypeChanged? ChangeWorkType(WorkType workType)
    {
        EnsurePlanned("change work type");
        ArgumentNullException.ThrowIfNull(workType);

        return workType == WorkType ? null : new WorkTypeChanged(Id, workType);
    }

    public JobRescheduled Reschedule(DateTimeOffset plannedStart)
    {
        EnsurePlanned("reschedule");

        return new JobRescheduled(Id, plannedStart);
    }

    public QualificationEvidenceRecorded RecordQualificationEvidence(QualificationEvidence evidence)
    {
        EnsurePlanned("record qualification evidence");
        ArgumentNullException.ThrowIfNull(evidence);

        EnsureCurrentTechnician(evidence.TechnicianId, "Qualification");

        if (evidence.WorkType != WorkType)
        {
            throw new EvidenceDoesNotMatchJobException(
                Id, "Qualification", $"work type '{evidence.WorkType}' is not the job's work type '{WorkType}'");
        }

        return new QualificationEvidenceRecorded(Id, evidence);
    }

    public SafetyTrainingEvidenceRecorded RecordSafetyTrainingEvidence(SafetyTrainingEvidence evidence)
    {
        EnsurePlanned("record safety training evidence");
        ArgumentNullException.ThrowIfNull(evidence);

        EnsureCurrentTechnician(evidence.TechnicianId, "Safety training");
        EnsureJobSite(evidence.SiteId, "Safety training");

        return new SafetyTrainingEvidenceRecorded(Id, evidence);
    }

    public SiteAccessEvidenceRecorded RecordSiteAccessEvidence(SiteAccessEvidence evidence)
    {
        EnsurePlanned("record site access evidence");
        ArgumentNullException.ThrowIfNull(evidence);

        EnsureCurrentTechnician(evidence.TechnicianId, "Site access");
        EnsureJobSite(evidence.SiteId, "Site access");

        return new SiteAccessEvidenceRecorded(Id, evidence);
    }

    public bool CanStart(DateTimeOffset at) =>
        State == JobState.Planned &&
        HasValidQualificationAt(at) &&
        HasValidSafetyTrainingAt(at) &&
        HasFreshSiteAccessAt(at);

    public JobStarted Start(DateTimeOffset at)
    {
        EnsurePlanned("start");

        if (!CanStart(at))
        {
            throw new JobNotReadyToStartException(Id, at);
        }

        return new JobStarted(Id, at);
    }

    // Evolution

    public static Job Create(JobCreated @event) => new()
    {
        Id = @event.JobId,
        TechnicianId = @event.TechnicianId,
        SiteId = @event.SiteId,
        WorkType = @event.WorkType,
        PlannedStart = @event.PlannedStart,
        State = JobState.Planned
    };

    public void Apply(TechnicianReassigned @event)
    {
        TechnicianId = @event.TechnicianId;

        // Every piece of evidence was obtained for the previous technician.
        QualificationEvidence = null;
        SafetyTrainingEvidence = null;
        SiteAccessEvidence = null;
    }

    public void Apply(WorkTypeChanged @event)
    {
        WorkType = @event.WorkType;

        // Only qualification depends on the work type.
        QualificationEvidence = null;
    }

    // Evidence is kept; whether it is still valid is decided when readiness is evaluated.
    public void Apply(JobRescheduled @event) => PlannedStart = @event.PlannedStart;

    public void Apply(QualificationEvidenceRecorded @event) => QualificationEvidence = @event.Evidence;

    public void Apply(SafetyTrainingEvidenceRecorded @event) => SafetyTrainingEvidence = @event.Evidence;

    public void Apply(SiteAccessEvidenceRecorded @event) => SiteAccessEvidence = @event.Evidence;

    public void Apply(JobStarted @event) => State = JobState.Started;

    private bool HasValidQualificationAt(DateTimeOffset at) =>
        QualificationEvidence is { } evidence &&
        evidence.TechnicianId == TechnicianId &&
        evidence.WorkType == WorkType &&
        evidence.IsValidAt(at);

    private bool HasValidSafetyTrainingAt(DateTimeOffset at) =>
        SafetyTrainingEvidence is { } evidence &&
        evidence.TechnicianId == TechnicianId &&
        evidence.SiteId == SiteId &&
        evidence.IsValidAt(at);

    private bool HasFreshSiteAccessAt(DateTimeOffset at) =>
        SiteAccessEvidence is { } evidence &&
        evidence.TechnicianId == TechnicianId &&
        evidence.SiteId == SiteId &&
        evidence.IsFreshAt(at);

    private void EnsurePlanned(string action)
    {
        if (State != JobState.Planned)
        {
            throw new JobNotPlannedException(Id, State, action);
        }
    }

    private void EnsureCurrentTechnician(Guid technicianId, string evidence)
    {
        if (technicianId != TechnicianId)
        {
            throw new EvidenceDoesNotMatchJobException(
                Id, evidence, $"technician '{technicianId}' is not the job's technician '{TechnicianId}'");
        }
    }

    private void EnsureJobSite(Guid siteId, string evidence)
    {
        if (siteId != SiteId)
        {
            throw new EvidenceDoesNotMatchJobException(
                Id, evidence, $"site '{siteId}' is not the job's site '{SiteId}'");
        }
    }
}
