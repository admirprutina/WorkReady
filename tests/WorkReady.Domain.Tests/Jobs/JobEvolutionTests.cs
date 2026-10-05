using WorkReady.Domain.Jobs;
using WorkReady.Domain.Jobs.Events;
using static WorkReady.Domain.Tests.Jobs.JobTestData;

namespace WorkReady.Domain.Tests.Jobs;

/// <summary>Applying events rebuilds the job's state. Readiness is derived from that state, never stored.</summary>
public class JobEvolutionTests
{
    [Fact]
    public void Job_created_builds_a_planned_job_without_evidence()
    {
        var job = Job.Create(Created());

        Assert.Equal(JobId, job.Id);
        Assert.Equal(TechnicianId, job.TechnicianId);
        Assert.Equal(SiteId, job.SiteId);
        Assert.Equal(HighVoltage, job.WorkType);
        Assert.Equal(Now.AddHours(2), job.PlannedStart);
        Assert.Equal(JobState.Planned, job.State);
        Assert.Null(job.QualificationEvidence);
        Assert.Null(job.SafetyTrainingEvidence);
        Assert.Null(job.SiteAccessEvidence);
    }

    [Fact]
    public void Qualification_evidence_recorded_stores_the_evidence()
    {
        var evidence = Qualification();

        var job = Given(new QualificationEvidenceRecorded(JobId, evidence));

        Assert.Same(evidence, job.QualificationEvidence);
    }

    [Fact]
    public void Safety_training_evidence_recorded_stores_the_evidence()
    {
        var evidence = SafetyTraining();

        var job = Given(new SafetyTrainingEvidenceRecorded(JobId, evidence));

        Assert.Same(evidence, job.SafetyTrainingEvidence);
    }

    [Fact]
    public void Site_access_evidence_recorded_stores_the_evidence()
    {
        var evidence = SiteAccess();

        var job = Given(new SiteAccessEvidenceRecorded(JobId, evidence));

        Assert.Same(evidence, job.SiteAccessEvidence);
    }

    [Fact]
    public void Recording_evidence_again_replaces_the_previous_evidence()
    {
        var newer = SiteAccess(freshUntil: Now.AddHours(8));

        var job = Given(
            new SiteAccessEvidenceRecorded(JobId, SiteAccess()),
            new SiteAccessEvidenceRecorded(JobId, newer));

        Assert.Same(newer, job.SiteAccessEvidence);
    }

    [Fact]
    public void Technician_reassigned_changes_the_technician_and_clears_all_evidence()
    {
        var otherTechnician = Guid.NewGuid();

        var job = GivenAllEvidence(new TechnicianReassigned(JobId, otherTechnician));

        Assert.Equal(JobId, job.Id);
        Assert.Equal(otherTechnician, job.TechnicianId);
        Assert.Null(job.QualificationEvidence);
        Assert.Null(job.SafetyTrainingEvidence);
        Assert.Null(job.SiteAccessEvidence);
        Assert.False(job.CanStart(Now));
    }

    [Fact]
    public void Work_type_changed_changes_the_work_type_and_clears_only_qualification_evidence()
    {
        var safetyTraining = SafetyTraining();
        var siteAccess = SiteAccess();

        var job = Given(
            new QualificationEvidenceRecorded(JobId, Qualification()),
            new SafetyTrainingEvidenceRecorded(JobId, safetyTraining),
            new SiteAccessEvidenceRecorded(JobId, siteAccess),
            new WorkTypeChanged(JobId, Plumbing));

        Assert.Equal(Plumbing, job.WorkType);
        Assert.Null(job.QualificationEvidence);
        Assert.Same(safetyTraining, job.SafetyTrainingEvidence);
        Assert.Same(siteAccess, job.SiteAccessEvidence);
        Assert.False(job.CanStart(Now));
    }

    [Fact]
    public void Job_rescheduled_changes_the_planned_start_and_keeps_evidence()
    {
        var job = GivenAllEvidence(new JobRescheduled(JobId, Now.AddHours(3)));

        Assert.Equal(JobId, job.Id);
        Assert.Equal(Now.AddHours(3), job.PlannedStart);
        Assert.NotNull(job.QualificationEvidence);
        Assert.NotNull(job.SafetyTrainingEvidence);
        Assert.NotNull(job.SiteAccessEvidence);
        Assert.True(job.CanStart(Now.AddHours(3)));
    }

    [Fact]
    public void Rescheduled_job_is_judged_on_evidence_validity_at_start_time()
    {
        var job = GivenAllEvidence(new JobRescheduled(JobId, Now.AddDays(2)));

        // Site access was only fresh for a few hours, so it is stale by the new start.
        Assert.False(job.CanStart(Now.AddDays(2)));
    }

    [Fact]
    public void Job_started_changes_the_state_to_started()
    {
        var job = GivenAllEvidence(new JobStarted(JobId, Now));

        Assert.Equal(JobState.Started, job.State);
        Assert.False(job.CanStart(Now));
    }

    [Fact]
    public void Readiness_is_derived_from_evidence_and_not_stored_as_a_state()
    {
        var job = GivenAllEvidence();

        Assert.True(job.CanStart(Now));
        Assert.Equal(JobState.Planned, job.State);
        Assert.Equal([JobState.Planned, JobState.Started], Enum.GetValues<JobState>());
    }

    [Fact]
    public void A_job_with_partial_or_invalid_evidence_cannot_start()
    {
        Assert.False(Given().CanStart(Now));
        Assert.False(Given(
            new QualificationEvidenceRecorded(JobId, Qualification()),
            new SafetyTrainingEvidenceRecorded(JobId, SafetyTraining())).CanStart(Now));
        Assert.False(Given(
            new QualificationEvidenceRecorded(JobId, Qualification(validUntil: Now.AddMinutes(-1))),
            new SafetyTrainingEvidenceRecorded(JobId, SafetyTraining()),
            new SiteAccessEvidenceRecorded(JobId, SiteAccess())).CanStart(Now));
        Assert.False(Given(
            new QualificationEvidenceRecorded(JobId, Qualification()),
            new SafetyTrainingEvidenceRecorded(JobId, SafetyTraining(validUntil: Now.AddMinutes(-1))),
            new SiteAccessEvidenceRecorded(JobId, SiteAccess())).CanStart(Now));
        Assert.False(Given(
            new QualificationEvidenceRecorded(JobId, Qualification()),
            new SafetyTrainingEvidenceRecorded(JobId, SafetyTraining()),
            new SiteAccessEvidenceRecorded(JobId, SiteAccess(freshUntil: Now.AddMinutes(-1)))).CanStart(Now));
    }
}
