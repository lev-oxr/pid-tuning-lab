using PidLab.Core.Linear;
using PidLab.Core.Plants;

namespace PidLab.Core.Simulation;

public static class OpenLoopSimulator
{
    /// <summary>
    /// Plant response to an input step at t = 0, initial state 0, exact ZOH discretization.
    /// Step order: y_k = Output → record → Advance(u_k) (u_k → delay line → u_{k−d} → x_{k+1}).
    /// </summary>
    public static OpenLoopResult Run(PlantModel plant, SimulationSettings s, double stepAmplitude = 1.0)
    {
        var n = s.StepCount;
        var t = new double[n];
        var u = new double[n];
        var y = new double[n];
        var stepper = new PlantStepper(Discretizer.Zoh(plant.ToStateSpace(), s.Dt), DelayLine.StepsFor(plant.Delay, s.Dt));

        for (var k = 0; k < n; k++)
        {
            t[k] = k * s.Dt;
            u[k] = stepAmplitude;
            y[k] = stepper.Output;
            stepper.Advance(u[k]);
        }

        return new OpenLoopResult(t, u, y);
    }
}
