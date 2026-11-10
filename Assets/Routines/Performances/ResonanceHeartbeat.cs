using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Resonance - Heartbeat. The row is a hospital heart-monitor line.
// After the Messenger's introduction the robots hold a row (the line) and the messenger glides
// back into its place. Each heartbeat is a monitor trace that sweeps along the row from one end to
// the other: a small bump, a sharp spike up, a short dip below the line, a lower bump. A new beat
// starts before the last one has left the row, so the beat rate shows as how close together the
// traces run. Rapid: beats come quickly and sweep fast, two traces on the row at once. Calming: each
// beat comes later and sweeps slower, its spike lower and rounder. Calm: the beats have become one
// broad wave travelling through the row; during the last one it flattens into the line.
public class ResonanceHeartbeat : SwarmRoutine
{
    public override string Name { get { return "v2 Resonance: heartbeat"; } }
    public override int MinRobots { get { return 2; } }

    // ---- Phrases (change the counts freely; the choreography follows) ----
    const int RapidBeats = 6;
    const int CalmingBeats = 3;
    const int CalmWaves = 2;               // the last one flattens into the line

    // ---- Rhythm: time from one beat to the next, and how fast its trace sweeps the row ----
    const float RapidEvery = 0.9f;         // seconds between rapid beats...
    const float RapidSweep = 5f;           // ...each sweeping 5 robots a second, so the next starts ~4.5 robots behind
    const float CalmEvery = 4f;            // seconds between calm waves...
    const float CalmSweep = 2f;            // ...sweeping slowly: one wave spans the row

    // ---- One heartbeat on the monitor (seconds after the trace reaches a robot) ----
    const float SpikeHeight = 26f;         // mat units away from the line at the spike (about 3.5 cm)
    const float CalmingHeight = 16f;       // the last calming beat
    const float WaveHeight = 10f;          // the broad, calm waves
    const float PAt = 0f, PLength = 0.18f, PSize = 0.12f;      // small bump
    const float RAt = 0.2f, RLength = 0.5f;                    // the spike (~160 mat units/s at its fastest)
    const float SAt = 0.7f, SLength = 0.18f, SSize = -0.18f;   // dip below the line
    const float TAt = 0.95f, TLength = 0.4f, TSize = 0.3f;     // lower bump

    // ---- Joining the row ----
    const float EnterSecs = 4.5f;          // a robot out of the row (the messenger) glides along below it...
    const float UpDelay = 1.4f;            // ...and steps up into it this much later
    const int MaxSpeed = 90;

    // Robots in the row move straight up and down, holding their heading on that line
    public override IEnumerator Run(RoutineContext ctx) { return Show.Run(ctx, start => new Plan(start), MaxSpeed, axisDeg: 90f); }

    // One beat: when it starts at the first robot, how fast it sweeps, how tall, how round
    struct Beat { public float at, sweep, height, round, width; public bool fades; }

    public class Plan : Show.IPlan
    {
        readonly Vector2[] start, spot;
        readonly int[] rank;
        readonly float[] upDelay;
        readonly List<Beat> beats = new List<Beat>();
        public float Total { get; private set; }

        public Plan(Vector2[] start)
        {
            this.start = start;
            int n = start.Length;
            spot = Show.KeepLineSpots(start);   // the row is the line: robots in it keep their spot
            rank = new int[n]; upDelay = new float[n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++) { if (spot[j].x < spot[i].x) { rank[i]++; } }
                upDelay[i] = UpDelay * Mathf.Clamp01(Mathf.Abs(start[i].x - spot[i].x) / Swarm.MinSpacing);
            }

            // The beats, one after another; rate, sweep, height and roundness ease from rapid to calm
            int count = RapidBeats + CalmingBeats + CalmWaves;
            float t = 0f;
            for (int k = 0; k < count; k++)
            {
                float calm = Shapes.Ease((k - RapidBeats + 1f) / (CalmingBeats + 1f));   // 0 while rapid, 1 for the waves
                var b = new Beat();
                b.at = t;
                b.sweep = Mathf.Lerp(RapidSweep, CalmSweep, calm);
                float every = Mathf.Lerp(RapidEvery, CalmEvery, calm);
                b.height = k < RapidBeats ? SpikeHeight : (k < RapidBeats + CalmingBeats ? Mathf.Lerp(SpikeHeight, CalmingHeight, calm) : WaveHeight);
                b.round = calm;
                b.width = every;               // a fully round beat lasts until the next: one continuous wave
                b.fades = k == count - 1;
                beats.Add(b);
                t += every;
            }
            var last = beats[count - 1];
            Total = last.at + (n - 1) / last.sweep + last.width;
        }

        // The monitor trace of one beat, spike height 1, tau seconds after it reaches a robot
        static float Trace(float tau)
        {
            return PSize * Show.Bump((tau - PAt) / PLength) + Show.Bump((tau - RAt) / RLength)
                 + SSize * Show.Bump((tau - SAt) / SLength) + TSize * Show.Bump((tau - TAt) / TLength);
        }

        // How far robot i is lifted off the line at t: every beat passing through it, added up
        float Lift(int i, float t)
        {
            float lift = 0f;
            foreach (var b in beats)
            {
                float tau = t - b.at - rank[i] / b.sweep;
                if (tau <= 0f || tau >= Mathf.Max(TAt + TLength, b.width)) { continue; }
                float shape = Mathf.Lerp(Trace(tau), Show.Bump(tau / b.width), b.round);
                float h = b.height;
                if (b.fades) { h *= 1f - Shapes.Ease(tau / b.width); }
                lift += h * shape;
            }
            return lift;
        }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            float lift = Lift(i, t);   // up = away from the participant
            if (upDelay[i] < 0.3f)
            {
                Vector2 b = Vector2.Lerp(start[i], spot[i], Shapes.Ease(t / EnterSecs));
                return new Vector2(b.x, b.y - lift);
            }
            float along = Shapes.Ease(t / EnterSecs);
            float up = Shapes.Ease((t - upDelay[i]) / EnterSecs);
            return new Vector2(Mathf.Lerp(start[i].x, spot[i].x, along), Mathf.Lerp(start[i].y, spot[i].y - lift, up));
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
    }
}
