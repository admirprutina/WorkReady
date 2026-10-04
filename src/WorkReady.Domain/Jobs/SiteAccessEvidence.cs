namespace WorkReady.Domain.Jobs;

/// <summary>
/// Point-in-time evidence that a Technician may access a Site. Only reusable while fresh, until <see cref="FreshUntil"/>.
/// </summary>
public sealed record SiteAccessEvidence(
    Guid CheckExecutionId,
    Guid TechnicianId,
    Guid SiteId,
    DateTimeOffset FreshUntil)
{
    public bool IsFreshAt(DateTimeOffset at) => at < FreshUntil;
}
