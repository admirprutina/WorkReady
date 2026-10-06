using WorkReady.Application.Jobs;
using WorkReady.Domain.Jobs;
using WorkReady.Domain.Jobs.Events;

namespace WorkReady.Application.Tests.Jobs;

/// <summary>
/// In-memory stand-in for the event store. Keeps each stream's events and rebuilds the Job from them on fetch,
/// the way the real store does, and records what the handler started, appended and saved.
/// </summary>
internal sealed class FakeJobEventStore : IJobEventStore
{
    private readonly Dictionary<Guid, List<object>> _streams = [];

    public List<JobCreated> StartedStreams { get; } = [];

    public List<FakeJobEventStream> FetchedStreams { get; } = [];

    public int SaveCount { get; private set; }

    public void Given(JobCreated created, params object[] events) =>
        _streams[created.JobId] = [created, .. events];

    public void StartStream(JobCreated created) => StartedStreams.Add(created);

    public Task<IJobEventStream> FetchForWriting(Guid jobId, CancellationToken cancellationToken)
    {
        var aggregate = _streams.TryGetValue(jobId, out var events) ? Rebuild(events) : null;
        var stream = new FakeJobEventStream(jobId, aggregate);
        FetchedStreams.Add(stream);
        return Task.FromResult<IJobEventStream>(stream);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    private static Job Rebuild(List<object> events)
    {
        var job = Job.Create((JobCreated)events[0]);

        foreach (var @event in events.Skip(1))
        {
            switch (@event)
            {
                case QualificationEvidenceRecorded e: job.Apply(e); break;
                case SafetyTrainingEvidenceRecorded e: job.Apply(e); break;
                case SiteAccessEvidenceRecorded e: job.Apply(e); break;
                case JobStarted e: job.Apply(e); break;
                default: throw new ArgumentException($"Unsupported event {@event.GetType().Name}.", nameof(events));
            }
        }

        return job;
    }
}

internal sealed class FakeJobEventStream(Guid jobId, Job? aggregate) : IJobEventStream
{
    public Guid JobId { get; } = jobId;

    public Job? Aggregate { get; } = aggregate;

    public List<object> Appended { get; } = [];

    public void AppendOne(object @event) => Appended.Add(@event);
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
