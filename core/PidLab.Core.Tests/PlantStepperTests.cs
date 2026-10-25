using PidLab.Core.Linear;
using PidLab.Core.Numerics;
using PidLab.Core.Simulation;

namespace PidLab.Core.Tests;

public class PlantStepperTests
{
    [Fact]
    public void Nonzero_D_is_rejected()
    {
        var d = new DiscreteStateSpace(new Matrix(1, 1), [1], [1], 0.5, 0.1);
        Assert.Throws<ArgumentException>(() => new PlantStepper(d, 0));
    }

    [Fact]
    public void Output_lags_input_by_delay_steps_plus_one_sample()
    {
        // x_{k+1} = x_k + u_{k−2}: y first moves at k = 3.
        var d = new DiscreteStateSpace(Matrix.Identity(1), [1], [1], 0, 0.1);
        var st = new PlantStepper(d, 2);
        var y = new List<double>();
        for (var k = 0; k < 6; k++) { y.Add(st.Output); st.Advance(1); }
        Assert.Equal([0.0, 0, 0, 1, 2, 3], y);
    }
}
