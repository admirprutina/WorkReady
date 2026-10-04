namespace WorkReady.Domain.CheckExecutions;

/// <summary>
/// One concrete execution of a check. Aggregate root.
/// The Job context is a historical snapshot of what was checked; running the check again means a new execution.
/// </summary>
public sealed class CheckExecution
{
    public CheckExecution(Guid jobId, CheckType checkType, Guid technicianId, Guid siteId, WorkType workType)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("Job is required.", nameof(jobId));
        }

        if (!Enum.IsDefined(checkType))
        {
            throw new ArgumentOutOfRangeException(nameof(checkType), checkType, "Unknown check type.");
        }

        if (technicianId == Guid.Empty)
        {
            throw new ArgumentException("Technician is required.", nameof(technicianId));
        }

        if (siteId == Guid.Empty)
        {
            throw new ArgumentException("Site is required.", nameof(siteId));
        }

        Id = Guid.NewGuid();
        JobId = jobId;
        CheckType = checkType;
        TechnicianId = technicianId;
        SiteId = siteId;
        WorkType = workType ?? throw new ArgumentNullException(nameof(workType));
        State = CheckExecutionState.Requested;
    }

    public Guid Id { get; }

    public Guid JobId { get; }

    public CheckType CheckType { get; }

    public Guid TechnicianId { get; }

    public Guid SiteId { get; }

    public WorkType WorkType { get; }

    public CheckExecutionState State { get; private set; }

    public string? ManualReviewReason { get; private set; }

    public Guid? ResolvedBySupervisorId { get; private set; }

    public string? DecisionReason { get; private set; }

    public bool IsTerminal => State is
        CheckExecutionState.Passed or
        CheckExecutionState.Failed or
        CheckExecutionState.Approved or
        CheckExecutionState.Rejected;

    public void Start() => MoveTo(CheckExecutionState.Running, from: CheckExecutionState.Requested);

    public void Pass() => MoveTo(CheckExecutionState.Passed, from: CheckExecutionState.Running);

    public void Fail() => MoveTo(CheckExecutionState.Failed, from: CheckExecutionState.Running);

    public void SendForManualReview(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A manual review reason is required.", nameof(reason));
        }

        MoveTo(CheckExecutionState.NeedsManualReview, from: CheckExecutionState.Running);
        ManualReviewReason = reason;
    }

    public void Approve(Guid supervisorId, string decisionReason) =>
        Resolve(CheckExecutionState.Approved, supervisorId, decisionReason);

    public void Reject(Guid supervisorId, string decisionReason) =>
        Resolve(CheckExecutionState.Rejected, supervisorId, decisionReason);

    private void Resolve(CheckExecutionState outcome, Guid supervisorId, string decisionReason)
    {
        if (supervisorId == Guid.Empty)
        {
            throw new ArgumentException("Supervisor is required.", nameof(supervisorId));
        }

        if (string.IsNullOrWhiteSpace(decisionReason))
        {
            throw new ArgumentException("A decision reason is required.", nameof(decisionReason));
        }

        MoveTo(outcome, from: CheckExecutionState.NeedsManualReview);
        ResolvedBySupervisorId = supervisorId;
        DecisionReason = decisionReason;
    }

    private void MoveTo(CheckExecutionState target, CheckExecutionState from)
    {
        if (State != from)
        {
            throw new InvalidCheckExecutionTransitionException(Id, State, target);
        }

        State = target;
    }
}
