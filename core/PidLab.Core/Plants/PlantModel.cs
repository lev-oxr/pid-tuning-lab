using PidLab.Core.Linear;
using PidLab.Core.Numerics;

namespace PidLab.Core.Plants;

public abstract record PlantModel
{
    /// <summary>Delay-free dynamics; the delay is applied separately by a DelayLine on the input.</summary>
    public abstract StateSpace ToStateSpace();

    /// <summary>Transport delay in seconds, 0 if none.</summary>
    public abstract double Delay { get; }

    /// <summary>(b₁s + b₀)/(s² + a₁s + a₀) in controllable canonical form.</summary>
    protected static StateSpace SecondOrder(double b1, double b0, double a1, double a0) =>
        new(new Matrix(new[,] { { 0, 1 }, { -a0, -a1 } }), [0, 1], [b0, b1], 0);

    /// <summary>K / ((T1·s + 1)(T2·s + 1)) with numerator K·(1 − β·s); β = 0 gives the plain lag chain.</summary>
    protected static StateSpace TwoLags(double k, double t1, double t2, double beta = 0)
    {
        var p = t1 * t2;
        return SecondOrder(-k * beta / p, k / p, (t1 + t2) / p, 1 / p);
    }

    protected static double Finite(double v, string name) =>
        double.IsFinite(v) ? v : throw new ArgumentOutOfRangeException(name, v, $"{name} must be finite.");

    protected static double Positive(double v, string name) =>
        double.IsFinite(v) && v > 0 ? v : throw new ArgumentOutOfRangeException(name, v, $"{name} must be finite and > 0.");

    protected static double NonNegative(double v, string name) =>
        double.IsFinite(v) && v >= 0 ? v : throw new ArgumentOutOfRangeException(name, v, $"{name} must be finite and >= 0.");
}
