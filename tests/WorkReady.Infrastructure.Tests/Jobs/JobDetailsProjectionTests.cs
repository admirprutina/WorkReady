using WorkReady.Application.Jobs.ReadModels;
using WorkReady.Domain;
using WorkReady.Domain.Jobs;
using WorkReady.Domain.Jobs.Events;
using WorkReady.Infrastructure.Jobs;

namespace WorkReady.Infrastructure.Tests.Jobs;

/// <summary>
/// How the read model evolves from events, without a database and without the Job aggregate.
/// </summary>
public class JobDetailsProjectionTests
{
    private static readonly DateTimeOffset PlannedStart = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    private static readonly Guid JobId = Guid.NewGuid();
    private static readonly Guid TechnicianId = Guid.NewGuid();
    private static readonly Guid SiteId = Guid.NewGuid();

    private readonly JobDetailsProjection _projection = new();

    private JobDetailsReadModel Created() =>
        _projection.Create(new JobCreated(JobId, TechnicianId, SiteId, new WorkType("HighVoltage"), PlannedStart));

    [Fact]
    public void Job_created_creates_a_planned_read_model()
    {
        var job = Created();

        Assert.Equal(JobId, job.Id);
        Assert.Equal(TechnicianId, job.TechnicianId);
        Assert.Equal(SiteId, job.SiteId);
        Assert.Equal("HighVoltage", job.WorkType);
        Assert.Equal(PlannedStart, job.PlannedStart);
        Assert.Equal(JobState.Planned, job.State);
        Assert.Null(job.StartedAt);
    }

    [Fact]
    public void Technician_reassigned_changes_the_technician()
    {
        var job = Created();
        var otherTechnician = Guid.NewGuid();

        _projection.Apply(new TechnicianReassigned(JobId, otherTechnician), job);

        Assert.Equal(otherTechnician, job.TechnicianId);
    }

    [Fact]
    public void Work_type_changed_changes_the_work_type()
    {
        var job = Created();

        _projection.Apply(new WorkTypeChanged(JobId, new WorkType("Plumbing")), job);

        Assert.Equal("Plumbing", job.WorkType);
    }

    [Fact]
    public void Job_rescheduled_changes_the_planned_start()
    {
        var job = Created();

        _projection.Apply(new JobRescheduled(JobId, PlannedStart.AddDays(1)), job);

        Assert.Equal(PlannedStart.AddDays(1), job.PlannedStart);
    }

    [Fact]
    public void Job_started_sets_the_state_and_the_start_time()
    {
        var job = Created();
        var startedAt = PlannedStart.AddMinutes(3);

        _projection.Apply(new JobStarted(JobId, startedAt), job);

        Assert.Equal(JobState.Started, job.State);
        Assert.Equal(startedAt, job.StartedAt);
    }
}
