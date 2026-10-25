using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using toio;
using toio.Simulator;

// Simulator: runs routines on `robots` simulated cubes on the 3D mat. The "Real robots" button
// at the top switches to the Control screen, which connects real toio cubes and runs the same
// routines on them. In the Unity editor, Play opens this scene. Add ?play=<number> to the web build's URL to start
// that routine automatically.
public class SimRoutinePlayer : MonoBehaviour
{
    // How many simulated robots (2-8), remembered between runs; change it with - / + on screen
    const string RobotsKey = "SimulatorRobots";
    int robots;


    CubeManager cubeManager;
    Cube[] cubes;
    List<SwarmRoutine> routines = new List<SwarmRoutine>();
    Coroutine running;
    RoutineContext ctx;
    string status = "Connecting robots...";

    void Awake()
    {
        robots = Mathf.Clamp(PlayerPrefs.GetInt(RobotsKey, 8), 2, 8);
        // Simulated robots start in the shared single line (the same line every routine
        // starts in and the Closing ends in)
        var prefab = Resources.Load<GameObject>("Cube");
        foreach (var spot in Swarm.Line(robots))
        {
            Instantiate(prefab, Mat.MatCoord2UnityCoord(spot.x, spot.y), Quaternion.identity);
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
        else { ctx.RecordHome(); } // positions before this routine, for the closing
        activeRoutine = routine.Name;
        running = StartCoroutine(Run(routine));
    }

    IEnumerator Run(SwarmRoutine routine)
    {
        status = "Running: " + routine.Name;
        yield return routine.Run(ctx);
        status = "Finished: " + routine.Name;
        running = null;
    }

    // ---------- Screen: a panel on the left, same look as the Control screen ----------

    GUIStyle panelStyle, titleStyle, textStyle, buttonStyle, primaryStyle, activeStyle;
    string activeRoutine;

    void OnGUI()
    {
        if (panelStyle == null) { BuildStyles(); }
        float scale = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));

        FitViewBesideSidebar(scale);
        GUILayout.BeginArea(new Rect(16, 16, 300, 720 - 32), panelStyle);
        GUILayout.Label("Routines", titleStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Button("Simulated", activeStyle, GUILayout.Height(36));
        if (GUILayout.Button("Real robots", buttonStyle, GUILayout.Height(36))) { SceneSwitcher.OpenControl(); }
        GUILayout.EndHorizontal();
        GUILayout.Label(status, textStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Robots: " + robots, textStyle, GUILayout.Width(120));
        if (GUILayout.Button("-", buttonStyle, GUILayout.Width(44), GUILayout.Height(34))) { SetRobots(robots - 1); }
        if (GUILayout.Button("+", buttonStyle, GUILayout.Width(44), GUILayout.Height(34))) { SetRobots(robots + 1); }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);
        // Routines always listed; in real mode they need enough connected robots to start
        int connected = cubes == null ? 0 : cubes.Length;
        foreach (var routine in routines)
        {
            var style = running != null && activeRoutine == routine.Name ? activeStyle : buttonStyle;
            GUI.enabled = ctx != null; // greyed out until the simulated robots are connected
            if (GUILayout.Button(routine.Name, style, GUILayout.Height(40)) && ctx != null)
            {
                if (connected < routine.MinRobots) { status = "Needs " + routine.MinRobots + " robots"; }
                else { Play(routine); }
            }
            GUI.enabled = true;
        }
        GUILayout.Space(8);
        if (running != null && GUILayout.Button("Stop", primaryStyle, GUILayout.Height(40)))
        {
            StopCoroutine(running); running = null; Swarm.StopAll(ctx); status = "Stopped";
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndArea();
    }

    // The 3D view uses only the area right of the sidebar. A background camera repaints the
    // whole window first, so nothing stale is left under the sidebar.
    Camera background;
    void FitViewBesideSidebar(float scale)
    {
        var view = Camera.main;
        if (view == null || Event.current.type != EventType.Repaint) { return; }
        if (background == null)
        {
            background = new GameObject("Background").AddComponent<Camera>();
            background.depth = view.depth - 1;
            background.clearFlags = CameraClearFlags.SolidColor;
            background.backgroundColor = new Color(0.95f, 0.96f, 0.98f);
            background.cullingMask = 0;
        }
        float left = (16 + 300 + 16) * scale / Screen.width;
        view.rect = new Rect(left, 0, 1 - left, 1);
    }

    // Restart the simulator with a different number of robots
    void SetRobots(int count)
    {
        count = Mathf.Clamp(count, 2, 8);
        if (count == robots) { return; }
        PlayerPrefs.SetInt(RobotsKey, count);
        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneSwitcher.SimulatorScene);
    }

    void BuildStyles()
    {
        var ink = new Color(0.06f, 0.14f, 0.25f);
        var accent = new Color(0.12f, 0.37f, 0.84f);
        panelStyle = new GUIStyle { normal = { background = Solid(Color.white) }, padding = new RectOffset(16, 16, 14, 14) };
        titleStyle = new GUIStyle { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = ink }, margin = new RectOffset(0, 0, 0, 6) };
        textStyle = new GUIStyle { fontSize = 15, wordWrap = true, normal = { textColor = new Color(0.35f, 0.42f, 0.52f) }, margin = new RectOffset(0, 0, 2, 4) };
        buttonStyle = new GUIStyle
        {
            fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
            normal = { background = Solid(Color.white), textColor = accent },
            hover = { background = Solid(new Color(0.9f, 0.93f, 1f)), textColor = accent },
            active = { background = Solid(new Color(0.8f, 0.85f, 0.95f)), textColor = accent },
            border = new RectOffset(2, 2, 2, 2), margin = new RectOffset(0, 0, 4, 4)
        };
        buttonStyle.normal.background = Outlined(accent);
        activeStyle = new GUIStyle(buttonStyle) { normal = { background = Solid(accent), textColor = Color.white } };
        primaryStyle = new GUIStyle(buttonStyle) { normal = { background = Solid(new Color(0.95f, 0.79f, 0.30f)), textColor = ink } };
    }

    static Texture2D Solid(Color c)
    {
        var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t;
    }

    // White square with a 1px border, stretched by the style's border setting
    static Texture2D Outlined(Color border)
    {
        var t = new Texture2D(5, 5);
        for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++)
            t.SetPixel(x, y, x == 0 || y == 0 || x == 4 || y == 4 ? border : Color.white);
        t.Apply();
        return t;
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
