using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

// Runs a SwarmRoutine on the connected robots. Lives on sceneUI in the SingleCubeInteraction
// scene; the Routines card's dropdown, Play button and status text are wired to it.
public class RoutineRunner : MonoBehaviour
{
    public SingleCubeInteractions app;
    public Dropdown routineDropdown;
    public Text playLabel;
    public Image playImage;
    public Text status;

    List<SwarmRoutine> routines = new List<SwarmRoutine>();
    Coroutine running;
    RoutineContext runningContext;

    void Start()
    {
        routines = FindRoutines();
        routineDropdown.ClearOptions();
        var names = routines.Select(r => r.Name + "  (" + r.MinRobots + "+)").ToList();
        if (names.Count == 0) { names.Add("No routines found"); }
        routineDropdown.AddOptions(names);
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

    // Wired to the Play button
    public void TogglePlay()
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
        runningContext = new RoutineContext { Cubes = cubes, CubeManager = app.Manager };
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
        playLabel.text = playing ? "Stop" : "Play";
        playImage.color = playing ? new Color(0.95f, 0.79f, 0.30f) : new Color(0.12f, 0.37f, 0.84f);
        playLabel.color = playing ? new Color(0.06f, 0.14f, 0.25f) : Color.white;
    }
}
