using System.Reflection;
using UnityEngine;
using toio;

// Sets the robots' lamps while any routine runs (v0, v1, v2, LLB - every SwarmRoutine), on real and
// simulated robots: off, all white, or each robot its own colour. Choose with the small "Lights"
// button in the bottom-right corner of either screen (it cycles Off > White > Colours), or in code
// with RoutineLeds.Mode; the choice is remembered between sessions.
// It installs itself when the app starts and watches the two screens that run routines
// (RoutineRunner on the Control screen, SimRoutinePlayer in the simulator), so no routine or
// screen has to call it. Outside routines the lamps are left alone, so the Control screen's
// interactions that use them still work.
public class RoutineLeds : MonoBehaviour
{
    public enum Lights { Off, White, Colours }

    const string ModeKey = "RoutineLights";
    const float RepeatSecs = 3f;   // repeated in case a lamp was switched on again or a command was lost

    // One colour a robot (by its place in the routine's list of robots; the LLB routines order it by line slot)
    static readonly Color32[] Palette =
    {
        new Color32(230, 60, 80, 255), new Color32(255, 160, 30, 255), new Color32(0, 170, 190, 255), new Color32(60, 110, 230, 255),
        new Color32(200, 50, 160, 255), new Color32(80, 200, 120, 255), new Color32(150, 90, 230, 255), new Color32(255, 110, 60, 255),
    };

    public static Lights Mode
    {
        get { return (Lights)Mathf.Clamp(PlayerPrefs.GetInt(ModeKey, (int)Lights.Off), 0, 2); }
        set { PlayerPrefs.SetInt(ModeKey, (int)value); PlayerPrefs.Save(); changed = true; }
    }
    static bool changed;

    float next;
    GUIStyle buttonStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var holder = new GameObject("RoutineLeds");
        DontDestroyOnLoad(holder);
        holder.AddComponent<RoutineLeds>();
    }

    void Update()
    {
        if (changed) { changed = false; next = 0f; }
        if (Time.unscaledTime < next) { return; }
        var ctx = RunningContext(FindObjectOfType<RoutineRunner>(), "runningContext") ?? RunningContext(FindObjectOfType<SimRoutinePlayer>(), "ctx");
        if (ctx == null || ctx.Cubes == null) { next = Time.unscaledTime + 0.2f; return; }   // look again soon: catch a routine as it starts
        next = Time.unscaledTime + RepeatSecs;
        var mode = Mode;
        for (int i = 0; i < ctx.Cubes.Length; i++)
        {
            var cube = ctx.Cubes[i];
            if (cube == null || !cube.isConnected) { continue; }
            if (mode == Lights.Off) { cube.TurnLedOff(Cube.ORDER_TYPE.Weak); continue; }
            Color32 c = mode == Lights.White ? new Color32(255, 255, 255, 255) : Palette[i % Palette.Length];
            cube.TurnLedOn(c.r, c.g, c.b, 0, Cube.ORDER_TYPE.Weak);   // 0 = no time limit
        }
    }

    // The small button that switches the lights, bottom right
    void OnGUI()
    {
        if (buttonStyle == null) { buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14 }; }
        string label = "Lights: " + (Mode == Lights.Off ? "off" : Mode == Lights.White ? "white" : "colours");
        if (GUI.Button(new Rect(Screen.width - 166, Screen.height - 50, 150, 34), label, buttonStyle))
        {
            Mode = (Lights)(((int)Mode + 1) % 3);
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
