using WorkReady.Domain.CheckExecutions;

namespace WorkReady.Domain.Tests.CheckExecutions;

public class CheckExecutionTests
{
    private static readonly Guid JobId = Guid.NewGuid();
    private static readonly Guid TechnicianId = Guid.NewGuid();
    private static readonly Guid SiteId = Guid.NewGuid();
    private static readonly Guid SupervisorId = Guid.NewGuid();
    private static readonly WorkType HighVoltage = new("HighVoltage");

    private static CheckExecution RequestedCheck(CheckType checkType = CheckType.SiteAccess) =>
        new(JobId, checkType, TechnicianId, SiteId, HighVoltage);

    private static CheckExecution RunningCheck()
    {
        var check = RequestedCheck();
        check.Start();
        return check;
    }

    private static CheckExecution CheckAwaitingManualReview()
    {
        var check = RunningCheck();
        check.SendForManualReview("Provider returned an inconclusive result");
        return check;
    }

    [Fact]
    public void A_new_check_execution_is_requested_and_captures_the_job_context()
    {
        var check = RequestedCheck(CheckType.Qualification);

        Assert.Equal(CheckExecutionState.Requested, check.State);
        Assert.Equal(JobId, check.JobId);
        Assert.Equal(CheckType.Qualification, check.CheckType);
        Assert.Equal(TechnicianId, check.TechnicianId);
        Assert.Equal(SiteId, check.SiteId);
        Assert.Equal(HighVoltage, check.WorkType);
        Assert.False(check.IsTerminal);
    }

    [Fact]
    public void A_requested_check_can_start_running()
    {
        var check = RequestedCheck();

        check.Start();

        Assert.Equal(CheckExecutionState.Running, check.State);
    }

    [Fact]
    public void A_running_check_can_pass()
    {
        var check = RunningCheck();

        check.Pass();

        Assert.Equal(CheckExecutionState.Passed, check.State);
        Assert.True(check.IsTerminal);
    }

    [Fact]
    public void A_running_check_can_fail()
    {
        var check = RunningCheck();

        check.Fail();

        Assert.Equal(CheckExecutionState.Failed, check.State);
        Assert.True(check.IsTerminal);
    }

    [Fact]
    public void A_running_check_can_be_sent_for_manual_review_with_a_reason()
    {
        var check = RunningCheck();

        check.SendForManualReview("Badge record is ambiguous");

        Assert.Equal(CheckExecutionState.NeedsManualReview, check.State);
        Assert.Equal("Badge record is ambiguous", check.ManualReviewReason);
        Assert.False(check.IsTerminal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Manual_review_requires_a_reason(string reason)
    {
        var check = RunningCheck();

        Assert.Throws<ArgumentException>(() => check.SendForManualReview(reason));
        Assert.Equal(CheckExecutionState.Running, check.State);
    }

    [Fact]
    public void A_supervisor_can_approve_a_check_in_manual_review()
    {
        var check = CheckAwaitingManualReview();

        check.Approve(SupervisorId, "Verified access with site manager");

        Assert.Equal(CheckExecutionState.Approved, check.State);
        Assert.Equal(SupervisorId, check.ResolvedBySupervisorId);
        Assert.Equal("Verified access with site manager", check.DecisionReason);
        Assert.True(check.IsTerminal);
    }

    [Fact]
    public void A_supervisor_can_reject_a_check_in_manual_review()
    {
        var check = CheckAwaitingManualReview();

        check.Reject(SupervisorId, "Access badge was revoked");

        Assert.Equal(CheckExecutionState.Rejected, check.State);
        Assert.Equal(SupervisorId, check.ResolvedBySupervisorId);
        Assert.Equal("Access badge was revoked", check.DecisionReason);
        Assert.True(check.IsTerminal);
    }

    [Fact]
    public void Approval_and_rejection_require_a_supervisor_and_a_reason()
    {
        var check = CheckAwaitingManualReview();

        Assert.Throws<ArgumentException>(() => check.Approve(Guid.Empty, "reason"));
        Assert.Throws<ArgumentException>(() => check.Approve(SupervisorId, " "));
        Assert.Throws<ArgumentException>(() => check.Reject(Guid.Empty, "reason"));
        Assert.Throws<ArgumentException>(() => check.Reject(SupervisorId, ""));
        Assert.Equal(CheckExecutionState.NeedsManualReview, check.State);
    }

    [Fact]
    public void Approve_and_reject_are_only_allowed_during_manual_review()
    {
        var requested = RequestedCheck();
        var running = RunningCheck();

        Assert.Throws<InvalidCheckExecutionTransitionException>(() => requested.Approve(SupervisorId, "reason"));
        Assert.Throws<InvalidCheckExecutionTransitionException>(() => requested.Reject(SupervisorId, "reason"));
        Assert.Throws<InvalidCheckExecutionTransitionException>(() => running.Approve(SupervisorId, "reason"));
        Assert.Throws<InvalidCheckExecutionTransitionException>(() => running.Reject(SupervisorId, "reason"));
    }

    [Fact]
    public void Only_a_running_check_can_pass_fail_or_go_to_manual_review()
    {
        var requested = RequestedCheck();

        Assert.Throws<InvalidCheckExecutionTransitionException>(() => requested.Pass());
        Assert.Throws<InvalidCheckExecutionTransitionException>(() => requested.Fail());
        Assert.Throws<InvalidCheckExecutionTransitionException>(() => requested.SendForManualReview("reason"));
        Assert.Equal(CheckExecutionState.Requested, requested.State);
    }

    public static TheoryData<Action<CheckExecution>> TerminalTransitions => new()
    {
        check => check.Pass(),
        check => check.Fail(),
        check =>
        {
            check.SendForManualReview("reason");
            check.Approve(SupervisorId, "decision");
        },
        check =>
        {
            check.SendForManualReview("reason");
            check.Reject(SupervisorId, "decision");
        }
    };

    [Theory]
    [MemberData(nameof(TerminalTransitions))]
    public void A_terminal_check_cannot_transition_again(Action<CheckExecution> reachTerminalState)
    {
        var check = RunningCheck();
        reachTerminalState(check);
        var terminalState = check.State;

        Assert.True(check.IsTerminal);
        Assert.Throws<InvalidCheckExecutionTransitionException>(() => check.Start());
        Assert.Throws<InvalidCheckExecutionTransitionException>(() => check.Pass());
        Assert.Throws<InvalidCheckExecutionTransitionException>(() => check.Fail());
        Assert.Throws<InvalidCheckExecutionTransitionException>(() => check.SendForManualReview("again"));
        Assert.Throws<InvalidCheckExecutionTransitionException>(() => check.Approve(SupervisorId, "again"));
        Assert.Throws<InvalidCheckExecutionTransitionException>(() => check.Reject(SupervisorId, "again"));
        Assert.Equal(terminalState, check.State);
    }

    [Fact]
    public void Running_the_same_check_again_is_a_separate_execution()
    {
        var first = RequestedCheck(CheckType.SiteAccess);
        var second = RequestedCheck(CheckType.SiteAccess);

        Assert.NotSame(first, second);
        Assert.NotEqual(first.Id, second.Id);

        first.Start();
        first.Fail();

        Assert.Equal(CheckExecutionState.Requested, second.State);
    }
}
