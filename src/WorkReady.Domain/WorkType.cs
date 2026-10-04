namespace WorkReady.Domain;

/// <summary>
/// The kind of work a Job requires (e.g. "HighVoltage"). Value object: compared by value.
/// </summary>
public sealed record WorkType
{
    public WorkType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Work type must not be empty.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
