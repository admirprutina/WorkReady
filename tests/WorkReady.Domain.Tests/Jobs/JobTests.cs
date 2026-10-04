using WorkReady.Domain.Jobs;

namespace WorkReady.Domain.Tests.Jobs;

public class JobTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);
    private static readonly WorkType HighVoltage = new("HighVoltage");
    private static readonly WorkType Plumbing = new("Plumbing");

    private readonly Guid _technicianId = Guid.NewGuid();
    private readonly Guid _siteId = Guid.NewGuid();

    private Job PlannedJob() => new(_technicianId, _siteId, HighVoltage, Now.AddHours(2));

    private Job JobWithAllEvidence()
    {
        var job = PlannedJob();
        job.RecordQualificationEvidence(Qualification(job));
        job.RecordSafetyTrainingEvidence(SafetyTraining(job));
        job.RecordSiteAccessEvidence(SiteAccess(job));
        return job;
    }

    private static QualificationEvidence Qualification(Job job, DateTimeOffset? validUntil = null) =>
        new(Guid.NewGuid(), job.TechnicianId, job.WorkType, validUntil ?? Now.AddDays(365));

    private static SafetyTrainingEvidence SafetyTraining(Job job, DateTimeOffset? validUntil = null) =>
        new(Guid.NewGuid(), job.TechnicianId, job.SiteId, validUntil ?? Now.AddDays(90));

    private static SiteAccessEvidence SiteAccess(Job job, DateTimeOffset? freshUntil = null) =>
        new(Guid.NewGuid(), job.TechnicianId, job.SiteId, freshUntil ?? Now.AddHours(4));

    [Fact]
    public void A_new_job_is_planned()
    {
        var job = PlannedJob();

        Assert.Equal(JobState.Planned, job.State);
        Assert.NotEqual(Guid.Empty, job.Id);
    }

    [Fact]
    public void Rescheduling_keeps_the_same_job()
    {
        var job = PlannedJob();
        var id = job.Id;

        job.Reschedule(Now.AddDays(1));

        Assert.Equal(id, job.Id);
        Assert.Equal(Now.AddDays(1), job.PlannedStart);
    }

    [Fact]
    public void Reassigning_the_technician_keeps_the_same_job()
    {
        var job = PlannedJob();
        var id = job.Id;
        var otherTechnician = Guid.NewGuid();

        job.ReassignTechnician(otherTechnician);

        Assert.Equal(id, job.Id);
        Assert.Equal(otherTechnician, job.TechnicianId);
    }

    [Fact]
    public void Reassigning_the_technician_invalidates_all_evidence()
    {
        var job = JobWithAllEvidence();

        job.ReassignTechnician(Guid.NewGuid());

        Assert.Null(job.QualificationEvidence);
        Assert.Null(job.SafetyTrainingEvidence);
        Assert.Null(job.SiteAccessEvidence);
        Assert.False(job.CanStart(Now));
    }

    [Fact]
    public void Changing_the_work_type_invalidates_only_qualification_evidence()
    {
        var job = JobWithAllEvidence();
        var safetyTraining = job.SafetyTrainingEvidence;
        var siteAccess = job.SiteAccessEvidence;

        job.ChangeWorkType(Plumbing);

        Assert.Equal(Plumbing, job.WorkType);
        Assert.Null(job.QualificationEvidence);
        Assert.Same(safetyTraining, job.SafetyTrainingEvidence);
        Assert.Same(siteAccess, job.SiteAccessEvidence);
    }

    [Fact]
    public void Rescheduling_does_not_erase_evidence()
    {
        var job = JobWithAllEvidence();

        job.Reschedule(Now.AddHours(3));

        Assert.NotNull(job.QualificationEvidence);
        Assert.NotNull(job.SafetyTrainingEvidence);
        Assert.NotNull(job.SiteAccessEvidence);
        Assert.True(job.CanStart(Now.AddHours(3)));
    }

    [Fact]
    public void Rescheduled_job_is_judged_on_evidence_validity_at_start_time()
    {
        var job = JobWithAllEvidence();

        job.Reschedule(Now.AddDays(2));

        // Site access was only fresh for a few hours, so it is stale by the new start.
        Assert.False(job.CanStart(Now.AddDays(2)));
    }

    [Fact]
    public void A_job_cannot_start_without_evidence()
    {
        var job = PlannedJob();

        Assert.False(job.CanStart(Now));
        Assert.Throws<JobNotReadyToStartException>(() => job.Start(Now));
        Assert.Equal(JobState.Planned, job.State);
    }

    [Fact]
    public void A_job_cannot_start_with_only_some_evidence()
    {
        var job = PlannedJob();
        job.RecordQualificationEvidence(Qualification(job));
        job.RecordSafetyTrainingEvidence(SafetyTraining(job));

        Assert.Throws<JobNotReadyToStartException>(() => job.Start(Now));
    }

    [Fact]
    public void A_job_cannot_start_with_expired_qualification()
    {
        var job = PlannedJob();
        job.RecordQualificationEvidence(Qualification(job, validUntil: Now.AddMinutes(-1)));
        job.RecordSafetyTrainingEvidence(SafetyTraining(job));
        job.RecordSiteAccessEvidence(SiteAccess(job));

        Assert.False(job.CanStart(Now));
        Assert.Throws<JobNotReadyToStartException>(() => job.Start(Now));
    }

    [Fact]
    public void A_job_cannot_start_with_expired_safety_training()
    {
        var job = PlannedJob();
        job.RecordQualificationEvidence(Qualification(job));
        job.RecordSafetyTrainingEvidence(SafetyTraining(job, validUntil: Now.AddMinutes(-1)));
        job.RecordSiteAccessEvidence(SiteAccess(job));

        Assert.False(job.CanStart(Now));
        Assert.Throws<JobNotReadyToStartException>(() => job.Start(Now));
    }

    [Fact]
    public void A_job_cannot_start_with_stale_site_access()
    {
        var job = PlannedJob();
        job.RecordQualificationEvidence(Qualification(job));
        job.RecordSafetyTrainingEvidence(SafetyTraining(job));
        job.RecordSiteAccessEvidence(SiteAccess(job, freshUntil: Now.AddMinutes(-1)));

        Assert.False(job.CanStart(Now));
        Assert.Throws<JobNotReadyToStartException>(() => job.Start(Now));
    }

    [Fact]
    public void A_job_starts_when_all_evidence_matches_and_is_valid()
    {
        var job = JobWithAllEvidence();

        Assert.True(job.CanStart(Now));

        job.Start(Now);

        Assert.Equal(JobState.Started, job.State);
    }

    [Fact]
    public void A_started_job_cannot_start_again()
    {
        var job = JobWithAllEvidence();
        job.Start(Now);

        Assert.False(job.CanStart(Now));
        Assert.Throws<JobNotPlannedException>(() => job.Start(Now));
    }

    [Fact]
    public void After_start_the_technician_cannot_change()
    {
        var job = JobWithAllEvidence();
        job.Start(Now);

        Assert.Throws<JobNotPlannedException>(() => job.ReassignTechnician(Guid.NewGuid()));
        Assert.Equal(_technicianId, job.TechnicianId);
    }

    [Fact]
    public void After_start_the_work_type_cannot_change()
    {
        var job = JobWithAllEvidence();
        job.Start(Now);

        Assert.Throws<JobNotPlannedException>(() => job.ChangeWorkType(Plumbing));
        Assert.Equal(HighVoltage, job.WorkType);
    }

    [Fact]
    public void After_start_the_job_cannot_be_rescheduled()
    {
        var job = JobWithAllEvidence();
        job.Start(Now);

        Assert.Throws<JobNotPlannedException>(() => job.Reschedule(Now.AddDays(1)));
    }

    [Fact]
    public void After_start_new_evidence_cannot_be_recorded()
    {
        var job = JobWithAllEvidence();
        job.Start(Now);

        Assert.Throws<JobNotPlannedException>(() => job.RecordQualificationEvidence(Qualification(job)));
        Assert.Throws<JobNotPlannedException>(() => job.RecordSafetyTrainingEvidence(SafetyTraining(job)));
        Assert.Throws<JobNotPlannedException>(() => job.RecordSiteAccessEvidence(SiteAccess(job)));
    }

    [Fact]
    public void Evidence_for_another_technician_is_rejected()
    {
        var job = PlannedJob();
        var otherTechnician = Guid.NewGuid();

        Assert.Throws<EvidenceDoesNotMatchJobException>(() => job.RecordQualificationEvidence(
            new QualificationEvidence(Guid.NewGuid(), otherTechnician, job.WorkType, Now.AddDays(1))));
        Assert.Throws<EvidenceDoesNotMatchJobException>(() => job.RecordSafetyTrainingEvidence(
            new SafetyTrainingEvidence(Guid.NewGuid(), otherTechnician, job.SiteId, Now.AddDays(1))));
        Assert.Throws<EvidenceDoesNotMatchJobException>(() => job.RecordSiteAccessEvidence(
            new SiteAccessEvidence(Guid.NewGuid(), otherTechnician, job.SiteId, Now.AddDays(1))));
    }

    [Fact]
    public void Evidence_for_another_site_is_rejected()
    {
        var job = PlannedJob();
        var otherSite = Guid.NewGuid();

        Assert.Throws<EvidenceDoesNotMatchJobException>(() => job.RecordSafetyTrainingEvidence(
            new SafetyTrainingEvidence(Guid.NewGuid(), job.TechnicianId, otherSite, Now.AddDays(1))));
        Assert.Throws<EvidenceDoesNotMatchJobException>(() => job.RecordSiteAccessEvidence(
            new SiteAccessEvidence(Guid.NewGuid(), job.TechnicianId, otherSite, Now.AddDays(1))));
    }

    [Fact]
    public void Qualification_for_another_work_type_is_rejected()
    {
        var job = PlannedJob();

        Assert.Throws<EvidenceDoesNotMatchJobException>(() => job.RecordQualificationEvidence(
            new QualificationEvidence(Guid.NewGuid(), job.TechnicianId, Plumbing, Now.AddDays(1))));
        Assert.Null(job.QualificationEvidence);
    }
}
