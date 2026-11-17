using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using toio;
using toio.Navigation;

// The shared plan of C22 (Cheerleading) and C24 (Cheerleading + Breathing).
//   1. The standard opening. The robots form the ring (the same ring, the same slow arrival and the same turn-up as the C13 breathing circle).
//   2. THE INNER PART: the ring turns slowly (the same rate as in C13) and the robots go in and out in two groups: four go in while the other four
//      go out, then they swap, neighbours always in opposite groups. Each robot's path is a zigzag round the ring ( > V \_ ).
//   3. The groups settle back onto the ring; then the ENDING of C13 exactly: the ring widens, (C24: breathes four times, large and slow),
//      opens and flattens into the line. Every robot ends on its own line spot.
// The in-and-out part is one motor command per half beat per robot (TargetMove, which may drive backwards: no turning round at the corners);
// nothing is steered meanwhile, and the plan below is the same zigzag, so robots without TargetMove are steered along exactly the same path.
// Steps 1 and 3 are steered the usual way (V2Show.Flow), as in C13.
public class V2CheerPlan : V2Show.IPlan, V2Show.IHeading, V2Show.ISections
{
    // ---- the in-and-out part (settings to calibrate)
    public const float InOutBy = 26f;       // how far the groups go out and in from the ring (mat units)
    public const float HalfBeatSecs = 3.5f; // one swing, out or in
    public const int HalfBeats = 10;        // number of swings in the inner part (so 5 full out-and-in beats)
    // ---- the ring and the ending: the values of the C13 breathing circle, not to be changed
    const float SectorRadius = 118f, WideRadius = 128f, SmallRadius = 52f, WidenSecs = 12f, BreathSecs = 20f;
    const float TurnUpSecs = 6f, RingRate0 = 0.11f, ReturnSecs = 12f, ExitRadius = 106f, GatherWithin = 20f;
    static readonly Vector2 Centre = new Vector2(250f, 250f);

    readonly Vector2[] start, line;
    readonly float[] sectorTo;
    readonly int n, breaths;
    readonly float[] side;                    // +1 for the group that goes out first, -1 for the other
    V2Show.WayToLine way;
    readonly float gatherEnd, midStart, midEnd, breatheFrom, stopFrom, ringStop;
    readonly Show.Eased turnRate;
    readonly string name;
    public bool Native;
    public float Total { get; private set; }

    public IEnumerable<KeyValuePair<float, string>> Sections
    {
        get
        {
            yield return new KeyValuePair<float, string>(0f, "ring forms");
            yield return new KeyValuePair<float, string>(gatherEnd, "ring turns up");
            yield return new KeyValuePair<float, string>(midStart, "four in, four out");
            yield return new KeyValuePair<float, string>(midEnd, breaths > 0 ? "ring widens" : "ring slows and widens a little");
            if (breaths > 0) { yield return new KeyValuePair<float, string>(breatheFrom, "breathing circle"); }
            yield return new KeyValuePair<float, string>(stopFrom, "back to the line");
        }
    }

    public V2CheerPlan(Vector2[] start, bool breathing)
    {
        this.start = start; n = start.Length; breaths = breathing ? 4 : 0;
        line = V2Show.OwnSpots(n);
        var order = V2Show.RingOrder(n);
        sectorTo = new float[n]; side = new float[n];
        for (int i = 0; i < n; i++) { sectorTo[i] = 135f * Mathf.Deg2Rad + 2f * Mathf.PI * order[i] / n; side[i] = order[i] % 2 == 0 ? 1f : -1f; }
        var targets = Enumerable.Range(0, n).Select(i => RingPoint(i, 0f)).ToArray();
        way = V2Show.Approach(start, targets, GatherWithin);
        gatherEnd = way != null ? way.Total + 0.3f : GatherWithin;
        midStart = gatherEnd + TurnUpSecs;
        midEnd = midStart + HalfBeats * HalfBeatSecs;
        breatheFrom = midEnd + WidenSecs;
        stopFrom = breatheFrom + (breaths > 0 ? breaths * BreathSecs : 6f);     // (C22: no breaths, a short settle on the wide ring before it opens)
        ringStop = stopFrom + ReturnSecs;
        Total = ringStop + 0.3f;
        // the ring turns as in C13 and, half way through the final return, stands at the angle from which it flattens into the line without crossing
        float tMid = stopFrom + ReturnSecs / 2f;
        Show.Eased RateFor(float c) { return new Show.Eased(gatherEnd, 0f, midStart, RingRate0, midEnd, RingRate0, breatheFrom, c, stopFrom, c, ringStop, 0f); }
        float unused; float opensAt = V2Show.BestAngle(order, 1, ExitRadius, line, out unused);
        float needed = Mathf.Repeat(opensAt - 135f * Mathf.Deg2Rad, 2f * Mathf.PI);
        float omega0 = RateFor(0f).Integral(tMid) - RateFor(0f).Integral(gatherEnd);
        float perUnit = RateFor(1f).Integral(tMid) - RateFor(1f).Integral(gatherEnd) - omega0;
        float chosen = 7f * Mathf.Deg2Rad;
        for (int k = 0; k < 40; k++)
        {
            float c = (needed + 2f * Mathf.PI * k - omega0) / perUnit;
            if (c >= 4f * Mathf.Deg2Rad && c <= 9f * Mathf.Deg2Rad) { chosen = c; break; }
        }
        turnRate = RateFor(chosen);
    }

    Vector2 RingPoint(int slot, float turn, float r = SectorRadius)
    {
        float a = sectorTo[slot] + turn;
        return Centre + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    float Ring(float t) { return turnRate.Integral(t) - turnRate.Integral(gatherEnd); }

    float Radius(float t)
    {
        float r = Mathf.Lerp(SectorRadius, WideRadius, Shapes.Ease((t - midEnd) / WidenSecs));
        for (int b = 0; b < breaths; b++) { r -= (WideRadius - SmallRadius) * Show.Bump((t - breatheFrom - b * BreathSecs) / BreathSecs); }
        return r + (ExitRadius - WideRadius) * Shapes.Ease((t - stopFrom) / (ReturnSecs * 0.5f));
    }

    // where robot `slot` is at the k-th corner of its zigzag: on the ring turned to that moment, in or out by one side or the other
    // (the first corner is out for one group and in for the other, they swap at every corner, and the last is back on the ring)
    Vector2 Corner(int slot, int k)
    {
        float t = midStart + k * HalfBeatSecs;
        float off = (k <= 0 || k >= HalfBeats) ? 0f : side[slot] * InOutBy * ((k % 2 == 1) ? 1f : -1f);
        return RingPoint(slot, Ring(t), SectorRadius + off);
    }

    public Vector2 Position(int slot, float t)
    {
        t = Mathf.Clamp(t, 0f, Total);
        Vector2 p;
        if (t < gatherEnd) { p = way != null ? way.Position(slot, t) : Vector2.Lerp(start[slot], RingPoint(slot, 0f), Shapes.Ease(t / gatherEnd)); }
        else if (t >= midStart && t < midEnd)
        {
            // straight from corner to corner: the same path the robots are given as one move each
            int k = Mathf.Min(HalfBeats - 1, Mathf.FloorToInt((t - midStart) / HalfBeatSecs));
            p = Vector2.Lerp(Corner(slot, k), Corner(slot, k + 1), (t - (midStart + k * HalfBeatSecs)) / HalfBeatSecs);
        }
        else { p = RingPoint(slot, Ring(t), Radius(t)); }
        if (t > stopFrom) { p = Vector2.Lerp(p, line[slot], Shapes.Ease((t - stopFrom) / ReturnSecs)); }
        return p;
    }

    public float Heading(int slot, float t) { return 270f; }
    public Vector2 LineSpot(int i) { return line[i]; }
    public bool InLane(int slot, float t) { return false; }

    // ---------------------------------------------------------------- the in-and-out part, one move per half beat
    bool begun, unhooked; int[] next, lastId;
    readonly Dictionary<Cube, int> arrived = new Dictionary<Cube, int>();
    Action<Cube, int, Cube.TargetMoveRespondType> onDone;

    // true while the robots are driven by Drive and not steered
    public bool Driving(float t) { return Native && t >= midStart && t < midEnd; }

    public void Drive(RoutineContext ctx, float t)
    {
        int count = ctx.Count;
        if (!Native) { return; }
        if (t >= midEnd) { Unhook(ctx); return; }
        if (t < midStart) { return; }
        if (!begun)
        {
            begun = true; next = Enumerable.Repeat(1, count).ToArray(); lastId = new int[count];
            onDone = (c, id, resp) => { arrived[c] = id; };
            foreach (var c in ctx.Cubes) { c.targetMoveCallback.AddListener("cheer", onDone); }
        }
        for (int i = 0; i < count; i++)
        {
            int k = next[i];
            if (k > HalfBeats) { continue; }
            bool free = lastId[i] == 0 || (arrived.ContainsKey(ctx.Cubes[i]) && arrived[ctx.Cubes[i]] == lastId[i]);
            if (!free) { continue; }
            if (t < midStart + (k - 1) * HalfBeatSecs - 0.15f) { continue; }                      // not before its time
            int due = Mathf.Min(HalfBeats, Mathf.FloorToInt((t - midStart) / HalfBeatSecs) + 1);
            if (k < due - 1) { k = due - 1; }                                                       // a robot that fell far behind skips ahead
            Vector2 from = Corner(i, k - 1), to = Corner(i, k);
            float unitsPerSec = Vector2.Distance(from, to) / HalfBeatSecs;
            int speed = Mathf.Clamp(Mathf.RoundToInt(unitsPerSec / (float)CubeHandle.VDotOverU * 1.05f), 10, 100);
            int id = (k % 250) + 1;
            Vector2 d = to - from;
            ctx.Cubes[i].TargetMove(Mathf.RoundToInt(to.x), Mathf.RoundToInt(to.y), Mathf.RoundToInt(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg), id, 8,
                Cube.TargetMoveType.RotatingMove, speed, Cube.TargetSpeedType.UniformSpeed, Cube.TargetRotationType.NotRotate);
            lastId[i] = id; next[i] = k + 1;
        }
    }

    void Unhook(RoutineContext ctx)
    {
        if (unhooked || !begun) { return; }
        unhooked = true;
        foreach (var c in ctx.Cubes) { c.targetMoveCallback.RemoveListener("cheer"); }
    }
}
