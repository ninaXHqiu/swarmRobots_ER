// Tests the final routines' plans outside Unity: reads every data file the way the app does
// (FinalPlan), and checks what the robots will be asked to do.
//   tools/final-sim/check/run.sh
using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class Check
{
    public static int Main(string[] args)
    {
        string dir = args[0];
        bool ok = true;
        foreach (var file in Directory.GetFiles(dir, "*.txt").OrderBy(f => f))
        {
            var plan = new FinalPlan(File.ReadAllText(file));
            int n = plan.Robots;
            float closest = float.MaxValue, top = 0f, edge = float.MaxValue, inPlace = 0f, spinTop = 0f, total = 0f;
            const float dt = 0.05f;
            for (float t = 0f; t <= plan.Total; t += dt)
            {
                for (int i = 0; i < n; i++)
                {
                    var p = plan.Position(i, t);
                    edge = Mathf.Clamp(Math.Min(Math.Min(p.x - 45f, 455f - p.x), Math.Min(p.y - 45f, 455f - p.y)), -999f, edge);
                    for (int j = i + 1; j < n; j++) { closest = Math.Min(closest, (plan.Position(j, t) - p).magnitude); }
                    if (t > 0f) { top = Math.Max(top, (p - plan.Position(i, t - dt)).magnitude / dt); }
                    total += dt;
                    if (plan.InPlace(i, t)) { inPlace += dt; spinTop = Math.Max(spinTop, Math.Abs(plan.TurnRate(i, t))); }
                }
            }
            // the messenger must be the lowest robot (nearest the participant) when "better?" starts
            float betterAt = plan.Sections.First(s => s.Value.StartsWith("7")).Key;
            var low = Enumerable.Range(0, n).OrderByDescending(i => plan.Position(i, betterAt).y).First();
            bool good = Math.Abs(plan.Total - 115f) < 0.01f && closest >= 26f && top <= 175f && edge >= 20f && low == 1;
            ok &= good;
            Console.WriteLine("{0} {1,-28} {2:F1} s | closest {3:F1} | top speed {4:F0} | edge {5:F0} | turning in place {6:F0}% (top {7:F0} deg/s) | messenger lowest at 'better?': {8} | sections {9}",
                good ? "  " : "!!", Path.GetFileNameWithoutExtension(file), plan.Total, closest, top, edge, 100f * inPlace / total, spinTop, low == 1, plan.Sections.Count());
        }
        // the routines and their data files match
        foreach (var r in new SwarmRoutine[] { new Final_Resonance1_Level1(), new Final_Resonance1_Level2(), new Final_Resonance1_Level3(), new Final_Resonance2_Level1(), new Final_Resonance2_Level2(), new Final_Resonance2_Level3(), new Final_Cheerleading1(), new Final_Cheerleading2() })
        {
            var data = (string)r.GetType().GetProperty("Data", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(r, null);
            bool exists = File.Exists(Path.Combine(dir, data + ".txt"));
            ok &= exists;
            Console.WriteLine("{0} {1} {2,-28} -> {3}.txt {4}", exists ? "  " : "!!", r.Code, r.Name, data, exists ? "" : "MISSING");
        }
        Console.WriteLine(ok ? "all good" : "PROBLEMS (lines with !!)");
        return ok ? 0 : 1;
    }
}
