namespace PidLab.Core.Simulation;

/// <summary>Ring buffer: Push returns the value pushed <c>steps</c> calls ago.</summary>
public sealed class DelayLine
{
    private readonly double[] _buf;
    private int _i;

    public DelayLine(int steps, double initial = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        _buf = new double[steps];
        Array.Fill(_buf, initial);
    }

    public double Push(double value)
    {
        if (_buf.Length == 0) return value;
        var old = _buf[_i];
        _buf[_i] = value;
        _i = (_i + 1) % _buf.Length;
        return old;
    }

    public static int StepsFor(double L, double dt) => (int)Math.Round(L / dt);
}
