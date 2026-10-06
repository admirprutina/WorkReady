using WorkReady.Application.Messaging;

namespace WorkReady.Application.Jobs.GetJobById;

/// <summary>Returns the Job's details, or <c>null</c> when there is no such Job.</summary>
public sealed record GetJobByIdQueryRequest(Guid JobId) : IRequest<GetJobByIdQueryResult?>;
