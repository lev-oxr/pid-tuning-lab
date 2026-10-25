using PidLab.Core.Numerics;

namespace PidLab.Core.Linear;

/// <summary>
/// SISO continuous model ẋ = Ax + Bu, y = Cx + Du, x(0) = 0. B is a column, C is a row.
/// Delay is not part of the model (see PlantModel.Delay).
/// </summary>
public sealed record StateSpace(Matrix A, double[] B, double[] C, double D)
{
    // Validation lives in the initializers, same pattern as Fopdt.
    public Matrix A { get; init; } = A.Rows == A.Cols
        ? A : throw new ArgumentException("A must be square.", nameof(A));

    public double[] B { get; init; } = B.Length == A.Rows
        ? B : throw new ArgumentException("B length must equal the state count.", nameof(B));

    public double[] C { get; init; } = C.Length == A.Rows
        ? C : throw new ArgumentException("C length must equal the state count.", nameof(C));

    public int Order => A.Rows;
}
