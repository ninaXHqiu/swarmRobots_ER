using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using toio;
using UnityEngine;

// v2 Stress, restless (C23): the swarm mirrors high arousal, then calms into the breathing circle.
// Inspired by the fear pattern of "From Motions to Emotions" (Santos & Egerstedt), adapted to stress.
//   0. The standard opening (the messenger's introduction), like every other routine.
//   1. The robots take a spread-out formation across the mat.
//   2. The participant's baseline and current skin conductance are read ONCE.
//   3. initial_speed = clamp(BaseSpeed x stress_multiplier, MinSpeed, MaxSpeed), where
//      stress_multiplier = 1 + Gain x (current - baseline) / baseline. At or below baseline: MinSpeed.
//      The speed is not changed again during the restless part.
//   4. The swarm moves in short straight segments with sharp turns between them. A segment is ONE motor command
//      (the robot's own TargetMove: rotate, drive straight to the point). Nothing is sent while a segment runs;
//      the next one goes out only after every robot has reported arrival (or a timeout), so there are no
//      per-frame corrections and no wiggling. Robots that have no TargetMove (old firmware) fall back to the
//      ordinary MoveTo for that segment.
//   5. Then it calms: from wherever the last segment left them the robots curve onto one ring, more slowly the
//      closer they get; the ring turns, breathes (large, slow) several times, opens and flattens into the line,
//      and the standard ending follows (every robot on its own slot, facing the participant).
// The segments are planned in advance (fixed seed, so it is the same every time): equal length for all robots in a
// step, sharp turns (mostly in unison, a few against), and every path checked for the gap to the others and for
// staying inside the play area before it is used.
public class V2StressRestless : SwarmRoutine
{
    public override string Name { get { return "v2 Stress (restless, angular)"; } }
    public override string Code { get { return "C23"; } }
    public override string Note { get { return "Fast angular restless swarm, speed from skin conductance"; } }
    public override int MinRobots { get { return 4; } }

    // ---- settings to calibrate
    public const float BaseSpeed = 45f, MinSpeed = 40f, MaxSpeed = 85f;   // motor speed units (toio: 8 to 115 for TargetMove); MaxSpeed is kept below the top the robots take
    public const float Gain = 1.5f;                                       // 0.5 = skin conductance 50% above baseline gives 1 + 0.75 x speed
    public const float FallbackRise = 0.3f;                               // no sensor: pretend skin conductance is 30% above baseline
    public const int Steps = 16;                                          // number of segments (routine length = steps x segment time)
    public const float SegMin = 70f, SegMax = 130f;                       // segment length (mat units)
    public const float TurnMinDeg = 70f, TurnMaxDeg = 125f;               // sharpness of the turn between segments
    public const float AgainstChance = 0.25f;                             // share of robots turning the other way from the group
    public const float MinGap = 55f;                                      // closest two robots may come (mat units; a robot is about 32 wide)
    public const float AreaMin = 90f, AreaMax = 410f;                     // play area the robot centres stay inside
    public const int PauseMs = 120;                                       // beat between segments
    public const float UnitsPerSecPerSpeed = 0.9f;                        // rough travel rate (for the timeouts and the plan check only)
    public const int Seed = 23;

    // initial speed from skin conductance (sc, baseline in microsiemens; either <= 0 means no reading)
    public static float InitialSpeed(float sc, float baseline, out float multiplier)
    {
        if (sc <= 0f || baseline <= 0f) { baseline = 1f; sc = 1f + FallbackRise; }
        multiplier = sc <= baseline ? 1f : 1f + Gain * (sc - baseline) / baseline;
        if (sc <= baseline) { return MinSpeed; }
        return Mathf.Clamp(BaseSpeed * multiplier, MinSpeed, MaxSpeed);
    }

    // a spread-out formation: the n points of a grid over the play area that lie furthest from the middle
    public static Vector2[] SpreadSpots(int n)
    {
        int k = Mathf.Max(3, Mathf.CeilToInt(Mathf.Sqrt(n + 1)));
        var pts = new List<Vector2>();
        for (int a = 0; a < k; a++) for (int b = 0; b < k; b++)
            { pts.Add(new Vector2(Mathf.Lerp(AreaMin + 20f, AreaMax - 20f, a / (float)(k - 1)), Mathf.Lerp(AreaMin + 20f, AreaMax - 20f, b / (float)(k - 1)))); }
        var c = new Vector2(250f, 250f);
        return pts.OrderByDescending(p => (p - c).sqrMagnitude).Take(n).ToArray();
    }

    // All the steps: result[k][i] = where robot i is after segment k (result[0] = the start). Same for every run.
    public static Vector2[][] BuildSteps(Vector2[] start, int steps, int seed)
    {
        var rng = new System.Random(seed);
        int n = start.Length;
        var all = new List<Vector2[]>();
        var pos = (Vector2[])start.Clone();
        all.Add((Vector2[])pos.Clone());
        var heading = new float[n];
        var centre = new Vector2(250f, 250f);
        for (int i = 0; i < n; i++) { heading[i] = Mathf.Atan2(pos[i].y - centre.y, pos[i].x - centre.x) + (float)(rng.NextDouble() - 0.5) * 1.2f; }
        float group = 1f;
        for (int k = 0; k < steps; k++)
        {
            float len = Mathf.Lerp(SegMin, SegMax, (float)rng.NextDouble());
            if (k % 3 == 2) { len = SegMin; }                                    // now and then a short, sharp one
            group = rng.NextDouble() < 0.7 ? -group : group;
            var next = new Vector2[n];
            var chosen = new bool[n];
            for (int i = 0; i < n; i++)
            {
                float mag = Mathf.Lerp(TurnMinDeg, TurnMaxDeg, (float)rng.NextDouble()) * Mathf.Deg2Rad;
                float sign = rng.NextDouble() < AgainstChance ? -group : group;
                var tries = new List<float[]>();                                  // {turn, length scale}
                foreach (float scale in new[] { 1f, 0.75f, 0.55f })
                {
                    tries.Add(new[] { sign * mag, scale }); tries.Add(new[] { -sign * mag, scale });
                    tries.Add(new[] { sign * mag * 1.4f, scale }); tries.Add(new[] { -sign * mag * 1.4f, scale });
                }
                for (int d = 0; d < 12; d++) { tries.Add(new[] { (d * 30f) * Mathf.Deg2Rad - heading[i], 0.6f }); }   // last resort: any direction
                bool done = false;
                foreach (var tr in tries)
                {
                    float h = heading[i] + tr[0];
                    var end = pos[i] + tr[1] * len * new Vector2(Mathf.Cos(h), Mathf.Sin(h));
                    if (end.x < AreaMin || end.x > AreaMax || end.y < AreaMin || end.y > AreaMax) { continue; }
                    bool ok = true;
                    for (int j = 0; j < i && ok; j++) { ok = Clear(pos[i], end, pos[j], next[j]); }
                    // robots not yet decided stay where they start for the check, then they check against this one
                    for (int j = i + 1; j < n && ok; j++) { ok = Clear(pos[i], end, pos[j], pos[j]); }
                    if (!ok) { continue; }
                    heading[i] = h; next[i] = end; chosen[i] = true; done = true; break;
                }
                if (!done) { next[i] = pos[i]; }
            }
            pos = next; all.Add((Vector2[])pos.Clone());
        }
        return all.ToArray();
    }

    // two straight moves made at the same time (each from its start to its end): do they keep MinGap apart?
    static bool Clear(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
    {
        for (int s = 0; s <= 12; s++)
        {
            float u = s / 12f;
            if (Vector2.Distance(Vector2.Lerp(a0, a1, u), Vector2.Lerp(b0, b1, u)) < MinGap) { return false; }
        }
        return true;
    }

    // ---------------------------------------------------------------- the routine
    public override IEnumerator Run(RoutineContext ctx)
    {
        // the standard opening and ending; between them the restless part, then the plan that calms into the breathing circle
        yield return V2Show.Run(ctx, start => new Plan(start), 100, between: () => Restless(ctx));
    }

    // 1-4: the spread-out formation, the skin conductance read once, the restless segments
    IEnumerator Restless(RoutineContext ctx)
    {
        int n = ctx.Count;
        Physio.Section("restless movement");
        var spread = SpreadSpots(n);
        Swarm.AssignNearest(ctx, spread.Select(p => Vector2Int.RoundToInt(p)).ToArray());
        yield return Swarm.GlideTo(ctx, spread.Select(p => Vector2Int.RoundToInt(p)).ToArray(), 60f, 70);
        yield return Swarm.Wait(800);

        // read the skin conductance once and fix the speed for the whole restless part
        float sc = Physio.Eda, baseline = Physio.EdaBaseline;
        float mult; int speed = Mathf.RoundToInt(InitialSpeed(sc, baseline, out mult));
        Physio.Marker(string.Format("stress speed {0} (sc {1:0.00}, baseline {2:0.00}, x{3:0.00})", speed, sc, baseline, mult));

        // plan every segment from where the robots really stand, then run them one by one
        var from = new Vector2[n];
        for (int i = 0; i < n; i++) { from[i] = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y); }
        var steps = BuildSteps(from, Steps, Seed);
        bool native = ctx.Cubes.All(c => SupportsTargetMove(c));
        var arrived = new Dictionary<Cube, int>();
        Action<Cube, int, Cube.TargetMoveRespondType> onDone = (c, id, resp) => { arrived[c] = id; };
        if (native) { foreach (var c in ctx.Cubes) { c.targetMoveCallback.AddListener("c23", onDone); } }
        for (int k = 1; k < steps.Length; k++)
        {
            var targets = steps[k];
            float longest = 0f; for (int i = 0; i < n; i++) { longest = Mathf.Max(longest, Vector2.Distance(steps[k - 1][i], targets[i])); }
            float estimate = longest / (speed * UnitsPerSecPerSpeed) + 1.5f;       // travel plus the turn on the spot
            if (native)
            {
                int id = (k % 250) + 1;
                for (int i = 0; i < n; i++)
                {
                    var d = targets[i] - steps[k - 1][i];
                    if (d.sqrMagnitude < 4f) { arrived[ctx.Cubes[i]] = id; continue; }      // this one stays put
                    int angle = Mathf.RoundToInt(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    ctx.Cubes[i].TargetMove(Mathf.RoundToInt(targets[i].x), Mathf.RoundToInt(targets[i].y), angle, id, Mathf.CeilToInt(estimate * 2f),
                        Cube.TargetMoveType.RoundBeforeMove, speed, Cube.TargetSpeedType.VariableSpeed, Cube.TargetRotationType.NotRotate);
                }
                float end = Time.time + estimate * 2f;                               // nothing is sent while waiting
                while (Time.time < end && !ctx.Cubes.All(c => arrived.ContainsKey(c) && arrived[c] == id)) { yield return null; }
            }
            else
            {
                yield return Swarm.MoveTo(ctx, targets.Select(p => Vector2Int.RoundToInt(p)).ToArray(), speed, Mathf.CeilToInt(estimate * 2000f));
            }
            yield return Swarm.Wait(PauseMs);
        }
        if (native) { foreach (var c in ctx.Cubes) { c.targetMoveCallback.RemoveListener("c23"); } }
        Swarm.StopAll(ctx);
        yield return Swarm.Wait(400);      // the segment commands have finished; the plan below takes the steering from here
    }

    public static bool SupportsTargetMove(Cube c)
    {
        Version v;
        return c != null && Version.TryParse(c.version, out v) && v >= new Version(2, 1, 0);
    }

    // ---------------------------------------------------------------- the calming plan
    // From where the restless part left the robots: onto one ring, then the breathing circle (large, slow, several times), then
    // open and flatten into the line. `start` is where the robots stand after the last segment.
    public class Plan : V2Show.IPlan, V2Show.IHeading, V2Show.ISections
    {
        const float SectorRadius = 118f, WideRadius = 128f, SmallRadius = 52f, WidenSecs = 12f, BreathSecs = 20f;
        const int Breaths = 4;
        const float TurnUpSecs = 6f, RingRate0 = 0.11f, ReturnSecs = 12f, ExitRadius = 106f, GatherWithin = 20f;
        static readonly Vector2 Centre = new Vector2(250f, 250f);

        readonly Vector2[] start, line;
        readonly float[] sectorTo;
        readonly int n;
        V2Show.WayToLine way;
        readonly float gatherEnd, breatheFrom, stopFrom, ringStop;
        readonly Show.Eased turnRate;
        public float Total { get; private set; }

        public IEnumerable<KeyValuePair<float, string>> Sections
        {
            get
            {
                yield return new KeyValuePair<float, string>(0f, "calming onto the ring");
                yield return new KeyValuePair<float, string>(gatherEnd, "ring widens");
                yield return new KeyValuePair<float, string>(breatheFrom, "breathing circle");
                yield return new KeyValuePair<float, string>(stopFrom, "back to the line");
            }
        }

        public Plan(Vector2[] start)
        {
            this.start = start; n = start.Length;
            line = V2Show.OwnSpots(n);
            var order = V2Show.RingOrder(n);
            sectorTo = new float[n];
            for (int i = 0; i < n; i++) { sectorTo[i] = 135f * Mathf.Deg2Rad + 2f * Mathf.PI * order[i] / n; }
            // the way onto the ring: the slowest clear paths that arrive in time, so the swarm slows gradually as it comes in
            var targets = Enumerable.Range(0, n).Select(i => RingPoint(i, 0f)).ToArray();
            way = V2Show.Approach(start, targets, GatherWithin);
            gatherEnd = way != null ? way.Total + 0.3f : GatherWithin;
            breatheFrom = gatherEnd + TurnUpSecs + WidenSecs;
            stopFrom = breatheFrom + Breaths * BreathSecs;
            ringStop = stopFrom + ReturnSecs;
            Total = ringStop + 0.3f;
            // the ring turns gently and, half way through the final return, stands at the angle from which it flattens into the
            // line without crossing: the turn rate is set to bring it there
            float tMid = stopFrom + ReturnSecs / 2f;
            Show.Eased RateFor(float c) { return new Show.Eased(gatherEnd, 0f, gatherEnd + TurnUpSecs, RingRate0, breatheFrom, c, stopFrom, c, ringStop, 0f); }
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
            float r = Mathf.Lerp(SectorRadius, WideRadius, Shapes.Ease((t - gatherEnd - TurnUpSecs) / WidenSecs));
            for (int b = 0; b < Breaths; b++) { r -= (WideRadius - SmallRadius) * Show.Bump((t - breatheFrom - b * BreathSecs) / BreathSecs); }
            return r + (ExitRadius - WideRadius) * Shapes.Ease((t - stopFrom) / (ReturnSecs * 0.5f));
        }

        public Vector2 Position(int slot, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            Vector2 p;
            if (t < gatherEnd) { p = way != null ? way.Position(slot, t) : Vector2.Lerp(start[slot], RingPoint(slot, 0f), Shapes.Ease(t / gatherEnd)); }
            else { p = RingPoint(slot, Ring(t), Radius(t)); }
            if (t > stopFrom) { p = Vector2.Lerp(p, line[slot], Shapes.Ease((t - stopFrom) / ReturnSecs)); }
            return p;
        }

        public float Heading(int slot, float t) { return 270f; }
        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int slot, float t) { return false; }
    }
}
