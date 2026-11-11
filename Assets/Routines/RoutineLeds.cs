using System.Reflection;
using UnityEngine;
using toio;

// Keeps the robots' lamps off while any routine runs (v0, v1, v2 - every SwarmRoutine), on real
// and simulated robots. toio lamps can be switched off (Cube.TurnLedOff), so no colour is needed.
// It installs itself when the app starts and watches the two screens that run routines
// (RoutineRunner on the Control screen, SimRoutinePlayer in the simulator), so no routine or
// screen has to call it. Outside routines the lamps are left alone, so the Control screen's
// interactions that use them still work.
public class RoutineLeds : MonoBehaviour
{
    const float RepeatSecs = 3f;   // repeated in case a lamp was switched on again or a command was lost

    float next;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var holder = new GameObject("RoutineLeds");
        DontDestroyOnLoad(holder);
        holder.AddComponent<RoutineLeds>();
    }

    void Update()
    {
        if (Time.unscaledTime < next) { return; }
        var ctx = RunningContext(FindObjectOfType<RoutineRunner>(), "runningContext") ?? RunningContext(FindObjectOfType<SimRoutinePlayer>(), "ctx");
        if (ctx == null || ctx.Cubes == null) { next = Time.unscaledTime + 0.2f; return; }   // look again soon: catch a routine as it starts
        next = Time.unscaledTime + RepeatSecs;
        foreach (var cube in ctx.Cubes)
        {
            if (cube != null && cube.isConnected) { cube.TurnLedOff(Cube.ORDER_TYPE.Weak); }
        }
    }

    // The robots of the routine a screen is running now, or null
    static RoutineContext RunningContext(MonoBehaviour screen, string contextField)
    {
        if (screen == null) { return null; }
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var running = screen.GetType().GetField("running", flags);
        if (running == null || running.GetValue(screen) == null) { return null; }
        var context = screen.GetType().GetField(contextField, flags);
        return context == null ? null : context.GetValue(screen) as RoutineContext;
    }
}
