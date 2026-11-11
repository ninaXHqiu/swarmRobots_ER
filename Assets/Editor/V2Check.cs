using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Checks the planned paths of the v2 performances without robots: every version with 8 robots,
// and the natural length with 2 to 8. Reports the closest two robots come, top speed and
// acceleration (jumps show up as huge accelerations), distance kept from the mat edge, and that
// every robot starts where it stands and ends at rest on its line spot. With -out <folder> it
// also writes each 8 robot plan as a CSV (time, then x, y per robot, every 0.1 s) for previews.
//   Unity -batchmode -quit -projectPath <swarm> -executeMethod V2Check.Run -out <folder> -logFile <log>
public static class V2Check
{
    const float Dt = 0.01f;

    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        int at = Array.IndexOf(args, "-out");
        string outDir = at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        var makers = new (string, Func<Vector2[], float, V2Show.IPlan>)[]
        {
            ("Vibration", (s, m) => V2Vibration.For(s, m)),
            ("Heartbeat", (s, m) => V2Heartbeat.For(s, m)),
            ("Kaleidoscope", (s, m) => V2Kaleidoscope.For(s, m)),
            ("SocialDance", (s, m) => V2SocialDance.For(s, m)),
        };
        var report = new StringBuilder("\nV2CHECK results\n");
        foreach (var (name, make) in makers)
        {
            foreach (float minutes in new[] { 0f, 3f, 5f })
            {
                for (int n = minutes == 0f ? 2 : 8; n <= 8; n++)
                {
                    var start = new Vector2[n];
                    var line = LineUp.Spots(n);
                    for (int i = 0; i < n; i++) { start[i] = line[i]; }
                    start[V2Show.MessengerSlot(n)] = V2Show.TalkSpot;
                    var plan = make(start, minutes);
                    report.AppendFormat("{0} ({1}) n={2}: {3}\n", name, V2Show.Label(minutes), n, Check(plan, start));
                    if (outDir != null && n == 8) { File.WriteAllText(Path.Combine(outDir, name + "_" + (int)minutes + ".csv"), Csv(plan, n)); }
                }
            }
        }
        Debug.Log(report.ToString());
    }

    static string Check(V2Show.IPlan plan, Vector2[] start)
    {
        int n = start.Length;
        float minDist = float.MaxValue, maxSpeed = 0f, maxAcc = 0f, edge = float.MaxValue, endErr = 0f;
        string minAt = "", accAt = "", edgeAt = "";
        int steps = Mathf.RoundToInt(plan.Total / Dt);
        var prev = new Vector2[n]; var prev2 = new Vector2[n]; var p = new Vector2[n];
        for (int k = 0; k <= steps; k++)
        {
            float t = k * Dt;
            for (int i = 0; i < n; i++) { p[i] = plan.Position(i, t); }
            for (int i = 0; i < n; i++)
            {
                float e = Mathf.Min(Mathf.Min(p[i].x - Swarm.MatMin, Swarm.MatMax - p[i].x), Mathf.Min(p[i].y - Swarm.MatMin, Swarm.MatMax - p[i].y));
                if (e < edge) { edge = e; edgeAt = t.ToString("F1"); }
                for (int j = i + 1; j < n; j++)
                {
                    float d = Vector2.Distance(p[i], p[j]);
                    if (d < minDist) { minDist = d; minAt = string.Format("t={0:F1} robots {1},{2}", t, i, j); }
                }
                if (k >= 1) { maxSpeed = Mathf.Max(maxSpeed, (p[i] - prev[i]).magnitude / Dt); }
                if (k >= 2)
                {
                    float a = (p[i] - 2f * prev[i] + prev2[i]).magnitude / (Dt * Dt);
                    if (a > maxAcc) { maxAcc = a; accAt = t.ToString("F1"); }
                }
                prev2[i] = prev[i]; prev[i] = p[i];
            }
        }
        for (int i = 0; i < n; i++)
        {
            endErr = Mathf.Max(endErr, (plan.Position(i, plan.Total) - plan.LineSpot(i)).magnitude);
            endErr = Mathf.Max(endErr, (plan.Position(i, plan.Total) - plan.Position(i, plan.Total - Dt)).magnitude / Dt);
            endErr = Mathf.Max(endErr, (plan.Position(i, 0f) - start[i]).magnitude);
        }
        return string.Format("{0:F1} s | closest {1:F1} ({2}) | top speed {3:F0} | top accel {4:F0} (t={5}) | from mat edge {6:F1} (t={7}) | start/end error {8:F2}",
            plan.Total, minDist, minAt, maxSpeed, maxAcc, accAt, edge, edgeAt, endErr);
    }

    static string Csv(V2Show.IPlan plan, int n)
    {
        var csv = new StringBuilder();
        for (float t = 0f; t <= plan.Total + 0.001f; t += 0.1f)
        {
            csv.Append(t.ToString("F2"));
            for (int i = 0; i < n; i++) { var p = plan.Position(i, t); csv.AppendFormat(",{0:F2},{1:F2}", p.x, p.y); }
            csv.Append("\n");
        }
        return csv.ToString();
    }
}
