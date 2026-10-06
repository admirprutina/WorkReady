using JasperFx.Events;
using JasperFx.Events.Projections;
using Marten;
using WorkReady.Infrastructure.Jobs;

namespace WorkReady.Infrastructure;

/// <summary>The Marten store settings, shared by the application and the integration tests.</summary>
public static class MartenConfiguration
{
    public static void Configure(StoreOptions options, string connectionString)
    {
        options.Connection(connectionString);

        // Streams are identified by the aggregate id; a Job stream's id is the JobId.
        options.Events.StreamIdentity = StreamIdentity.AsGuid;

        // Write side. The Job aggregate is rebuilt with Job.Create(JobCreated) and Job.Apply(...) (see JobAggregation).
        // Live: rebuilt from the events on every fetch for a command, never stored.
        options.Projections.Add(new JobAggregation(), ProjectionLifecycle.Live);

        // Read side. JobDetailsReadModel is a stored document, updated from the Job events.
        // Inline: written in the same transaction as the events, so a query right after a command sees it.
        options.Projections.Add(new JobDetailsProjection(), ProjectionLifecycle.Inline);
    }
}
