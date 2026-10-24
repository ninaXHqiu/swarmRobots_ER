using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

// Adds a "Routines" panel to the SingleCubeInteraction scene when it loads:
// a list of every SwarmRoutine in the project, and Play / Stop.
// Built in code so the scene file doesn't need hand editing.
public class RoutinePanel : MonoBehaviour
{
    // Panel placement on the canvas (top-right corner, in canvas units)
    static readonly Vector2 PanelSize = new Vector2(260, 170);
    static readonly Vector2 PanelOffset = new Vector2(-10, -10);

    SingleCubeInteractions app;
    List<SwarmRoutine> routines = new List<SwarmRoutine>();
    Dropdown routineDropdown;
    Text playLabel;
    Text status;
    Coroutine running;
    RoutineContext runningContext;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateOnLoad()
    {
        var app = FindObjectOfType<SingleCubeInteractions>();
        if (app == null) { return; }
        var panel = new GameObject("RoutinePanel").AddComponent<RoutinePanel>();
        panel.app = app;
    }

    void Start()
    {
        routines = FindRoutines();
        var connectText = GameObject.Find("ConnectText");
        Canvas canvas = connectText != null ? connectText.GetComponentInParent<Canvas>().rootCanvas : FindObjectOfType<Canvas>();
        BuildUi(canvas.transform);
    }

    // Every non-abstract SwarmRoutine with a no-argument constructor, sorted by name
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

    void BuildUi(Transform canvas)
    {
        var resources = new DefaultControls.Resources();

        var panel = DefaultControls.CreatePanel(resources);
        panel.name = "RoutinePanel";
        panel.transform.SetParent(canvas, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
        rect.sizeDelta = PanelSize;
        rect.anchoredPosition = PanelOffset;
        var panelImage = panel.GetComponent<Image>();
        panelImage.sprite = null;
        panelImage.color = MockupTheme.Panel;
        panel.AddComponent<Outline>().effectColor = MockupTheme.Border;
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 6;
        layout.childControlHeight = true; layout.childControlWidth = true;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;

        AddText(panel.transform, "Routines", 18, FontStyle.Bold);
        AddText(panel.transform, "Routine", 13, FontStyle.Normal);
        var names = routines.Select(r => r.Name + "  (" + r.MinRobots + "+)").ToList();
        if (names.Count == 0) { names.Add("No routines found"); }
        routineDropdown = AddDropdown(panel.transform, resources, names);

        var play = DefaultControls.CreateButton(resources);
        play.transform.SetParent(panel.transform, false);
        play.AddComponent<LayoutElement>().preferredHeight = 34;
        var playImage = play.GetComponent<Image>();
        playImage.sprite = null;
        playImage.color = MockupTheme.Accent;
        playLabel = play.GetComponentInChildren<Text>();
        playLabel.text = "Play";
        playLabel.color = Color.white;
        playLabel.fontStyle = FontStyle.Bold;
        play.GetComponent<Button>().onClick.AddListener(TogglePlay);

        status = AddText(panel.transform, "Connect robots, then pick a routine", 12, FontStyle.Normal);
    }

    Text AddText(Transform parent, string content, int size, FontStyle style)
    {
        var go = DefaultControls.CreateText(new DefaultControls.Resources());
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = MockupTheme.Ink;
        go.AddComponent<LayoutElement>().preferredHeight = size + 6;
        return text;
    }

    Dropdown AddDropdown(Transform parent, DefaultControls.Resources resources, List<string> options)
    {
        var go = DefaultControls.CreateDropdown(resources);
        go.transform.SetParent(parent, false);
        go.AddComponent<LayoutElement>().preferredHeight = 30;
        foreach (var image in go.GetComponentsInChildren<Image>(true))
        {
            image.sprite = null;
            image.color = Color.white;
        }
        go.AddComponent<Outline>().effectColor = MockupTheme.Accent;
        foreach (var text in go.GetComponentsInChildren<Text>(true)) { text.color = MockupTheme.Ink; }
        var dropdown = go.GetComponent<Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        return dropdown;
    }

    void TogglePlay()
    {
        if (running != null) { StopRoutine("Stopped"); return; }
        if (routines.Count == 0) { status.text = "Add a routine file under Assets/Routines"; return; }

        var routine = routines[routineDropdown.value];
        var cubes = app.ConnectedRobots;
        int count = cubes == null ? 0 : cubes.Length;
        if (count < routine.MinRobots)
        {
            status.text = "Needs " + routine.MinRobots + " robots, " + count + " connected";
            return;
        }

        app.StopInteractions();
        runningContext = new RoutineContext
        {
            Cubes = cubes,
            CubeManager = app.Manager
        };
        running = StartCoroutine(RunRoutine(routine, runningContext));
    }

    IEnumerator RunRoutine(SwarmRoutine routine, RoutineContext ctx)
    {
        playLabel.text = "Stop";
        status.text = "Running: " + routine.Name;
        yield return routine.Run(ctx);
        running = null;
        playLabel.text = "Play";
        status.text = "Finished: " + routine.Name;
    }

    void StopRoutine(string message)
    {
        StopCoroutine(running);
        running = null;
        if (runningContext != null) { Swarm.StopAll(runningContext); }
        playLabel.text = "Play";
        status.text = message;
    }
}
