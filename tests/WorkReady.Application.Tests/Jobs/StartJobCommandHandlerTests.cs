using WorkReady.Application.Jobs;
using WorkReady.Application.Jobs.StartJob;
using WorkReady.Domain;
using WorkReady.Domain.Jobs;
using WorkReady.Domain.Jobs.Events;

namespace WorkReady.Application.Tests.Jobs;

public class StartJobCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);

    private static readonly Guid JobId = Guid.NewGuid();
    private static readonly Guid TechnicianId = Guid.NewGuid();
    private static readonly Guid SiteId = Guid.NewGuid();
    private static readonly WorkType HighVoltage = new("HighVoltage");

    private readonly FakeJobEventStore _eventStore = new();

    private StartJobCommandHandler Handler() => new(_eventStore, new FixedTimeProvider(Now));

    private static JobCreated Created() => new(JobId, TechnicianId, SiteId, HighVoltage, Now.AddHours(2));

    private static object[] AllEvidenceRecorded() =>
    [
        new QualificationEvidenceRecorded(
            JobId, new QualificationEvidence(Guid.NewGuid(), TechnicianId, HighVoltage, Now.AddDays(365))),
        new SafetyTrainingEvidenceRecorded(
            JobId, new SafetyTrainingEvidence(Guid.NewGuid(), TechnicianId, SiteId, Now.AddDays(90))),
        new SiteAccessEvidenceRecorded(
            JobId, new SiteAccessEvidence(Guid.NewGuid(), TechnicianId, SiteId, Now.AddHours(4)))
    ];

    [Fact]
    public async Task Starting_a_ready_job_appends_job_started_to_the_fetched_stream_and_saves()
    {
        _eventStore.Given(Created(), AllEvidenceRecorded());

        var result = await Handler().Handle(new StartJobCommandRequest(JobId), CancellationToken.None);

        var stream = Assert.Single(_eventStore.FetchedStreams);
        Assert.Equal(JobId, stream.JobId);
        Assert.Equal(new JobStarted(JobId, Now), Assert.Single(stream.Appended));
        Assert.Equal(1, _eventStore.SaveCount);
        Assert.Equal(new StartJobCommandResult(JobId, Now), result);
    }

    [Fact]
    public async Task Starting_does_not_change_the_loaded_job()
    {
        _eventStore.Given(Created(), AllEvidenceRecorded());

        await Handler().Handle(new StartJobCommandRequest(JobId), CancellationToken.None);

        // The handler only appends the event; the store rebuilds state from it on the next fetch.
        var stream = Assert.Single(_eventStore.FetchedStreams);
        Assert.Equal(JobState.Planned, stream.Aggregate!.State);
    }

    [Fact]
    public async Task Starting_a_missing_job_throws_job_not_found_and_saves_nothing()
    {
        var missingJobId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<JobNotFoundException>(
            () => Handler().Handle(new StartJobCommandRequest(missingJobId), CancellationToken.None));

        Assert.Equal(missingJobId, exception.JobId);
        Assert.Empty(Assert.Single(_eventStore.FetchedStreams).Appended);
        Assert.Equal(0, _eventStore.SaveCount);
    }

    [Fact]
    public async Task Starting_a_job_without_evidence_is_rejected_by_the_domain_and_nothing_is_appended()
    {
        _eventStore.Given(Created());

        await Assert.ThrowsAsync<JobNotReadyToStartException>(
            () => Handler().Handle(new StartJobCommandRequest(JobId), CancellationToken.None));

        Assert.Empty(Assert.Single(_eventStore.FetchedStreams).Appended);
        Assert.Equal(0, _eventStore.SaveCount);
    }

    [Fact]
    public async Task Starting_an_already_started_job_is_rejected_by_the_domain_and_nothing_is_appended()
    {
        _eventStore.Given(Created(), [.. AllEvidenceRecorded(), new JobStarted(JobId, Now.AddMinutes(-5))]);

        await Assert.ThrowsAsync<JobNotPlannedException>(
            () => Handler().Handle(new StartJobCommandRequest(JobId), CancellationToken.None));

        Assert.Empty(Assert.Single(_eventStore.FetchedStreams).Appended);
        Assert.Equal(0, _eventStore.SaveCount);
    }
}
