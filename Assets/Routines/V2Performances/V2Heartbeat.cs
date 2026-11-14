using System.Collections;
using UnityEngine;

// v2 Heartbeat. Each robot is one moving point on a heart-monitor trace. The baseline is the
// line across the mat centre and every robot has a fixed place along it. Once the row is set,
// a robot only moves along its own lane, straight across the baseline: it keeps its heading on
// the lane and drives forward and backward, also through the reversals.
// One travelling signal drives the whole row: there is one beat clock, and the robot in place s
// along the row shows what the first robot showed s / TravelSpeed seconds earlier. The clock's
// phase is continuous (the beat rate is integrated), so slowing down never jumps.
//   1. Rapid:     strong, quick beats; a new one starts before the last has crossed the row, so
//                 several peaks are on the row at once.
//   2. Calmer:    beat by beat the rate and the height come down; peaks spread apart and soften.
//   3. Wave:      the heartbeat profile rounds into a sine-like wave on the same phase.
//   4. Calm wave: broader, slower, shallower; on the last cycle the height tapers to zero, so the
//                 row straightens through the motion itself. The row is the final line.
// Recovered: the row = the line, the messenger gliding back into its place, and holding the
// heading on the lane come from ResonanceHeartbeat / Swarm.Flow. New: the single delayed signal
// with a continuous phase, the larger envelope and the blend into the wave.
public abstract class V2HeartbeatBase : SwarmRoutine
{
    protected abstract float Minutes { get; }
    public override string Name { get { return "v2 Heartbeat (" + V2Show.Label(Minutes) + ")"; } }
    public override string Code { get { return "C07" + (Minutes <= 0f ? ".n" : (Minutes < 4f ? ".3" : ".5")); } }
    public override string Note { get { return "Heartbeat becomes wave"; } }
    public override int MinRobots { get { return 2; } }
    public override IEnumerator Run(RoutineContext ctx) { return V2Show.Run(ctx, start => V2Heartbeat.For(start, Minutes), maxSpeed: 110); }
}
public class V2HeartbeatNatural : V2HeartbeatBase { protected override float Minutes { get { return 0f; } } }
public class V2Heartbeat3Min : V2HeartbeatBase { protected override float Minutes { get { return 3f; } } }
public class V2Heartbeat5Min : V2HeartbeatBase { protected override float Minutes { get { return 5f; } } }

public static class V2Heartbeat
{
    // ---- The travelling signal ----
    const float TravelSpeed = 2.1f;        // places along the row per second, the same throughout
    // Seconds per beat / cycle at the end of each section (the rate eases between them)
    const float RapidEvery = 1.5f;         // 2.1 x 1.5 = a peak every 3.15 places: 2 to 3 on an 8 robot row
    const float CalmerEvery = 2.4f;
    const float WaveEvery = 3f;
    const float CalmEvery = 4.2f;
    // Height: mat units from the baseline at the peak (heartbeat), or each way (wave)
    const float RapidHeight = 60f;         // about 8 cm of travel
    const float CalmerHeight = 40f;
    const float WaveHeight = 26f;
    const float CalmHeight = 16f;

    // ---- One heartbeat on the monitor, over one beat (phase 0..1) ----
    const float SpikeAt = 0.04f, SpikeLength = 0.66f;                  // the spike: up and back down
    const float DipAt = 0.66f, DipLength = 0.26f, DipSize = -0.16f;    // short dip below the line
    const float PeakPhase = SpikeAt + SpikeLength / 2f;                // the wave's crest sits on the spike

    // ---- Joining the row ----
    const float OnsetBeats = 3.5f;         // beats over which the first robot's height grows from nothing
    const float EnterSecs = 3.5f;          // the messenger glides back into its place, sideways first

    public static Plan For(Vector2[] start, float minutes)
    {
        int rapid = 8, calmer = 5, wave = 4, calm = 4;
        if (minutes <= 0f) { return new Plan(start, rapid, calmer, wave, calm); }
        float budget = V2Show.Budget(minutes) - EnterSecs - (start.Length - 1) / TravelSpeed;
        bool added = true;
        while (added)
        {
            added = false;
            for (int k = 0; k < 2; k++) { if (Signal.Secs(rapid + 1, calmer, wave, calm) <= budget) { rapid++; added = true; } }
            if (Signal.Secs(rapid, calmer + 1, wave, calm) <= budget) { calmer++; added = true; }
            if (Signal.Secs(rapid, calmer, wave + 1, calm) <= budget) { wave++; added = true; }
            if (Signal.Secs(rapid, calmer, wave, calm + 1) <= budget) { calm++; added = true; }
        }
        return new Plan(start, rapid, calmer, wave, calm);
    }

    // The signal at the first place of the row: lift off the baseline, t seconds in
    public class Signal
    {
        readonly Show.Eased every, height, round;
        readonly Show.Paced clock;
        readonly float beats;
        public float Duration { get { return clock.Duration; } }

        public Signal(int rapid, int calmer, int wave, int calm)
        {
            float a = rapid, b = a + calmer, c = b + wave, d = c + calm;
            beats = d;
            every = new Show.Eased(0f, RapidEvery, a, RapidEvery, b, CalmerEvery, c, WaveEvery, d, CalmEvery);
            height = new Show.Eased(0f, RapidHeight, a, RapidHeight, b, CalmerHeight, c, WaveHeight, d - 1f, CalmHeight);
            round = new Show.Eased(b, 0f, c, 1f);
            clock = new Show.Paced(x => 1f / every.At(x), beats);
        }

        public static float Secs(int rapid, int calmer, int wave, int calm) { return new Signal(rapid, calmer, wave, calm).Duration; }

        static float Heartbeat(float u)
        {
            return Show.Bump((u - SpikeAt) / SpikeLength) + DipSize * Show.Bump((u - DipAt) / DipLength);
        }

        public float At(float t)
        {
            if (t <= 0f || t >= Duration) { return 0f; }
            float x = clock.At(t);                       // beats done: the continuous phase
            float u = x - Mathf.Floor(x);
            float r = round.At(x);
            float shape = Mathf.Lerp(Heartbeat(u), Mathf.Cos(2f * Mathf.PI * (u - PeakPhase)), r);
            float taper = 1f - Shapes.Ease(x - (beats - 1f));   // the last cycle flattens
            float onset = Shapes.Ease(x / OnsetBeats);   // the first beats start small and grow: it comes in, it does not start at full strength
            return height.At(x) * shape * taper * onset;
        }
    }

    public class Plan : V2Show.IPlan
    {
        readonly Vector2[] start, spot;
        readonly int[] rank;
        readonly bool[] entering;
        readonly Signal signal;
        public float Total { get; private set; }

        public Plan(Vector2[] start, int rapid, int calmer, int wave, int calm)
        {
            this.start = start;
            int n = start.Length;
            spot = V2Show.OwnSpots(n);   // the row is the line: every robot in its own slot
            rank = new int[n]; entering = new bool[n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++) { if (spot[j].x < spot[i].x) { rank[i]++; } }
                entering[i] = (start[i] - spot[i]).magnitude > 12f;
            }
            signal = new Signal(rapid, calmer, wave, calm);
            Total = EnterSecs + signal.Duration + (n - 1) / TravelSpeed + 0.3f;
        }

        // Lift of robot i off the baseline at t: the one signal, delayed by its place in the row
        public float Lift(int i, float t) { return signal.At(t - EnterSecs - rank[i] / TravelSpeed); }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            float lift = Lift(i, t);   // up = away from the participant
            if (!entering[i])
            {
                // Already in its lane: only the offset across the baseline ever changes
                return new Vector2(spot[i].x, Mathf.Lerp(start[i].y, spot[i].y, Shapes.Ease(t / EnterSecs)) - lift);
            }
            // The messenger: sideways into its lane first, so it arrives moving along the lane
            float along = Shapes.Ease(t / (EnterSecs * 0.7f));
            float up = Shapes.Ease((t - EnterSecs * 0.2f) / (EnterSecs * 0.8f));
            return new Vector2(Mathf.Lerp(start[i].x, spot[i].x, along), Mathf.Lerp(start[i].y, spot[i].y, up) - lift);
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
        public bool InLane(int i, float t) { return !entering[i] || t >= EnterSecs * 0.7f; }
    }
}
