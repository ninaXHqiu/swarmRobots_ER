using System.Collections;
using System.Linq;
using UnityEngine;

// v2 Maze to Breathing Circle.
//   1. One shared winding route. A closed loop round the mat centre: a broad outer sweep, a tight
//      turn in toward the centre, an inner sweep back the other way, and a curved return to the
//      outside (the outline of a thick letter C; it never crosses itself). Each robot joins at
//      the nearest point of the route from wherever it stands, moved along only as far as it
//      takes to keep clear of the robots already placed. The approach is short and ends moving
//      with the route, so a robot is already travelling in the right direction as it joins.
//   2. Changing gaps. The robots have slightly different speeds, so some catch up and travel
//      close together while bigger gaps open elsewhere. A faster robot eases off smoothly as it
//      nears the one ahead; they keep their order.
//   3. Into a turning circle. The inner sweep is part of a circle round the centre. From the
//      switch on, a robot reaching the end of the inner sweep stays on that circle instead of
//      returning outside; the tight turn in from the outer sweep is the way onto it for
//      everyone still outside. Each robot is given a place in the turning ring and drifts onto
//      it while still travelling, so speeds match and the gaps even out while the ring moves.
//   4. Breathing. The whole ring keeps turning gently on one shared radius, which opens wide and
//      then slowly shrinks, eases through its smallest size and opens again, several times.
// It runs on its own: no line-up and no Messenger introduction.
public class V2MazeBreathing : SwarmRoutine
{
    public override string Name { get { return "v2 Maze to Breathing Circle"; } }
    public override int MinRobots { get { return 2; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        // the fixed opening (the Messenger's introduction) and ending (all in the line, facing the participant)
        yield return V2Show.Run(ctx, start => new Plan(start), 80);
    }

    // ---- The route ----
    const float OuterRadius = 150f;        // broad outer sweep
    const float InnerRadius = 82f;         // inner sweep; also the ring the robots gather on
    const float GapHalfDeg = 36f;          // the opening of the C, at the far side from the participant
    const float TurnRadius = (OuterRadius - InnerRadius) / 2f;   // the tight turns at the C's two ends

    // ---- Joining ----
    const float ApproachSecs = 4.5f;       // at least; longer if a robot has to wait for another to pass
    const float SetOffEarly = 0.8f;        // a robot starts along the route this long before its approach ends...
    const float SetOffSecs = 3f;           // ...and takes this long to reach its pace
    const float EntryGap = 66f;            // entry points at least this far apart along the route

    // ---- Travelling ----
    const float Pace = 55f;                // mat units per second
    static readonly float[] PaceShare = { 0.8f, 1.2f, 1.08f, 0.86f, 1.22f, 0.95f, 0.82f, 1.15f };   // by order along the route
    const float CloseGap = 58f;            // a robot this near the one ahead goes no faster than it
    const float FreeGap = 115f;            // further back than this it travels at its own pace
    const float Wobble = 0.30f;            // how far a robot's pace swings round its own pace
    static readonly float[] WobbleSecs = { 7.5f, 11f, 9f, 13f, 8.2f, 10.4f, 12.3f, 6.8f };
    const float MazeSecs = 30f;            // travelling the route before the switch, at least
    const float SwitchWithinSecs = 12f;    // ...and at most this much longer, waiting for a clear moment to switch
    const float MergeRoom = 46f;           // closest two robots may come while the ring forms

    // ---- Ring ----
    const float RingPace = 48f;            // along the ring as it forms
    const float MatchSecs = 10f;           // drifting onto ring places
    const float SettleSecs = 2f;           // the complete ring turns on before it opens
    const float WidenSecs = 12f;
    const float WideRadius = 128f;         // 77 inside the mat edge
    const float SmallRadius = 52f;         // 8 robots 40 apart
    const float BreathSecs = 20f;         // wide -> small -> wide: slow, about 12 units a second at most, so the wheels stay quiet
    const int Breaths = 4;
    const float CalmDegPerSec = 6f;        // turning while breathing: very gentle (about 13 units a second at the widest)
    const float StopSecs = 6f;

    const float Step = 0.02f;

    public class Plan : V2Show.IPlan
    {
        readonly int n;
        readonly Vector2[] start;
        readonly float a0, a1, outerLength, turnLength, innerLength, length, toRing;   // toRing: route distance at which the inner sweep (the ring) starts
        float[] entry; float approach; V2Show.WayToLine way;
        float[][] route;           // per robot: distance along the route, every Step, until the switch
        float[][] ring;            // then: distance past the ring's start, every Step, until the ring is complete
        float switchAt, ringAt;             // the switch; the complete ring starts opening
        float[] ringAngle;         // each robot's angle on the ring at ringAt
        readonly Show.Eased turnRate;       // radians per second, from ringAt
        readonly float breatheFrom;
        public float Total { get; private set; }
        float ringStop; V2Show.WayToLine home;
        public float SwitchAt { get { return switchAt; } }
        public string Approach { get { return (way.Clear ? "clear" : "NOT clear") + " in " + way.Total.ToString("F1") + " s"; } }
        public float RingAt { get { return ringAt; } }
        public float BreatheFrom { get { return breatheFrom; } }

        public Plan(Vector2[] start)
        {
            this.start = start;
            n = start.Length;
            float gap = GapHalfDeg * Mathf.Deg2Rad;
            a0 = -Mathf.PI / 2f + gap;                 // outer sweep starts just right of the opening...
            a1 = a0 + 2f * Mathf.PI - 2f * gap;        // ...and goes clockwise round to just left of it
            outerLength = OuterRadius * (a1 - a0);
            turnLength = Mathf.PI * TurnRadius;
            innerLength = InnerRadius * (a1 - a0);
            length = outerLength + innerLength + 2f * turnLength;
            toRing = outerLength + turnLength;

            // 1. Entry points: the nearest point of the route for each robot, robots nearest the
            // route choosing first, each at least EntryGap along the route from those already
            // chosen. The robots then share those points out for the shortest trips in all, so
            // their approaches do not cross.
            int samples = Mathf.CeilToInt(length / 2f);
            var points = new float[n]; var chosen = new bool[n];
            foreach (int i in Enumerable.Range(0, n).OrderBy(i => Enumerable.Range(0, samples).Min(k => (RouteAt(k * 2f) - start[i]).sqrMagnitude)))
            {
                float best = float.MaxValue;
                for (int k = 0; k < samples; k++)
                {
                    float s = k * 2f;
                    bool free = true;
                    for (int j = 0; j < n; j++) { if (chosen[j] && Apart(s, points[j]) < EntryGap) { free = false; break; } }
                    float d = (RouteAt(s) - start[i]).sqrMagnitude;
                    if (free && d < best) { best = d; points[i] = s; }
                }
                chosen[i] = true;
            }
            var takes = Swarm.LeastTravel(start, points.Select(s => RouteAt(s)).ToArray());
            entry = new float[n];
            for (int i = 0; i < n; i++) { entry[i] = points[takes[i]]; }
            // The approaches: direct, planned clear of each other (a robot bows aside or leaves a
            // moment later where two would meet)
            way = new V2Show.WayToLine(start, entry.Select(s => RouteAt(s)).ToArray());
            approach = Mathf.Max(ApproachSecs, way.Total);

            // 2. Travelling with different paces, easing off behind the robot ahead. Each robot
            // sets off along the route as it arrives on it, picking up its pace smoothly.
            var order = Enumerable.Range(0, n).OrderBy(i => entry[i]).ToArray();   // order along the route
            var pace = new float[n]; var ahead = new int[n];
            for (int r = 0; r < n; r++) { pace[order[r]] = Pace * PaceShare[r % PaceShare.Length]; ahead[order[r]] = order[(r + 1) % n]; }
            int steps = Mathf.RoundToInt((approach + MazeSecs + SwitchWithinSecs) / Step);
            route = new float[n][]; var speed = new float[n][];
            for (int i = 0; i < n; i++)
            {
                route[i] = new float[steps + 1]; speed[i] = new float[steps + 1];
                route[i][0] = entry[i];
            }
            for (int k = 0; k < steps; k++)
            {
                float t = k * Step;
                for (int i = 0; i < n; i++)
                {
                    float room = n > 1 ? Mathf.Repeat(route[ahead[i]][k] - route[i][k], length) : float.MaxValue;
                    // each robot's pace drifts up and down at its own rhythm (some fast, some slow, now
                    // and then a quick adjustment), and settles to the one shared ring pace by the end
                    float settle = Shapes.Ease((t - (approach + MazeSecs)) / SwitchWithinSecs);
                    float drift = 1f + Wobble * (1f - settle) * (Mathf.Sin(2f * Mathf.PI * t / WobbleSecs[i % WobbleSecs.Length] + 1.9f * i)
                                                               + 0.45f * Mathf.Sin(2f * Mathf.PI * t / (WobbleSecs[(i + 3) % WobbleSecs.Length] * 0.37f) + 0.8f * i));
                    speed[i][k] = pace[i] * drift * Shapes.Ease((t - (way.ArrivesAt(i) - SetOffEarly)) / SetOffSecs) * Shapes.Ease((room - CloseGap) / (FreeGap - CloseGap));
                    route[i][k + 1] = route[i][k] + speed[i][k] * Step;
                }
            }

            // 3. The switch: the first moment (from MazeSecs on) at which the robots can drift onto
            // ring places and merge with room to spare
            float bestRoom = -1f;
            for (float wait = 0f; wait <= SwitchWithinSecs; wait += 0.5f)
            {
                float room = FormRing(approach + MazeSecs + wait, speed);
                if (room > bestRoom) { bestRoom = room; switchAt = approach + MazeSecs + wait; }
                if (room >= MergeRoom) { break; }
            }
            FormRing(switchAt, speed);

            // 4. The complete ring: opens wide while the turn calms, then breathes
            float calm = CalmDegPerSec * Mathf.Deg2Rad;
            breatheFrom = ringAt + WidenSecs;
            float stopFrom = breatheFrom + Breaths * BreathSecs;
            ringStop = stopFrom + StopSecs;
            turnRate = new Show.Eased(ringAt, RingPace / InnerRadius, breatheFrom, calm, stopFrom, calm, ringStop, 0f);
            // the fixed ending: the ring slows to a stop, then each robot returns to its own place in the line
            var atStop = new Vector2[n];
            Total = ringStop;
            for (int i = 0; i < n; i++) { atStop[i] = Position(i, ringStop); }
            home = new V2Show.WayToLine(atStop, V2Show.OwnSpots(n), 28f);
            Total = ringStop + home.Total + 0.5f;
        }

        // From the switch each robot is measured by its distance past the start of the ring
        // (negative: still that far to go along the route). Ring places are evenly spaced and
        // turn at the ring's pace; they carry on backwards along the route, so a robot still
        // outside travels with its place and arrives on the ring in it. Places are given out in
        // the order the robots already have round the ring, turned to where the robots have the
        // least to make up. Each robot drifts from its own position and speed onto its place.
        // Returns the closest two robots come until the ring is complete.
        float FormRing(float at, float[][] speed)
        {
            int k0 = Mathf.RoundToInt(at / Step);
            float around = 2f * Mathf.PI * InnerRadius, spacing = around / n;
            var past = new float[n];
            for (int i = 0; i < n; i++)
            {
                float r = Mathf.Repeat(route[i][k0] - toRing, length);
                past[i] = r <= innerLength ? r : r - length;
            }
            // order round the ring, counting a robot still outside by where it will come in
            var round = Enumerable.Range(0, n).OrderBy(i => Mathf.Repeat(past[i], around)).ToArray();
            float bestCost = float.MaxValue, bestRef = 0f; int bestShift = 0;
            for (int shift = 0; shift < n; shift++)
            {
                for (int g = 0; g < 16; g++)
                {
                    float reference = spacing * g / 16f, cost = 0f;
                    for (int r = 0; r < n; r++)
                    {
                        float off = Mathf.DeltaAngle(0f, (past[round[r]] - (reference + ((r + shift) % n) * spacing)) / around * 360f) / 360f * around;
                        cost += off * off;
                    }
                    if (cost < bestCost) { bestCost = cost; bestRef = reference; bestShift = shift; }
                }
            }
            int more = Mathf.RoundToInt((MatchSecs + SettleSecs + 5f + (length + 200f) / RingPace) / Step);
            int matched = Mathf.RoundToInt(MatchSecs / Step);
            ring = new float[n][];
            float joined = 0f;
            for (int r = 0; r < n; r++)
            {
                int i = round[r];
                // how far the robot is from its place (the short way round)
                float off = Mathf.DeltaAngle(0f, (past[i] - (bestRef + ((r + bestShift) % n) * spacing)) / around * 360f) / 360f * around;
                float faster = speed[i][Mathf.Max(0, k0 - 1)] - RingPace;
                ring[i] = new float[more + 1];
                int on = -1;
                for (int k = 0; k <= more; k++)
                {
                    float u = Mathf.Clamp01(k / (float)matched);
                    // the place, plus what is left of the robot's own offset and own speed
                    ring[i][k] = past[i] - off + RingPace * k * Step + off * (1f - Shapes.Ease(u)) + faster * MatchSecs * u * (1f - u) * (1f - u);
                    if (on < 0 && ring[i][k] >= 0f) { on = k; }
                }
                joined = Mathf.Max(joined, (on < 0 ? more : on) * Step);
            }
            int toRingAt = Mathf.Min(more, Mathf.RoundToInt((Mathf.Max(joined, MatchSecs) + SettleSecs) / Step));
            ringAt = at + toRingAt * Step;
            ringAngle = new float[n];
            for (int i = 0; i < n; i++) { ringAngle[i] = a1 - ring[i][toRingAt] / InnerRadius; }

            float room = float.MaxValue;
            for (int k = 0; k <= toRingAt; k += 5)
            {
                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++) { room = Mathf.Min(room, (RingOrRoute(ring[i][k]) - RingOrRoute(ring[j][k])).magnitude); }
                }
            }
            return room;
        }

        // Route distance between two points of the closed route, the short way
        float Apart(float s1, float s2)
        {
            float d = Mathf.Repeat(s1 - s2, length);
            return Mathf.Min(d, length - d);
        }

        static Vector2 Unit(float a) { return new Vector2(Mathf.Cos(a), Mathf.Sin(a)); }

        // The point `s` along the route: outer sweep clockwise, tight turn in, inner sweep back
        // anticlockwise, curved return to the outside
        Vector2 RouteAt(float s)
        {
            s = Mathf.Repeat(s, length);
            float middle = (OuterRadius + InnerRadius) / 2f;
            if (s < outerLength) { return Shapes.Centre + OuterRadius * Unit(a0 + s / OuterRadius); }
            s -= outerLength;
            if (s < turnLength) { return Shapes.Centre + middle * Unit(a1) + TurnRadius * Unit(a1 + s / TurnRadius); }
            s -= turnLength;
            if (s < innerLength) { return Shapes.Centre + InnerRadius * Unit(a1 - s / InnerRadius); }
            s -= innerLength;
            return Shapes.Centre + middle * Unit(a0) + TurnRadius * Unit(a0 + Mathf.PI + s / TurnRadius);
        }

        // The point a distance `past` beyond the ring's start: on the ring, or (negative) still on the route
        Vector2 RingOrRoute(float past)
        {
            return past >= 0f ? Shapes.Centre + InnerRadius * Unit(a1 - past / InnerRadius) : RouteAt(toRing + past);
        }

        public float Radius(float t)
        {
            float r = Mathf.Lerp(InnerRadius, WideRadius, Shapes.Ease((t - ringAt) / WidenSecs));
            for (int b = 0; b < Breaths; b++) { r -= (WideRadius - SmallRadius) * Show.Bump((t - breatheFrom - b * BreathSecs) / BreathSecs); }
            return r;
        }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            if (home != null && t >= ringStop) { return home.Position(i, t - ringStop); }
            if (t >= ringAt) { return Shapes.Centre + Radius(t) * Unit(ringAngle[i] - (turnRate.Integral(t) - turnRate.Integral(ringAt))); }
            if (t >= switchAt) { return RingOrRoute(At(ring[i], t - switchAt)); }
            Vector2 onRoute = RouteAt(At(route[i], t));
            // the approach: from where the robot stands to its entry point; it starts along the
            // route just before it arrives, so the approach runs into the travelling
            return t < way.ArrivesAt(i) ? way.Position(i, t) + onRoute - RouteAt(entry[i]) : onRoute;
        }

        static float At(float[] every, float t)
        {
            float x = Mathf.Clamp(t / Step, 0f, every.Length - 1.001f);
            int k = (int)x;
            return Mathf.Lerp(every[k], every[k + 1], x - k);
        }

        public Vector2 LineSpot(int i) { return V2Show.OwnSpots(n)[i]; }
        public bool InLane(int i, float t) { return false; }
    }
}
