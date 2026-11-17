using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Checks the planned paths of the LLB routines (LLB_resonance_v1..v3, LLB_cheering_v1..v3) without
// robots, with 8 robots (the resonance ones also with made-up heart rates: very high, very low, noisy,
// dropping out). Reports the length (must be LLBPlan.TotalSecs), the closest two robots come (34 at
// least: the line is 36 apart) and the closest to a robot turning on the spot, top speed and
// acceleration, distance from the mat edge, how near the participant's edge they get (the messenger's
// visit in the introduction aside), that robots turning on the spot do not drift, that robots in a lane
// only move along it, and that every robot starts on its line spot and ends at rest on it. With
// -out <folder> it also writes each plan as a CSV (t, then x, y per robot). The same check runs outside
// Unity in tools/llb-sim (see its README).
//   Unity -batchmode -quit -projectPath <swarm> -executeMethod LLBCheck.Run [-out <folder>] -logFile <log>
// Or from the menu: Tools > LLB > Check routines.
public static class LLBCheck
{
    const float Dt = 0.01f;

    [MenuItem("Tools/LLB/Check routines")]
    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        int at = Array.IndexOf(args, "-out");
        string outDir = at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return new Type[0]; } })
            .Where(t => t.Name.StartsWith("LLB_") && typeof(SwarmRoutine).IsAssignableFrom(t)).OrderBy(t => t.Name).ToList();
        var report = new StringBuilder("\nLLBCHECK results\n");
        bool allOk = true;
        foreach (var rt in types)
        {
            var routine = (SwarmRoutine)Activator.CreateInstance(rt);
            var pt = rt.GetNestedType("Plan");
            report.AppendFormat("{0} [{1}]\n", rt.Name, routine.Code);
            const int n = 8;
            string[] hearts = { "no sensor", "140 bpm", "50 bpm", "noisy", "drops out" };
            for (int h = 0; h < hearts.Length; h++)
            {
                if (h > 0 && !rt.Name.Contains("resonance")) { continue; }
                LLB.UnclearWays = 0;
                var start = LLB.OwnSpots(n);
                var plan = (LLBPlan)Activator.CreateInstance(pt, new object[] { start });
                plan.Heart = MadeUpHeart(h);
                bool ok;
                string line = Check(plan, start, out ok);
                if (LLB.UnclearWays > 0) { ok = false; line = "NO CLEAR WAY | " + line; }
                allOk &= ok;
                report.AppendFormat("  {0}: {1}{2}\n", hearts[h], ok ? "" : "!! ", line);
                if (outDir != null && h == 0) { File.WriteAllText(Path.Combine(outDir, rt.Name + ".csv"), Csv(plan, n)); }
            }
        }
        report.Append(allOk ? "All LLB plans pass.\n" : "Some LLB plans need attention (!!).\n");
        Debug.Log(report.ToString());
    }

    class FakeHeart : LLB.IHeart
    {
        readonly Func<float, float> f;
        public FakeHeart(Func<float, float> f) { this.f = f; }
        public float Bpm(float t) { return f(t); }
    }

    static LLB.IHeart MadeUpHeart(int k)
    {
        switch (k)
        {
            case 1: return new FakeHeart(t => 140f);
            case 2: return new FakeHeart(t => 50f);
            case 3: return new FakeHeart(t => 80f + 30f * Mathf.Sin(t * 1.7f) + (Mathf.Repeat(t, 7f) < 1.5f ? 50f : 0f));
            case 4: return new FakeHeart(t => Mathf.Repeat(t, 20f) < 9f ? 110f : float.NaN);
            default: return null;
        }
    }

    static string Check(LLBPlan plan, Vector2[] start, out bool ok)
    {
        int n = start.Length;
        float minD = float.MaxValue, minSpin = float.MaxValue, top = 0f, acc = 0f, laneAcc = 0f, edge = float.MaxValue, maxY = 0f, drift = 0f, side = 0f;
        int steps = Mathf.RoundToInt(plan.Total / Dt);
        var p = new Vector2[n]; var prev = new Vector2[n]; var prev2 = new Vector2[n]; var spinning = new bool[n];
        for (int k = 0; k <= steps; k++)
        {
            float t = k * Dt;
            for (int i = 0; i < n; i++)
            {
                p[i] = plan.Position(i, t);
                float s, f; spinning[i] = plan.InPlace(i, t, out s, out f) && Mathf.Abs(s) > 0.5f;
            }
            for (int i = 0; i < n; i++)
            {
                edge = Mathf.Min(edge, Mathf.Min(Mathf.Min(p[i].x - Swarm.MatMin, Swarm.MatMax - p[i].x), Mathf.Min(p[i].y - Swarm.MatMin, Swarm.MatMax - p[i].y)));
                if (!(i == LLB.MessengerSlot(n) && t < 25f)) { maxY = Mathf.Max(maxY, p[i].y); }
                for (int j = i + 1; j < n; j++)
                {
                    float d = Vector2.Distance(p[i], p[j]);
                    minD = Mathf.Min(minD, d);
                    if (spinning[i] || spinning[j]) { minSpin = Mathf.Min(minSpin, d); }
                }
                if (k >= 1)
                {
                    Vector2 v = (p[i] - prev[i]) / Dt;
                    top = Mathf.Max(top, v.magnitude);
                    float s, f;
                    if (t > 1.6f && plan.InPlace(i, t, out s, out f)) { drift = Mathf.Max(drift, v.magnitude); }
                    if (plan.InLane(i, t))
                    {
                        float deg = plan.Heading(i, t); if (float.IsNaN(deg)) { deg = 90f; }
                        Vector2 along = LLB.Dir(deg);
                        side = Mathf.Max(side, Mathf.Abs(v.x * -along.y + v.y * along.x));
                    }
                }
                if (k >= 2)
                {
                    float a = (p[i] - 2f * prev[i] + prev2[i]).magnitude / (Dt * Dt);
                    if (plan.InLane(i, t)) { laneAcc = Mathf.Max(laneAcc, a); } else { acc = Mathf.Max(acc, a); }
                }
                prev2[i] = prev[i]; prev[i] = p[i];
            }
        }
        float endErr = 0f, startErr = 0f;
        for (int i = 0; i < n; i++)
        {
            endErr = Mathf.Max(endErr, (plan.Position(i, plan.Total) - plan.LineSpot(i)).magnitude);
            startErr = Mathf.Max(startErr, (plan.Position(i, 0f) - start[i]).magnitude);
        }
        ok = Mathf.Abs(plan.Total - LLBPlan.TotalSecs) < 0.01f && minD >= 34f && (minSpin >= 35f || minSpin == float.MaxValue) && top <= 170f && acc <= 900f && laneAcc <= 2600f
             && edge >= 20f && maxY <= 412f && drift < 2f && side < 6f && endErr < 1f && startErr < 1f;
        return string.Format("{0:F1} s | closest {1:F1} | spinning closest {2} | top speed {3:F0} | top accel {4:F0} (in lanes {5:F0}) | edge {6:F0} | lowest y {7:F0} | in-place drift {8:F1} | lane sideways {9:F1} | start/end {10:F2}/{11:F2}",
            plan.Total, minD, minSpin == float.MaxValue ? "-" : minSpin.ToString("F1"), top, acc, laneAcc, edge, maxY, drift, side, startErr, endErr);
    }

    static string Csv(LLBPlan plan, int n)
    {
        var csv = new StringBuilder();
        for (float t = 0f; t <= plan.Total + 0.001f; t += 0.1f)
        {
            csv.Append(t.ToString("F2"));
            for (int i = 0; i < n; i++) { var q = plan.Position(i, t); csv.AppendFormat(",{0:F2},{1:F2}", q.x, q.y); }
            csv.Append("\n");
        }
        return csv.ToString();
    }
}
