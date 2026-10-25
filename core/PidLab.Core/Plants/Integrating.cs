using PidLab.Core.Linear;
using PidLab.Core.Numerics;

namespace PidLab.Core.Plants;

/// <summary>W(s) = K·e^(−Ls) / s.</summary>
public sealed record Integrating(double K, double L) : PlantModel
{
    public double K { get; init; } = Finite(K, nameof(K));
    public double L { get; init; } = NonNegative(L, nameof(L));

    public override double Delay => L;
    public override StateSpace ToStateSpace() => new(new Matrix(1, 1), [1], [K], 0);
}
