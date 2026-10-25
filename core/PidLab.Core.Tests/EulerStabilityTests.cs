using PidLab.Core.Plants;
using PidLab.Core.Simulation;
using PidLab.Core.Tests.Support;

namespace PidLab.Core.Tests;

// Stability of explicit Euler for T·y′ + y = K·u (L = 0, u = 1, x_0 = 0).
//   x_{k+1} = x_k + Δt·(K − x_k)/T
//   x_{k+1} − K = (x_k − K) − (Δt/T)(x_k − K) = (1 − Δt/T)(x_k − K)
//   ⇒ x_k − K = (1 − Δt/T)^k · (x_0 − K).
// Divergence ⇔ |1 − Δt/T| > 1 ⇔ Δt > 2T.
// Δt < T: factor in (0, 1) — monotone approach; T < Δt < 2T: factor in (−1, 0) — decaying oscillations.
public class EulerStabilityTests
{
    private const double K = 1, T = 0.5;
    private const int Steps = 60;

    // Deviation from the steady state, k = 0 … Steps−1.
    private static double[] Deviation(double dt)
    {
        var r = EulerReference.Run(new Fopdt(K, T, 0), new SimulationSettings(dt, Steps * dt));
        Assert.Equal(Steps, r.Y.Length);
        return r.Y.Select(y => y - K).ToArray();
    }

    [Fact]
    public void Small_step_approaches_K_monotonically()
    {
        var e = Deviation(0.2); // Δt/T = 0.4, factor 0.6
        for (var k = 0; k < e.Length; k++) Assert.True(e[k] <= 0, $"overshoot at k={k}");
        for (var k = 1; k < e.Length; k++) Assert.True(Math.Abs(e[k]) < Math.Abs(e[k - 1]));
    }

    [Fact]
    public void Medium_step_oscillates_but_decays()
    {
        var e = Deviation(0.9); // Δt/T = 1.8, factor −0.8
        for (var k = 1; k < e.Length; k++)
        {
            Assert.True(e[k] * e[k - 1] < 0, $"sign does not alternate at k={k}");
            Assert.True(Math.Abs(e[k]) < Math.Abs(e[k - 1]), $"amplitude does not decay at k={k}");
        }
    }

    [Fact]
    public void Large_step_diverges()
    {
        var e = Deviation(1.2); // Δt/T = 2.4, factor −1.4
        for (var k = 1; k < e.Length; k++) Assert.True(Math.Abs(e[k]) > Math.Abs(e[k - 1]));
        Assert.True(Math.Abs(e[^1]) > 1e3 * Math.Abs(e[0]));
    }
}
