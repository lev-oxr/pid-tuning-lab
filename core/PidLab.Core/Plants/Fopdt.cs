using PidLab.Core.Linear;
using PidLab.Core.Numerics;

namespace PidLab.Core.Plants;

/// <summary>W(s) = K·e^(−Ls) / (Ts + 1).</summary>
public sealed record Fopdt(double K, double T, double L) : PlantModel
{
    // Validation lives in the initializers: primary-constructor parameters are visible only here.
    // `with` bypasses it; acceptable for phase 1.
    public double K { get; init; } = double.IsFinite(K)
        ? K : throw new ArgumentOutOfRangeException(nameof(K), K, "K must be finite.");

    public double T { get; init; } = double.IsFinite(T) && T > 0
        ? T : throw new ArgumentOutOfRangeException(nameof(T), T, "T must be finite and > 0.");

    public double L { get; init; } = double.IsFinite(L) && L >= 0
        ? L : throw new ArgumentOutOfRangeException(nameof(L), L, "L must be finite and >= 0.");

    public override double Delay => L;

    public override StateSpace ToStateSpace() => new(new Matrix(new[,] { { -1 / T } }), [K / T], [1], 0);
}
