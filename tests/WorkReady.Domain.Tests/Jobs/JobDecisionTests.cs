using WorkReady.Domain.Jobs;
using WorkReady.Domain.Jobs.Events;
using static WorkReady.Domain.Tests.Jobs.JobTestData;

namespace WorkReady.Domain.Tests.Jobs;

/// <summary>Given current state, a decision returns the resulting event or throws. It never changes the job.</summary>
public class JobDecisionTests
{
    // Plan

    [Fact]
    public void Planning_a_job_returns_job_created()
    {
        var created = Job.Plan(JobId, TechnicianId, SiteId, HighVoltage, Now.AddHours(2));

        Assert.Equal(new JobCreated(JobId, TechnicianId, SiteId, HighVoltage, Now.AddHours(2)), created);
    }

    [Fact]
    public void Planning_a_job_requires_job_technician_site_and_work_type()
    {
        Assert.Throws<ArgumentException>(() => Job.Plan(Guid.Empty, TechnicianId, SiteId, HighVoltage, Now));
        Assert.Throws<ArgumentException>(() => Job.Plan(JobId, Guid.Empty, SiteId, HighVoltage, Now));
        Assert.Throws<ArgumentException>(() => Job.Plan(JobId, TechnicianId, Guid.Empty, HighVoltage, Now));
        Assert.Throws<ArgumentNullException>(() => Job.Plan(JobId, TechnicianId, SiteId, null!, Now));
    }

    // ReassignTechnician

    [Fact]
    public void Reassigning_the_technician_returns_technician_reassigned()
    {
        var job = GivenAllEvidence();
        var otherTechnician = Guid.NewGuid();

        var @event = job.ReassignTechnician(otherTechnician);

        Assert.Equal(new TechnicianReassigned(JobId, otherTechnician), @event);
    }

    [Fact]
    public void Reassigning_to_the_current_technician_returns_no_event()
    {
        var job = Given();

        Assert.Null(job.ReassignTechnician(TechnicianId));
    }

    [Fact]
    public void Reassigning_requires_a_technician()
    {
        var job = Given();

        Assert.Throws<ArgumentException>(() => job.ReassignTechnician(Guid.Empty));
    }

    [Fact]
    public void After_start_the_technician_cannot_change()
    {
        var job = GivenStarted();

        Assert.Throws<JobNotPlannedException>(() => job.ReassignTechnician(Guid.NewGuid()));
    }

    // ChangeWorkType

    [Fact]
    public void Changing_the_work_type_returns_work_type_changed()
    {
        var job = GivenAllEvidence();

        var @event = job.ChangeWorkType(Plumbing);

        Assert.Equal(new WorkTypeChanged(JobId, Plumbing), @event);
    }

    [Fact]
    public void Changing_to_the_current_work_type_returns_no_event()
    {
        var job = Given();

        Assert.Null(job.ChangeWorkType(new WorkType("HighVoltage")));
    }

    [Fact]
    public void After_start_the_work_type_cannot_change()
    {
        var job = GivenStarted();

        Assert.Throws<JobNotPlannedException>(() => job.ChangeWorkType(Plumbing));
    }

    // Reschedule

    [Fact]
    public void Rescheduling_returns_job_rescheduled()
    {
        var job = GivenAllEvidence();

        var @event = job.Reschedule(Now.AddDays(1));

        Assert.Equal(new JobRescheduled(JobId, Now.AddDays(1)), @event);
    }

    [Fact]
    public void After_start_the_job_cannot_be_rescheduled()
    {
        var job = GivenStarted();

        Assert.Throws<JobNotPlannedException>(() => job.Reschedule(Now.AddDays(1)));
    }

    // Record evidence

    [Fact]
    public void Recording_matching_qualification_returns_qualification_evidence_recorded()
    {
        var job = Given();
        var evidence = Qualification();

        var @event = job.RecordQualificationEvidence(evidence);

        Assert.Equal(new QualificationEvidenceRecorded(JobId, evidence), @event);
    }

    [Fact]
    public void Recording_matching_safety_training_returns_safety_training_evidence_recorded()
    {
        var job = Given();
        var evidence = SafetyTraining();

        var @event = job.RecordSafetyTrainingEvidence(evidence);

        Assert.Equal(new SafetyTrainingEvidenceRecorded(JobId, evidence), @event);
    }

    [Fact]
    public void Recording_matching_site_access_returns_site_access_evidence_recorded()
    {
        var job = Given();
        var evidence = SiteAccess();

        var @event = job.RecordSiteAccessEvidence(evidence);

        Assert.Equal(new SiteAccessEvidenceRecorded(JobId, evidence), @event);
    }

    [Fact]
    public void Evidence_for_another_technician_is_rejected()
    {
        var job = Given();
        var otherTechnician = Guid.NewGuid();

        Assert.Throws<EvidenceDoesNotMatchJobException>(
            () => job.RecordQualificationEvidence(Qualification(technicianId: otherTechnician)));
        Assert.Throws<EvidenceDoesNotMatchJobException>(
            () => job.RecordSafetyTrainingEvidence(SafetyTraining(technicianId: otherTechnician)));
        Assert.Throws<EvidenceDoesNotMatchJobException>(
            () => job.RecordSiteAccessEvidence(SiteAccess(technicianId: otherTechnician)));
    }

    [Fact]
    public void Evidence_for_the_previous_technician_is_rejected_after_reassignment()
    {
        var job = Given(new TechnicianReassigned(JobId, Guid.NewGuid()));

        Assert.Throws<EvidenceDoesNotMatchJobException>(() => job.RecordQualificationEvidence(Qualification()));
    }

    [Fact]
    public void Evidence_for_another_site_is_rejected()
    {
        var job = Given();
        var otherSite = Guid.NewGuid();

        Assert.Throws<EvidenceDoesNotMatchJobException>(
            () => job.RecordSafetyTrainingEvidence(SafetyTraining(siteId: otherSite)));
        Assert.Throws<EvidenceDoesNotMatchJobException>(
            () => job.RecordSiteAccessEvidence(SiteAccess(siteId: otherSite)));
    }

    [Fact]
    public void Qualification_for_another_work_type_is_rejected()
    {
        var job = Given();

        Assert.Throws<EvidenceDoesNotMatchJobException>(
            () => job.RecordQualificationEvidence(Qualification(workType: Plumbing)));
    }

    [Fact]
    public void After_start_new_evidence_cannot_be_recorded()
    {
        var job = GivenStarted();

        Assert.Throws<JobNotPlannedException>(() => job.RecordQualificationEvidence(Qualification()));
        Assert.Throws<JobNotPlannedException>(() => job.RecordSafetyTrainingEvidence(SafetyTraining()));
        Assert.Throws<JobNotPlannedException>(() => job.RecordSiteAccessEvidence(SiteAccess()));
    }

    // Start

    [Fact]
    public void Starting_with_all_matching_valid_evidence_returns_job_started()
    {
        var job = GivenAllEvidence();

        var @event = job.Start(Now);

        Assert.Equal(new JobStarted(JobId, Now), @event);
    }

    [Fact]
    public void A_job_cannot_start_without_evidence()
    {
        var job = Given();

        Assert.Throws<JobNotReadyToStartException>(() => job.Start(Now));
    }

    [Fact]
    public void A_job_cannot_start_with_only_some_evidence()
    {
        var job = Given(
            new QualificationEvidenceRecorded(JobId, Qualification()),
            new SafetyTrainingEvidenceRecorded(JobId, SafetyTraining()));

        Assert.Throws<JobNotReadyToStartException>(() => job.Start(Now));
    }

    [Fact]
    public void A_job_cannot_start_with_expired_qualification()
    {
        var job = Given(
            new QualificationEvidenceRecorded(JobId, Qualification(validUntil: Now.AddMinutes(-1))),
            new SafetyTrainingEvidenceRecorded(JobId, SafetyTraining()),
            new SiteAccessEvidenceRecorded(JobId, SiteAccess()));

        Assert.Throws<JobNotReadyToStartException>(() => job.Start(Now));
    }

    [Fact]
    public void A_job_cannot_start_with_expired_safety_training()
    {
        var job = Given(
            new QualificationEvidenceRecorded(JobId, Qualification()),
            new SafetyTrainingEvidenceRecorded(JobId, SafetyTraining(validUntil: Now.AddMinutes(-1))),
            new SiteAccessEvidenceRecorded(JobId, SiteAccess()));

        Assert.Throws<JobNotReadyToStartException>(() => job.Start(Now));
    }

    [Fact]
    public void A_job_cannot_start_with_stale_site_access()
    {
        var job = Given(
            new QualificationEvidenceRecorded(JobId, Qualification()),
            new SafetyTrainingEvidenceRecorded(JobId, SafetyTraining()),
            new SiteAccessEvidenceRecorded(JobId, SiteAccess(freshUntil: Now.AddMinutes(-1))));

        Assert.Throws<JobNotReadyToStartException>(() => job.Start(Now));
    }

    [Fact]
    public void A_job_cannot_start_after_its_work_type_changed_without_new_qualification()
    {
        var job = GivenAllEvidence(new WorkTypeChanged(JobId, Plumbing));

        Assert.Throws<JobNotReadyToStartException>(() => job.Start(Now));
    }

    [Fact]
    public void A_started_job_cannot_start_again()
    {
        var job = GivenStarted();

        Assert.Throws<JobNotPlannedException>(() => job.Start(Now));
    }

    // Decisions are pure with respect to the aggregate

    [Fact]
    public void Decisions_do_not_change_the_job()
    {
        var job = GivenAllEvidence();
        var before = Snapshot(job);

        job.ReassignTechnician(Guid.NewGuid());
        job.ChangeWorkType(Plumbing);
        job.Reschedule(Now.AddDays(1));
        job.RecordQualificationEvidence(Qualification());
        job.RecordSafetyTrainingEvidence(SafetyTraining());
        job.RecordSiteAccessEvidence(SiteAccess());
        job.Start(Now);

        Assert.Equal(before, Snapshot(job));
    }

    [Fact]
    public void Rejected_decisions_do_not_change_the_job()
    {
        var job = Given();
        var before = Snapshot(job);

        Assert.Throws<JobNotReadyToStartException>(() => job.Start(Now));
        Assert.Throws<EvidenceDoesNotMatchJobException>(
            () => job.RecordQualificationEvidence(Qualification(workType: Plumbing)));

        Assert.Equal(before, Snapshot(job));
    }

    private static object Snapshot(Job job) => (
        job.Id,
        job.TechnicianId,
        job.SiteId,
        job.WorkType,
        job.PlannedStart,
        job.State,
        job.QualificationEvidence,
        job.SafetyTrainingEvidence,
        job.SiteAccessEvidence);
}
