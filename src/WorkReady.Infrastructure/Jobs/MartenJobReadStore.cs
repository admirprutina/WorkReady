using Marten;
using WorkReady.Application.Jobs;
using WorkReady.Application.Jobs.ReadModels;

namespace WorkReady.Infrastructure.Jobs;

internal sealed class MartenJobReadStore(IQuerySession session) : IJobReadStore
{
    // Loads the document the inline projection stored; the event stream is not touched.
    public Task<JobDetailsReadModel?> GetById(Guid jobId, CancellationToken cancellationToken) =>
        session.LoadAsync<JobDetailsReadModel>(jobId, cancellationToken);
}
