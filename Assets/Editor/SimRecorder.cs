using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using toio;

// Records one routine running in the simulator scene, without a window, so its real motion
// (introduction, performance, ending) can be checked and previewed:
//   Unity -batchmode -projectPath <swarm> -executeMethod SimRecorder.Record
//         -routine "<routine name>" -robots 8 -out <file.csv> -logFile <log>
// Leave out -quit: Unity exits by itself when the routine has finished. Writes one line every
// 0.05 s: time, seconds since the performance started (-1 before), then x, y, angle per robot.
[InitializeOnLoad]
public static class SimRecorder
{
    const string Key = "SwarmSimRecorder.";
    const float Every = 0.05f;
    const float GiveUpSecs = 420f;

    static object player;
    static bool started;
    static float startTime, lastSample = -1f, finishedAt = -1f;
    static StringBuilder csv;

    static SimRecorder()
    {
        if (EditorPrefs.GetBool(Key + "Active", false)) { EditorApplication.update += Tick; }
    }

    public static void Record()
    {
        var args = Environment.GetCommandLineArgs();
        Func<string, string> arg = name => { int k = Array.IndexOf(args, name); return k >= 0 && k + 1 < args.Length ? args[k + 1] : null; };
        EditorPrefs.SetBool(Key + "Active", true);
        EditorPrefs.SetString(Key + "Routine", arg("-routine"));
        EditorPrefs.SetString(Key + "Out", arg("-out"));
        PlayerPrefs.SetInt("SimulatorRobots", int.Parse(arg("-robots") ?? "8"));
        PlayerPrefs.Save();
        EditorSceneManager.OpenScene("Assets/development/RoutineSimulator.unity");
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) { return; }
        if (Time.realtimeSinceStartup > GiveUpSecs) { Finish(1, "gave up"); return; }
        if (player == null) { player = UnityEngine.Object.FindObjectOfType<SimRoutinePlayer>(); return; }
        var type = player.GetType();
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var ctx = (RoutineContext)type.GetField("ctx", flags).GetValue(player);
        if (ctx == null) { return; }
        if (!started)
        {
            string name = EditorPrefs.GetString(Key + "Routine");
            var routines = (IList)type.GetField("routines", flags).GetValue(player);
            var routine = routines.Cast<SwarmRoutine>().FirstOrDefault(r => r.Name == name);
            if (routine == null) { Finish(1, "no routine named " + name); return; }
            csv = new StringBuilder("time,performance," + string.Join(",", Enumerable.Range(0, ctx.Count).Select(i => "x" + i + ",y" + i + ",a" + i)) + "\n");
            type.GetMethod("Play", flags).Invoke(player, new object[] { routine });
            Show.StartedAt = -1f;
            started = true; startTime = Time.time;
            return;
        }
        if (Time.time - lastSample >= Every)
        {
            lastSample = Time.time;
            float perf = Show.StartedAt < 0f ? -1f : Time.time - Show.StartedAt;
            csv.AppendFormat("{0:F3},{1:F3}", Time.time - startTime, perf);
            foreach (Cube c in ctx.Cubes) { csv.AppendFormat(",{0},{1},{2}", c.x, c.y, c.angle); }
            csv.Append("\n");
        }
        bool running = type.GetField("running", flags).GetValue(player) != null;
        if (!running && finishedAt < 0f) { finishedAt = Time.time; }
        if (finishedAt >= 0f && Time.time - finishedAt > 1f)
        {
            File.WriteAllText(EditorPrefs.GetString(Key + "Out"), csv.ToString());
            Finish(0, "recorded");
        }
    }

    static void Finish(int code, string message)
    {
        Debug.Log("SimRecorder: " + message);
        EditorPrefs.SetBool(Key + "Active", false);
        EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }
}
