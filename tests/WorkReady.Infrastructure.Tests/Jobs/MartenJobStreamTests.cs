using JasperFx;
using Marten;
using WorkReady.Domain;
using WorkReady.Domain.Jobs;
using WorkReady.Domain.Jobs.Events;

namespace WorkReady.Infrastructure.Tests.Jobs;

/// <summary>
/// The real Marten flow against PostgreSQL, using the application's store configuration:
/// events go in, and Marten rebuilds the Job through Job.Create / Job.Apply.
/// </summary>
public sealed class MartenJobStreamTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);
    private static readonly WorkType HighVoltage = new("HighVoltage");

    private readonly Guid _jobId = Guid.CreateVersion7();
    private readonly Guid _technicianId = Guid.NewGuid();
    private readonly Guid _siteId = Guid.NewGuid();

    private DocumentStore _store = null!;

    public Task InitializeAsync()
    {
        if (PostgresFactAttribute.ConnectionString is { } connectionString)
        {
            _store = DocumentStore.For(options => MartenConfiguration.Configure(options, connectionString));
        }

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_store is not null)
        {
            await _store.DisposeAsync();
        }
    }

    private JobCreated Created() => new(_jobId, _technicianId, _siteId, HighVoltage, Now.AddHours(2));

    [PostgresFact]
    public async Task Job_is_rebuilt_from_its_stream_after_each_append()
    {
        // 1. Start the stream with its first event.
        await using (var session = _store.LightweightSession())
        {
            session.Events.StartStream<Job>(_jobId, Created());
            await session.SaveChangesAsync();
        }

        // 2. Fetch for writing: Marten rebuilds the Job with Job.Create(JobCreated).
        await using (var session = _store.LightweightSession())
        {
            var stream = await session.Events.FetchForWriting<Job>(_jobId);
            var job = stream.Aggregate!;

            Assert.Equal(_jobId, job.Id);
            Assert.Equal(_technicianId, job.TechnicianId);
            Assert.Equal(_siteId, job.SiteId);
            Assert.Equal(HighVoltage, job.WorkType);
            Assert.Equal(Now.AddHours(2), job.PlannedStart);
            Assert.Equal(JobState.Planned, job.State);
            Assert.False(job.CanStart(Now));

            // 3. Append later events, decided by the loaded Job, to the same stream.
            stream.AppendOne(job.RecordQualificationEvidence(
                new QualificationEvidence(Guid.NewGuid(), _technicianId, HighVoltage, Now.AddDays(365))));
            stream.AppendOne(job.RecordSafetyTrainingEvidence(
                new SafetyTrainingEvidence(Guid.NewGuid(), _technicianId, _siteId, Now.AddDays(90))));
            stream.AppendOne(job.RecordSiteAccessEvidence(
                new SiteAccessEvidence(Guid.NewGuid(), _technicianId, _siteId, Now.AddHours(4))));
            await session.SaveChangesAsync();
        }

        // 4. Reload: Job.Apply(...) rebuilt the evidence, so the job is now ready.
        await using (var session = _store.LightweightSession())
        {
            var stream = await session.Events.FetchForWriting<Job>(_jobId);
            var job = stream.Aggregate!;

            Assert.Equal(HighVoltage, job.QualificationEvidence?.WorkType);
            Assert.NotNull(job.SafetyTrainingEvidence);
            Assert.NotNull(job.SiteAccessEvidence);
            Assert.True(job.CanStart(Now));

            stream.AppendOne(job.Start(Now));
            await session.SaveChangesAsync();
        }

        // 5. Reload again: Job.Apply(JobStarted) moved it to Started.
        await using (var session = _store.LightweightSession())
        {
            var job = await session.Events.AggregateStreamAsync<Job>(_jobId);

            Assert.Equal(JobState.Started, job!.State);

            var events = await session.Events.FetchStreamAsync(_jobId);
            Assert.Equal(
                [typeof(JobCreated), typeof(QualificationEvidenceRecorded), typeof(SafetyTrainingEvidenceRecorded),
                 typeof(SiteAccessEvidenceRecorded), typeof(JobStarted)],
                events.Select(e => e.EventType));
        }
    }

    [PostgresFact]
    public async Task Fetching_a_missing_stream_gives_no_aggregate()
    {
        await using var session = _store.LightweightSession();

        var stream = await session.Events.FetchForWriting<Job>(Guid.CreateVersion7());

        Assert.Null(stream.Aggregate);
    }

    [PostgresFact]
    public async Task Saving_fails_when_the_stream_moved_after_it_was_fetched()
    {
        await using (var session = _store.LightweightSession())
        {
            session.Events.StartStream<Job>(_jobId, Created());
            await session.SaveChangesAsync();
        }

        await using var first = _store.LightweightSession();
        await using var second = _store.LightweightSession();

        var firstStream = await first.Events.FetchForWriting<Job>(_jobId);
        var secondStream = await second.Events.FetchForWriting<Job>(_jobId);

        firstStream.AppendOne(firstStream.Aggregate!.Reschedule(Now.AddDays(1)));
        await first.SaveChangesAsync();

        // The second writer decided on a stale version of the Job.
        secondStream.AppendOne(secondStream.Aggregate!.Reschedule(Now.AddDays(2)));
        await Assert.ThrowsAnyAsync<ConcurrencyException>(() => second.SaveChangesAsync());
    }
}
