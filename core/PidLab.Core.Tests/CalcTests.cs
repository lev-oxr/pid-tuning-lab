using PidLab.Core;

public class CalcTests
{
    [Fact] public void Add_Basic() => Assert.Equal(5.0, Calc.Add(2, 3));
    [Fact] public void Add_NegativeAndFractional() => Assert.Equal(-0.5, Calc.Add(-2, 1.5));
}
