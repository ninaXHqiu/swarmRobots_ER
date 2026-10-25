using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

// Runs a SwarmRoutine on the connected robots. Lives on sceneUI in the SingleCubeInteraction
// scene. The Routines card shows one button per routine (built here, so new routine files
// appear automatically); clicking one runs it, and the Stop button shows while it runs.
public class RoutineRunner : MonoBehaviour
{
    public SingleCubeInteractions app;
    public Dropdown routineDropdown;
    public Text playLabel;
    public Image playImage;
    public Text status;

    List<SwarmRoutine> routines = new List<SwarmRoutine>();
    List<Image> routineButtons = new List<Image>();
    Coroutine running;
    RoutineContext runningContext;
    int activeIndex = -1;

    static readonly Color Accent = new Color(0.12f, 0.37f, 0.84f);
    static readonly Color Ink = new Color(0.06f, 0.14f, 0.25f);

    void Start()
    {
        routines = FindRoutines();

        // Layout inside the Routines card: Stop (always visible) at the top, then a scrolling
        // list with one button per routine, then the status line
        var card = routineDropdown.transform.parent;
        var top = ((RectTransform)routineDropdown.transform).anchoredPosition;
        routineDropdown.gameObject.SetActive(false);
        var font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var stopRect = (RectTransform)playImage.transform;
        stopRect.anchoredPosition = new Vector2(top.x, top.y);
        stopRect.sizeDelta = new Vector2(200, 38);

        var list = new GameObject("RoutineList", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        list.transform.SetParent(card, false);
        var listRect = (RectTransform)list.transform;
        listRect.anchorMin = listRect.anchorMax = listRect.pivot = new Vector2(0.5f, 1f);
        listRect.anchoredPosition = new Vector2(top.x, top.y - 30);
        listRect.sizeDelta = new Vector2(210, 260);
        list.GetComponent<Image>().color = new Color(1, 1, 1, 0); // invisible, but catches scroll/drag

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(list.transform, false);
        var contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = new Vector2(1, 1); contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero; contentRect.sizeDelta = Vector2.zero;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 6; layout.padding = new RectOffset(5, 5, 4, 4);
        layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = list.GetComponent<ScrollRect>();
        scroll.content = contentRect; scroll.viewport = listRect;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 25;

        for (int k = 0; k < routines.Count; k++)
        {
            int index = k;
            var go = DefaultControls.CreateButton(new DefaultControls.Resources());
            go.name = "Routine " + routines[k].Name;
            go.transform.SetParent(content.transform, false);
            go.AddComponent<LayoutElement>().preferredHeight = 38;
            var image = go.GetComponent<Image>();
            image.sprite = null; image.color = Color.white;
            go.AddComponent<Outline>().effectColor = Accent;
            var label = go.GetComponentInChildren<Text>();
            label.font = font; label.text = routines[k].Name; label.fontSize = 15; label.fontStyle = FontStyle.Bold; label.color = Accent;
            go.GetComponent<Button>().onClick.AddListener(() => PlayRoutine(index));
            routineButtons.Add(image);
        }

        var statusRect = (RectTransform)status.transform;
        statusRect.anchoredPosition = new Vector2(top.x, listRect.anchoredPosition.y - listRect.sizeDelta.y - 30);
        if (routines.Count == 0) { status.text = "Add a routine file under Assets/Routines"; }
        SetPlaying(false);
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

    // Wired to the Stop robots button (always visible): ends the routine, turns off the
    // built-in interactions and halts every connected robot
    public void TogglePlay()
    {
        if (running != null) { StopRoutine("Stopped"); }
        app.StopInteractions();
        var cubes = app.ConnectedRobots;
        if (cubes != null) { foreach (var cube in cubes) { cube.Move(0, 0, 0, toio.Cube.ORDER_TYPE.Strong); } }
        status.text = "All robots stopped";
    }

    void PlayRoutine(int index)
    {
        if (running != null) { StopRoutine("Stopped"); }
        var routine = routines[index];
        var cubes = app.ConnectedRobots;
        int count = cubes == null ? 0 : cubes.Length;
        if (count < routine.MinRobots)
        {
            status.text = "Needs " + routine.MinRobots + " robots, " + count + " connected";
            return;
        }
        app.StopInteractions();
        runningContext = new RoutineContext { Cubes = cubes, Navigators = app.Manager.navigators.ToArray(), CubeManager = app.Manager };
        runningContext.RecordHome(); // positions before this routine, for the closing
        activeIndex = index;
        running = StartCoroutine(Run(routine, runningContext));
    }

    IEnumerator Run(SwarmRoutine routine, RoutineContext ctx)
    {
        SetPlaying(true);
        status.text = "Running: " + routine.Name;
        yield return routine.Run(ctx);
        running = null;
        SetPlaying(false);
        status.text = "Finished: " + routine.Name;
    }

    void StopRoutine(string message)
    {
        StopCoroutine(running);
        running = null;
        if (runningContext != null) { Swarm.StopAll(runningContext); }
        SetPlaying(false);
        status.text = message;
    }

    void SetPlaying(bool playing)
    {
        playLabel.text = "Stop robots";
        playImage.color = new Color(0.95f, 0.79f, 0.30f);
        playLabel.color = Ink;
        for (int k = 0; k < routineButtons.Count; k++)
        {
            bool active = playing && k == activeIndex;
            routineButtons[k].color = active ? Accent : Color.white;
            routineButtons[k].GetComponentInChildren<Text>().color = active ? Color.white : Accent;
        }
    }
}
