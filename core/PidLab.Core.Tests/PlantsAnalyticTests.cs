using PidLab.Core.Plants;
using PidLab.Core.Simulation;
using Xunit.Abstractions;

namespace PidLab.Core.Tests;

/// <summary>Main criterion of phase 2: ZOH + DelayLine reproduce closed-form step responses of all five plants.</summary>
public class PlantsAnalyticTests(ITestOutputHelper output)
{
    // (plant, Dt, Horizon): the same parameters as the python references.
    public static TheoryData<string, PlantModel, double, double> Cases => new()
    {
        { "fopdt", new Fopdt(2, 5, 1), 0.01, 30 },
        { "two_tank", new TwoTank(2, 4, 1, 0.5), 0.01, 30 },
        { "two_tank_equal", new TwoTank(1, 2, 2, 0), 0.01, 30 },
        { "dc_motor", new DcMotor(3, 0.5, 0.05), 0.002, 5 },
        { "integrating", new Integrating(0.5, 2), 0.01, 20 },
        { "inverse", new InverseResponse(1, 2, 5, 2), 0.01, 40 },
    };

    // Step response of K/((T1 s+1)(T2 s+1)) at time t ≥ 0.
    private static double Lags(double k, double t1, double t2, double t) =>
        t1 == t2
            ? k * (1 - (1 + t / t1) * Math.Exp(-t / t1))
            : k * (1 - (t1 * Math.Exp(-t / t1) - t2 * Math.Exp(-t / t2)) / (t1 - t2));

    // Derivative of the above.
    private static double LagsDot(double k, double t1, double t2, double t) =>
        t1 == t2 ? k * t / (t1 * t1) * Math.Exp(-t / t1)
                 : k * (Math.Exp(-t / t1) - Math.Exp(-t / t2)) / (t1 - t2);

    public static double Ref(PlantModel p, double t, double a = 1)
    {
        var tt = t - p.Delay;
        if (tt < 0) return 0;
        return a * p switch
        {
            Fopdt f => f.K * (1 - Math.Exp(-tt / f.T)),
            TwoTank w => Lags(w.K, w.T1, w.T2, tt),
            DcMotor m => Lags(m.K, m.Tm, m.Te, tt),
            Integrating i => i.K * tt,
            InverseResponse r => Lags(r.K, r.T1, r.T2, tt) - r.Beta * LagsDot(r.K, r.T1, r.T2, tt),
            _ => throw new NotSupportedException(),
        };
    }

    // Measured relative errors (max|Δ|/max|y_ref|): 1.7e-14 … 6.5e-14, i.e. rounding of exp/expm.
    // Tolerance 1e-12 = 3× the worst, rounded up to a power of ten.
    [Theory, MemberData(nameof(Cases))]
    public void Matches_analytic_response(string id, PlantModel p, double dt, double horizon)
    {
        var r = OpenLoopSimulator.Run(p, new SimulationSettings(dt, horizon));
        var yMax = r.T.Max(t => Math.Abs(Ref(p, t)));
        var err = r.T.Select((t, k) => Math.Abs(r.Y[k] - Ref(p, t))).Max();
        output.WriteLine($"{id}: max|y - y_ref| = {err:E3} (rel {err / yMax:E3})");
        Assert.True(err < 1e-12 * yMax, $"{id}: {err}");
    }

    [Fact]
    public void Integrating_grows_linearly_after_delay()
    {
        var p = new Integrating(0.5, 2);
        var s = new SimulationSettings(0.01, 20);
        var y = OpenLoopSimulator.Run(p, s).Y;
        var d = DelayLine.StepsFor(p.L, s.Dt);
        Assert.Equal(0, y[d]);
        for (var k = d + 2; k < y.Length; k++)
            Assert.Equal(y[d + 1], y[k] - y[k - 1], 1e-12);
        Assert.Equal(p.K * s.Dt, y[d + 1], 1e-14);
    }

    [Fact]
    public void Inverse_response_goes_the_wrong_way_first_then_settles()
    {
        var p = new InverseResponse(1, 2, 5, 2);
        var y = OpenLoopSimulator.Run(p, new SimulationSettings(0.01, 80)).Y;
        Assert.True(y[1] < 0);
        Assert.Contains(y, v => v > 0);
        Assert.Equal(p.K, y[^1], 1e-6);
    }

    public static TheoryData<PlantModel> WithDelay => new()
    {
        new Fopdt(2, 5, 1),
        new TwoTank(2, 4, 1, 0.5),
        new Integrating(0.5, 2),
    };

    // ZOH is exact and the delay is a whole number of samples, so the delayed response is the undelayed one shifted.
    [Theory, MemberData(nameof(WithDelay))]
    public void Delay_shifts_response_by_d_samples(PlantModel p)
    {
        var s = new SimulationSettings(0.01, 30);
        PlantModel p0 = p switch
        {
            Fopdt f => f with { L = 0 },
            TwoTank w => w with { L = 0 },
            Integrating i => i with { L = 0 },
            _ => throw new NotSupportedException(),
        };
        var d = DelayLine.StepsFor(p.Delay, s.Dt);
        var y = OpenLoopSimulator.Run(p, s).Y;
        var y0 = OpenLoopSimulator.Run(p0, s).Y;
        for (var k = 0; k < y.Length; k++)
            Assert.Equal(k < d ? 0 : y0[k - d], y[k], 1e-12);
    }

    [Fact]
    public void Delay_is_quantized_to_whole_samples()
    {
        var s = new SimulationSettings(0.01, 30);
        var a = OpenLoopSimulator.Run(new Fopdt(2, 5, 1.003), s);
        var b = OpenLoopSimulator.Run(new Fopdt(2, 5, 1.0), s);
        Assert.True(a.Y.SequenceEqual(b.Y));
    }

    [Theory, MemberData(nameof(Cases))]
    public void Is_linear_in_amplitude_and_deterministic(string id, PlantModel p, double dt, double horizon)
    {
        var s = new SimulationSettings(dt, horizon);
        var y1 = OpenLoopSimulator.Run(p, s, 1).Y;
        var y3 = OpenLoopSimulator.Run(p, s, 3).Y;
        var y1b = OpenLoopSimulator.Run(p, s, 1).Y;
        Assert.True(y1.SequenceEqual(y1b), id);
        var yMax = y1.Max(Math.Abs);
        for (var k = 0; k < y1.Length; k++) Assert.Equal(3 * y1[k], y3[k], 1e-12 * yMax);
    }
}
