using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Checks the planned paths of the v2 test routines without robots (see V2Check for the four
// performances): closest two robots come, top speed, distance kept from the mat edge.
// With -out <folder> it also writes each plan as a CSV for previews.
//   Unity -batchmode -quit -projectPath <swarm> -executeMethod V2TestCheck.Run -out <folder> -logFile <log>
public static class V2TestCheck
{
    const float Dt = 0.02f;

    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        int at = Array.IndexOf(args, "-out");
        string outDir = at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
        var report = new StringBuilder("\nV2TESTCHECK results\n");
        foreach (bool wideLine in new[] { true, false })
        {
            var start = (wideLine ? LineUp.Spots(8) : Swarm.Line(8)).Select(s => (Vector2)s).ToArray();
            var plan = new V2MazeBreathing.Plan(start);
            string name = "Maze_" + (wideLine ? "fromLineUp" : "fromSpawnLine");
            report.AppendFormat("{0}: {1} | approach {5} | switch {2:F1} ring complete {3:F1} breathing from {4:F1}\n", name, Check(plan, start.Length), plan.SwitchAt, plan.RingAt, plan.BreatheFrom, plan.Approach);
            if (outDir != null) { File.WriteAllText(Path.Combine(outDir, name + ".csv"), Csv(plan, start.Length)); }
        }
        Debug.Log(report.ToString());
    }

    static string Check(V2Show.IPlan plan, int n)
    {
        float minDist = float.MaxValue, maxSpeed = 0f, edge = float.MaxValue, jump = 0f;
        string minAt = "", jumpAt = "";
        var prev = new Vector2[n]; var p = new Vector2[n];
        for (int k = 0; k * Dt <= plan.Total; k++)
        {
            float t = k * Dt;
            for (int i = 0; i < n; i++) { p[i] = plan.Position(i, t); }
            for (int i = 0; i < n; i++)
            {
                edge = Mathf.Min(edge, Mathf.Min(Mathf.Min(p[i].x - Swarm.MatMin, Swarm.MatMax - p[i].x), Mathf.Min(p[i].y - Swarm.MatMin, Swarm.MatMax - p[i].y)));
                for (int j = i + 1; j < n; j++)
                {
                    float d = Vector2.Distance(p[i], p[j]);
                    if (d < minDist && t > 0.5f) { minDist = d; minAt = string.Format("t={0:F1} robots {1},{2}", t, i, j); }
                }
                if (k >= 1)
                {
                    float v = (p[i] - prev[i]).magnitude / Dt;
                    maxSpeed = Mathf.Max(maxSpeed, v);
                    if (v > jump) { jump = v; jumpAt = t.ToString("F1"); }
                }
                prev[i] = p[i];
            }
        }
        return string.Format("{0:F1} s | closest {1:F1} ({2}) | top speed {3:F0} (t={4}) | from mat edge {5:F1}", plan.Total, minDist, minAt, maxSpeed, jumpAt, edge);
    }

    static string Csv(V2Show.IPlan plan, int n)
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
