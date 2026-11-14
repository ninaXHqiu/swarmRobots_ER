using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// v2 Traffic to Wave (C14).
//   1. Two streams of four, from the left and from the right, along shallow curves toward one opening
//      near the middle of the mat (wide enough for one robot). The two leaders reach it together,
//      each stops short of the space they would share; the right leader gives way with one small
//      move back, keeping its heading, and the left leader goes through. The followers creep up in
//      order behind them.
//   2. A zipper merge, L1 R1 L2 R2 L3 R3 L4 R4: a robot is released into the opening only after the
//      one before has cleared it; the next is already easing up to its entry. The first gaps are
//      uneven, then the cadence settles.
//   3. Beyond the opening each robot takes a short curve that straightens into its own lane, one
//      lane per slot, perpendicular to the row. The first pair take the two middle slots, then
//      outward a pair at a time. As each reaches its slot its first shallow stroke grows out of
//      the exit path, into a wave that is already running.
//   4. One broad wave travels across all eight, offset = amplitude * sin(k x - phase), for several
//      cycles; then it tapers and the row goes straight back down its lanes to the line.
// Slots 0..7 from the left; the left stream is slots 3 2 1 0, the right 4 5 6 7 (front first), and
// each robot's slot is where it stands in the line, so the wave row lies above the line.
public class V2TrafficWave : SwarmRoutine
{
    public override string Name { get { return "v2 Traffic to Wave"; } }
    public override string Code { get { return "C14"; } }
    public override string Note { get { return "Merge, zipper, travelling wave"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        // the fixed opening (the Messenger's introduction) and ending (all in the line, facing the participant)
        yield return V2Show.Run(ctx, start => new Plan(start), 90);
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading
    {
        static readonly Vector2 Gate = new Vector2(250f, 232f);      // the opening: one robot wide
        const float RowY = 126f;                                      // the wave row's resting line
        const float LaneTopY = 156f;                                  // where an exit curve has straightened into its lane
        static readonly int[] Left = { 3, 2, 1, 0 }, Right = { 4, 5, 6, 7 };    // front first
        static readonly int[] Release = { 3, 4, 2, 5, 1, 6, 0, 7 };           // L1 R1 L2 R2 L3 R3 L4 R4
        static readonly float[] Behind = { 0f, 52f, 110f, 166f };               // along the stream, behind the leader
        static readonly float[] BehindR = { 0f, 48f, 114f, 160f };
        const float FormSecs = 8f, CreepFrom = 8f, CreepSecs = 5f;
        const float YieldAt = 14.6f, YieldBack = 16.2f;   // the right leader steps back, then forward again, before its release
        static readonly float[] Gaps = { 3.0f, 2.6f, 2.3f, 2.1f, 2.0f, 1.9f, 1.9f };   // between releases: uneven, then regular
        const float FirstRelease = 15.8f;
        const float PassSecs = 4.6f, EaseUpSecs = 1.6f;
        const float WaveAmp = 38f, WavePeriod = 7f, WaveLength = 336f, Cycles = 6f;
        const float JoinRampSecs = 6f, TaperSecs = 9f, ReturnSecs = 9f;

        readonly Vector2[] start, line;
        readonly float[] releaseAt = new float[8], arriveAt = new float[8];
        readonly bool[] isLeft = new bool[8];
        readonly int[] rank = new int[8];
        readonly float waveFrom, waveTo, taperTo;
        public float Total { get; private set; }
        public float WaveFrom { get { return waveFrom; } }
        public float ReleaseOf(int slot) { return releaseAt[slot]; }
        public float ArriveOf(int slot) { return arriveAt[slot]; }
        public float ReturnAt { get { return taperTo; } }

        public Plan(Vector2[] start)
        {
            this.start = start;
            line = V2Show.OwnSpots(8);
            for (int r = 0; r < 4; r++) { isLeft[Left[r]] = true; rank[Left[r]] = r; rank[Right[r]] = r; }
            float t = FirstRelease;
            for (int k = 0; k < 8; k++)
            {
                releaseAt[Release[k]] = t;
                if (k < 7) { t += Gaps[k]; }
            }
            for (int s = 0; s < 8; s++) { arriveAt[s] = releaseAt[s] + PassSecs; }
            waveFrom = arriveAt[Release[0]];
            waveTo = arriveAt[Release[7]] + JoinRampSecs + Cycles * WavePeriod;
            taperTo = waveTo + TaperSecs;
            Total = taperTo + ReturnSecs + 0.3f;
        }

        // ---------------- The streams ----------------
        // A stream's curve: s = 0 at the stop-short point beside the opening, negative further back
        static Vector2 StreamAt(bool left, float s)
        {
            float x = left ? 232f + s * 0.72f : 268f - s * 0.72f;
            return new Vector2(x, 262f + 0.0007f * s * s);
        }

        float Behind_(int slot) { return isLeft[slot] ? Behind[rank[slot]] : BehindR[rank[slot]]; }

        // where robot `slot` stands in its stream at t (before its release), s along the curve
        float YieldS(int slot, float t)
        {
            if (slot != 4) { return 0f; }
            return -16f * (Shapes.Ease((t - YieldAt) / 0.7f) - Shapes.Ease((t - YieldBack) / 1.2f));
        }

        float StreamS(int slot, float t)
        {
            // each robot ahead in its stream that is released lets this one move up a place
            float eff = rank[slot];
            for (int r = 0; r < rank[slot]; r++)
            {
                int a = isLeft[slot] ? Left[r] : Right[r];
                eff -= Shapes.Ease((t - releaseAt[a] - 0.3f) / 2.4f);
            }
            float[] places = isLeft[slot] ? Behind : BehindR;
            int lo = Mathf.Clamp(Mathf.FloorToInt(eff), 0, places.Length - 2);
            float behind = Mathf.Lerp(places[lo], places[lo + 1], eff - lo);
            // everyone creeps up 20 toward the opening as the leaders ease toward it (the nearest first)
            float start0 = CreepFrom + 0.9f * rank[slot];
            float s = -behind - 20f + 20f * Shapes.Ease((t - start0) / CreepSecs);
            if (rank[slot] == 0) { s = Mathf.Min(s, 0f); }
            s += YieldS(slot, t);
            // the second right robot waits while its leader gives way
            if (slot == 5) { s = Mathf.Min(s, StreamS(4, t) - 42f); }
            return s;
        }

        int AheadOf(int slot)
        {
            int r = rank[slot];
            return r == 0 ? -1 : (isLeft[slot] ? Left[r - 1] : Right[r - 1]);
        }

        // ---------------- Through the opening ----------------
        Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float u)
        {
            float v = 1f - u;
            return v * v * v * a + 3f * v * v * u * b + 3f * v * u * u * c + u * u * u * d;
        }

        // the way through: the entry beside the opening, the opening, then a curve straightening into the lane
        Vector2 Pass(int slot, float tau)
        {
            Vector2 entry = StreamAt(isLeft[slot], 0f);
            float x = line[slot].x;
            float p = Mathf.Lerp(tau, Shapes.Ease(tau), 0.6f);   // leaves the entry smoothly, then keeps a steady pace
            const float Split = 0.31f;                           // the entry-to-opening part, and the exit curve after it
            if (p < Split) { return Bezier(entry, entry + new Vector2(isLeft[slot] ? 12f : -12f, -14f), Gate + new Vector2(0f, 14f), Gate, p / Split); }
            float u = (p - Split) / (1f - Split);
            return Bezier(Gate, Gate + new Vector2(0f, -40f), new Vector2(x, LaneTopY + 40f), new Vector2(x, LaneTopY), u);
        }

        // the wave: offset from the row's line
        float Wave(int slot, float t)
        {
            float k = 2f * Mathf.PI / WaveLength;
            float x = line[slot].x - 250f;
            float own = Shapes.Ease((t - arriveAt[slot]) / JoinRampSecs);           // shallow at first, growing
            float taper = 1f - Shapes.Ease((t - waveTo) / TaperSecs);
            return WaveAmp * own * taper * Mathf.Sin(k * x - 2f * Mathf.PI * (t - waveFrom) / WavePeriod);
        }

        public Vector2 Position(int slot, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            float x = line[slot].x;
            // the final return: straight down its lane to the line
            if (t >= taperTo)
            {
                float y = Mathf.Lerp(RowY, line[slot].y, Shapes.Ease((t - taperTo) / ReturnSecs));
                return new Vector2(x, y);
            }
            // in the row: the wave
            if (t >= arriveAt[slot])
            {
                float join = Shapes.Ease((t - arriveAt[slot]) / 2f);
                float y = Mathf.Lerp(LaneTopY, RowY, join) + Wave(slot, t);
                return new Vector2(x, y);
            }
            // going through the opening
            if (t >= releaseAt[slot]) { return Pass(slot, (t - releaseAt[slot]) / PassSecs); }
            // easing up to the entry just before its release
            Vector2 entry = StreamAt(isLeft[slot], 0f);
            float easeFrom = releaseAt[slot] - EaseUpSecs;
            float s = StreamS(slot, t);
            Vector2 p;
            if (t < FormSecs) { p = Vector2.Lerp(start[slot], StreamAt(isLeft[slot], -Behind_(slot) - 20f), Shapes.Ease(t / FormSecs)); }
            else
            {
                p = StreamAt(isLeft[slot], s);
                if (t > easeFrom) { p = Vector2.Lerp(p, entry, Shapes.Ease((t - easeFrom) / EaseUpSecs)); }
            }
            return p;
        }

        // robots in the row face the way the traffic went (away from the participant)
        public float Heading(int slot, float t) { return 270f; }
        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int slot, float t) { return t >= arriveAt[slot] + 0.5f; }
    }
}
