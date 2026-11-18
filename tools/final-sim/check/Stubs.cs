// Stand-ins for the parts of Unity, the toio SDK and the project that the Final routines use, so the
// routines' code can be compiled and the plans tested with Mono outside Unity (tools/final-sim/check/run.sh).
// They copy the real signatures; they do not drive anything.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y); } }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator *(Vector2 a, float s) { return new Vector2(a.x * s, a.y * s); }
        public static Vector2 operator /(Vector2 a, float s) { return new Vector2(a.x / s, a.y / s); }
        public static float Distance(Vector2 a, Vector2 b) { return (a - b).magnitude; }
    }
    public struct Vector2Int
    {
        public int x, y;
        public static Vector2Int RoundToInt(Vector2 v) { return new Vector2Int { x = (int)Math.Round(v.x), y = (int)Math.Round(v.y) }; }
    }
    public static class Mathf
    {
        public const float Deg2Rad = (float)(Math.PI / 180.0), Rad2Deg = (float)(180.0 / Math.PI);
        public static float Clamp(float v, float a, float b) { return v < a ? a : v > b ? b : v; }
        public static int Max(int a, int b) { return Math.Max(a, b); }
        public static int Min(int a, int b) { return Math.Min(a, b); }
        public static float Abs(float v) { return Math.Abs(v); }
        public static float Sqrt(float v) { return (float)Math.Sqrt(v); }
        public static int FloorToInt(float v) { return (int)Math.Floor(v); }
        public static int RoundToInt(float v) { return (int)Math.Round(v); }
        public static float Repeat(float t, float l) { return t - (float)Math.Floor(t / l) * l; }
        public static float DeltaAngle(float a, float b) { float d = Repeat(b - a, 360f); if (d > 180f) { d -= 360f; } return d; }
    }
    public static class Debug { public static void LogWarning(object o) { Console.WriteLine("WARN " + o); } }
    public static class Time { public static float time; }
    public class TextAsset { public string text; }
    public static class Resources { public static T Load<T>(string path) where T : class { return null; } }
}

namespace toio
{
    public class Cube { public int x, y; public string addr, id; }
    public class CubeUnity : Cube { }
    public class CubeHandle
    {
        public static double lag = 0.130;
        public static double TireWidthDot { get { return 0.0266 * 410 / 0.56; } }
        public static double VDotOverU { get { return 2.06; } }
        public double radPred, xPred, yPred;
        public void Move(double translate, double rotate, int durationMs, bool border) { }
        public Movement Move2Target(double x, double y, double maxSpd = 70, double tolerance = 8) { return new Movement(); }
        public class Movement { public void Exec() { } }
    }
    public class CubeManager { public bool synced = true; }
    namespace Navigation { public class CubeNavigator { public CubeHandle handle = new CubeHandle(); } }
}

public class RoutineContext
{
    public toio.Cube[] Cubes;
    public toio.Navigation.CubeNavigator[] Navigators;
    public toio.CubeManager CubeManager = new toio.CubeManager();
    public int Count { get { return Cubes.Length; } }
}
public abstract class SwarmRoutine
{
    public abstract string Name { get; }
    public virtual string Code { get { return ""; } }
    public virtual string Note { get { return ""; } }
    public virtual int MinRobots { get { return 1; } }
    public abstract IEnumerator Run(RoutineContext ctx);
}
public static class Swarm
{
    public static int[] LeastTravel(UnityEngine.Vector2[] from, UnityEngine.Vector2[] to) { var r = new int[from.Length]; for (int i = 0; i < r.Length; i++) { r[i] = i; } return r; }
    public static IEnumerator MoveToAvoiding(RoutineContext ctx, UnityEngine.Vector2Int[] targets, int maxSpeed = 60, int timeoutMs = 15000) { yield break; }
    public static IEnumerator Wait(int ms) { yield break; }
    public static void StopAll(RoutineContext ctx) { }
}
public static class LLB
{
    public static UnityEngine.Vector2[] OwnSpots(int n) { var s = new UnityEngine.Vector2[n]; for (int i = 0; i < n; i++) { s[i] = new UnityEngine.Vector2(250f + (i - (n - 1) / 2f) * 36f, 352f); } return s; }
}
public static class Physio { public static void Marker(string text) { } public static void Section(string name) { } }
public static class Show
{
    public static float StartedAt = -1f;
    public interface IPlan { float Total { get; } UnityEngine.Vector2 Position(int i, float t); UnityEngine.Vector2 LineSpot(int i); }
}
public static class V2Show
{
    public interface IPlan : Show.IPlan { bool InLane(int i, float t); }
    public interface ISections { IEnumerable<KeyValuePair<float, string>> Sections { get; } }
    public interface INoSafetyPush { }
    public class WayToLine : IPlan
    {
        public WayToLine(UnityEngine.Vector2[] from, UnityEngine.Vector2[] to, float unitsPerSec, float keep) { }
        public bool Clear { get { return true; } }
        public float Total { get { return 1f; } }
        public UnityEngine.Vector2 Position(int i, float t) { return new UnityEngine.Vector2(); }
        public UnityEngine.Vector2 LineSpot(int i) { return new UnityEngine.Vector2(); }
        public bool InLane(int i, float t) { return false; }
    }
    public static IEnumerator Flow(RoutineContext ctx, IPlan plan, float seconds, int maxSpeed,
                                   Func<int, float, bool> skip = null, Action<float> extra = null) { yield break; }
}
