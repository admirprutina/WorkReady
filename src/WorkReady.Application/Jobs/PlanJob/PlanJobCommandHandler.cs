using WorkReady.Application.Messaging;
using WorkReady.Domain;
using WorkReady.Domain.Jobs;

namespace WorkReady.Application.Jobs.PlanJob;

public sealed class PlanJobCommandHandler(IJobEventStore eventStore)
    : IRequestHandler<PlanJobCommandRequest, PlanJobCommandResult>
{
    public async Task<PlanJobCommandResult> Handle(PlanJobCommandRequest request, CancellationToken cancellationToken)
    {
        var jobId = Guid.CreateVersion7();
        var workType = new WorkType(request.WorkType);

        // Decide: the domain validates and returns the fact. No Job state exists yet.
        var created = Job.Plan(jobId, request.TechnicianId, request.SiteId, workType, request.PlannedStart);

        // Persist: the first event of the stream is the source of truth for the new Job.
        eventStore.StartStream(created);
        await eventStore.SaveChangesAsync(cancellationToken);

        return new PlanJobCommandResult(created.JobId);
    }
}
