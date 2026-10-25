using PidLab.Core.Plants;
using PidLab.Core.Simulation;

namespace PidLab.Core.Tests;

public class OpenLoopSimulatorShapeTests
{
    [Fact]
    public void Result_has_expected_shape()
    {
        var s = new SimulationSettings(0.01, 30);
        var r = OpenLoopSimulator.Run(new Fopdt(2, 5, 1), s, stepAmplitude: 3);
        var n = s.StepCount;

        Assert.Equal(3000, n);
        Assert.Equal(n, r.T.Length);
        Assert.Equal(n, r.U.Length);
        Assert.Equal(n, r.Y.Length);
        Assert.Equal(0, r.T[0]);
        Assert.Equal((n - 1) * s.Dt, r.T[^1]);
        Assert.All(r.U, u => Assert.Equal(3, u));
        Assert.Equal(0, r.Y[0]);
    }
}
