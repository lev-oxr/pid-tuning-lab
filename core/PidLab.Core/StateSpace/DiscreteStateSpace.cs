using PidLab.Core.Numerics;

namespace PidLab.Core.Linear;

/// <summary>x_{k+1} = Ad·x_k + Bd·u_k, y_k = C·x_k + D·u_k with sample time Dt.</summary>
public sealed record DiscreteStateSpace(Matrix Ad, double[] Bd, double[] C, double D, double Dt)
{
    public Matrix Ad { get; init; } = Ad.Rows == Ad.Cols
        ? Ad : throw new ArgumentException("Ad must be square.", nameof(Ad));

    public double[] Bd { get; init; } = Bd.Length == Ad.Rows
        ? Bd : throw new ArgumentException("Bd length must equal the state count.", nameof(Bd));

    public double[] C { get; init; } = C.Length == Ad.Rows
        ? C : throw new ArgumentException("C length must equal the state count.", nameof(C));

    public double Dt { get; init; } = double.IsFinite(Dt) && Dt > 0
        ? Dt : throw new ArgumentOutOfRangeException(nameof(Dt), Dt, "Dt must be finite and > 0.");

    public int Order => Ad.Rows;
}
