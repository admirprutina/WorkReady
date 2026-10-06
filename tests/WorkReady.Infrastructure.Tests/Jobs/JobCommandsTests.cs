using Marten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkReady.Application;
using WorkReady.Application.Jobs.PlanJob;
using WorkReady.Application.Jobs.StartJob;
using WorkReady.Application.Messaging;
using WorkReady.Domain;
using WorkReady.Domain.Jobs;
using WorkReady.Domain.Jobs.Events;

namespace WorkReady.Infrastructure.Tests.Jobs;

/// <summary>The commands end to end, through the mediator, the handlers and the Marten event store.</summary>
public sealed class JobCommandsTests : IAsyncLifetime
{
    private ServiceProvider _services = null!;

    public Task InitializeAsync()
    {
        if (PostgresFactAttribute.ConnectionString is { } connectionString)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = connectionString
                })
                .Build();

            _services = new ServiceCollection()
                .AddApplication()
                .AddInfrastructure(configuration)
                .BuildServiceProvider();
        }

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }
    }

    private async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request, CancellationToken.None);
    }

    [PostgresFact]
    public async Task Planned_job_is_persisted_as_job_created_and_can_be_started_once_ready()
    {
        var technicianId = Guid.NewGuid();
        var siteId = Guid.NewGuid();
        var plannedStart = DateTimeOffset.UtcNow.AddHours(1);

        var planned = await Send(new PlanJobCommandRequest(technicianId, siteId, "HighVoltage", plannedStart));

        // Without evidence the domain rejects the start, and nothing is appended.
        await Assert.ThrowsAsync<JobNotReadyToStartException>(() => Send(new StartJobCommandRequest(planned.JobId)));

        // Evidence commands are not part of this slice yet, so record the evidence directly on the stream.
        await using (var scope = _services.CreateAsyncScope())
        {
            var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
            var workType = new WorkType("HighVoltage");
            var until = DateTimeOffset.UtcNow.AddDays(1);

            session.Events.Append(
                planned.JobId,
                new QualificationEvidenceRecorded(
                    planned.JobId, new QualificationEvidence(Guid.NewGuid(), technicianId, workType, until)),
                new SafetyTrainingEvidenceRecorded(
                    planned.JobId, new SafetyTrainingEvidence(Guid.NewGuid(), technicianId, siteId, until)),
                new SiteAccessEvidenceRecorded(
                    planned.JobId, new SiteAccessEvidence(Guid.NewGuid(), technicianId, siteId, until)));
            await session.SaveChangesAsync();
        }

        var started = await Send(new StartJobCommandRequest(planned.JobId));

        await using (var scope = _services.CreateAsyncScope())
        {
            var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

            var events = await session.Events.FetchStreamAsync(planned.JobId);
            Assert.Equal(
                new JobCreated(planned.JobId, technicianId, siteId, new WorkType("HighVoltage"), plannedStart),
                events[0].Data);
            Assert.Equal(new JobStarted(planned.JobId, started.StartedAt), events[^1].Data);

            var job = await session.Events.AggregateStreamAsync<Job>(planned.JobId);
            Assert.Equal(JobState.Started, job!.State);
        }
    }
}
