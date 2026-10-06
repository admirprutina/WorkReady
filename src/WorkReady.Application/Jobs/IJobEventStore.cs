using WorkReady.Domain.Jobs;
using WorkReady.Domain.Jobs.Events;

namespace WorkReady.Application.Jobs;

/// <summary>
/// The event store, seen from the Job command handlers. Only what they need: start a Job stream, fetch an
/// existing one for writing, and commit. State is never saved directly; it is rebuilt from the stream's events.
/// </summary>
public interface IJobEventStore
{
    /// <summary>Starts a new Job stream whose id is <see cref="JobCreated.JobId"/>, with the event as its first event.</summary>
    void StartStream(JobCreated created);

    /// <summary>
    /// Loads the Job stream for writing: the Job rebuilt from its events, plus the stream version it was rebuilt at,
    /// so that saving fails if someone else appended to the stream in the meantime.
    /// </summary>
    Task<IJobEventStream> FetchForWriting(Guid jobId, CancellationToken cancellationToken);

    /// <summary>Commits everything started or appended since the last save, in one transaction.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>A Job stream fetched for writing.</summary>
public interface IJobEventStream
{
    Guid JobId { get; }

    /// <summary>The Job rebuilt from the stream's events, or <c>null</c> when the stream does not exist.</summary>
    Job? Aggregate { get; }

    /// <summary>Appends an event to this stream. It is persisted on save; <see cref="Aggregate"/> is not changed.</summary>
    void AppendOne(object @event);
}
