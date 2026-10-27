using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;
using toio.Navigation;

// How to add a routine
// --------------------
// 1. Make a new .cs file anywhere under Assets/Routines.
// 2. Write a class that extends SwarmRoutine: give it a Name, the robots it needs
//    (MinRobots), and the steps in Run().
// 3. It shows up in the app's Routine list automatically. Nothing else to register.
// See StudyFlowExample.cs for a complete example.
//
// Mat coordinates: x grows to the right, y grows downward, the usable mat is about
// 45..455 on both axes, and (250, 250) is the centre.
// The participant always sits at the bottom-centre edge of the mat (see Swarm.Participant).

// What a running routine can see: the connected robots
public class RoutineContext
{
    public Cube[] Cubes;
    public CubeNavigator[] Navigators;   // same order as Cubes
    public CubeManager CubeManager;
    // Where each robot stood when the routine was started (used by Closing to send them back)
    public Dictionary<Cube, Vector2Int> Home = new Dictionary<Cube, Vector2Int>();

    public void RecordHome()
    {
        Home.Clear();
        foreach (var cube in Cubes) { Home[cube] = new Vector2Int(cube.x, cube.y); }
    }

    public int Count { get { return Cubes.Length; } }
}

public abstract class SwarmRoutine
{
    public abstract string Name { get; }
    public virtual int MinRobots { get { return 1; } }
    public abstract IEnumerator Run(RoutineContext ctx);
}

// Shared "how" for routines: moving safely, timing, and where the participant is.
// Choreography (which shapes, in what order) belongs in the routine files, not here.
public static class Swarm
{
    public const int MatMin = 45;
    public const int MatMax = 455;
    public const int SafeMargin = 35;   // keeps robots from driving off the edge
    public const int Centre = 250;
    const int ArriveTolerance = 15;     // mat units

    // Keep a target inside the safe area of the mat
    public static Vector2Int Clamp(Vector2Int p)
    {
        int lo = MatMin + SafeMargin; int hi = MatMax - SafeMargin;
        return new Vector2Int(Mathf.Clamp(p.x, lo, hi), Mathf.Clamp(p.y, lo, hi));
    }

    // Where the participant sits: bottom-centre edge of the mat.
    // If the setup changes, this is the one line to edit.
    public static readonly Vector2 Participant = new Vector2(Centre, MatMax);

    // Point straight in front of the participant, `distance` mat units in from their edge
    public static Vector2 InFrontOfParticipant(int distance)
    {
        return Participant + Inward * distance;
    }

    // Direction from the participant into the mat, and along their edge
    public static Vector2 Inward { get { return (new Vector2(Centre, Centre) - Participant).normalized; } }
    public static Vector2 Along { get { return new Vector2(-Inward.y, Inward.x); } }

    // Mat angle (degrees) that faces the participant
    public static double FacingParticipant
    {
        get
        {
            Vector2 toParticipant = -Inward;
            return Mathf.Atan2(toParticipant.y, toParticipant.x) * Mathf.Rad2Deg;
        }
    }

    // Drive every robot straight to its target (no avoidance: routine shapes are planned so
    // robots don't cross, and avoidance stalls formations where robots sit close). Finishes when all have
    // arrived or timeoutMs has passed, so a stuck robot can't hang the routine.
    public static IEnumerator MoveTo(RoutineContext ctx, Vector2Int[] targets, int maxSpeed = 60, int timeoutMs = 8000)
    {
        float end = Time.time + timeoutMs / 1000f;
        var arrived = new bool[ctx.Count];
        while (Time.time < end)
        {
            if (ctx.CubeManager.synced)
            {
                bool all = true;
                for (int i = 0; i < ctx.Count && i < targets.Length; i++)
                {
                    var t = Clamp(targets[i]);
                    int dx = ctx.Cubes[i].x - t.x; int dy = ctx.Cubes[i].y - t.y;
                    if (arrived[i]) { continue; }
                    if (dx * dx + dy * dy <= ArriveTolerance * ArriveTolerance)
                    {
                        arrived[i] = true;
                        ctx.Cubes[i].Move(0, 0, 0);
                        continue;
                    }
                    all = false;
                    ctx.Navigators[i].handle.Move2Target(t.x, t.y, maxSpd: maxSpeed).Exec();
                }
                if (all) { break; }
            }
            yield return null;
        }
        StopAll(ctx);
    }

    // The shared line every routine starts in and the Closing ends in: robots 35 apart, centred
    public const float LineSpacing = 35f;
    public static Vector2Int[] Line(int n)
    {
        var line = new Vector2Int[n];
        for (int i = 0; i < n; i++) { line[i] = new Vector2Int(Mathf.RoundToInt(Centre + (i - (n - 1) / 2f) * LineSpacing), Centre); }
        return line;
    }

    // Start of every routine: robots glide into the shared line
    public static IEnumerator StartInLine(RoutineContext ctx)
    {
        var line = Line(ctx.Count);
        AssignNearest(ctx, line);
        yield return GlideTo(ctx, line, maxSpeed: 90);
    }

    // Ease every robot from where it is to its target over a fixed time (longest trip at about
    // `unitsPerSec`, at least 2 s), then carry straight on - no waiting for stragglers, so the
    // routine never pauses.
    public static IEnumerator GlideTo(RoutineContext ctx, Vector2Int[] targets, float unitsPerSec = 100f, int maxSpeed = 100)
    {
        int n = ctx.Count;
        var from = new Vector2[n];
        float longest = 0f;
        for (int i = 0; i < n; i++)
        {
            from[i] = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y);
            longest = Mathf.Max(longest, Vector2.Distance(from[i], targets[i]));
        }
        float secs = Mathf.Max(2f, longest / unitsPerSec);
        yield return Follow(ctx, (i, t) => Vector2.Lerp(from[i], targets[i], Mathf.SmoothStep(0, 1, t / secs)), (int)(secs * 1000), maxSpeed);
    }

    // Like MoveTo, but robots steer around each other. Slower to settle; use it when robots'
    // paths may cross (e.g. going back to their own start spots).
    public static IEnumerator MoveToAvoiding(RoutineContext ctx, Vector2Int[] targets, int maxSpeed = 60, int timeoutMs = 15000)
    {
        foreach (var navigator in ctx.Navigators)
        {
            navigator.ClearOther();
            foreach (var other in ctx.Navigators) { if (other != navigator) { navigator.AddOther(other); } }
        }
        float end = Time.time + timeoutMs / 1000f;
        var arrived = new bool[ctx.Count];
        while (Time.time < end)
        {
            if (ctx.CubeManager.synced)
            {
                bool all = true;
                for (int i = 0; i < ctx.Count && i < targets.Length; i++)
                {
                    if (arrived[i]) { continue; }
                    var t = Clamp(targets[i]);
                    int dx = ctx.Cubes[i].x - t.x; int dy = ctx.Cubes[i].y - t.y;
                    if (dx * dx + dy * dy <= ArriveTolerance * ArriveTolerance) { arrived[i] = true; ctx.Cubes[i].Move(0, 0, 0); continue; }
                    all = false;
                    ctx.Navigators[i].Navi2Target(t.x, t.y, maxSpd: maxSpeed).Exec();
                }
                if (all) { break; }
            }
            yield return null;
        }
        StopAll(ctx);
    }

    // Keep every robot chasing a moving target for durationMs. targetAt(i, t) gives robot i's
    // position at t seconds into the step; use it for anything that flows (turning shapes, waves).
    public static IEnumerator Follow(RoutineContext ctx, System.Func<int, float, Vector2> targetAt, int durationMs, int maxSpeed = 70)
    {
        float start = Time.time;
        float duration = durationMs / 1000f;
        while (Time.time - start < duration)
        {
            if (ctx.CubeManager.synced)
            {
                float t = Time.time - start;
                for (int i = 0; i < ctx.Count; i++)
                {
                    var target = targetAt(i, t) + Repel(ctx, i);
                    var p = Clamp(Vector2Int.RoundToInt(target));
                    ctx.Navigators[i].handle.Move2Target(p.x, p.y, maxSpd: maxSpeed).Exec();
                }
            }
            yield return null;
        }
        StopAll(ctx);
    }

    // Keeps robots from touching: when another robot is closer than RepelDistance (measured on
    // the mat, not planned), push this robot's target straight away from it - the closer, the
    // harder. Added to every Follow/GlideTo target, so robots that drift into each other separate.
    const float RepelDistance = 50f;
    const float RepelStrength = 2f;
    static Vector2 Repel(RoutineContext ctx, int i)
    {
        Vector2 me = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y);
        Vector2 push = Vector2.zero;
        for (int j = 0; j < ctx.Count; j++)
        {
            if (j == i) { continue; }
            Vector2 away = me - new Vector2(ctx.Cubes[j].x, ctx.Cubes[j].y);
            float d = away.magnitude;
            if (d >= RepelDistance) { continue; }
            if (d < 0.01f) { away = new Vector2(i < j ? -1f : 1f, 0f); d = 0.01f; } // exactly on top: split sideways
            push += away / d * (RepelDistance - d) * RepelStrength;
        }
        return push;
    }

    // ---------- Smooth path following (used by the v2_motion routines) ----------

    // The closest two robots are ever planned to be: the shared line spacing
    public const float MinSpacing = LineSpacing;

    // Robot i follows path(i, t), its planned mat position t seconds into the step, for
    // `seconds`. Unlike Follow it steers with the path's own speed and direction, so motion
    // carries through smoothly, and it drives in reverse when the path turns back, instead of
    // spinning on the spot at every reversal. Paths should start where the robots are and end
    // at rest; robots stop when the step ends.
    public static IEnumerator Flow(RoutineContext ctx, System.Func<int, float, Vector2> path, float seconds, int maxSpeed = 100)
    {
        var reverse = new bool[ctx.Count];
        float start = Time.time;
        while (Time.time - start < seconds)
        {
            if (ctx.CubeManager.synced)
            {
                float t = Time.time - start;
                for (int i = 0; i < ctx.Count; i++) { Steer(ctx, i, path, t, maxSpeed, ref reverse[i]); }
            }
            yield return null;
        }
        StopAll(ctx);
    }

    const float FlowGain = 3f;        // per second: how firmly robots close the gap to the path
    const float FlowTurnSecs = 0.25f; // how quickly robots turn to the wanted direction
    const float FlowFlipDeg = 110f;   // switch between forward and reverse past this angle

    static void Steer(RoutineContext ctx, int i, System.Func<int, float, Vector2> path, float t, int maxSpeed, ref bool reverse)
    {
        var handle = ctx.Navigators[i].handle;
        // Commands take effect after the cube's lag, so aim at where the path will be then,
        // from where the robot will be then
        float at = t + (float)CubeHandle.lag;
        const float h = 0.05f;
        Vector2 target = path(i, at) + SafetyPush(ctx, i);
        Vector2 pathVelocity = (path(i, at + h) - path(i, Mathf.Max(0f, at - h))) / (at + h - Mathf.Max(0f, at - h));
        Vector2 here = new Vector2((float)handle.xPred, (float)handle.yPred);
        Vector2 want = pathVelocity + FlowGain * (target - here); // mat units per second

        float limit = maxSpeed * (float)CubeHandle.VDotOverU;
        if (want.magnitude > limit) { want = want.normalized * limit; }
        if (want.magnitude < 2f) { handle.Move(0, 0, 100, false); return; }

        float wantDeg = Mathf.Atan2(want.y, want.x) * Mathf.Rad2Deg;
        float facingDeg = (float)handle.radPred * Mathf.Rad2Deg + (reverse ? 180f : 0f);
        float turnDeg = Mathf.DeltaAngle(facingDeg, wantDeg);
        if (Mathf.Abs(turnDeg) > FlowFlipDeg)
        {
            reverse = !reverse;
            turnDeg = Mathf.DeltaAngle(facingDeg + 180f, wantDeg);
        }
        float drive = want.magnitude * Mathf.Max(0f, Mathf.Cos(turnDeg * Mathf.Deg2Rad));
        double translate = (reverse ? -drive : drive) / CubeHandle.VDotOverU;
        double rotate = turnDeg * Mathf.Deg2Rad / FlowTurnSecs * CubeHandle.TireWidthDot / CubeHandle.VDotOverU;
        handle.Move(translate, rotate, 200, false);
    }

    // Safety net for Flow: planned paths keep robots at least MinSpacing apart, so this only acts
    // if a robot falls behind and another comes closer than that. It grows gently from zero.
    const float SafetyStrength = 3f;
    static Vector2 SafetyPush(RoutineContext ctx, int i)
    {
        float reach = MinSpacing - 3f;
        Vector2 me = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y);
        Vector2 push = Vector2.zero;
        for (int j = 0; j < ctx.Count; j++)
        {
            if (j == i) { continue; }
            Vector2 away = me - new Vector2(ctx.Cubes[j].x, ctx.Cubes[j].y);
            float d = away.magnitude;
            if (d >= reach) { continue; }
            if (d < 0.01f) { away = new Vector2(i < j ? -1f : 1f, 0f); d = 0.01f; }
            push += away / d * SafetyStrength * (reach - d) * (reach - d) / reach;
        }
        return push;
    }

    // Which target each robot takes: the assignment with the least total squared travel (tries
    // every assignment, fine for up to 8 robots). Straight, simultaneous trips planned this way
    // don't cross. Returns slot[i] = the target index for robot i.
    public static int[] LeastTravel(Vector2[] from, Vector2[] to)
    {
        int n = from.Length;
        var best = new int[n]; var current = new int[n]; var used = new bool[n];
        float bestCost = float.MaxValue;
        System.Action<int, float> search = null;
        search = (i, cost) =>
        {
            if (cost >= bestCost) { return; }
            if (i == n) { bestCost = cost; current.CopyTo(best, 0); return; }
            for (int j = 0; j < n; j++)
            {
                if (used[j]) { continue; }
                used[j] = true; current[i] = j;
                search(i + 1, cost + (from[i] - to[j]).sqrMagnitude);
                used[j] = false;
            }
        };
        search(0, 0f);
        return best;
    }

    // Re-number the robots so robot i is the nearest free robot to targets[i]. Call before a
    // new formation so robots take short trips instead of crossing the mat.
    public static void AssignNearest(RoutineContext ctx, Vector2Int[] targets)
    {
        int n = ctx.Count;
        var used = new bool[n];
        var cubes = (Cube[])ctx.Cubes.Clone();
        var navigators = (CubeNavigator[])ctx.Navigators.Clone();
        for (int i = 0; i < n && i < targets.Length; i++)
        {
            int best = -1; float bestDist = float.MaxValue;
            for (int j = 0; j < n; j++)
            {
                if (used[j]) { continue; }
                float dx = ctx.Cubes[j].x - targets[i].x, dy = ctx.Cubes[j].y - targets[i].y;
                float d = dx * dx + dy * dy;
                if (d < bestDist) { bestDist = d; best = j; }
            }
            used[best] = true;
            cubes[i] = ctx.Cubes[best];
            navigators[i] = ctx.Navigators[best];
        }
        ctx.Cubes = cubes;
        ctx.Navigators = navigators;
    }

    // Turn each robot to face a point on the mat (each robot gets its own angle)
    public static IEnumerator FacePoint(RoutineContext ctx, Vector2 point, int timeoutMs = 3000)
    {
        float end = Time.time + timeoutMs / 1000f;
        while (Time.time < end)
        {
            if (ctx.CubeManager.synced)
            {
                bool all = true;
                for (int i = 0; i < ctx.Count; i++)
                {
                    double deg = Mathf.Atan2(point.y - ctx.Cubes[i].y, point.x - ctx.Cubes[i].x) * Mathf.Rad2Deg;
                    if (!ctx.Navigators[i].handle.Rotate2Deg(deg).Exec().reached) { all = false; }
                }
                if (all) { break; }
            }
            yield return null;
        }
        StopAll(ctx);
    }

    // Turn every robot to a mat angle in degrees (0 = right, 90 = down)
    public static IEnumerator TurnTo(RoutineContext ctx, double degrees, int timeoutMs = 3000)
    {
        float end = Time.time + timeoutMs / 1000f;
        while (Time.time < end)
        {
            if (ctx.CubeManager.synced)
            {
                bool all = true;
                for (int i = 0; i < ctx.Count; i++)
                {
                    var mv = ctx.Navigators[i].handle.Rotate2Deg(degrees).Exec();
                    if (!mv.reached) { all = false; }
                }
                if (all) { break; }
            }
            yield return null;
        }
        StopAll(ctx);
    }

    // Run both wheels for ms on every robot (same speed = straight, opposite = spin)
    public static IEnumerator Wheels(RoutineContext ctx, int left, int right, int ms)
    {
        for (int i = 0; i < ctx.Count; i++) { ctx.Cubes[i].Move(left, right, ms, Cube.ORDER_TYPE.Strong); }
        yield return Wait(ms);
    }

    public static IEnumerator Wait(int ms)
    {
        yield return new WaitForSeconds(ms / 1000f);
    }

    public static void StopAll(RoutineContext ctx)
    {
        for (int i = 0; i < ctx.Count; i++) { ctx.Cubes[i].Move(0, 0, 0, Cube.ORDER_TYPE.Strong); }
    }
}
