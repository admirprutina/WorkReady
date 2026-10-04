using Microsoft.Extensions.DependencyInjection;

namespace WorkReady.Application.Messaging;

internal abstract class RequestHandlerWrapper
{
    public abstract Task<object?> Handle(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}

internal sealed class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerWrapper
    where TRequest : IRequest<TResponse>
{
    public override async Task<object?> Handle(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest, TResponse>>();

        RequestHandlerDelegate<TResponse> next = () => handler.Handle(typedRequest, cancellationToken);

        foreach (var behavior in behaviors.Reverse())
        {
            var currentNext = next;
            next = () => behavior.Handle(typedRequest, currentNext, cancellationToken);
        }

        return await next();
    }
}

internal sealed class RequestHandlerWrapper<TRequest> : RequestHandlerWrapper
    where TRequest : IRequest
{
    public override async Task<object?> Handle(
        object request,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest>>();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TRequest>>();

        RequestHandlerDelegate next = () => handler.Handle(typedRequest, cancellationToken);

        foreach (var behavior in behaviors.Reverse())
        {
            var currentNext = next;
            next = () => behavior.Handle(typedRequest, currentNext, cancellationToken);
        }

        await next();

        return null;
    }
}
