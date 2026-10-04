namespace WorkReady.Domain.Tests;

public class WorkTypeTests
{
    [Fact]
    public void Work_types_with_the_same_value_are_equal()
    {
        Assert.Equal(new WorkType("HighVoltage"), new WorkType("HighVoltage"));
        Assert.NotEqual(new WorkType("HighVoltage"), new WorkType("Plumbing"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_work_type_must_have_a_value(string? value)
    {
        Assert.ThrowsAny<ArgumentException>(() => new WorkType(value!));
    }
}
