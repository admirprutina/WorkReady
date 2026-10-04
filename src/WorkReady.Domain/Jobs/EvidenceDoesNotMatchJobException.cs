namespace WorkReady.Domain.Jobs;

public sealed class EvidenceDoesNotMatchJobException(Guid jobId, string evidence, string mismatch)
    : InvalidOperationException($"{evidence} evidence does not match job '{jobId}': {mismatch}.");
