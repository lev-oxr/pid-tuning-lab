using PidLab.Core.Linear;
using PidLab.Core.Plants;
using PidLab.Core.Simulation;
using PidLab.Core.Tests.Support;
using Xunit.Abstractions;

namespace PidLab.Core.Tests;

public class IntegratorComparisonTests(ITestOutputHelper output)
{
    private static readonly Fopdt Plant = new(1, 1, 0);
    private static readonly StateSpace Ss = Plant.ToStateSpace();

    private static double Ref(double t) => 1 - Math.Exp(-t);

    private static double MaxErr(double[] y, double dt) => y.Select((v, k) => Math.Abs(v - Ref(k * dt))).Max();

    private static double Ratio(Func<StateSpace, double, int, double, double[]> scheme)
    {
        // Same horizon (10 s), halved step.
        var coarse = MaxErr(scheme(Ss, 0.2, 50, 1), 0.2);
        var fine = MaxErr(scheme(Ss, 0.1, 100, 1), 0.1);
        return coarse / fine;
    }

    [Fact]
    public void Euler_is_first_order()
    {
        var r = Ratio(Integrators.Euler);
        output.WriteLine($"Euler err(0.2)/err(0.1) = {r:F4}");
        Assert.InRange(r, 1.8, 2.2);
    }

    [Fact]
    public void Rk4_is_fourth_order()
    {
        var r = Ratio(Integrators.Rk4);
        output.WriteLine($"RK4 err(0.2)/err(0.1) = {r:F4}");
        Assert.InRange(r, 13, 19);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.1)]
    [InlineData(0.2)]
    [InlineData(1.0)]
    [InlineData(5.0)]
    public void Zoh_is_exact_for_any_dt(double dt)
    {
        var n = (int)Math.Round(10 / dt);
        var y = OpenLoopSimulator.Run(Plant, new SimulationSettings(dt, n * dt)).Y;
        var err = MaxErr(y, dt);
        output.WriteLine($"ZOH dt={dt}: max err {err:E3}");
        Assert.True(err < 1e-10);
    }

    // Euler: x_{k+1} − K = (1 − Δt/T)(x_k − K) diverges for Δt > 2T.
    // ZOH: x_{k+1} − K = e^{−Δt/T}(x_k − K), and 0 < e^{−Δt/T} < 1 for every Δt > 0: the discrete pole
    // e^{λΔt} of a stable λ always lies inside the unit circle. The only requirement is that u is constant between samples.
    [Fact]
    public void Zoh_stays_stable_where_Euler_diverges()
    {
        var p = new Fopdt(1, 0.5, 0);
        const double dt = 1.2; // > 2T
        const int n = 40;
        var euler = Integrators.Euler(p.ToStateSpace(), dt, n, 1);
        Assert.True(Math.Abs(euler[^1] - 1) > 1e3);

        var zoh = OpenLoopSimulator.Run(p, new SimulationSettings(dt, n * dt)).Y;
        for (var k = 0; k < n; k++)
        {
            Assert.Equal(1 - Math.Exp(-k * dt / p.T), zoh[k], 1e-10);
            if (k > 0) Assert.True(zoh[k] >= zoh[k - 1]); // >=: saturates at K within rounding
        }
    }
}
