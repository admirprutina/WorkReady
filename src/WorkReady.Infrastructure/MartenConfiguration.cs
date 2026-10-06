using JasperFx.Events;
using Marten;
using WorkReady.Domain.Jobs;

namespace WorkReady.Infrastructure;

/// <summary>The Marten store settings, shared by the application and the integration tests.</summary>
public static class MartenConfiguration
{
    public static void Configure(StoreOptions options, string connectionString)
    {
        options.Connection(connectionString);

        // Streams are identified by the aggregate id; a Job stream's id is the JobId.
        options.Events.StreamIdentity = StreamIdentity.AsGuid;

        // Job is self-aggregating: Marten rebuilds it with Job.Create(JobCreated) and Job.Apply(...).
        // Live: rebuilt from the events on every fetch, nothing is stored besides the events.
        options.Projections.LiveStreamAggregation<Job>();
    }
}
