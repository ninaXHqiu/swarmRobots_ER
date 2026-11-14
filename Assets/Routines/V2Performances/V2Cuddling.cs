using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using toio;

// v2 Cuddling (C12).
//   1. The conductor (the Messenger's robot) stays where the introduction leaves it and keeps
//      turning gently in place, its centre fixed. The first robot approaches it slowly from the
//      left. When that robot is close, the conductor eases out of the turn and the two make one
//      gentle touch. The first arrival stays beside the conductor.
//   2. The others arrive one at a time, right, left, right, left, then the last two, each straight
//      to an open place around the pair. While the next one is on its way, neighbours in the group
//      lean in, touch lightly, ease apart a little, come close again, and ease off. After the last
//      arrival the whole group touches three times.
//   3. The robot that arrived last peels away first, the others follow one by one at even
//      intervals onto the same heart outline, the same direction at an even pace, joining at the
//      same place. Several laps.
//   4. The heart outline slowly rounds into a circle (which slides to the middle of the mat), the
//      gaps even out to 45 degrees, the circle turns for a few laps, then it opens and flattens into
//      the line, each robot onto its own place.
// Robots are numbered by line slot, 0..7 from the left: the conductor is slot 3, the first arrival
// slot 2. The order the robots leave in is the order round the ring that flattens into the line
// without crossing (V2Show.RingOrder, here the reverse way round).
public class V2Cuddling : SwarmRoutine
{
    public override string Name { get { return "v2 Cuddling"; } }
    public override string Code { get { return "C12"; } }
    public override string Note { get { return "Cuddle, heart, circle"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        // the fixed opening (the Messenger's introduction) and ending (all in the line, facing the participant)
        Plan plan = null;
        yield return V2Show.Run(ctx, start => { plan = new Plan(start); return plan; }, 80,
            skip: (i, t) => plan != null && i == Plan.Conductor && plan.Spinning(t),
            extra: t =>
            {
                if (plan == null || !plan.Spinning(t)) { return; }
                int sp = Mathf.RoundToInt(plan.SpinSpeed(t));
                ctx.Cubes[Plan.Conductor].Move(sp, -sp, 300, Cube.ORDER_TYPE.Strong);
            });
    }

    public class Plan : V2Show.IPlan
    {
        public const int Conductor = 3, First = 2;

        // ---- The group: places, found by search so that straight paths in and out stay clear ----
        const float Gap = 44f;
        struct Arrival { public int slot; public Vector2 offset; public float start; }   // offset from the conductor
        static readonly Arrival[] Arrivals =
        {
            new Arrival { slot = 5, offset = new Vector2(31f, 31f),   start = 15f },   // right: lower right of the conductor
            new Arrival { slot = 1, offset = new Vector2(-75f, 31f),  start = 24f },   // left: lower left of the first arrival
            new Arrival { slot = 4, offset = new Vector2(44f, 0f),    start = 33f },   // right: beside the conductor
            new Arrival { slot = 0, offset = new Vector2(-88f, 0f),   start = 42f },   // left: beside the first arrival
            new Arrival { slot = 6, offset = new Vector2(-75f, -31f), start = 50f },   // right: round to the upper left
            new Arrival { slot = 7, offset = new Vector2(0f, -43f),   start = 58f },   // right: the last, above the conductor
        };
        const float ArriveSecs = 8f;
        const float FirstStart = 4f, FirstSecs = 9f;
        static readonly Vector2 FirstOffset = new Vector2(-Gap, 0f);

        // ---- Spinning, touching ----
        const float SpinFrom = 1f, SpinUpSecs = 1.5f, SpinDownFrom = 12f, SpinDownSecs = 2f, SpinSpeedUnits = 12f;
        public bool Spinning(float t) { return t > SpinFrom && t < SpinDownFrom + SpinDownSecs; }
        public float SpinSpeed(float t) { return SpinSpeedUnits * Shapes.Ease((t - SpinFrom) / SpinUpSecs) * (1f - Shapes.Ease((t - SpinDownFrom) / SpinDownSecs)); }
        const float FirstTouchAt = 13.5f;
        const float PhraseSecs = 5.2f;
        static float Lean(float u)
        {
            if (u <= 0f || u >= PhraseSecs) { return 0f; }
            if (u < 1.2f) { return Shapes.Ease(u / 1.2f); }
            if (u < 1.6f) { return 1f; }
            if (u < 2.4f) { return Mathf.Lerp(1f, 0.4f, Shapes.Ease((u - 1.6f) / 0.8f)); }
            if (u < 3.2f) { return Mathf.Lerp(0.4f, 1f, Shapes.Ease((u - 2.4f) / 0.8f)); }
            if (u < 3.6f) { return 1f; }
            return 1f - Shapes.Ease((u - 3.6f) / 1.6f);
        }
        struct Pair { public int a, b; public float at, d; }
        readonly List<Pair> cuddles = new List<Pair>();
        readonly float[] groupTouch = new float[3];
        const float SqueezeBy = 7f;

        // ---- Heart and circle ----
        const float Scale = 4.4f;
        const float TipY = 205f;               // the point of the heart: just above the group's way out
        const float CircleR = 62f;
        static readonly Vector2 CircleCentre = new Vector2(250f, 250f);   // the circle ends in the middle of the mat
        const float LapSecs = 16f;
        const float GateSecs = 3.2f, DropSecs = 1.6f, JoinSecs = GateSecs + DropSecs, GateSpeed = 28f;
        const float HeartLaps = 2.5f, MorphSecs = 15f, CircleLaps = 2.2f;
        const float ReturnSecs = 12f, ExitRadius = 62f;
        const float LandU = 0.5f;              // the point at the bottom of the heart; the robots go on up its left side

        readonly Vector2[] start, line;
        readonly Vector2 conductor0;
        readonly Vector2[] spot = new Vector2[8];
        readonly float[] arriveAt = new float[8];
        readonly Vector2[] heart; readonly float[] cum; readonly float perimeter;
        readonly Vector2 heartCentre;
        readonly int[] peelOrder = { 7, 6, 4, 2, 0, 1, 3, 5 };
        readonly float[] peelAt = new float[8];
        readonly float peelFrom, morphStart, morphEnd, returnFrom, ringStop;
        Show.Integrated phase;
        float endCircle;
        Vector2 gate;
        public float Total { get; private set; }
        public float PeelFrom { get { return peelFrom; } }
        public float TouchFrom { get { return groupTouch[0]; } }
        public float ReturnFrom { get { return returnFrom; } }
        public float ArrivalOf(int k) { return arriveAt[Arrivals[k].slot]; }
        public float FirstTouch { get { return FirstTouchAt; } }
        public int CuddleCount { get { return cuddles.Count; } }
        public float CuddleAt(int k) { return cuddles[k].at; }
        public float MorphStart { get { return morphStart; } }

        public Plan(Vector2[] start)
        {
            this.start = start;
            line = V2Show.OwnSpots(start.Length);
            conductor0 = start[Conductor];
            spot[Conductor] = conductor0;
            spot[First] = conductor0 + FirstOffset;
            arriveAt[First] = FirstStart + FirstSecs;
            foreach (var a in Arrivals) { spot[a.slot] = conductor0 + a.offset; arriveAt[a.slot] = a.start + ArriveSecs; }

            // lean-ins while the next robot is on its way: neighbours in the group
            Cuddle(Conductor, 5, 24f, 5.5f);
            Cuddle(First, 1, 33f, 5.5f);
            Cuddle(Conductor, 4, 42f, 5.5f); Cuddle(First, Conductor, 42f, 7.5f);
            Cuddle(First, 0, 51f, 5.5f); Cuddle(Conductor, 5, 51f, 5.5f);
            Cuddle(First, 6, 59f, 5.5f); Cuddle(1, 0, 59f, 3f);
            groupTouch[0] = 67f; groupTouch[1] = 67f + PhraseSecs; groupTouch[2] = 67f + 2f * PhraseSecs;
            peelFrom = groupTouch[2] + PhraseSecs + 1f;

            // the heart: a smooth outline, by arc length
            const int Samples = 2400;
            var raw = new Vector2[Samples];
            for (int k = 0; k < Samples; k++)
            {
                float t = 2f * Mathf.PI * k / Samples;
                raw[k] = new Vector2(16f * Mathf.Pow(Mathf.Sin(t), 3f),
                                     -(13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t)));
            }
            for (int pass = 0; pass < 4; pass++)   // round the tightest turns just enough
            {
                var soft = new Vector2[Samples];
                for (int k = 0; k < Samples; k++)
                {
                    Vector2 sum = Vector2.zero;
                    for (int w = -30; w <= 30; w++) { sum += raw[(k + w + Samples) % Samples]; }
                    soft[k] = sum / 61f;
                }
                raw = soft;
            }
            Vector2 lo = raw[0], hi = raw[0];
            foreach (var p in raw) { lo = Vector2.Min(lo, p); hi = Vector2.Max(hi, p); }
            Vector2 mid = (lo + hi) / 2f;
            float shift = TipY - (hi.y - mid.y) * Scale;   // the point at the bottom sits at TipY
            heartCentre = new Vector2(250f, shift + (mid.y - mid.y) + 0f);
            heartCentre = new Vector2(250f, TipY - (hi.y - mid.y) * Scale);
            heart = raw.Select(p => heartCentre + Scale * (p - mid)).ToArray();
            cum = new float[Samples + 1];
            for (int k = 0; k < Samples; k++) { cum[k + 1] = cum[k] + (heart[(k + 1) % Samples] - heart[k]).magnitude; }
            perimeter = cum[Samples];
            // where u = 0.5 is the point at the bottom: find it by position, then count u from there
            float bottomS = 0f, bottomY = float.MinValue;
            for (int k = 0; k < Samples; k++) { if (heart[k].y > bottomY) { bottomY = heart[k].y; bottomS = cum[k]; } }
            uBottom = bottomS / perimeter;
            gate = HeartAt(LandU) + new Vector2(0f, 28f);

            float interval = LapSecs / peelOrder.Length;
            for (int k = 0; k < peelOrder.Length; k++) { peelAt[peelOrder[k]] = peelFrom + k * interval; }
            float lastJoined = peelAt[peelOrder[peelOrder.Length - 1]] + JoinSecs;
            morphStart = lastJoined + HeartLaps * LapSecs;
            morphEnd = morphStart + MorphSecs;
            returnFrom = morphEnd + CircleLaps * 18f;
            ringStop = returnFrom + ReturnSecs;

            // The circle's turn is set so that, half way through the return, the ring stands at the
            // angle from which it flattens into the line without crossing
            int n = start.Length;
            var order = V2Show.RingOrder(n);
            float unusedCost; float opensAt = V2Show.BestAngle(order, 1, ExitRadius, line, out unusedCost);
            int leader = peelOrder[0];
            float baseAngle = opensAt + 2f * Mathf.PI * order[leader] / n;   // the leader's angle on the ring at that moment
            float tMid = returnFrom + ReturnSecs / 2f;
            Show.Eased RateFor(float c) { return new Show.Eased(0f, 1f / LapSecs, morphStart, 1f / LapSecs, morphEnd, c, returnFrom, c, ringStop, 0f); }
            float phaseLanding = LandPhaseOffset();
            float omega0 = new Show.Integrated(s => RateFor(0f).At(s), tMid + 1f).At(tMid);
            float omega1 = new Show.Integrated(s => RateFor(1f).At(s), tMid + 1f).At(tMid);
            float perUnit = omega1 - omega0;
            float chosen = 1f / 18f;
            // the leader's angle is 2 pi (LandU + laps since it landed) - pi / 2; find the rate that makes it baseAngle
            float landedAt = peelAt[leader] + JoinSecs;
            float lapsAtLanding = new Show.Integrated(s => RateFor(0f).At(s), tMid + 1f).At(landedAt);
            for (int k = 0; k < 60; k++)
            {
                // laps from landing to tMid = (omega0 - lapsAtLanding0) + c * (perUnit - perUnitAtLanding); solve below
                float c = SolveRate(baseAngle, k, tMid, landedAt, RateFor);
                if (c >= 1f / 26f && c <= 1f / 12f) { chosen = c; break; }
            }
            phase = new Show.Integrated(s => RateFor(chosen).At(s), ringStop + 1f);
            Total = ringStop + 0.3f;
        }

        // laps (turns of the circle) the leader has made between landing and tMid for circle rate c is
        // linear in c: a0 + c * a1. Solve 2 pi (LandU + a0 + c a1 + k) - pi / 2 = baseAngle (mod 2 pi) for c
        float SolveRate(float baseAngle, int k, float tMid, float landedAt, System.Func<float, Show.Eased> rateFor)
        {
            Func0(rateFor, 0f, tMid, landedAt, out float a0);
            Func0(rateFor, 1f, tMid, landedAt, out float a1);
            a1 -= a0;
            float target = (baseAngle + Mathf.PI / 2f) / (2f * Mathf.PI) - LandU;   // laps wanted, mod 1
            float want = Mathf.Repeat(target - a0, 1f) + k;
            return want / a1;
        }

        static void Func0(System.Func<float, Show.Eased> rateFor, float c, float tMid, float landedAt, out float laps)
        {
            var e = rateFor(c);
            laps = e.Integral(tMid) - e.Integral(landedAt);
        }

        float LandPhaseOffset() { return 0f; }
        float uBottom;

        void Cuddle(int a, int b, float at, float d) { cuddles.Add(new Pair { a = a, b = b, at = at, d = d }); }

        // A point of the heart, by lap fraction counted from the point at the bottom
        public Vector2 HeartAt(float u)
        {
            float s = Mathf.Repeat(u + uBottom - LandU, 1f) * perimeter;
            int lo = 0, hi = heart.Length;
            while (hi - lo > 1) { int m = (lo + hi) / 2; if (cum[m] <= s) { lo = m; } else { hi = m; } }
            float f = (s - cum[lo]) / Mathf.Max(0.0001f, cum[lo + 1] - cum[lo]);
            return Vector2.Lerp(heart[lo], heart[(lo + 1) % heart.Length], f);
        }

        Vector2 CircleAt(float u, float e)
        {
            float a = 2f * Mathf.PI * u - Mathf.PI / 2f;
            Vector2 centre = Vector2.Lerp(heartCentre, CircleCentre, e);
            float r = CircleR + (e >= 1f ? 0f : 0f);
            return centre + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        float U(int i, float t) { return LandU + phase.At(t) - phase.At(peelAt[i] + JoinSecs); }

        Vector2 OnRoute(int i, float t)
        {
            float u = U(i, t);
            float e = Shapes.Ease((t - morphStart) / MorphSecs);
            Vector2 p = Vector2.Lerp(HeartAt(u), CircleAt(u, e), e);
            if (t > returnFrom)
            {
                // the circle opens out a little, then everyone flattens onto the line
                float r = Mathf.Lerp(CircleR, ExitRadius, Shapes.Ease((t - returnFrom) / (ReturnSecs * 0.5f)));
                float a = 2f * Mathf.PI * u - Mathf.PI / 2f;
                Vector2 ring = CircleCentre + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                p = Vector2.Lerp(ring, line[i], Shapes.Ease((t - returnFrom) / ReturnSecs));
            }
            return p;
        }

        // ---- The gathering ----
        static Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float u)
        {
            float v = 1f - u;
            return v * v * v * a + 3f * v * v * u * b + 3f * v * u * u * c + u * u * u * d;
        }

        // Straight (with an even ease) from the line to its place in the group
        Vector2 Group(int i, float t)
        {
            if (i == Conductor) { return conductor0; }
            if (i == First)
            {
                float w = Shapes.Ease((t - FirstStart) / FirstSecs);
                Vector2 p = Vector2.Lerp(start[i], spot[i], w);
                // the one gentle touch: a little closer to the conductor, then back
                float touch = Shapes.Ease((t - FirstTouchAt) / 1.5f) - Shapes.Ease((t - FirstTouchAt - 2f) / 1.6f);
                return p + new Vector2(Gap - 29f, 0f) * touch;
            }
            var arr = Arrivals[System.Array.FindIndex(Arrivals, a => a.slot == i)];
            return Vector2.Lerp(start[i], spot[i], Shapes.Ease((t - arr.start) / ArriveSecs));
        }

        Vector2 Centroid()
        {
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < 8; i++) { sum += spot[i]; }
            return sum / 8f;
        }

        Vector2 Leaned(int i, float t)
        {
            Vector2 p = Group(i, t);
            if (t < arriveAt[i]) { return p; }
            foreach (var c in cuddles)
            {
                if (c.a != i && c.b != i) { continue; }
                int other = c.a == i ? c.b : c.a;
                p += (spot[other] - spot[i]).normalized * c.d * Lean(t - c.at);
            }
            // the whole group touches: closer round the middle of the group, three times
            float squeeze = 0f;
            foreach (float g in groupTouch) { squeeze = Mathf.Max(squeeze, Lean(t - g)); }
            if (squeeze > 0f) { p += (Centroid() - spot[i]).normalized * SqueezeBy * squeeze; }
            return p;
        }

        // ---- Leaving: everyone the same way, to a gate below the heart's point, then up onto the
        // outline, arriving moving along it at its pace ----
        Vector2 Peeling(int i, float t, Vector2 from)
        {
            Vector2 landing = HeartAt(LandU);
            Vector2 up = (landing - gate).normalized;
            float tGate = peelAt[i] + GateSecs;
            if (t < tGate)
            {
                float tau = Mathf.Clamp01((t - peelAt[i]) / GateSecs);
                Vector2 c1 = from + (gate - from) * 0.35f;
                Vector2 c2 = gate - up * GateSpeed * GateSecs / 4.95f;
                return Bezier(from, c1, c2, gate, 0.35f * tau + 0.65f * tau * tau);
            }
            float sig = Mathf.Clamp01((t - tGate) / DropSecs);
            Vector2 tangent = (HeartAt(LandU + 0.002f) - HeartAt(LandU - 0.002f)).normalized;
            Vector2 v0 = up * GateSpeed * DropSecs, v1 = tangent * (perimeter / LapSecs) * DropSecs;
            float s2 = sig * sig, s3 = s2 * sig;
            return (2f * s3 - 3f * s2 + 1f) * gate + (s3 - 2f * s2 + sig) * v0 + (-2f * s3 + 3f * s2) * landing + (s3 - s2) * v1;
        }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            if (t >= ringStop) { return line[i]; }
            if (t < peelAt[i]) { return Leaned(i, t); }
            if (t < peelAt[i] + JoinSecs) { return Peeling(i, t, Leaned(i, peelAt[i])); }
            return OnRoute(i, t);
        }

        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int i, float t) { return false; }
    }
}
