namespace PidLab.Core.Simulation;

/// <summary>Time grid t_k = k·Dt, k = 0 … StepCount−1.</summary>
public sealed record SimulationSettings(double Dt, double Horizon)
{
    public double Dt { get; init; } = double.IsFinite(Dt) && Dt > 0
        ? Dt : throw new ArgumentOutOfRangeException(nameof(Dt), Dt, "Dt must be finite and > 0.");

    public double Horizon { get; init; } = double.IsFinite(Horizon) && Horizon >= Dt
        ? Horizon : throw new ArgumentOutOfRangeException(nameof(Horizon), Horizon, "Horizon must be finite and >= Dt.");

    public int StepCount => (int)Math.Round(Horizon / Dt);
}
