using System.Text.Json;
using PidLab.Core.Linear;
using PidLab.Core.Simulation;
using Xunit.Abstractions;

namespace PidLab.Core.Tests;

/// <summary>
/// Compares the C# core with python-control (reference/generate.py, committed CSVs only).
/// Python models the delay with Padé(5), the core with an exact DelayLine; they disagree near t = L and agree later.
/// So y is checked in two windows: t ≤ WindowFactor·L (wide tolerance) and beyond it (tight tolerance).
/// Delay-free plants have no window and use the tight tolerance throughout.
/// </summary>
public class GoldenTests(ITestOutputHelper output)
{
    // Tolerances are relative to max|y_ref|; each is the measured worst case ×3, rounded up to a power of ten (docs/adr).
    // Measured, delay-free plants:                    worst 1.7e-12  → 1e-11.
    // Measured, delayed plants, t > 5·L (Padé decay): worst 1.8e-11  → 1e-10 (error falls ~100× per extra L: c=2 → 3e-5, c=3 → 4e-7, c=4 → 2e-9).
    // Measured, delayed plants, t ≤ 5·L (peak at t≈L): worst 6.6e-3  → 1e-1.
    private const double WindowFactor = 5;
    private const double MatrixTol = 1e-12;
    private const double GridTol = 1e-12;
    private const double DelayFreeTol = 1e-11;
    private const double OutsideTol = 1e-10;
    private const double WindowTol = 1e-1;

    public static IEnumerable<object[]> Ids() => GoldenLoader.Cases().Select(c => new object[] { c.Id });

    [Theory, MemberData(nameof(Ids))]
    public void Zoh_matrices_match_scipy(string id)
    {
        var c = GoldenLoader.Cases().Single(x => x.Id == id);
        using var doc = GoldenLoader.Zoh();
        var z = doc.RootElement.GetProperty(id);
        var ss = c.Plant.ToStateSpace();
        var ad = Discretizer.Zoh(ss, c.Dt);

        Vec(z, "B", ss.B);
        Vec(z, "C", ss.C);
        Assert.Equal(z.GetProperty("D").GetDouble(), ss.D);
        var a = z.GetProperty("A").EnumerateArray().Select(r => r.EnumerateArray().Select(v => v.GetDouble()).ToArray()).ToArray();
        var adRef = z.GetProperty("Ad").EnumerateArray().Select(r => r.EnumerateArray().Select(v => v.GetDouble()).ToArray()).ToArray();
        for (var i = 0; i < ss.Order; i++)
            for (var j = 0; j < ss.Order; j++)
            {
                Assert.Equal(a[i][j], ss.A[i, j], MatrixTol);
                Assert.Equal(adRef[i][j], ad.Ad[i, j], MatrixTol);
            }
        Vec(z, "Bd", ad.Bd);
    }

    private static void Vec(JsonElement z, string name, double[] actual)
    {
        var expected = z.GetProperty(name).EnumerateArray().Select(v => v.GetDouble()).ToArray();
        Assert.Equal(expected.Length, actual.Length);
        for (var i = 0; i < expected.Length; i++) Assert.Equal(expected[i], actual[i], MatrixTol);
    }

    [Fact]
    public void Output_matches_python_control_and_writes_verification()
    {
        var results = new List<object>();
        var failures = new List<string>();

        foreach (var c in GoldenLoader.Cases())
        {
            var g = GoldenLoader.Series(c.Id);
            var s = new SimulationSettings(c.Dt, c.Horizon);
            var r = OpenLoopSimulator.Run(c.Plant, s, c.Amplitude);

            Assert.Equal(c.N, s.StepCount);
            Assert.Equal(c.N, g.T.Length);
            Assert.Equal(c.N, r.T.Length);
            for (var k = 0; k < c.N; k++)
            {
                Assert.True(Math.Abs(g.T[k] - r.T[k]) <= GridTol, $"{c.Id}: t[{k}]");
                Assert.True(Math.Abs(g.U[k] - r.U[k]) <= GridTol, $"{c.Id}: u[{k}]");
            }

            var yMax = g.Y.Max(Math.Abs);
            var windowEnd = WindowFactor * c.Plant.Delay;
            double inMax = 0, outMax = 0, inAt = 0, outAt = 0;
            for (var k = 0; k < c.N; k++)
            {
                var e = Math.Abs(r.Y[k] - g.Y[k]);
                if (r.T[k] <= windowEnd) { if (e > inMax) (inMax, inAt) = (e, r.T[k]); }
                else if (e > outMax) (outMax, outAt) = (e, r.T[k]);
            }

            var hasWindow = c.Plant.Delay > 0;
            var wTol = (hasWindow ? WindowTol : DelayFreeTol) * yMax;
            var tTol = (hasWindow ? OutsideTol : DelayFreeTol) * yMax;
            var ok = inMax <= wTol && outMax <= tTol;
            output.WriteLine($"{c.Id}: window(t<={windowEnd:G3}) max {inMax:E3} at t={inAt:G4}; outside max {outMax:E3} at t={outAt:G4}; |y|max {yMax:G4}");
            if (!ok) failures.Add($"{c.Id}: window {inMax:E3} (tol {wTol:E3}), outside {outMax:E3} (tol {tTol:E3})");

            results.Add(new
            {
                id = c.Id,
                windowEnd,
                maxAbsErrorWindow = inMax,
                maxAbsErrorOutside = outMax,
                tMaxError = outMax >= inMax ? outAt : inAt,
                toleranceWindow = wTol,
                toleranceOutside = tTol,
                status = ok ? "pass" : "fail",
            });
        }

        var path = Environment.GetEnvironmentVariable("PIDLAB_VERIFICATION_OUT")
                   ?? Path.Combine(AppContext.BaseDirectory, "verification.json");
        File.WriteAllText(path, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));

        Assert.True(failures.Count == 0, string.Join("; ", failures));
    }
}
