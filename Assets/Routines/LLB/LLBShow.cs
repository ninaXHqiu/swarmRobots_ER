using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using toio;

// Runs an LLB routine on the robots (real or simulated):
//   1. Set-up, not part of the timed routine: every robot goes to its own spot in the line
//      (LLB.LineY, a quarter of the mat in from the participant) and faces the participant.
//   2. The plan (LLBPlan): introduction, the routine's middle, the shared closing, exactly
//      LLBPlan.TotalSecs (3:20), followed with V2Show's steering. Robots the plan turns on the spot
//      (look at someone, shake, spin) are turned instead of driven.
// Routines that follow the participant (the resonance ones) get the filtered heart rate (PhysioHeart).
public static class LLBShow
{
    const int TurnMs = 1500;

    public static IEnumerator Run(RoutineContext ctx, Func<Vector2[], LLBPlan> makePlan, int maxSpeed = 100, bool followHeart = false)
    {
        yield return ToOwnSlots(ctx);
        yield return Swarm.TurnTo(ctx, Swarm.FacingParticipant, timeoutMs: 4000, rotateTimeMs: TurnMs);
        yield return Swarm.Wait(800);
        Physio.Marker("LLB timed start");
        Show.StartedAt = Time.time;   // read by the simulator recorder
        int n = ctx.Count;
        var start = new Vector2[n];
        for (int i = 0; i < n; i++) { start[i] = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y); }
        var plan = makePlan(start);
        PhysioHeart heart = null;
        if (followHeart) { heart = new PhysioHeart(); plan.Heart = heart; }
        float spin, faceDeg, nextLog = 0f;
        yield return V2Show.Flow(ctx, plan, plan.Total + 0.3f, maxSpeed,
            skip: (i, t) => plan.InPlace(i, t, out spin, out faceDeg),
            extra: t =>
            {
                for (int i = 0; i < ctx.Count; i++)
                {
                    float s, deg;
                    if (plan.InPlace(i, t, out s, out deg)) { TurnOnSpot(ctx, i, s, deg); }
                }
                // the heart rate the routine used, every 5 s, into the sensor recording
                if (heart != null && t >= nextLog) { nextLog = t + 5f; Physio.Marker("LLB heart " + heart.Report()); }
            });
        Swarm.StopAll(ctx);
        Physio.Marker("LLB end");
    }

    // Turn robot i where it stands: toward faceDeg (NaN = no particular way) plus a spin (wheel speed).
    // While it also shakes, it is held toward faceDeg more gently, so the shake shows.
    const float FaceTurnSecs = 0.45f;
    static void TurnOnSpot(RoutineContext ctx, int i, float spin, float faceDeg)
    {
        var handle = ctx.Navigators[i].handle;
        double rotate = 2.0 * spin;
        if (!float.IsNaN(faceDeg))
        {
            float turnDeg = Mathf.DeltaAngle((float)handle.radPred * Mathf.Rad2Deg, faceDeg);
            float gain = Mathf.Abs(spin) > 1f ? 0.3f : 1f;
            if (Mathf.Abs(turnDeg) > 3f) { rotate += gain * turnDeg * Mathf.Deg2Rad / FaceTurnSecs * CubeHandle.TireWidthDot / CubeHandle.VDotOverU; }
        }
        if (Math.Abs(rotate) < 1.0) { handle.Move(0, 0, 100, false); return; }
        handle.Move(0, rotate, 200, false);
    }

    // Which line slot each physical robot owns, kept for as long as the app runs (as in V2Show:
    // real robots by Bluetooth address, simulated ones by their first line-up), so the same robot
    // is the messenger in every routine and every replay.
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

// The participant's heart rate, cleaned of the usual errors of a finger pulse sensor before a routine
// follows it. Reads the beats Physio reports (Physio.BeatCount) and keeps its own list of beat-to-beat
// intervals:
//   - an interval outside 45..140 beats a minute is ignored;
//   - so is one more than 25 % away from the median of the recent good ones (a missed beat gives one
//     twice as long, an extra beat one half as long);
//   - the rate is the median of the last 11 good intervals, and it may change by at most 2 beats a
//     minute per second, so a burst of noise barely moves it while a lasting change gets in slowly;
//   - with no good beat for 4 s (finger off, hand moving) it holds the last value, and after 8 s it
//     reports nothing, and the routine eases back to its script (LLBPlan.LiveTempo does that gently).
// With no sensor it reports nothing, and routines run on their script alone.
public class PhysioHeart : LLB.IHeart
{
    const float MinBpm = 45f, MaxBpm = 140f, MaxJump = 0.25f, SlewPerSec = 2f, HoldSecs = 4f, ForgetSecs = 8f;
    const int Keep = 11, Needed = 4;
    readonly List<float> good = new List<float>();
    int lastCount = -1; float lastBeat = -1f, lastGood = -1f, lastUpdate = -1f, smooth = float.NaN;
    int accepted, rejected;

    public float Bpm(float planSecs)
    {
        float now = Time.time;
        if (now != lastUpdate) { Update(now); lastUpdate = now; }
        if (float.IsNaN(smooth) || lastGood < 0f || now - lastGood > ForgetSecs) { return float.NaN; }
        return smooth;
    }

    void Update(float now)
    {
        int count = Physio.BeatCount;
        if (lastCount >= 0 && count > lastCount && lastBeat >= 0f)
        {
            float ibi = (now - lastBeat) / (count - lastCount);
            float bpm = 60f / Mathf.Max(0.01f, ibi);
            bool ok = bpm >= MinBpm && bpm <= MaxBpm;
            if (ok && good.Count >= 3)
            {
                float med = Median(good);
                ok = Mathf.Abs(ibi - med) / med <= MaxJump;
            }
            if (ok) { good.Add(ibi); if (good.Count > Keep) { good.RemoveAt(0); } lastGood = now; accepted++; }
            else { rejected++; }
        }
        if (count != lastCount) { lastBeat = now; lastCount = count; }
        if (good.Count < Needed) { return; }
        float target = 60f / Median(good);
        float dt = Mathf.Clamp(now - lastUpdate, 0f, 0.5f);
        if (float.IsNaN(smooth)) { smooth = target; }
        else if (now - lastGood <= HoldSecs) { smooth += Mathf.Clamp(target - smooth, -SlewPerSec * dt, SlewPerSec * dt); }
    }

    static float Median(List<float> a)
    {
        var s = a.OrderBy(v => v).ToList();
        return s[s.Count / 2];
    }

    public string Report()
    {
        return (float.IsNaN(smooth) ? "none" : smooth.ToString("0.0") + " bpm") + " (beats used " + accepted + ", ignored " + rejected + ")";
    }
}
