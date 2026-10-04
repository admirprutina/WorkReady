namespace WorkReady.Application.Messaging;

public interface ISender
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken);

    Task Send(IRequest request, CancellationToken cancellationToken);
}
