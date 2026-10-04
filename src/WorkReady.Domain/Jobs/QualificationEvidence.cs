namespace WorkReady.Domain.Jobs;

/// <summary>
/// Evidence that a Technician is qualified for a WorkType. Reusable until <see cref="ValidUntil"/>.
/// </summary>
public sealed record QualificationEvidence(
    Guid CheckExecutionId,
    Guid TechnicianId,
    WorkType WorkType,
    DateTimeOffset ValidUntil)
{
    public WorkType WorkType { get; } = WorkType ?? throw new ArgumentNullException(nameof(WorkType));

    public bool IsValidAt(DateTimeOffset at) => at < ValidUntil;
}
