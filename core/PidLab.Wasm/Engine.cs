using System.Runtime.InteropServices.JavaScript;
using PidLab.Core;

namespace PidLab.Wasm;

public static partial class Engine
{
    [JSExport] public static string Ping() => "pong from C# (.NET " + Environment.Version + ")";
    [JSExport] public static double Add(double a, double b) => Calc.Add(a, b);
    [JSExport] public static string Echo(string s) => $"{s} (len {s.Length})";

    // Blocks the worker thread to verify the UI stays responsive.
    [JSExport] public static void Busy(int ms)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < ms) { }
    }
}
