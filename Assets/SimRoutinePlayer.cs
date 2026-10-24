using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using toio;
using toio.Simulator;

// Watch routines on simulated robots: open Assets/development/RoutineSimulator.unity and press
// Play. Spawns `robots` simulated cubes on the mat, then lists every routine with a Play button.
// Add ?play=<number> to the web build's URL to start that routine automatically.
public class SimRoutinePlayer : MonoBehaviour
{
    public int robots = 8;

    CubeManager cubeManager;
    Cube[] cubes;
    List<SwarmRoutine> routines = new List<SwarmRoutine>();
    Coroutine running;
    RoutineContext ctx;
    string status = "Connecting simulated robots...";

    void Awake()
    {
        // Simulated robots in a row near the top of the mat
        var prefab = Resources.Load<GameObject>("Cube");
        for (int i = 0; i < robots; i++)
        {
            float x = robots == 1 ? 250 : Mathf.Lerp(90, 410, i / (float)(robots - 1));
            Instantiate(prefab, Mat.MatCoord2UnityCoord(x, 100), Quaternion.identity);
        }
    }

    async void Start()
    {
        routines = FindRoutines();
        cubeManager = new CubeManager(ConnectType.Simulator);
        cubes = await cubeManager.MultiConnect(robots);
        ctx = new RoutineContext { Cubes = cubes, Navigators = cubeManager.navigators.ToArray(), CubeManager = cubeManager };
        status = cubes.Length + " simulated robots ready";

        int auto = AutoPlayIndex();
        if (auto >= 0 && auto < routines.Count) { Play(routines[auto]); }
    }

    static int AutoPlayIndex()
    {
        string url = Application.absoluteURL ?? "";
        int at = url.IndexOf("play=", StringComparison.Ordinal);
        if (at < 0) { return -1; }
        int value;
        return int.TryParse(url.Substring(at + 5).Split('&')[0], out value) ? value : -1;
    }

    void Play(SwarmRoutine routine)
    {
        if (running != null) { StopCoroutine(running); Swarm.StopAll(ctx); }
        running = StartCoroutine(Run(routine));
    }

    // Logs every robot's mat position every 2 s (browser console / Unity Console) to check routines
    IEnumerator LogPositions()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            if (cubes == null) { continue; }
            var sb = new System.Text.StringBuilder("[sim] t=" + Time.time.ToString("0") + " ");
            foreach (var c in cubes) { sb.Append("(" + c.x + "," + c.y + ") "); }
            Debug.Log(sb.ToString());
        }
    }

    IEnumerator Run(SwarmRoutine routine)
    {
        StartCoroutine(LogPositions());
        status = "Running: " + routine.Name;
        yield return routine.Run(ctx);
        status = "Finished: " + routine.Name;
        running = null;
    }

    void OnGUI()
    {
        GUI.skin.label.fontSize = 18;
        GUI.skin.button.fontSize = 18;
        GUILayout.BeginArea(new Rect(16, 16, 320, Screen.height - 32));
        GUILayout.Label(status);
        if (ctx != null)
        {
            foreach (var routine in routines)
            {
                if (cubes.Length < routine.MinRobots) { continue; }
                if (GUILayout.Button(routine.Name, GUILayout.Height(40))) { Play(routine); }
            }
            if (running != null && GUILayout.Button("Stop", GUILayout.Height(40)))
            {
                StopCoroutine(running); running = null; Swarm.StopAll(ctx); status = "Stopped";
            }
        }
        GUILayout.EndArea();
    }

    static List<SwarmRoutine> FindRoutines()
    {
        var found = new List<SwarmRoutine>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
            foreach (var type in types)
            {
                if (!typeof(SwarmRoutine).IsAssignableFrom(type) || type.IsAbstract) { continue; }
                if (type.GetConstructor(Type.EmptyTypes) == null) { continue; }
                found.Add((SwarmRoutine)Activator.CreateInstance(type));
            }
        }
        return found.OrderBy(r => r.Name).ToList();
    }
}
