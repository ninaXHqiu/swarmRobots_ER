using System.Runtime.InteropServices;
using UnityEngine;

// The participant's physical signals, for routines to react to and for the recording to line up with.
// In the web build the values come from the page's Physio object (Shimmer sensor or the simulated sensor,
// connected from the panel under the Control screen). Everywhere else (the editor, headless tests) there is
// no sensor, and the "...Or" methods give a steady stand-in so every routine still runs.
public static class Physio
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern float PhysioHr();
    [DllImport("__Internal")] static extern float PhysioEda();
    [DllImport("__Internal")] static extern float PhysioEdaPhasic();
    [DllImport("__Internal")] static extern int PhysioBeatCount();
    [DllImport("__Internal")] static extern float PhysioSecondsSinceBeat();
    [DllImport("__Internal")] static extern int PhysioConnected();
    [DllImport("__Internal")] static extern void PhysioMarker(string text);
    [DllImport("__Internal")] static extern void PhysioSection(string name);
    [DllImport("__Internal")] static extern void PhysioBeginRoutine(string code, string name);
    [DllImport("__Internal")] static extern void PhysioEndRoutine();
    public static bool Connected { get { return PhysioConnected() == 1; } }
    public static float HeartRate { get { return PhysioHr(); } }                      // beats per minute, -1 when there is none
    public static float Eda { get { return PhysioEda(); } }                           // skin conductance in microsiemens, -1 when there is none
    public static float EdaPhasic { get { return PhysioEdaPhasic(); } }               // the quick part of it (positive = rising)
    public static int BeatCount { get { return PhysioBeatCount(); } }
    static float SecondsSinceBeat { get { return PhysioSecondsSinceBeat(); } }
    public static void Marker(string text) { PhysioMarker(text); }
    public static void Section(string name) { PhysioSection(name); }
    public static void BeginRoutine(string code, string name) { PhysioBeginRoutine(code ?? "", name ?? ""); }
    public static void EndRoutine() { PhysioEndRoutine(); }
#else
    public static bool Connected { get { return false; } }
    public static float HeartRate { get { return -1f; } }
    public static float Eda { get { return -1f; } }
    public static float EdaPhasic { get { return 0f; } }
    public static int BeatCount { get { return 0; } }
    static float SecondsSinceBeat { get { return -1f; } }
    public static void Marker(string text) { }
    public static void Section(string name) { }
    public static void BeginRoutine(string code, string name) { }
    public static void EndRoutine() { }
#endif

    public static bool HasHeartRate { get { return HeartRate > 0f; } }

    // The heart rate, or `fallback` when there is no sensor or no beat yet
    public static float HeartRateOr(float fallback) { float h = HeartRate; return h > 0f ? h : fallback; }

    // The smoothed skin conductance in microsiemens, or `fallback` when there is no sensor or no reading yet
    public static float EdaOr(float fallback) { float e = Eda; return e > 0f ? e : fallback; }

    // Seconds since the participant's last heartbeat. With a sensor that is the real beat; without one it is a
    // metronome at `fallbackBpm` counted from `t` (the routine's own clock), so routines behave the same in tests.
    public static float SecondsSinceBeatOr(float t, float fallbackBpm)
    {
        float s = SecondsSinceBeat;
        if (s >= 0f && s < 5f) { return s; }
        return Mathf.Repeat(t, 60f / fallbackBpm);
    }
}
