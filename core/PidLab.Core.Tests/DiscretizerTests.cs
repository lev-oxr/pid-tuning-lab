using PidLab.Core.Numerics;
using PidLab.Core.Linear;

namespace PidLab.Core.Tests;

public class DiscretizerTests
{
    [Fact]
    public void First_order()
    {
        const double K = 2, T = 5, dt = 0.3;
        var d = Discretizer.Zoh(new StateSpace(new Matrix(new[,] { { -1 / T } }), [K / T], [1], 0), dt);
        Assert.Equal(Math.Exp(-dt / T), d.Ad[0, 0], 1e-15);
        Assert.Equal(K * (1 - Math.Exp(-dt / T)), d.Bd[0], 1e-15);
        Assert.Equal(dt, d.Dt);
    }

    // A = 0: B_d = A⁻¹(A_d − I)B would be 0/0; the extended matrix has no such problem.
    [Fact]
    public void Integrator()
    {
        var d = Discretizer.Zoh(new StateSpace(new Matrix(1, 1), [1], [1], 0), 0.25);
        Assert.Equal(1, d.Ad[0, 0]);
        Assert.Equal(0.25, d.Bd[0], 1e-16);
    }

    [Fact]
    public void Double_integrator()
    {
        const double dt = 0.4;
        var d = Discretizer.Zoh(new StateSpace(new Matrix(new double[,] { { 0, 1 }, { 0, 0 } }), [0, 1], [1, 0], 0), dt);
        Assert.Equal(1, d.Ad[0, 0], 1e-15);
        Assert.Equal(dt, d.Ad[0, 1], 1e-15);
        Assert.Equal(0, d.Ad[1, 0], 1e-15);
        Assert.Equal(1, d.Ad[1, 1], 1e-15);
        Assert.Equal(dt * dt / 2, d.Bd[0], 1e-15);
        Assert.Equal(dt, d.Bd[1], 1e-15);
    }

    [Fact]
    public void Tiny_step_gives_identity()
    {
        var d = Discretizer.Zoh(new StateSpace(new Matrix(new[,] { { -1.0, 2 }, { 0, -3 } }), [1, 1], [1, 0], 0), 1e-9);
        Assert.Equal(1, d.Ad[0, 0], 1e-8);
        Assert.Equal(0, d.Ad[0, 1], 1e-8);
        Assert.Equal(1, d.Ad[1, 1], 1e-8);
    }

    [Fact]
    public void Validation()
    {
        var a = new Matrix(1, 1);
        Assert.Throws<ArgumentException>(() => new StateSpace(new Matrix(2, 3), [1, 1], [1, 1], 0));
        Assert.Throws<ArgumentException>(() => new StateSpace(a, [1, 1], [1], 0));
        Assert.Throws<ArgumentException>(() => new StateSpace(a, [1], [1, 1], 0));
        var ss = new StateSpace(a, [1], [1], 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => Discretizer.Zoh(ss, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Discretizer.Zoh(ss, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DiscreteStateSpace(a, [1], [1], 0, -1));
    }
}
