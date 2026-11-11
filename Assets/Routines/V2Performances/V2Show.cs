using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using toio;

// Shared frame of the v2 performances (Vibration, Heartbeat, Kaleidoscope, Social Dance):
//   1. the Messenger introduction (same steps, constants and pauses as Messenger.cs / Show.cs),
//      with every physical robot always in the same line slot, so the same robot visits the
//      participant in every routine and every replay;
//   2. the performance, one continuous planned path per robot, starting from where the
//      introduction actually leaves the robots;
//   3. the robots end on LineUp's line and face the participant.
// Every routine comes in a natural length and in 3 and 5 minute versions. Longer versions add
// whole phrases at the same tempo; the introduction and ending are the same in all of them.
public static class V2Show
{
    // Measured in simulator recordings: start of the routine to the start of the performance,
    // and end of the planned path to the routine finishing. Used to size the timed versions.
    public const float IntroSecs = 18.6f;
    public const float EndSecs = 3.8f;

    // The compact dance area: a square this far each way from the mat centre (drawn in previews)
    public const float DanceHalf = 140f;

    const float HoldSecs = 1f;      // robots hold their line spots this long after the path ends
    public const float LaneDeg = 90f;   // heading of a robot in a lane: straight across the line

    // A performance path. InLane: robot i only moves along its lane (straight toward and away
    // from the participant) around t, so it keeps its heading and drives forward and backward.
    public interface IPlan : Show.IPlan
    {
        bool InLane(int i, float t);
    }

    public static string Label(float minutes)
    {
        return minutes <= 0f ? "natural" : minutes.ToString("0.#") + " min";
    }

    // Performance seconds available in a version that should last `minutes` in all
    public static float Budget(float minutes) { return minutes * 60f - IntroSecs - EndSecs; }

    public static IEnumerator Run(RoutineContext ctx, Func<Vector2[], IPlan> makePlan, int maxSpeed = 60)
    {
        yield return Introduction(ctx);
        Show.StartedAt = Time.time;   // read by the simulator recorder
        int n = ctx.Count;
        var start = new Vector2[n];
        for (int i = 0; i < n; i++) { start[i] = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y); }
        var plan = makePlan(start);
        yield return Flow(ctx, plan, plan.Total + HoldSecs, maxSpeed);
        yield return Swarm.TurnTo(ctx, Swarm.FacingParticipant, timeoutMs: 4000, rotateTimeMs: 1500);
    }

    // ---------------- Introduction ----------------

    // Messenger.cs steps 1-5 with its constants
    const int VisitDistance = 70, LookMs = 3000, TurnMs = 1800, WalkMaxSpeed = 70;
    const float WalkUnitsPerSec = 60f, StepOut = 50f, TalkGap = 55f;
    const int Shakes = 3, ShakeSpeed = 25, ShakeMs = 150, AfterTalkMs = 800;

    public static Vector2 TalkSpot { get { return Shapes.Centre - Swarm.Inward * TalkGap; } }

    // The messenger's line slot, counted from the left (left of the middle with an even number)
    public static int MessengerSlot(int n) { return (n - 1) / 2; }

    static IEnumerator Introduction(RoutineContext ctx)
    {
        int n = ctx.Count;
        // 1. Everyone in line (the working line-up: shortest trips, nobody crosses), each robot
        //    then in its own slot, facing the participant
        yield return LineUp.Form(ctx);
        yield return TakeOwnSlots(ctx);
        yield return Swarm.TurnTo(ctx, Swarm.FacingParticipant, timeoutMs: 4000, rotateTimeMs: 1500);
        yield return Swarm.Wait(1000);

        // 2. The messenger: always the robot of the same slot (robot i stands in slot i now)
        var messenger = Only(ctx, MessengerSlot(n));

        // 3. Step straight out of the line, go to the participant and look at them
        var here = new Vector2(messenger.Cubes[0].x, messenger.Cubes[0].y);
        var stepOut = Vector2Int.RoundToInt(here - Swarm.Inward * StepOut);
        yield return Swarm.GlideTo(messenger, new[] { stepOut }, unitsPerSec: WalkUnitsPerSec, maxSpeed: WalkMaxSpeed);
        var visit = Vector2Int.RoundToInt(Swarm.InFrontOfParticipant(VisitDistance));
        yield return Swarm.GlideTo(messenger, new[] { visit }, unitsPerSec: WalkUnitsPerSec, maxSpeed: WalkMaxSpeed);
        yield return Swarm.TurnTo(messenger, Swarm.FacingParticipant, timeoutMs: 4000, rotateTimeMs: TurnMs);
        yield return Swarm.Wait(LookMs);

        // 4. Turn slowly around and go back to just in front of the group, facing it
        double facingGroup = Swarm.FacingParticipant + 180;
        yield return Swarm.TurnTo(messenger, facingGroup, timeoutMs: 5000, rotateTimeMs: TurnMs);
        var talkSpot = Vector2Int.RoundToInt(TalkSpot);
        yield return Swarm.GlideTo(messenger, new[] { talkSpot }, unitsPerSec: WalkUnitsPerSec, maxSpeed: WalkMaxSpeed);
        yield return Swarm.TurnTo(messenger, facingGroup, timeoutMs: 3000, rotateTimeMs: 800);

        // 5. Talk: a little shake while the others listen from the line
        for (int k = 0; k < Shakes; k++)
        {
            yield return Swarm.Wheels(messenger, ShakeSpeed, -ShakeSpeed, ShakeMs);
            yield return Swarm.Wheels(messenger, -ShakeSpeed, ShakeSpeed, ShakeMs);
        }
        yield return Swarm.Wait(AfterTalkMs);
    }

    // Which line slot each physical robot owns, kept for as long as the app runs
    static readonly Dictionary<string, int> slotOf = new Dictionary<string, int>();

    // After the line-up: put every robot in its own slot and number the robots by slot
    // (ctx.Cubes[k] = the robot of slot k). Real robots own slots in the order of their
    // Bluetooth addresses, the same in every session. Simulated robots have no lasting identity,
    // so they keep the slots of their first line-up. Robots standing in the wrong slot (after a
    // routine that ended in another order) swap without crossing: each steps out to its own row,
    // slides along it to its own column, and steps back into the line.
    static IEnumerator TakeOwnSlots(RoutineContext ctx)
    {
        int n = ctx.Count;
        var line = LineUp.Spots(n).Select(s => (Vector2)s).ToArray();
        var at = new Vector2[n];
        for (int i = 0; i < n; i++) { at[i] = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y); }
        var standing = Swarm.LeastTravel(at, line);   // the slot each robot stands in now

        var id = ctx.Cubes.Select(c => c.addr ?? c.id).ToArray();
        bool known = id.All(k => slotOf.ContainsKey(k)) && id.Select(k => slotOf[k]).OrderBy(s => s).SequenceEqual(Enumerable.Range(0, n));
        if (!known)
        {
            slotOf.Clear();
            bool simulated = ctx.Cubes.All(c => c is CubeUnity);
            var byAddress = Enumerable.Range(0, n).OrderBy(i => id[i], StringComparer.Ordinal).ToArray();
            for (int i = 0; i < n; i++) { slotOf[id[i]] = simulated ? standing[i] : Array.IndexOf(byAddress, i); }
        }
        var own = id.Select(k => slotOf[k]).ToArray();

        // Number the robots by slot
        var cubes = new Cube[n]; var navigators = new toio.Navigation.CubeNavigator[n]; var nowIn = new int[n];
        for (int i = 0; i < n; i++) { cubes[own[i]] = ctx.Cubes[i]; navigators[own[i]] = ctx.Navigators[i]; nowIn[own[i]] = standing[i]; }
        ctx.Cubes = cubes; ctx.Navigators = navigators;
        if (Enumerable.Range(0, n).All(k => nowIn[k] == k)) { yield break; }

        const float RowGap = 40f;
        Func<int, float> row = k => Swarm.Centre + (k - (n - 1) / 2f) * RowGap;
        var step = new Vector2Int[n];
        for (int k = 0; k < n; k++) { step[k] = Vector2Int.RoundToInt(new Vector2(line[nowIn[k]].x, row(k))); }
        yield return Swarm.GlideTo(ctx, step, unitsPerSec: 70f, maxSpeed: 90);
        for (int k = 0; k < n; k++) { step[k] = Vector2Int.RoundToInt(new Vector2(line[k].x, row(k))); }
        yield return Swarm.GlideTo(ctx, step, unitsPerSec: 70f, maxSpeed: 90);
        for (int k = 0; k < n; k++) { step[k] = Vector2Int.RoundToInt(line[k]); }
        yield return Swarm.GlideTo(ctx, step, unitsPerSec: 70f, maxSpeed: 90);
        float end = Time.time + 1.5f;
        while (Time.time < end)
        {
            if (ctx.CubeManager.synced)
            {
                for (int k = 0; k < n; k++) { ctx.Navigators[k].handle.Move2Target(step[k].x, step[k].y, maxSpd: 30, tolerance: 4).Exec(); }
            }
            yield return null;
        }
        Swarm.StopAll(ctx);
    }

    static RoutineContext Only(RoutineContext ctx, int i)
    {
        var one = new RoutineContext();
        one.Cubes = new[] { ctx.Cubes[i] };
        one.Navigators = new[] { ctx.Navigators[i] };
        one.CubeManager = ctx.CubeManager;
        return one;
    }

    // ---------------- Following the planned paths ----------------

    // Robot i follows plan.Position(i, t). Free robots steer as Swarm.Flow does (the path's own
    // speed and direction, forward or reverse, turning with the curve). Robots in a lane hold
    // their heading on the lane and only drive forward and backward along it.
    public static IEnumerator Flow(RoutineContext ctx, IPlan plan, float seconds, int maxSpeed)
    {
        var reverse = new bool[ctx.Count];
        float start = Time.time;
        while (Time.time - start < seconds)
        {
            if (ctx.CubeManager.synced)
            {
                float t = Time.time - start;
                for (int i = 0; i < ctx.Count; i++)
                {
                    if (plan.InLane(i, t + (float)CubeHandle.lag)) { SteerLane(ctx, i, plan, t, maxSpeed); }
                    else { Steer(ctx, i, plan, t, maxSpeed, ref reverse[i]); }
                }
            }
            yield return null;
        }
        Swarm.StopAll(ctx);
    }

    const float FlowGain = 3f;        // per second: how firmly robots close the gap to the path
    const float FlowTurnSecs = 0.45f; // how quickly robots turn to the wanted direction
    const float FlowFlipDeg = 110f;   // switch between forward and reverse past this angle
    const float FlowTurnSpeed = 40f;  // below this speed robots turn more gently
    const float LaneLeanDeg = 4f;     // most a lane robot leans off its lane to drift back onto it
    const float LaneLeanPerUnit = 0.8f;

    // Heading stays on the lane whatever the speed, also through zero at a reversal: the wheels
    // are driven by the lane speed alone, and the only turning is the small correction that
    // keeps the body square to the lane (plus a lean of a few degrees, while moving, if the
    // robot has drifted sideways off its lane).
    static void SteerLane(RoutineContext ctx, int i, IPlan plan, float t, int maxSpeed)
    {
        var handle = ctx.Navigators[i].handle;
        float at = t + (float)CubeHandle.lag;
        const float h = 0.05f;
        Vector2 target = plan.Position(i, at);
        float from = Mathf.Max(0f, at - h);
        Vector2 pathVelocity = (plan.Position(i, at + h) - plan.Position(i, from)) / (at + h - from);
        Vector2 here = new Vector2((float)handle.xPred, (float)handle.yPred);
        Vector2 want = pathVelocity + FlowGain * (target - here);

        float facingDeg = (float)handle.radPred * Mathf.Rad2Deg;
        float alongDeg = Mathf.Abs(Mathf.DeltaAngle(facingDeg, LaneDeg)) <= 90f ? LaneDeg : LaneDeg + 180f;
        float a = alongDeg * Mathf.Deg2Rad;
        var along = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        var across = new Vector2(-along.y, along.x);
        float drive = Vector2.Dot(want, along);
        float limit = maxSpeed * (float)CubeHandle.VDotOverU;
        drive = Mathf.Clamp(drive, -limit, limit);

        float moving = Mathf.Clamp01(Mathf.Abs(drive) / FlowTurnSpeed);
        float lean = Mathf.Clamp(LaneLeanPerUnit * Vector2.Dot(target - here, across), -LaneLeanDeg, LaneLeanDeg) * moving * Mathf.Sign(drive);
        float turnDeg = Mathf.DeltaAngle(facingDeg, alongDeg + lean);
        double rotate = turnDeg * Mathf.Deg2Rad / FlowTurnSecs * CubeHandle.TireWidthDot / CubeHandle.VDotOverU;
        handle.Move(drive / CubeHandle.VDotOverU, rotate, 200, false);
    }

    // Same steering as Swarm.Flow's
    static void Steer(RoutineContext ctx, int i, IPlan plan, float t, int maxSpeed, ref bool reverse)
    {
        var handle = ctx.Navigators[i].handle;
        float at = t + (float)CubeHandle.lag;
        const float h = 0.05f;
        Vector2 target = plan.Position(i, at) + SafetyPush(ctx, i);
        float from = Mathf.Max(0f, at - h);
        Vector2 pathVelocity = (plan.Position(i, at + h) - plan.Position(i, from)) / (at + h - from);
        Vector2 here = new Vector2((float)handle.xPred, (float)handle.yPred);
        Vector2 want = pathVelocity + FlowGain * (target - here);

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
        float turnWeight = Mathf.Clamp01(want.magnitude / FlowTurnSpeed);
        Vector2 aheadVelocity = (plan.Position(i, at + 2f * h) - plan.Position(i, at)) / (2f * h);
        float pathTurn = 0f;
        if (pathVelocity.magnitude > FlowTurnSpeed && aheadVelocity.magnitude > FlowTurnSpeed)
        {
            pathTurn = Mathf.DeltaAngle(Mathf.Atan2(pathVelocity.y, pathVelocity.x) * Mathf.Rad2Deg, Mathf.Atan2(aheadVelocity.y, aheadVelocity.x) * Mathf.Rad2Deg) * Mathf.Deg2Rad / h;
        }
        double rotate = (turnWeight * turnDeg * Mathf.Deg2Rad / FlowTurnSecs + pathTurn) * CubeHandle.TireWidthDot / CubeHandle.VDotOverU;
        handle.Move(translate, rotate, 200, false);
    }

    // Safety net: paths are planned with clearance, so this only acts if a robot falls behind
    // and another comes closer than the planned minimum. It grows gently from zero.
    const float SafetyStrength = 3f;
    static Vector2 SafetyPush(RoutineContext ctx, int i)
    {
        float reach = Swarm.MinSpacing - 3f;
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
}
