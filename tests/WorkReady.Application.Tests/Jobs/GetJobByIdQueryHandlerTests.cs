using WorkReady.Application.Jobs;
using WorkReady.Application.Jobs.GetJobById;
using WorkReady.Application.Jobs.ReadModels;
using WorkReady.Domain.Jobs;

namespace WorkReady.Application.Tests.Jobs;

public class GetJobByIdQueryHandlerTests
{
    private static readonly DateTimeOffset PlannedStart = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    private readonly FakeJobReadStore _readStore = new();

    private GetJobByIdQueryHandler Handler() => new(_readStore);

    [Fact]
    public async Task An_existing_job_is_returned_from_its_read_model()
    {
        var readModel = new JobDetailsReadModel
        {
            Id = Guid.NewGuid(),
            TechnicianId = Guid.NewGuid(),
            SiteId = Guid.NewGuid(),
            WorkType = "HighVoltage",
            PlannedStart = PlannedStart,
            State = JobState.Planned,
            StartedAt = null
        };
        _readStore.Add(readModel);

        var result = await Handler().Handle(new GetJobByIdQueryRequest(readModel.Id), CancellationToken.None);

        Assert.Equal(
            new GetJobByIdQueryResult(
                readModel.Id, readModel.TechnicianId, readModel.SiteId, "HighVoltage", PlannedStart, JobState.Planned, null),
            result);
    }

    [Fact]
    public async Task A_started_job_returns_its_state_and_start_time()
    {
        var startedAt = PlannedStart.AddMinutes(3);
        var readModel = new JobDetailsReadModel
        {
            Id = Guid.NewGuid(),
            TechnicianId = Guid.NewGuid(),
            SiteId = Guid.NewGuid(),
            WorkType = "HighVoltage",
            PlannedStart = PlannedStart,
            State = JobState.Started,
            StartedAt = startedAt
        };
        _readStore.Add(readModel);

        var result = await Handler().Handle(new GetJobByIdQueryRequest(readModel.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(JobState.Started, result.State);
        Assert.Equal(startedAt, result.StartedAt);
    }

    [Fact]
    public async Task A_missing_job_returns_null()
    {
        var result = await Handler().Handle(new GetJobByIdQueryRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    private sealed class FakeJobReadStore : IJobReadStore
    {
        private readonly Dictionary<Guid, JobDetailsReadModel> _jobs = [];

        public void Add(JobDetailsReadModel job) => _jobs[job.Id] = job;

        public Task<JobDetailsReadModel?> GetById(Guid jobId, CancellationToken cancellationToken) =>
            Task.FromResult(_jobs.GetValueOrDefault(jobId));
    }
}
