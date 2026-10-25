using PidLab.Core.Plants;
using PidLab.Core.Simulation;

namespace PidLab.Core.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData(2, 5, 1)]
    [InlineData(0, 1, 0)] // K = 0 and L = 0 are allowed
    [InlineData(-3, 0.1, 10)]
    public void Fopdt_valid(double k, double t, double l)
    {
        var p = new Fopdt(k, t, l);
        Assert.Equal((k, t, l), (p.K, p.T, p.L));
    }

    [Theory]
    [InlineData(double.NaN, 1, 0)]
    [InlineData(double.PositiveInfinity, 1, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, double.NaN, 0)]
    [InlineData(1, 1, -0.1)]
    [InlineData(1, 1, double.NaN)]
    public void Fopdt_invalid(double k, double t, double l) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Fopdt(k, t, l));

    [Fact]
    public void Settings_StepCount() =>
        Assert.Equal(3000, new SimulationSettings(0.01, 30).StepCount);

    [Fact]
    public void Settings_Horizon_equal_to_Dt_is_valid() =>
        Assert.Equal(1, new SimulationSettings(0.5, 0.5).StepCount);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-0.1, 1)]
    [InlineData(0.1, 0)]
    [InlineData(0.1, -1)]
    [InlineData(1, 0.5)] // Horizon < Dt
    [InlineData(double.NaN, 1)]
    [InlineData(0.1, double.NaN)]
    public void Settings_invalid(double dt, double horizon) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationSettings(dt, horizon));
}
