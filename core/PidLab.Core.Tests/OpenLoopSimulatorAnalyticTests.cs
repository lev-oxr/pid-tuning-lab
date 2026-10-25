using PidLab.Core.Plants;
using PidLab.Core.Simulation;
using PidLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace PidLab.Core.Tests;

/// <summary>
/// Reference: y(t) = K·u·(1 − e^(−(t−L)/T)) for t ≥ L, and 0 for t &lt; L.
/// This is the main criterion of phase 1: the scheme must match the analytic solution.
/// </summary>
public class OpenLoopSimulatorAnalyticTests(ITestOutputHelper output)
{
    private static double Ref(Fopdt p, double t, double u = 1) =>
        t < p.L ? 0 : p.K * u * (1 - Math.Exp(-(t - p.L) / p.T));

    private static double MaxError(Fopdt p, double dt, double horizon, bool euler = false)
    {
        var s = new SimulationSettings(dt, horizon);
        var r = euler ? EulerReference.Run(p, s) : OpenLoopSimulator.Run(p, s);
        return r.T.Select((t, k) => Math.Abs(r.Y[k] - Ref(p, t))).Max();
    }

    // Phase 2: ZOH is exact; measured max error 1.1e-13, tolerance 1e-12 (≈3× margin rounded up to a power of ten).
    // Phase 1 used 1e-3 for explicit Euler.
    [Fact]
    public void Matches_analytic_response()
    {
        var err = MaxError(new Fopdt(2, 5, 1), 0.01, 30);
        output.WriteLine($"T4.1 max|y - y_ref| = {err:E3}");
        Assert.True(err < 1e-12, $"max error {err}");
    }

    [Fact]
    public void Error_scales_linearly_with_dt()
    {
        var p = new Fopdt(1, 1, 0);
        var ratio = MaxError(p, 0.02, 10, euler: true) / MaxError(p, 0.01, 10, euler: true);
        output.WriteLine($"T4.2 err(0.02)/err(0.01) = {ratio:F4}");
        Assert.InRange(ratio, 1.8, 2.2);
    }

    [Fact]
    public void Reaches_63_percent_at_L_plus_T()
    {
        var p = new Fopdt(2, 5, 1);
        var r = OpenLoopSimulator.Run(p, new SimulationSettings(0.01, 30));
        var k = (int)Math.Round((p.L + p.T) / 0.01);
        Assert.Equal(0.632 * p.K, r.Y[k], 5e-3);
    }

    [Fact]
    public void Output_is_exactly_zero_until_delay_elapses()
    {
        var p = new Fopdt(2, 5, 1);
        var r = OpenLoopSimulator.Run(p, new SimulationSettings(0.01, 30));
        var d = DelayLine.StepsFor(p.L, 0.01);
        Assert.Equal(100, d);
        for (var k = 0; k <= d; k++) Assert.Equal(0, r.Y[k]);
        Assert.NotEqual(0, r.Y[d + 1]);
    }

    [Fact]
    public void Settles_to_K_times_u()
    {
        var p = new Fopdt(2, 5, 1);
        var r = OpenLoopSimulator.Run(p, new SimulationSettings(0.01, 10 * p.T), 1.5);
        Assert.Equal(p.K * 1.5, r.Y[^1], 1e-3 * p.K);
    }

    [Fact]
    public void Response_is_linear_in_amplitude()
    {
        var p = new Fopdt(2, 5, 1);
        var s = new SimulationSettings(0.01, 30);
        var y1 = OpenLoopSimulator.Run(p, s, 1).Y;
        var y2 = OpenLoopSimulator.Run(p, s, 2).Y;
        for (var k = 0; k < y1.Length; k++) Assert.Equal(2 * y1[k], y2[k], 1e-12);
    }

    [Fact]
    public void Is_deterministic()
    {
        var p = new Fopdt(2, 5, 1);
        var s = new SimulationSettings(0.01, 30);
        var a = OpenLoopSimulator.Run(p, s);
        var b = OpenLoopSimulator.Run(p, s);
        Assert.True(a.T.SequenceEqual(b.T));
        Assert.True(a.U.SequenceEqual(b.U));
        Assert.True(a.Y.SequenceEqual(b.Y));
    }
}
