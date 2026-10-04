using Microsoft.Extensions.DependencyInjection;
using WorkReady.Application.Messaging;

namespace WorkReady.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISender, Mediator>();

        var assembly = typeof(DependencyInjection).Assembly;

        var handlerTypes = assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(
                implementationType => implementationType
                    .GetInterfaces()
                    .Where(interfaceType =>
                        interfaceType.IsGenericType &&
                        IsRequestHandler(interfaceType))
                    .Select(interfaceType => new
                    {
                        ServiceType = interfaceType,
                        ImplementationType = implementationType
                    }))
            .ToArray();

        var duplicateHandler = handlerTypes
            .GroupBy(handler => handler.ServiceType)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateHandler is not null)
        {
            var requestType = duplicateHandler.Key.GetGenericArguments()[0];

            var implementations = string.Join(
                ", ",
                duplicateHandler.Select(handler => handler.ImplementationType.Name));

            throw new InvalidOperationException(
                $"Multiple handlers found for request '{requestType.Name}': {implementations}.");
        }

        foreach (var handlerType in handlerTypes)
        {
            services.AddScoped(
                handlerType.ServiceType,
                handlerType.ImplementationType);
        }

        return services;
    }

    private static bool IsRequestHandler(Type type)
    {
        var genericType = type.GetGenericTypeDefinition();

        return genericType == typeof(IRequestHandler<,>) ||
               genericType == typeof(IRequestHandler<>);
    }
}
