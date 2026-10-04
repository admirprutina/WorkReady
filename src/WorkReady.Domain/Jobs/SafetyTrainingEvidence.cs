namespace WorkReady.Domain.Jobs;

/// <summary>
/// Evidence that a Technician completed the safety training for a Site. Reusable until <see cref="ValidUntil"/>.
/// </summary>
public sealed record SafetyTrainingEvidence(
    Guid CheckExecutionId,
    Guid TechnicianId,
    Guid SiteId,
    DateTimeOffset ValidUntil)
{
    public bool IsValidAt(DateTimeOffset at) => at < ValidUntil;
}
