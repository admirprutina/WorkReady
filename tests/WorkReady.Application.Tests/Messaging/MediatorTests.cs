using Microsoft.Extensions.DependencyInjection;
using WorkReady.Application.Messaging;

namespace WorkReady.Application.Tests.Messaging;

public class MediatorTests
{
    private sealed record Ping(string Text) : IRequest<string>;

    private sealed class PingHandler : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken)
            => Task.FromResult(request.Text);
    }

    private sealed record Notify(string Text) : IRequest;

    private sealed class NotifyHandler : IRequestHandler<Notify>
    {
        public List<string> Received { get; } = [];

        public Task Handle(Notify request, CancellationToken cancellationToken)
        {
            Received.Add(request.Text);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingBehavior<TRequest, TResponse>(string name, List<string> trace)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            trace.Add($"{name}:before");
            var response = await next();
            trace.Add($"{name}:after");
            return response;
        }
    }

    private static ISender BuildSender(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        var provider = services.BuildServiceProvider();
        return new Mediator(provider);
    }

    [Fact]
    public async Task Send_WithResponse_DispatchesToRegisteredHandler()
    {
        var sender = BuildSender(services =>
            services.AddScoped<IRequestHandler<Ping, string>, PingHandler>());

        var result = await sender.Send(new Ping("hello"), CancellationToken.None);

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task Send_Void_DispatchesToRegisteredHandler()
    {
        var handler = new NotifyHandler();
        var sender = BuildSender(services =>
            services.AddScoped<IRequestHandler<Notify>>(_ => handler));

        await sender.Send(new Notify("hi"), CancellationToken.None);

        Assert.Equal(["hi"], handler.Received);
    }

    [Fact]
    public async Task Send_WithResponse_RunsBehaviorsInRegistrationOrder()
    {
        var trace = new List<string>();
        var sender = BuildSender(services =>
        {
            services.AddScoped<IRequestHandler<Ping, string>, PingHandler>();
            services.AddScoped<IPipelineBehavior<Ping, string>>(
                _ => new RecordingBehavior<Ping, string>("outer", trace));
            services.AddScoped<IPipelineBehavior<Ping, string>>(
                _ => new RecordingBehavior<Ping, string>("inner", trace));
        });

        await sender.Send(new Ping("hello"), CancellationToken.None);

        Assert.Equal(
            ["outer:before", "inner:before", "inner:after", "outer:after"],
            trace);
    }

    [Fact]
    public async Task Send_WithResponse_UnregisteredHandler_Throws()
    {
        var sender = BuildSender(_ => { });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new Ping("hello"), CancellationToken.None));
    }

    [Fact]
    public async Task Send_WithResponse_NullRequest_ThrowsArgumentNullException()
    {
        var sender = BuildSender(_ => { });

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sender.Send((IRequest<string>)null!, CancellationToken.None));
    }

    [Fact]
    public async Task Send_Void_NullRequest_ThrowsArgumentNullException()
    {
        var sender = BuildSender(_ => { });

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sender.Send((IRequest)null!, CancellationToken.None));
    }
}
