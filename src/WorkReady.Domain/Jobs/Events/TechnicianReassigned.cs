namespace WorkReady.Domain.Jobs.Events;

/// <summary>The Job was handed to another Technician. All previously recorded evidence no longer applies.</summary>
public sealed record TechnicianReassigned(Guid JobId, Guid TechnicianId);
