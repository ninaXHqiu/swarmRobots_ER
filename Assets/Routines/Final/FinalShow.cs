using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using toio;

// Runs one of the final routines (FinalPlan) on the robots, real or simulated:
//   1. Set-up, not part of the timed routine: every robot goes to its own spot in the line (the
//      LLB line: LLB.OwnSpots) and turns to the way the routine starts, facing the participant.
//      The same robot owns the same slot every time (robot 1 = the messenger), as in LLBShow.
//   2. The routine, exactly plan.Total seconds. Robots that move follow their planned path with
//      V2Show's steering (forward or in reverse). Robots that the plan only turns where they stand
//      (looking at the participant, at the hand, at the team, spinning) are turned on the spot to
//      the planned heading, at the planned turning speed.
// The sections go into the sensor recording as markers (V2Show.Flow does that for ISections plans).
public static class FinalShow
{
    public static IEnumerator Run(RoutineContext ctx, FinalPlan plan, int maxSpeed = 100)
    {
        if (ctx.Count != plan.Robots)
        {
            Debug.LogWarning("Final routine " + plan.Name + " is planned for " + plan.Robots + " robots, not " + ctx.Count);
            yield break;
        }
        yield return ToOwnSlots(ctx);

        // turn to the starting headings (everyone looks at the participant)
        float turnEnd = Time.time + 2f;
        while (Time.time < turnEnd)
        {
            if (ctx.CubeManager.synced) { for (int i = 0; i < ctx.Count; i++) { TurnOnSpot(ctx, i, 0f, plan.Heading(i, 0f)); } }
            yield return null;
        }
        Swarm.StopAll(ctx);
        yield return Swarm.Wait(500);

        Physio.Marker("Final start " + plan.Name);
        Show.StartedAt = Time.time;   // read by the simulator recorder
        float lag = (float)CubeHandle.lag;
        yield return V2Show.Flow(ctx, plan, plan.Total + 0.3f, maxSpeed,
            skip: (i, t) => plan.InPlace(i, t + lag),
            extra: t =>
            {
                for (int i = 0; i < ctx.Count; i++)
                {
                    if (plan.InPlace(i, t + lag)) { TurnOnSpot(ctx, i, plan.TurnRate(i, t + lag), plan.Heading(i, t + lag)); }
                }
            });
        Swarm.StopAll(ctx);
        Physio.Marker("Final end " + plan.Name);
    }

    // Turn robot i where it stands toward faceDeg (mat degrees), adding the planned turning speed
    // (deg/s, + = clockwise on the mat) so that spins keep up instead of lagging behind.
    const float FaceTurnSecs = 0.35f;
    static void TurnOnSpot(RoutineContext ctx, int i, float rateDeg, float faceDeg)
    {
        var handle = ctx.Navigators[i].handle;
        float turnDeg = Mathf.DeltaAngle((float)handle.radPred * Mathf.Rad2Deg, faceDeg);
        double radPerSec = (rateDeg + turnDeg / FaceTurnSecs) * Mathf.Deg2Rad;
        double rotate = radPerSec * CubeHandle.TireWidthDot / CubeHandle.VDotOverU;
        if (Math.Abs(rotate) < 1.0 && Mathf.Abs(turnDeg) < 3f) { handle.Move(0, 0, 100, false); return; }
        handle.Move(0, rotate, 200, false);
    }

    // The same as LLBShow's set-up (kept here so the shared LLB code stays untouched): which line slot
    // each physical robot owns, for as long as the app runs (real robots by Bluetooth address,
    // simulated ones by their first line-up); then everyone goes to its own spot.
    static readonly Dictionary<string, int> slotOf = new Dictionary<string, int>();

    static IEnumerator ToOwnSlots(RoutineContext ctx)
    {
        int n = ctx.Count;
        var line = LLB.OwnSpots(n);
        var at = new Vector2[n];
        for (int i = 0; i < n; i++) { at[i] = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y); }
        var nearest = Swarm.LeastTravel(at, line);

        var id = ctx.Cubes.Select(c => c.addr ?? c.id).ToArray();
        bool known = id.All(k => slotOf.ContainsKey(k)) && id.Select(k => slotOf[k]).OrderBy(s => s).SequenceEqual(Enumerable.Range(0, n));
        if (!known)
        {
            slotOf.Clear();
            bool simulated = ctx.Cubes.All(c => c is CubeUnity);
            var byAddress = Enumerable.Range(0, n).OrderBy(i => id[i], StringComparer.Ordinal).ToArray();
            for (int i = 0; i < n; i++) { slotOf[id[i]] = simulated ? nearest[i] : Array.IndexOf(byAddress, i); }
        }
        var own = id.Select(k => slotOf[k]).ToArray();

        var cubes = new Cube[n]; var navigators = new toio.Navigation.CubeNavigator[n];
        for (int i = 0; i < n; i++) { cubes[own[i]] = ctx.Cubes[i]; navigators[own[i]] = ctx.Navigators[i]; }
        ctx.Cubes = cubes; ctx.Navigators = navigators;

        for (int k = 0; k < n; k++) { at[k] = new Vector2(ctx.Cubes[k].x, ctx.Cubes[k].y); }
        var way = new V2Show.WayToLine(at, line, 70f, 38f);
        var targets = line.Select(p => Vector2Int.RoundToInt(p)).ToArray();
        if (way.Clear) { yield return V2Show.Flow(ctx, way, way.Total + 0.3f, 70); }
        else { yield return Swarm.MoveToAvoiding(ctx, targets, maxSpeed: 70, timeoutMs: 15000); }
        float end = Time.time + 1.5f;
        while (Time.time < end)
        {
            if (ctx.CubeManager.synced)
            {
                for (int k = 0; k < n; k++) { ctx.Navigators[k].handle.Move2Target(targets[k].x, targets[k].y, maxSpd: 30, tolerance: 4).Exec(); }
            }
            yield return null;
        }
        Swarm.StopAll(ctx);
    }
}
