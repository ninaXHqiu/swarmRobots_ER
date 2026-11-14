using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// v2 Traffic to Wave (C14).
//   1. After the standard opening all eight head for the same place on the left of the mat, each at
//      its own speed, along paths that cross: they run into each other (3 and 6 first).
//   2. They keep going, bumping along in S and figure-eight loops, a tangle that drifts to the
//      left-middle. Disorder, no lanes.
//   3. Slowly it unwinds from the right: the rightmost robot peels out like a spring uncoiling and
//      goes to the far end of the row; then the next one, and so on, each into its own lane.
//   4. As soon as the rightmost reaches its place the wave starts there and travels along the row as
//      each robot arrives; several cycles; then it tapers and the row goes straight back to the line.
public class V2TrafficWave : SwarmRoutine
{
    public override string Name { get { return "v2 Traffic to Wave"; } }
    public override string Code { get { return "C14"; } }
    public override string Note { get { return "Collide, untangle, travelling wave"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return V2Show.Run(ctx, start => new Plan(start), 90);
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading
    {
        const float RowY = 126f;
        const float WaveAmp = 38f, WavePeriod = 7f, WaveLength = 336f, Cycles = 5f;
        const float JoinRampSecs = 6f, TaperSecs = 9f, ReturnSecs = 9f;
        const float TangleFrom = 12f, PeelFrom = 42f, PeelGap = 3f, PeelSecs = 9f;
        static readonly float[] RunSecs = { 9f, 10.5f, 8.5f, 8f, 12f, 9.5f, 11f, 13f };     // the run to the left, per robot
        static readonly float[] RunFrom = { 1.5f, 1f, 1.8f, 1f, 2.2f, 1.2f, 1f, 1.6f };
        static readonly float[] AmpX = { 78f, 100f, 70f, 108f, 90f, 74f, 98f, 84f };
        static readonly float[] AmpY = { 46f, 60f, 70f, 52f, 64f, 44f, 68f, 50f };
        static readonly float[] Period = { 17f, 21f, 15.5f, 18.5f, 23f, 16f, 21.5f, 19f };
        static readonly float[] Phase = { 0f, 1.9f, 3.4f, 0.8f, 4.6f, 2.5f, 5.5f, 1.2f };
        static readonly float[] Spin = { 0.7f, -0.9f, 0.5f, -0.6f, 0.8f, -0.5f, 0.6f, -0.8f };

        readonly Vector2[] start, line;
        readonly float[] arriveAt = new float[8];
        readonly float waveFrom, waveTo, taperTo;
        public float Total { get; private set; }
        public float WaveFrom { get { return waveFrom; } }
        public float ArriveOf(int slot) { return arriveAt[slot]; }
        public float ReturnAt { get { return taperTo; } }

        public Plan(Vector2[] start)
        {
            this.start = start;
            line = V2Show.OwnSpots(8);
            for (int s = 0; s < 8; s++) { arriveAt[s] = PeelFrom + PeelGap * (7 - s) + PeelSecs; }
            waveFrom = arriveAt[7];
            waveTo = arriveAt[0] + JoinRampSecs + Cycles * WavePeriod;
            taperTo = waveTo + TaperSecs;
            Total = taperTo + ReturnSecs + 0.3f;
        }

        float PeelAt(int slot) { return PeelFrom + PeelGap * (7 - slot); }

        // ---------------- The tangle ----------------
        Vector2 Hub(float t)
        {
            return Vector2.Lerp(new Vector2(230f, 262f), new Vector2(172f, 252f), Shapes.Ease((t - TangleFrom) / (PeelFrom - TangleFrom)));
        }

        // S and figure-eight loops about the drifting hub, each robot its own size, speed and phase
        Vector2 Tangle(int slot, float t)
        {
            float w = 2f * Mathf.PI / Period[slot];
            float a = w * t + Phase[slot];
            return Hub(t) + new Vector2(AmpX[slot] * Mathf.Sin(a), AmpY[slot] * Mathf.Sin(2f * a + 0.7f * slot));
        }

        // ---------------- The wave ----------------
        Vector2 RowSpot(int slot) { return new Vector2(line[slot].x, RowY); }

        float Wave(int slot, float t)
        {
            float k = 2f * Mathf.PI / WaveLength;
            float x = line[slot].x - 250f;
            float own = Shapes.Ease((t - arriveAt[slot]) / JoinRampSecs);
            float taper = 1f - Shapes.Ease((t - waveTo) / TaperSecs);
            return WaveAmp * own * taper * Mathf.Sin(k * x - 2f * Mathf.PI * (t - waveFrom) / WavePeriod);
        }

        public Vector2 Position(int slot, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            float x = line[slot].x;
            if (t >= taperTo)
            {
                return new Vector2(x, Mathf.Lerp(RowY, line[slot].y, Shapes.Ease((t - taperTo) / ReturnSecs)));
            }
            if (t >= arriveAt[slot]) { return new Vector2(x, RowY + Wave(slot, t)); }
            Vector2 p = Tangle(slot, t);
            if (t < TangleFrom + 4f)
            {
                // the run to the left: each at its own speed, straight at the others' way
                float r = Shapes.Ease((t - RunFrom[slot]) / RunSecs[slot]);
                p = Vector2.Lerp(start[slot], p, r);
            }
            float e = Shapes.Ease((t - PeelAt(slot)) / PeelSecs);
            if (e > 0f)
            {
                // uncoiling like a spring: it spirals outward while it travels to its place in the row
                float s = 40f * Mathf.Sin(Mathf.PI * e);
                float ang = Spin[slot] * 2f * Mathf.PI * 1.2f * e + Phase[slot];
                p = Vector2.Lerp(p, RowSpot(slot), e) + s * new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            }
            return p;
        }

        public float Heading(int slot, float t) { return 270f; }
        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int slot, float t) { return false; }   // free path steering: a lane robot cannot correct sideways drift
    }
}
