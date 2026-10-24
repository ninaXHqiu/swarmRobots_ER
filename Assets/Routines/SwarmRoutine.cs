using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using toio;

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

    // Drive every robot to its target with collision avoidance. Finishes when all have
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
                    ctx.CubeManager.navigators[i].Navi2Target(t.x, t.y, maxSpd: maxSpeed).Exec();
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
                    var mv = ctx.CubeManager.navigators[i].handle.Rotate2Deg(degrees).Exec();
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
