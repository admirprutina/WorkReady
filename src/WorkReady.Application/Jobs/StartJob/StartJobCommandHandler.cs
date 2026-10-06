using WorkReady.Application.Messaging;

namespace WorkReady.Application.Jobs.StartJob;

public sealed class StartJobCommandHandler(IJobEventStore eventStore, TimeProvider timeProvider)
    : IRequestHandler<StartJobCommandRequest, StartJobCommandResult>
{
    public async Task<StartJobCommandResult> Handle(StartJobCommandRequest request, CancellationToken cancellationToken)
    {
        // Load: the Job is rebuilt from its events, and the stream remembers the version it was rebuilt at.
        var stream = await eventStore.FetchForWriting(request.JobId, cancellationToken);
        var job = stream.Aggregate ?? throw new JobNotFoundException(request.JobId);

        // Decide: the domain validates against the loaded state and returns the fact. The Job is not changed.
        var started = job.Start(timeProvider.GetUtcNow());

        // Persist: append to the same stream; saving fails if the stream moved past the fetched version.
        stream.AppendOne(started);
        await eventStore.SaveChangesAsync(cancellationToken);

        return new StartJobCommandResult(started.JobId, started.StartedAt);
    }
}
