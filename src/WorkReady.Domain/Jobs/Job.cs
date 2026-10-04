namespace WorkReady.Domain.Jobs;

/// <summary>
/// One concrete planned piece of work at one Site. Aggregate root.
/// Readiness is not stored: it is derived from the check evidence that is valid at a given moment.
/// </summary>
public sealed class Job
{
    public Job(Guid technicianId, Guid siteId, WorkType workType, DateTimeOffset plannedStart)
    {
        if (technicianId == Guid.Empty)
        {
            throw new ArgumentException("Technician is required.", nameof(technicianId));
        }

        if (siteId == Guid.Empty)
        {
            throw new ArgumentException("Site is required.", nameof(siteId));
        }

        Id = Guid.NewGuid();
        TechnicianId = technicianId;
        SiteId = siteId;
        WorkType = workType ?? throw new ArgumentNullException(nameof(workType));
        PlannedStart = plannedStart;
        State = JobState.Planned;
    }

    public Guid Id { get; }

    public Guid TechnicianId { get; private set; }

    public Guid SiteId { get; }

    public WorkType WorkType { get; private set; }

    public DateTimeOffset PlannedStart { get; private set; }

    public JobState State { get; private set; }

    public QualificationEvidence? QualificationEvidence { get; private set; }

    public SafetyTrainingEvidence? SafetyTrainingEvidence { get; private set; }

    public SiteAccessEvidence? SiteAccessEvidence { get; private set; }

    public void ReassignTechnician(Guid technicianId)
    {
        EnsurePlanned("reassign technician");

        if (technicianId == Guid.Empty)
        {
            throw new ArgumentException("Technician is required.", nameof(technicianId));
        }

        if (technicianId == TechnicianId)
        {
            return;
        }

        TechnicianId = technicianId;

        // Every piece of evidence was obtained for the previous technician.
        QualificationEvidence = null;
        SafetyTrainingEvidence = null;
        SiteAccessEvidence = null;
    }

    public void ChangeWorkType(WorkType workType)
    {
        EnsurePlanned("change work type");
        ArgumentNullException.ThrowIfNull(workType);

        if (workType == WorkType)
        {
            return;
        }

        WorkType = workType;

        // Only qualification depends on the work type.
        QualificationEvidence = null;
    }

    public void Reschedule(DateTimeOffset plannedStart)
    {
        EnsurePlanned("reschedule");

        // Evidence is kept; whether it is still valid is decided when readiness is evaluated.
        PlannedStart = plannedStart;
    }

    public void RecordQualificationEvidence(QualificationEvidence evidence)
    {
        EnsurePlanned("record qualification evidence");
        ArgumentNullException.ThrowIfNull(evidence);

        EnsureCurrentTechnician(evidence.TechnicianId, "Qualification");

        if (evidence.WorkType != WorkType)
        {
            throw new EvidenceDoesNotMatchJobException(
                Id, "Qualification", $"work type '{evidence.WorkType}' is not the job's work type '{WorkType}'");
        }

        QualificationEvidence = evidence;
    }

    public void RecordSafetyTrainingEvidence(SafetyTrainingEvidence evidence)
    {
        EnsurePlanned("record safety training evidence");
        ArgumentNullException.ThrowIfNull(evidence);

        EnsureCurrentTechnician(evidence.TechnicianId, "Safety training");
        EnsureJobSite(evidence.SiteId, "Safety training");

        SafetyTrainingEvidence = evidence;
    }

    public void RecordSiteAccessEvidence(SiteAccessEvidence evidence)
    {
        EnsurePlanned("record site access evidence");
        ArgumentNullException.ThrowIfNull(evidence);

        EnsureCurrentTechnician(evidence.TechnicianId, "Site access");
        EnsureJobSite(evidence.SiteId, "Site access");

        SiteAccessEvidence = evidence;
    }

    public bool CanStart(DateTimeOffset at) =>
        State == JobState.Planned &&
        HasValidQualificationAt(at) &&
        HasValidSafetyTrainingAt(at) &&
        HasFreshSiteAccessAt(at);

    public void Start(DateTimeOffset at)
    {
        EnsurePlanned("start");

        if (!CanStart(at))
        {
            throw new JobNotReadyToStartException(Id, at);
        }

        State = JobState.Started;
    }

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
