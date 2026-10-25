using PidLab.Core.Numerics;

namespace PidLab.Core.Linear;

public static class Discretizer
{
    /// <summary>
    /// Exact discretization for a piecewise-constant input (zero-order hold).
    /// With u constant on [kΔt, (k+1)Δt): Ad = e^{AΔt}, Bd = (∫₀^Δt e^{Aτ}dτ)·B.
    /// Both come from one expm of the block-triangular M = [[A·Δt, B·Δt], [0, 0]]. For P = AΔt, Q = BΔt,
    /// M^k = [[P^k, P^{k−1}Q], [0, 0]], so the top-right block of e^M is Σ P^{k−1}Q/k! = ∫₀¹ e^{sP}ds·Q,
    /// and with s = τ/Δt that equals ∫₀^Δt e^{Aτ}dτ·B. Result: e^M = [[Ad, Bd], [0, 1]].
    /// A⁻¹ is never needed, so a singular A (integrator) is fine.
    /// </summary>
    public static DiscreteStateSpace Zoh(StateSpace sys, double dt)
    {
        if (!(double.IsFinite(dt) && dt > 0))
            throw new ArgumentOutOfRangeException(nameof(dt), dt, "dt must be finite and > 0.");
        var n = sys.Order;
        var m = new Matrix(n + 1, n + 1);
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++) m[i, j] = sys.A[i, j] * dt;
            m[i, n] = sys.B[i] * dt;
        }

        var e = MatrixExp.Expm(m);
        var ad = new Matrix(n, n);
        var bd = new double[n];
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++) ad[i, j] = e[i, j];
            bd[i] = e[i, n];
        }
        return new DiscreteStateSpace(ad, bd, sys.C, sys.D, dt);
    }
}
