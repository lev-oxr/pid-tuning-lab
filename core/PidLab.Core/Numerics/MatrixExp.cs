namespace PidLab.Core.Numerics;

public static class MatrixExp
{
    private const int Q = 6;

    /// <summary>
    /// e^M by scaling and squaring with a diagonal (6,6) Padé approximant (Moler–Van Loan, 2003):
    /// pick s with ‖M‖₁/2^s ≤ 0.5, R = D⁻¹N with N = Σ c_k X^k, D = Σ c_k (−X)^k, X = M/2^s, then square R s times.
    /// c_0 = 1, c_k = c_{k−1}·(q−k+1) / (k·(2q−k+1)).
    /// </summary>
    public static Matrix Expm(Matrix m)
    {
        if (m.Rows != m.Cols) throw new ArgumentException("Expm needs a square matrix.");
        var norm = m.Norm1();
        if (!double.IsFinite(norm)) throw new ArgumentException("Matrix has non-finite entries.");

        var s = norm > 0.5 ? (int)Math.Ceiling(Math.Log2(norm / 0.5)) : 0;
        var x = Math.ScaleB(1.0, -s) * m;

        var n = m.Rows;
        var num = Matrix.Identity(n);
        var den = Matrix.Identity(n);
        var power = Matrix.Identity(n);
        var c = 1.0;
        for (var k = 1; k <= Q; k++)
        {
            c *= (double)(Q - k + 1) / (k * (2 * Q - k + 1));
            power = power * x;
            num += c * power;
            den += (k % 2 == 0 ? c : -c) * power;
        }

        var r = den.Solve(num);
        for (var i = 0; i < s; i++) r = r * r;
        return r;
    }
}
