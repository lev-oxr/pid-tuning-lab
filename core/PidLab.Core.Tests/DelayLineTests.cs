using PidLab.Core.Simulation;

namespace PidLab.Core.Tests;

public class DelayLineTests
{
    private static double[] PushAll(DelayLine d, params double[] xs) => xs.Select(d.Push).ToArray();

    [Fact]
    public void Zero_steps_returns_input() =>
        Assert.Equal([5.0, -1.0, 7.0], PushAll(new DelayLine(0), 5, -1, 7));

    [Fact]
    public void One_step() =>
        Assert.Equal([0.0, 1.0, 2.0, 3.0], PushAll(new DelayLine(1), 1, 2, 3, 4));

    [Fact]
    public void Three_steps_wraps_buffer() =>
        Assert.Equal([0.0, 0.0, 0.0, 1.0, 2.0, 3.0, 4.0, 5.0], PushAll(new DelayLine(3), 1, 2, 3, 4, 5, 6, 7, 8));

    [Fact]
    public void Initial_value_fills_delay() =>
        Assert.Equal([9.0, 9.0, 1.0, 2.0], PushAll(new DelayLine(2, 9), 1, 2, 3, 4));

    [Fact]
    public void Negative_steps_throw() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new DelayLine(-1));

    [Theory]
    [InlineData(0, 0.01, 0)]
    [InlineData(0.005, 0.01, 0)] // 0.5·Δt → 0 (banker's rounding)
    [InlineData(0.0051, 0.01, 1)]
    [InlineData(1, 0.01, 100)]
    [InlineData(0.015, 0.01, 2)] // 1.5 → 2
    public void StepsFor(double l, double dt, int expected) =>
        Assert.Equal(expected, DelayLine.StepsFor(l, dt));
}
