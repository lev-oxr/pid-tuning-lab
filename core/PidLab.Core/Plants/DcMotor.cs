using PidLab.Core.Linear;

namespace PidLab.Core.Plants;

/// <summary>Voltage → speed: W(s) = K / ((Tm·s + 1)(Te·s + 1)).</summary>
public sealed record DcMotor(double K, double Tm, double Te) : PlantModel
{
    public double K { get; init; } = Finite(K, nameof(K));
    public double Tm { get; init; } = Positive(Tm, nameof(Tm));
    public double Te { get; init; } = Positive(Te, nameof(Te));

    public override double Delay => 0;
    public override StateSpace ToStateSpace() => TwoLags(K, Tm, Te);
}
