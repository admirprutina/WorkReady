using WorkReady.Application.Messaging;

namespace WorkReady.Application.Jobs.GetJobById;

public sealed class GetJobByIdQueryHandler(IJobReadStore readStore)
    : IRequestHandler<GetJobByIdQueryRequest, GetJobByIdQueryResult?>
{
    public async Task<GetJobByIdQueryResult?> Handle(GetJobByIdQueryRequest request, CancellationToken cancellationToken)
    {
        // Read the projected document as it is. No event stream, no Job aggregate, no business rules.
        var job = await readStore.GetById(request.JobId, cancellationToken);

        return job is null
            ? null
            : new GetJobByIdQueryResult(
                job.Id,
                job.TechnicianId,
                job.SiteId,
                job.WorkType,
                job.PlannedStart,
                job.State,
                job.StartedAt);
    }
}
