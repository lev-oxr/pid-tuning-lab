using PidLab.Core.Plants;

namespace PidLab.Core.Simulation;

public static class OpenLoopSimulator
{
    /// <summary>
    /// FOPDT response to an input step at t = 0, initial state 0.
    /// Explicit Euler (a temporary phase-1 scheme; ZOH replaces it in phase 2).
    /// Step order: u_k → delay line → u_{k−d} → y_k = x_k → x_{k+1}.
    /// </summary>
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
