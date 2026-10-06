using WorkReady.Application.Messaging;

namespace WorkReady.Application.Jobs.PlanJob;

public sealed record PlanJobCommandRequest(
    Guid TechnicianId,
    Guid SiteId,
    string WorkType,
    DateTimeOffset PlannedStart) : IRequest<PlanJobCommandResult>;
