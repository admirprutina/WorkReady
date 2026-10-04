using System.Collections.Concurrent;

namespace WorkReady.Application.Messaging;

public sealed class Mediator(IServiceProvider serviceProvider) : ISender
{
    private static readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), RequestHandlerWrapper>
        ResponseWrappers = new();

    private static readonly ConcurrentDictionary<Type, RequestHandlerWrapper> RequestWrappers = new();

    public async Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var key = (request.GetType(), typeof(TResponse));

        var wrapper = ResponseWrappers.GetOrAdd(
            key,
            static key => CreateResponseWrapper(key.RequestType, key.ResponseType));

        var response = await wrapper.Handle(request, serviceProvider, cancellationToken);

        return (TResponse)response!;
    }

    public async Task Send(IRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        var wrapper = RequestWrappers.GetOrAdd(
            requestType,
            static type => CreateRequestWrapper(type));

        await wrapper.Handle(request, serviceProvider, cancellationToken);
    }

    private static RequestHandlerWrapper CreateResponseWrapper(Type requestType, Type responseType)
    {
        var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(requestType, responseType);

        return CreateWrapper(wrapperType, requestType);
    }

    private static RequestHandlerWrapper CreateRequestWrapper(Type requestType)
    {
        var wrapperType = typeof(RequestHandlerWrapper<>).MakeGenericType(requestType);

        return CreateWrapper(wrapperType, requestType);
    }

    private static RequestHandlerWrapper CreateWrapper(Type wrapperType, Type requestType)
    {
        return Activator.CreateInstance(wrapperType) as RequestHandlerWrapper
            ?? throw new InvalidOperationException(
                $"Could not create a handler wrapper for request '{requestType.Name}'.");
    }
}
