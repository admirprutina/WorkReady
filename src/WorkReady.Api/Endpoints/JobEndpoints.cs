using WorkReady.Application.Jobs.PlanJob;
using WorkReady.Application.Jobs.StartJob;
using WorkReady.Application.Messaging;

namespace WorkReady.Api.Endpoints;

public static class JobEndpoints
{
    public static IEndpointRouteBuilder MapJobEndpoints(this IEndpointRouteBuilder app)
    {
        var jobs = app.MapGroup("/api/jobs");

        jobs.MapPost("/", PlanJob);
        jobs.MapPost("/{jobId:guid}/start", StartJob);

        return app;
    }

    private static async Task<IResult> PlanJob(PlanJobBody body, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new PlanJobCommandRequest(body.TechnicianId, body.SiteId, body.WorkType, body.PlannedStart),
            cancellationToken);

        return Results.Created($"/api/jobs/{result.JobId}", result);
    }

    private static async Task<IResult> StartJob(Guid jobId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartJobCommandRequest(jobId), cancellationToken);

        return Results.Ok(result);
    }

    public sealed record PlanJobBody(Guid TechnicianId, Guid SiteId, string WorkType, DateTimeOffset PlannedStart);
}
