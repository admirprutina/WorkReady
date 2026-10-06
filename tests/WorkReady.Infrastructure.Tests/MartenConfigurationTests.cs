using JasperFx.Events.Projections;
using Marten;
using WorkReady.Application.Jobs.ReadModels;
using WorkReady.Infrastructure.Jobs;

namespace WorkReady.Infrastructure.Tests;

/// <summary>Builds the store from the real configuration; Marten validates projections here, no database needed.</summary>
public class MartenConfigurationTests
{
    private static DocumentStore Store() =>
        DocumentStore.For(options => MartenConfiguration.Configure(options, "Host=localhost;Database=not_used"));

    [Fact]
    public async Task Job_aggregate_is_rebuilt_live_for_commands()
    {
        await using var store = Store();

        var projection = Assert.Single(store.Options.Projections.All, source => source is JobAggregation);

        Assert.Equal(ProjectionLifecycle.Live, projection.Lifecycle);
    }

    [Fact]
    public async Task Job_details_projection_is_registered_inline()
    {
        await using var store = Store();

        var projection = Assert.Single(store.Options.Projections.All, source => source is JobDetailsProjection);

        Assert.Equal(ProjectionLifecycle.Inline, projection.Lifecycle);
        Assert.NotNull(store.Options.Schema.For<JobDetailsReadModel>());
    }
}
