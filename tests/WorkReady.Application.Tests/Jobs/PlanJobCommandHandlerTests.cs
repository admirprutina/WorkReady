using WorkReady.Application.Jobs.PlanJob;
using WorkReady.Domain;
using WorkReady.Domain.Jobs.Events;

namespace WorkReady.Application.Tests.Jobs;

public class PlanJobCommandHandlerTests
{
    private static readonly DateTimeOffset PlannedStart = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    private readonly FakeJobEventStore _eventStore = new();

    private PlanJobCommandHandler Handler() => new(_eventStore);

    [Fact]
    public async Task Planning_starts_a_job_stream_with_job_created_and_saves()
    {
        var technicianId = Guid.NewGuid();
        var siteId = Guid.NewGuid();

        var result = await Handler().Handle(
            new PlanJobCommandRequest(technicianId, siteId, " HighVoltage ", PlannedStart), CancellationToken.None);

        var created = Assert.Single(_eventStore.StartedStreams);
        Assert.Equal(
            new JobCreated(result.JobId, technicianId, siteId, new WorkType("HighVoltage"), PlannedStart),
            created);
        Assert.NotEqual(Guid.Empty, result.JobId);
        Assert.Equal(1, _eventStore.SaveCount);
    }

    [Fact]
    public async Task Each_planned_job_gets_its_own_id()
    {
        var request = new PlanJobCommandRequest(Guid.NewGuid(), Guid.NewGuid(), "HighVoltage", PlannedStart);

        var first = await Handler().Handle(request, CancellationToken.None);
        var second = await Handler().Handle(request, CancellationToken.None);

        Assert.NotEqual(first.JobId, second.JobId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Planning_with_an_empty_work_type_is_rejected_and_nothing_is_persisted(string workType)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Handler().Handle(
            new PlanJobCommandRequest(Guid.NewGuid(), Guid.NewGuid(), workType, PlannedStart),
            CancellationToken.None));

        Assert.Empty(_eventStore.StartedStreams);
        Assert.Equal(0, _eventStore.SaveCount);
    }

    [Fact]
    public async Task Planning_without_a_technician_is_rejected_by_the_domain_and_nothing_is_persisted()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => Handler().Handle(
            new PlanJobCommandRequest(Guid.Empty, Guid.NewGuid(), "HighVoltage", PlannedStart),
            CancellationToken.None));

        Assert.Empty(_eventStore.StartedStreams);
        Assert.Equal(0, _eventStore.SaveCount);
    }
}
