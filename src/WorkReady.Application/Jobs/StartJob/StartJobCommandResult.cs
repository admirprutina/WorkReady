namespace WorkReady.Application.Jobs.StartJob;

public sealed record StartJobCommandResult(Guid JobId, DateTimeOffset StartedAt);
