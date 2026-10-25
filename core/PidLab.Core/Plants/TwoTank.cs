using PidLab.Core.Linear;

namespace PidLab.Core.Plants;

/// <summary>W(s) = K·e^(−Ls) / ((T1·s + 1)(T2·s + 1)).</summary>
public sealed record TwoTank(double K, double T1, double T2, double L) : PlantModel
{
    public double K { get; init; } = Finite(K, nameof(K));
    public double T1 { get; init; } = Positive(T1, nameof(T1));
    public double T2 { get; init; } = Positive(T2, nameof(T2));
    public double L { get; init; } = NonNegative(L, nameof(L));

    public override double Delay => L;
    public override StateSpace ToStateSpace() => TwoLags(K, T1, T2);
}
