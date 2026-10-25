using PidLab.Core.Linear;

namespace PidLab.Core.Plants;

/// <summary>W(s) = K·(1 − β·s) / ((T1·s + 1)(T2·s + 1)): a right-half-plane zero at 1/β.</summary>
public sealed record InverseResponse(double K, double Beta, double T1, double T2) : PlantModel
{
    public double K { get; init; } = Finite(K, nameof(K));
    public double Beta { get; init; } = Positive(Beta, nameof(Beta));
    public double T1 { get; init; } = Positive(T1, nameof(T1));
    public double T2 { get; init; } = Positive(T2, nameof(T2));

    public override double Delay => 0;
    public override StateSpace ToStateSpace() => TwoLags(K, T1, T2, Beta);
}
