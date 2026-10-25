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
                    var p = Clamp(Vector2Int.RoundToInt(targetAt(i, t)));
                    ctx.Navigators[i].handle.Move2Target(p.x, p.y, maxSpd: maxSpeed).Exec();
                }
            }
            yield return null;
        }
        StopAll(ctx);
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
