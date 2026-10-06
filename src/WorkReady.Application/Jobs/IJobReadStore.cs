using WorkReady.Application.Jobs.ReadModels;

namespace WorkReady.Application.Jobs;

/// <summary>
/// The Job read models, seen from the query handlers. Reads already-projected documents;
/// it never loads the event stream or rebuilds the Job aggregate.
/// </summary>
public interface IJobReadStore
{
    Task<JobDetailsReadModel?> GetById(Guid jobId, CancellationToken cancellationToken);
}
