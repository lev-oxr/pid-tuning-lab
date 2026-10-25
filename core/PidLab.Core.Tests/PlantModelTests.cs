using PidLab.Core.Linear;
using PidLab.Core.Numerics;
using PidLab.Core.Plants;

namespace PidLab.Core.Tests;

public class PlantModelTests
{
    // Static gain C·(−A)⁻¹·B + D.
    private static double DcGain(StateSpace s)
    {
        var b = new Matrix(s.Order, 1);
        for (var i = 0; i < s.Order; i++) b[i, 0] = s.B[i];
        var x = (-1 * s.A).Solve(b);
        var g = s.D;
        for (var i = 0; i < s.Order; i++) g += s.C[i] * x[i, 0];
        return g;
    }

    public static TheoryData<PlantModel, double> Gains => new()
    {
        { new Fopdt(2, 5, 1), 2 },
        { new TwoTank(2, 4, 1, 0.5), 2 },
        { new TwoTank(1, 2, 2, 0), 1 },
        { new DcMotor(3, 0.5, 0.05), 3 },
        { new InverseResponse(1, 2, 5, 2), 1 },
    };

    [Theory, MemberData(nameof(Gains))]
    public void Static_gain_equals_K(PlantModel p, double k) =>
        Assert.Equal(k, DcGain(p.ToStateSpace()), 1e-12);

    [Fact]
    public void Second_order_poles_via_trace_and_determinant()
    {
        var a = new TwoTank(2, 4, 1, 0.5).ToStateSpace().A;
        Assert.Equal(-(1 / 4.0 + 1 / 1.0), a[0, 0] + a[1, 1], 1e-14);
        Assert.Equal(1 / 4.0, a[0, 0] * a[1, 1] - a[0, 1] * a[1, 0], 1e-14);
    }

    [Fact]
    public void Inverse_response_numerator_is_one_minus_beta_s()
    {
        var c = new InverseResponse(1, 2, 5, 2).ToStateSpace().C;
        Assert.Equal(1 / 10.0, c[0], 1e-15);
        Assert.Equal(-2 / 10.0, c[1], 1e-15);
    }

    [Fact]
    public void Delay_is_reported_separately()
    {
        Assert.Equal(1, new Fopdt(2, 5, 1).Delay);
        Assert.Equal(0.5, new TwoTank(2, 4, 1, 0.5).Delay);
        Assert.Equal(2, new Integrating(0.5, 2).Delay);
        Assert.Equal(0, new DcMotor(3, 0.5, 0.05).Delay);
        Assert.Equal(0, new InverseResponse(1, 2, 5, 2).Delay);
    }

    [Fact]
    public void Integrating_has_zero_A()
    {
        var s = new Integrating(0.5, 2).ToStateSpace();
        Assert.Equal(0, s.A[0, 0]);
        Assert.Equal(0.5, s.C[0]);
    }

    [Fact]
    public void Equal_time_constants_are_fine()
    {
        var s = new TwoTank(1, 2, 2, 0).ToStateSpace();
        Assert.Equal(1, DcGain(s), 1e-12);
        var d = Discretizer.Zoh(s, 0.01);
        Assert.True(double.IsFinite(d.Ad[0, 0]) && double.IsFinite(d.Bd[1]));
    }

    public static TheoryData<Func<PlantModel>> Invalid => new()
    {
        () => new Fopdt(double.NaN, 1, 0),
        () => new TwoTank(double.PositiveInfinity, 1, 1, 0),
        () => new TwoTank(1, 0, 1, 0),
        () => new TwoTank(1, 1, double.NaN, 0),
        () => new TwoTank(1, 1, 1, -0.1),
        () => new DcMotor(double.NaN, 1, 1),
        () => new DcMotor(1, -1, 1),
        () => new DcMotor(1, 1, 0),
        () => new Integrating(double.NaN, 0),
        () => new Integrating(1, -1),
        () => new Integrating(1, double.PositiveInfinity),
        () => new InverseResponse(1, 0, 1, 1),
        () => new InverseResponse(1, double.NaN, 1, 1),
        () => new InverseResponse(1, 1, 0, 1),
        () => new InverseResponse(1, 1, 1, double.NaN),
    };

    [Theory, MemberData(nameof(Invalid))]
    public void Invalid_parameters_are_rejected(Func<PlantModel> make) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => make());

    [Fact]
    public void Valid_parameters_are_accepted()
    {
        _ = new TwoTank(-1, 1, 1, 0);
        _ = new DcMotor(0, 1, 1);
        _ = new Integrating(0, 0);
        _ = new InverseResponse(-1, 1, 1, 1);
    }
}
