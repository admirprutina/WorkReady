using WorkReady.Application.Messaging;

namespace WorkReady.Application.Jobs.StartJob;

public sealed record StartJobCommandRequest(Guid JobId) : IRequest<StartJobCommandResult>;
