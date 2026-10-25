using PidLab.Core.Plants;
using PidLab.Core.Simulation;

namespace PidLab.Core.Tests.Support;

/// <summary>
/// Explicit Euler for FOPDT: the phase-1 scheme, kept as a test-only reference after phase 2 replaces it with ZOH.
/// Step order: u_k → delay line → u_{k−d} → y_k = x_k → x_{k+1}.
/// </summary>
public static class EulerReference
{
    public static OpenLoopResult Run(Fopdt plant, SimulationSettings s, double stepAmplitude = 1.0)
    {
        var n = s.StepCount;
        var t = new double[n];
        var u = new double[n];
        var y = new double[n];
        var delay = new DelayLine(DelayLine.StepsFor(plant.L, s.Dt));
        var x = 0.0;

        for (var k = 0; k < n; k++)
        {
            t[k] = k * s.Dt;
            u[k] = stepAmplitude;
            var delayed = delay.Push(u[k]);
            y[k] = x;
            x += s.Dt * (plant.K * delayed - x) / plant.T;
        }

        return new OpenLoopResult(t, u, y);
    }
}
