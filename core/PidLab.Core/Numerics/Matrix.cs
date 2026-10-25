namespace PidLab.Core.Numerics;

/// <summary>Dense row-major matrix. Just enough linear algebra for ZOH discretization; no external packages.</summary>
public sealed class Matrix
{
    private readonly double[] _a;

    public int Rows { get; }
    public int Cols { get; }

    public Matrix(int rows, int cols)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cols);
        Rows = rows;
        Cols = cols;
        _a = new double[rows * cols];
    }

    public Matrix(double[,] values) : this(values.GetLength(0), values.GetLength(1))
    {
        for (var i = 0; i < Rows; i++)
            for (var j = 0; j < Cols; j++)
                _a[i * Cols + j] = values[i, j];
    }

    public double this[int i, int j]
    {
        get => _a[i * Cols + j];
        set => _a[i * Cols + j] = value;
    }

    public static Matrix Identity(int n)
    {
        var m = new Matrix(n, n);
        for (var i = 0; i < n; i++) m[i, i] = 1;
        return m;
    }

    public static Matrix operator +(Matrix a, Matrix b)
    {
        RequireSameShape(a, b);
        var r = new Matrix(a.Rows, a.Cols);
        for (var i = 0; i < a._a.Length; i++) r._a[i] = a._a[i] + b._a[i];
        return r;
    }

    public static Matrix operator -(Matrix a, Matrix b)
    {
        RequireSameShape(a, b);
        var r = new Matrix(a.Rows, a.Cols);
        for (var i = 0; i < a._a.Length; i++) r._a[i] = a._a[i] - b._a[i];
        return r;
    }

    public static Matrix operator *(double s, Matrix a)
    {
        var r = new Matrix(a.Rows, a.Cols);
        for (var i = 0; i < a._a.Length; i++) r._a[i] = s * a._a[i];
        return r;
    }

    public static Matrix operator *(Matrix a, Matrix b)
    {
        if (a.Cols != b.Rows) throw new ArgumentException($"Cannot multiply {a.Rows}x{a.Cols} by {b.Rows}x{b.Cols}.");
        var r = new Matrix(a.Rows, b.Cols);
        for (var i = 0; i < a.Rows; i++)
            for (var k = 0; k < a.Cols; k++)
            {
                var aik = a[i, k];
                for (var j = 0; j < b.Cols; j++) r._a[i * r.Cols + j] += aik * b[k, j];
            }
        return r;
    }

    /// <summary>result = this·x, no allocation. <paramref name="result"/> must not alias <paramref name="x"/>.</summary>
    public void Multiply(double[] x, double[] result)
    {
        if (x.Length != Cols || result.Length != Rows) throw new ArgumentException("Vector length mismatch.");
        for (var i = 0; i < Rows; i++)
        {
            var sum = 0.0;
            for (var j = 0; j < Cols; j++) sum += _a[i * Cols + j] * x[j];
            result[i] = sum;
        }
    }

    /// <summary>Induced 1-norm: max column sum of |a_ij|.</summary>
    public double Norm1()
    {
        var max = 0.0;
        for (var j = 0; j < Cols; j++)
        {
            var sum = 0.0;
            for (var i = 0; i < Rows; i++) sum += Math.Abs(this[i, j]);
            max = Math.Max(max, sum);
        }
        return max;
    }

    /// <summary>Solves this·X = b by LU decomposition with partial pivoting. Throws if this is singular.</summary>
    public Matrix Solve(Matrix b)
    {
        if (Rows != Cols) throw new InvalidOperationException("Solve needs a square matrix.");
        if (b.Rows != Rows) throw new ArgumentException("Right-hand side has wrong row count.");
        var n = Rows;
        var lu = (double[])_a.Clone();
        var x = new Matrix(n, b.Cols);
        Array.Copy(b._a, x._a, b._a.Length);
        var scale = Norm1();

        for (var c = 0; c < n; c++)
        {
            var p = c;
            for (var i = c + 1; i < n; i++)
                if (Math.Abs(lu[i * n + c]) > Math.Abs(lu[p * n + c])) p = i;
            if (!(Math.Abs(lu[p * n + c]) > 1e-14 * scale))
                throw new InvalidOperationException("Matrix is singular to working precision.");
            if (p != c)
            {
                for (var j = 0; j < n; j++) (lu[c * n + j], lu[p * n + j]) = (lu[p * n + j], lu[c * n + j]);
                for (var j = 0; j < x.Cols; j++) (x[c, j], x[p, j]) = (x[p, j], x[c, j]);
            }
            for (var i = c + 1; i < n; i++)
            {
                var f = lu[i * n + c] / lu[c * n + c];
                if (f == 0) continue;
                for (var j = c; j < n; j++) lu[i * n + j] -= f * lu[c * n + j];
                for (var j = 0; j < x.Cols; j++) x[i, j] -= f * x[c, j];
            }
        }

        for (var j = 0; j < x.Cols; j++)
            for (var i = n - 1; i >= 0; i--)
            {
                var sum = x[i, j];
                for (var k = i + 1; k < n; k++) sum -= lu[i * n + k] * x[k, j];
                x[i, j] = sum / lu[i * n + i];
            }
        return x;
    }

    private static void RequireSameShape(Matrix a, Matrix b)
    {
        if (a.Rows != b.Rows || a.Cols != b.Cols) throw new ArgumentException("Matrix shapes differ.");
    }
}
