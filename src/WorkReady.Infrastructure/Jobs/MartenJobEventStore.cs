using JasperFx.Events;
using Marten;
using WorkReady.Application.Jobs;
using WorkReady.Domain.Jobs;
using WorkReady.Domain.Jobs.Events;

namespace WorkReady.Infrastructure.Jobs;

internal sealed class MartenJobEventStore(IDocumentSession session) : IJobEventStore
{
    public void StartStream(JobCreated created) =>
        session.Events.StartStream<Job>(created.JobId, created);

    public async Task<IJobEventStream> FetchForWriting(Guid jobId, CancellationToken cancellationToken)
    {
        var stream = await session.Events.FetchForWriting<Job>(jobId, cancellationToken);

        return new MartenJobEventStream(stream);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        session.SaveChangesAsync(cancellationToken);

    private sealed class MartenJobEventStream(IEventStream<Job> stream) : IJobEventStream
    {
        public Guid JobId => stream.Id;

        public Job? Aggregate => stream.Aggregate;

        public void AppendOne(object @event) => stream.AppendOne(@event);
    }
}
