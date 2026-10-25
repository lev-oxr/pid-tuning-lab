using PidLab.Core.Numerics;

namespace PidLab.Core.Tests;

public class MatrixTests
{
    private static void AssertClose(Matrix expected, Matrix actual, double tol)
    {
        Assert.Equal(expected.Rows, actual.Rows);
        Assert.Equal(expected.Cols, actual.Cols);
        for (var i = 0; i < expected.Rows; i++)
            for (var j = 0; j < expected.Cols; j++)
                Assert.True(Math.Abs(expected[i, j] - actual[i, j]) <= tol * Math.Max(1, Math.Abs(expected[i, j])),
                    $"[{i},{j}] expected {expected[i, j]:R}, got {actual[i, j]:R}");
    }

    [Fact]
    public void Expm_of_zero_is_identity() =>
        AssertClose(Matrix.Identity(3), MatrixExp.Expm(new Matrix(3, 3)), 1e-15);

    [Fact]
    public void Expm_of_diagonal() =>
        AssertClose(new Matrix(new[,] { { Math.E, 0 }, { 0, Math.Exp(-2) } }),
            MatrixExp.Expm(new Matrix(new double[,] { { 1, 0 }, { 0, -2 } })), 1e-14);

    [Fact]
    public void Expm_of_nilpotent() =>
        AssertClose(new Matrix(new double[,] { { 1, 1 }, { 0, 1 } }),
            MatrixExp.Expm(new Matrix(new double[,] { { 0, 1 }, { 0, 0 } })), 1e-15);

    [Fact]
    public void Expm_of_rotation_generator()
    {
        const double th = 0.7;
        AssertClose(new Matrix(new[,] { { Math.Cos(th), Math.Sin(th) }, { -Math.Sin(th), Math.Cos(th) } }),
            MatrixExp.Expm(new Matrix(new[,] { { 0, th }, { -th, 0 } })), 1e-14);
    }

    // ‖M‖₁ = 50 forces s = 7 squarings.
    [Fact]
    public void Expm_of_large_norm_uses_scaling() =>
        AssertClose(new Matrix(new[,] { { Math.Exp(-50), 0 }, { 0, Math.Exp(-1) } }),
            MatrixExp.Expm(new Matrix(new double[,] { { -50, 0 }, { 0, -1 } })), 1e-13);

    [Fact]
    public void Expm_semigroup_property()
    {
        var m = new Matrix(new[,] { { -0.3, 1.2, 0.1 }, { -2.0, -0.5, 0.4 }, { 0.2, 0.0, -1.1 } });
        var e = MatrixExp.Expm(m);
        AssertClose(MatrixExp.Expm(2 * m), e * e, 1e-13);
    }

    [Fact]
    public void Solve_known_system()
    {
        // 2x + y = 5, x + 3y = 10 → x = 1, y = 3
        var a = new Matrix(new double[,] { { 2, 1 }, { 1, 3 } });
        var x = a.Solve(new Matrix(new double[,] { { 5 }, { 10 } }));
        Assert.Equal(1, x[0, 0], 1e-14);
        Assert.Equal(3, x[1, 0], 1e-14);
    }

    [Fact]
    public void Solve_needs_pivoting()
    {
        var a = new Matrix(new double[,] { { 0, 1 }, { 1, 0 } });
        var x = a.Solve(new Matrix(new double[,] { { 2 }, { 3 } }));
        Assert.Equal(3, x[0, 0]);
        Assert.Equal(2, x[1, 0]);
    }

    [Fact]
    public void Solve_singular_throws()
    {
        var a = new Matrix(new double[,] { { 1, 2 }, { 2, 4 } });
        Assert.Throws<InvalidOperationException>(() => a.Solve(new Matrix(new double[,] { { 1 }, { 1 } })));
    }

    [Fact]
    public void Multiply_vector_matches_matrix_product()
    {
        var a = new Matrix(new double[,] { { 1, 2 }, { 3, 4 } });
        var r = new double[2];
        a.Multiply([5, 6], r);
        Assert.Equal([17.0, 39.0], r);
    }
}
