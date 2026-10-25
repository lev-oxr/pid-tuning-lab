using PidLab.Core.Linear;

namespace PidLab.Core.Tests.Support;

/// <summary>Fixed-step integrators for ẋ = Ax + Bu with u constant over each step; x(0) = 0. Returns y_k = C·x_k, k = 0 … n−1.</summary>
public static class Integrators
{
    public static double[] Euler(StateSpace s, double dt, int n, double u) =>
        Run(s, n, u, x => Deriv(s, x, u), (x, f) => Axpy(x, dt, f));

    public static double[] Rk4(StateSpace s, double dt, int n, double u) =>
        Run(s, n, u, x => Deriv(s, x, u), (x, _) =>
        {
            var k1 = Deriv(s, x, u);
            var k2 = Deriv(s, Axpy(x, dt / 2, k1), u);
            var k3 = Deriv(s, Axpy(x, dt / 2, k2), u);
            var k4 = Deriv(s, Axpy(x, dt, k3), u);
            var next = (double[])x.Clone();
            for (var i = 0; i < next.Length; i++) next[i] += dt / 6 * (k1[i] + 2 * k2[i] + 2 * k3[i] + k4[i]);
            return next;
        });

    private static double[] Run(StateSpace s, int n, double u, Func<double[], double[]> f, Func<double[], double[], double[]> step)
    {
        var x = new double[s.Order];
        var y = new double[n];
        for (var k = 0; k < n; k++)
        {
            y[k] = s.D * u;
            for (var i = 0; i < x.Length; i++) y[k] += s.C[i] * x[i];
            x = step(x, f(x));
        }
        return y;
    }

    private static double[] Deriv(StateSpace s, double[] x, double u)
    {
        var d = new double[x.Length];
        s.A.Multiply(x, d);
        for (var i = 0; i < d.Length; i++) d[i] += s.B[i] * u;
        return d;
    }

    private static double[] Axpy(double[] x, double a, double[] f)
    {
        var r = new double[x.Length];
        for (var i = 0; i < r.Length; i++) r[i] = x[i] + a * f[i];
        return r;
    }
}
