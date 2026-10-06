using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkReady.Application.Jobs;
using WorkReady.Infrastructure.Jobs;

namespace WorkReady.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "WorkReady";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured.");

        services
            .AddMarten(options => MartenConfiguration.Configure(options, connectionString))
            .UseLightweightSessions();

        services.AddScoped<IJobEventStore, MartenJobEventStore>();
        services.AddScoped<IJobReadStore, MartenJobReadStore>();

        return services;
    }
}
