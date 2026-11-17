using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using toio;
using UnityEngine;

// v2 Stress, restless (C23): the swarm mirrors high arousal, then calms into the breathing circle.
// Inspired by the fear pattern of "From Motions to Emotions" (Santos & Egerstedt), adapted to stress.
//   0. The standard opening (the messenger's introduction), like every other routine.
//   1. Every robot starts at once with sharp moves of its own: an angular dog-leg to its place in a spread-out formation.
//   2. The participant's baseline and current skin conductance are read ONCE.
//   3. initial_speed = clamp(BaseSpeed x stress_multiplier, MinSpeed, MaxSpeed), where
//      stress_multiplier = 1 + Gain x (current - baseline) / baseline. At or below baseline: MinSpeed.
//      The speed is not changed again during the restless part.
//   4. Each robot moves in short straight segments with sharp turns between them, on its own clock: a segment is ONE motor
//      command (the robot's own TargetMove: rotate, drive straight to the point), and the robot's next one is chosen at random
//      (turn, length, a beat of its own) when IT has arrived, without waiting for the others. Nothing is sent while a segment
//      runs, so there are no per-frame corrections and no wiggling. Robots that have no TargetMove (old firmware) fall back to the
//      ordinary MoveTo for that segment.
//   5. Each robot, at its own random moment, stops panicking and goes to its own place in the ring (its last run along the way the
//      ring will turn). On its place it stays, and turns about at random times to random directions (some do not turn at all); its
//      last turn always points it the way the ring will turn. When every robot is on its place, the ring starts to turn, breathes (large, slow) several times, opens and flattens into the line,
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
    public const float SettleMinSecs = 20f, SettleMaxSecs = 42f;          // each robot stops panicking at its own random moment in this range and goes to its place in the ring
    public const int MaxTurns = 2;                                        // a settled robot turns about 0 to this many times, at random times and to random directions; the last always to the way the ring will turn
    public const float TurnWindowSecs = 8f;                               // the turns happen within this long after it has settled
    public const int PauseMinMs = 0, PauseMaxMs = 450;                    // each robot's own random beat between its segments
    public const float RingGap = 44f;                                     // the last run onto the ring only has to keep this far from the robots already there
    public const float ForceAfterSecs = 3f;                               // a first leg that has been blocked this long goes anyway
    public const int RandomSeed = -1;                                     // -1: different every run; 0 or more: the same every run
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

    // A fixed set of steps, used only by the plan check (Editor/V2TestCheck) to get a plausible spread-out start for the calming plan;
    // the routine itself chooses its segments at run time (Restless).
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

    // 1-4: the panic, then each robot settles on its own place in the ring, turns about at random, and the circling starts when all are there
    class Bot
    {
        public Vector2 to; public bool active, turning; public int id, count, state;     // state: 0 panic, 1 on its way to the ring, 2 settled
        public float giveUpAt, pauseUntil, heading, waitingSince = -1f, settleAt, arrivedAt;
        public Queue<Vector2> forced = new Queue<Vector2>();                              // the first legs, then the last ones onto the ring
        public List<float[]> turns = new List<float[]>();                                  // {seconds after arriving, direction in degrees}, the last one always the way the ring will turn
    }

    IEnumerator Restless(RoutineContext ctx)
    {
        int n = ctx.Count;
        Physio.Section("restless movement");
        // which robot takes which spot of the spread-out start formation (bookkeeping only: nothing moves here)
        var spread = SpreadSpots(n);
        Swarm.AssignNearest(ctx, spread.Select(p => Vector2Int.RoundToInt(p)).ToArray());

        // read the skin conductance once and fix the speed for the whole restless part
        float sc = Physio.Eda, baseline = Physio.EdaBaseline;
        float mult; int speed = Mathf.RoundToInt(InitialSpeed(sc, baseline, out mult));
        Physio.Marker(string.Format("stress speed {0} (sc {1:0.00}, baseline {2:0.00}, x{3:0.00})", speed, sc, baseline, mult));

        var rng = RandomSeed >= 0 ? new System.Random(RandomSeed) : new System.Random();
        bool native = ctx.Cubes.All(c => SupportsTargetMove(c));
        var arrived = new Dictionary<Cube, int>();
        Action<Cube, int, Cube.TargetMoveRespondType> onDone = (c, id, resp) => { arrived[c] = id; };
        if (native) { foreach (var c in ctx.Cubes) { c.targetMoveCallback.AddListener("c23", onDone); } }

        // Every robot starts at once with sharp moves of its own: an angular dog-leg from where it stands to its spot in the spread-out formation.
        // Each has its own moment (settleAt) when it stops panicking and heads for its place in the ring.
        float t0 = Time.time;
        var bots = new Bot[n];
        var pos = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            pos[i] = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y);
            bots[i] = new Bot { pauseUntil = t0 + (float)rng.NextDouble() * 0.4f, settleAt = t0 + Mathf.Lerp(SettleMinSecs, SettleMaxSecs, (float)rng.NextDouble()) };
            Vector2 d = spread[i] - pos[i];
            if (d.magnitude > 90f)
            {
                var side = new Vector2(-d.y, d.x).normalized * (rng.NextDouble() < 0.5 ? 1f : -1f);
                var elbow = pos[i] + d * (0.4f + 0.2f * (float)rng.NextDouble()) + side * d.magnitude * 0.35f;
                elbow = new Vector2(Mathf.Clamp(elbow.x, AreaMin, AreaMax), Mathf.Clamp(elbow.y, AreaMin, AreaMax));
                bots[i].forced.Enqueue(elbow);
            }
            bots[i].forced.Enqueue(spread[i]);
            bots[i].heading = Mathf.Atan2(d.y, d.x);
        }

        bool compressed = false;
        while (true)
        {
            float now = Time.time;
            if (ctx.CubeManager.synced)
            {
                for (int i = 0; i < n; i++) { pos[i] = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y); }
                var notIn = bots.Where(x => x.state != 2).ToList();
                if (notIn.Count == 1 && notIn[0].state == 0) { notIn[0].settleAt = Mathf.Min(notIn[0].settleAt, now); }      // the last one does not keep the others waiting
                if (notIn.Count == 0 && !compressed)
                {
                    compressed = true;                                                                                       // all in: no more waiting for turns still to come, each only makes its last one, now
                    foreach (var x in bots) { if (x.turns.Count > 0) { var lastTurn = x.turns[x.turns.Count - 1]; lastTurn[0] = 0f; x.turns.Clear(); x.turns.Add(lastTurn); x.arrivedAt = now - 1f; } }
                }
                for (int i = 0; i < n; i++)
                {
                    var b = bots[i];
                    if (b.active)
                    {
                        int got; bool back = arrived.TryGetValue(ctx.Cubes[i], out got) && got == b.id;
                        bool close = !b.turning && (pos[i] - b.to).magnitude < 12f;       // (a turn on the spot ends only by its report or its time)
                        if (back || close || now > b.giveUpAt)
                        {
                            b.active = false; b.turning = false;
                            b.pauseUntil = now + Mathf.Lerp(PauseMinMs, PauseMaxMs, (float)rng.NextDouble()) / 1000f;   // its own beat before the next, nobody else waits
                        }
                        else
                        {
                            if (!native && !b.turning) { var q = Vector2Int.RoundToInt(b.to); ctx.Navigators[i].handle.Move2Target(q.x, q.y, maxSpd: speed).Exec(); }   // old firmware: steered, as before
                            continue;
                        }
                    }
                    if (now < b.pauseUntil && b.state != 2) { continue; }

                    if (b.state == 2)
                    {
                        // settled on its place: turns about at the times it drew, the last one to the way the ring will turn
                        if (b.turns.Count > 0 && now - b.arrivedAt >= b.turns[0][0] && native)
                        {
                            float deg = b.turns[0][1]; b.turns.RemoveAt(0);
                            var here = Vector2Int.RoundToInt(pos[i]);
                            b.id = (++b.count % 250) + 1; b.active = true; b.turning = true; b.giveUpAt = now + 3f; b.to = pos[i];
                            int dg = Mathf.RoundToInt(Mathf.Repeat(deg, 360f));
                            ctx.Cubes[i].TargetMove(here.x + Mathf.RoundToInt(4f * Mathf.Cos(dg * Mathf.Deg2Rad)), here.y + Mathf.RoundToInt(4f * Mathf.Sin(dg * Mathf.Deg2Rad)), dg, b.id, 4,
                                Cube.TargetMoveType.RoundBeforeMove, 40, Cube.TargetSpeedType.UniformSpeed, Cube.TargetRotationType.AbsoluteLeastAngle);
                        }
                        continue;
                    }
                    if (b.state == 1 && b.forced.Count == 0)
                    {
                        // it has reached its place in the ring: stay, and draw when to turn (some robots do not turn at all)
                        b.state = 2; b.arrivedAt = now;
                        int k = rng.Next(0, MaxTurns + 1);
                        float tan = TangentDeg(i, n);
                        bool aligned = Mathf.Abs(Mathf.DeltaAngle(b.heading * Mathf.Rad2Deg * 2f, tan * 2f)) / 2f < 25f;   // already pointing along its line (either way)?
                        if (k == 0 && !aligned) { k = 1; }                                // a robot that arrives pointing wrong must turn; one that arrives right may stay as it is
                        for (int q = 0; q < k; q++)
                        {
                            bool last = q == k - 1;
                            b.turns.Add(new[] { Mathf.Lerp(0.4f, TurnWindowSecs, (float)rng.NextDouble()), last ? tan : (float)rng.NextDouble() * 360f });
                        }
                        b.turns.Sort((x, y) => x[0].CompareTo(y[0]));
                        if (k > 0) { b.turns[b.turns.Count - 1][1] = tan; }                // whichever comes last points the way the ring will turn
                        continue;
                    }
                    if (b.state == 0 && now >= b.settleAt)
                    {
                        // time to go to its place in the ring: straight there
                        b.state = 1; b.forced.Clear(); b.forced.Enqueue(RingSpot(i, n));
                    }
                    if (b.state == 0 && now >= b.settleAt) { continue; }
                    Vector2 end;
                    if (!Pick(i, pos, bots, rng, now, out end)) { if (b.waitingSince < 0f) { b.waitingSince = now; } b.pauseUntil = now + 0.25f; continue; }   // blocked for the moment: wait, do not wiggle
                    b.waitingSince = -1f;
                    Vector2 dd = end - pos[i];
                    float travel = dd.magnitude / (speed * UnitsPerSecPerSpeed) + 1.5f;
                    b.to = end; b.active = true; b.turning = false; b.id = (++b.count % 250) + 1; b.giveUpAt = now + travel * 2f; b.heading = Mathf.Atan2(dd.y, dd.x);
                    if (native)
                    {
                        ctx.Cubes[i].TargetMove(Mathf.RoundToInt(end.x), Mathf.RoundToInt(end.y), Mathf.RoundToInt(b.heading * Mathf.Rad2Deg), b.id, Mathf.CeilToInt(travel * 2f),
                            Cube.TargetMoveType.RoundBeforeMove, speed, Cube.TargetSpeedType.VariableSpeed, Cube.TargetRotationType.NotRotate);
                    }
                }
                // everybody on its place, every turn made: the circling can start
                if (bots.All(x => x.state == 2 && !x.active && x.turns.Count == 0)) { break; }       // straight into the breathing: no pause
                if (now > t0 + SettleMaxSecs + 60f) { break; }      // never hold the show up for a robot that cannot get there: the plan gathers it
            }
            yield return null;
        }
        if (native) { foreach (var c in ctx.Cubes) { c.targetMoveCallback.RemoveListener("c23"); } }
        // no stop and no pause: the robots are already where they stay, and the plan below goes straight into the circling
    }

    // where robot slot i stands in the ring, and the way it points when the breathing starts (the same ring as the plan below)
    static Vector2 RingSpot(int slot, int n) { return Plan.RingSpotOf(slot, n); }
    static float TangentDeg(int slot, int n) { return Plan.TangentDegOf(slot, n); }

    // The next segment for robot i, chosen from where everybody stands now. The first legs are fixed (to its spot in the formation);
    // after that it is random: a sharp turn from its last direction, a random length, kept inside the play area, clear of every other robot's place and
    // of the segment each other robot is on, and preferring the candidate that leaves it furthest from the others (so the swarm stays spread out).
    bool Pick(int i, Vector2[] pos, Bot[] bots, System.Random rng, float now, out Vector2 end)
    {
        var b = bots[i];
        if (b.forced.Count > 0)
        {
            end = b.forced.Peek();
            bool free = ClearOnline(i, pos[i], end, pos, bots, b.state == 1 ? RingGap : MinGap) || (b.waitingSince >= 0f && now - b.waitingSince > ForceAfterSecs);
            if (free) { b.forced.Dequeue(); }
            return free;
        }
        float bestScore = float.MinValue; end = pos[i]; bool found = false;
        for (int k = 0; k < 20; k++)
        {
            float h = k < 14
                ? b.heading + (rng.NextDouble() < 0.5 ? -1f : 1f) * Mathf.Lerp(TurnMinDeg, TurnMaxDeg, (float)rng.NextDouble()) * Mathf.Deg2Rad * (k >= 8 ? 1.3f : 1f)
                : (float)rng.NextDouble() * 2f * Mathf.PI;
            float len = Mathf.Lerp(SegMin, SegMax, (float)rng.NextDouble()) * (k % 3 == 2 ? 0.7f : 1f);
            var e = pos[i] + len * new Vector2(Mathf.Cos(h), Mathf.Sin(h));
            if (e.x < AreaMin || e.x > AreaMax || e.y < AreaMin || e.y > AreaMax) { continue; }
            if (!ClearOnline(i, pos[i], e, pos, bots)) { continue; }
            float nearest = float.MaxValue;
            for (int j = 0; j < pos.Length; j++) { if (j != i) { nearest = Mathf.Min(nearest, Vector2.Distance(e, pos[j])); } }
            float score = Mathf.Min(nearest, 200f) + (float)rng.NextDouble() * 60f;
            if (score > bestScore) { bestScore = score; end = e; found = true; }
        }
        return found;
    }

    // Does a move from p to e stay clear of every other robot, wherever on its way that robot is? (Each other robot is taken to be anywhere on
    // the segment it is travelling, or where it stands.) Timing does not matter, so robots need not move in step.
    static bool ClearOnline(int i, Vector2 p, Vector2 e, Vector2[] pos, Bot[] bots, float gap = MinGap)
    {
        for (int j = 0; j < pos.Length; j++)
        {
            if (j == i) { continue; }
            Vector2 a = pos[j], c = bots[j].active ? bots[j].to : pos[j];
            float need = Mathf.Min(gap, Vector2.Distance(p, a) - 0.5f);        // robots that already stand closer than MinGap (the line) must not come any closer
            for (int s = 0; s <= 10; s++)
            {
                var q = Vector2.Lerp(p, e, s / 10f);
                if (DistToSegment(q, a, c) < need) { return false; }
            }
        }
        return true;
    }

    static float DistToSegment(Vector2 q, Vector2 a, Vector2 b)
    {
        var ab = b - a; float l2 = ab.sqrMagnitude;
        float u = l2 < 1e-4f ? 0f : Mathf.Clamp01(Vector2.Dot(q - a, ab) / l2);
        return Vector2.Distance(q, a + u * ab);
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
            bool onRing = Enumerable.Range(0, n).All(i => Vector2.Distance(start[i], targets[i]) < 25f);   // the restless part has put every robot on its place already
            way = onRing ? null : V2Show.Approach(start, targets, GatherWithin);
            gatherEnd = onRing ? 0.3f : (way != null ? way.Total + 0.3f : GatherWithin);
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

        // where robot slot i stands in the ring and which way it points when the ring starts to turn (the same ring as below)
        public static Vector2 RingSpotOf(int slot, int n) { float a = SectorOf(slot, n); return Centre + SectorRadius * new Vector2(Mathf.Cos(a), Mathf.Sin(a)); }
        public static float TangentDegOf(int slot, int n) { return SectorOf(slot, n) * Mathf.Rad2Deg + 90f; }
        static float SectorOf(int slot, int n) { return 135f * Mathf.Deg2Rad + 2f * Mathf.PI * V2Show.RingOrder(n)[slot] / n; }

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
