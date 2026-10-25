using PidLab.Core.Linear;

namespace PidLab.Core.Simulation;

/// <summary>
/// One discrete plant stepped sample by sample: read <see cref="Output"/> (y_k), then <see cref="Advance"/> with u_k.
/// x_{k+1} = Ad·x_k + Bd·u_{k−d}, y_k = C·x_k. D must be 0: with D ≠ 0, y_k would depend on the current u_k,
/// which a feedback loop turns into an algebraic loop.
/// </summary>
public sealed class PlantStepper
{
    private readonly DiscreteStateSpace _sys;
    private readonly DelayLine _delay;
    private double[] _x;
    private double[] _next;

    public PlantStepper(DiscreteStateSpace sys, int delaySteps)
    {
        if (sys.D != 0) throw new ArgumentException("PlantStepper requires D = 0 (no algebraic loop).", nameof(sys));
        _sys = sys;
        _delay = new DelayLine(delaySteps);
        _x = new double[sys.Order];
        _next = new double[sys.Order];
    }

    public double Output
    {
        get
        {
            var y = 0.0;
            for (var i = 0; i < _x.Length; i++) y += _sys.C[i] * _x[i];
            return y;
        }
    }

    public void Advance(double u)
    {
        var ud = _delay.Push(u);
        _sys.Ad.Multiply(_x, _next);
        for (var i = 0; i < _next.Length; i++) _next[i] += _sys.Bd[i] * ud;
        (_x, _next) = (_next, _x);
    }
}
