namespace WorkReady.Domain.CheckExecutions;

public sealed class InvalidCheckExecutionTransitionException(
    Guid checkExecutionId,
    CheckExecutionState currentState,
    CheckExecutionState targetState)
    : InvalidOperationException(
        $"Check execution '{checkExecutionId}' cannot move from {currentState} to {targetState}.");
