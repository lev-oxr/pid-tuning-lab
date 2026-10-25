using System.Globalization;
using System.Text.Json;
using PidLab.Core.Plants;

namespace PidLab.Core.Tests;

public sealed record GoldenCase(string Id, PlantModel Plant, double Dt, double Horizon, int N, double Amplitude);

public sealed record GoldenSeries(double[] T, double[] U, double[] Y);

public static class GoldenLoader
{
    private static string Dir => Path.Combine(AppContext.BaseDirectory, "golden");

    public static IReadOnlyList<GoldenCase> Cases()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Dir, "manifest.json")));
        var amp = doc.RootElement.GetProperty("amplitude").GetDouble();
        return doc.RootElement.GetProperty("cases").EnumerateArray().Select(c => new GoldenCase(
            c.GetProperty("id").GetString()!,
            Plant(c.GetProperty("plant")),
            c.GetProperty("dt").GetDouble(),
            c.GetProperty("horizon").GetDouble(),
            c.GetProperty("n").GetInt32(),
            amp)).ToList();
    }

    public static GoldenSeries Series(string id)
    {
        var rows = File.ReadAllLines(Path.Combine(Dir, id + ".csv"));
        if (rows[0] != "t,u,y") throw new InvalidDataException($"{id}.csv: unexpected header '{rows[0]}'.");
        var cols = rows.Skip(1).Where(r => r.Length > 0)
            .Select(r => r.Split(',').Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray()).ToArray();
        return new GoldenSeries(cols.Select(c => c[0]).ToArray(), cols.Select(c => c[1]).ToArray(), cols.Select(c => c[2]).ToArray());
    }

    public static JsonDocument Zoh() => JsonDocument.Parse(File.ReadAllText(Path.Combine(Dir, "zoh.json")));

    private static PlantModel Plant(JsonElement p)
    {
        double D(string name) => p.GetProperty(name).GetDouble();
        return p.GetProperty("type").GetString() switch
        {
            "Fopdt" => new Fopdt(D("K"), D("T"), D("L")),
            "TwoTank" => new TwoTank(D("K"), D("T1"), D("T2"), D("L")),
            "DcMotor" => new DcMotor(D("K"), D("Tm"), D("Te")),
            "Integrating" => new Integrating(D("K"), D("L")),
            "InverseResponse" => new InverseResponse(D("K"), D("Beta"), D("T1"), D("T2")),
            var t => throw new InvalidDataException($"Unknown plant type '{t}'."),
        };
    }
}
