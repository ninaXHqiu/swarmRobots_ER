using System.Collections;
using UnityEngine;

// Resonance - Heartbeat. One coordinated group action, driven by one master heartbeat.
// After the Messenger's introduction the robots hold a row (the line) and the messenger slides
// back into its place. Strong: everyone together makes a large, fast THUMP away from the row and
// back, a slightly smaller thump, and a short release, over and over. Becoming a wave: the same
// heartbeat keeps repeating while each cycle rounds the double beat into one smooth pulse, lets it
// travel along the row, and grows slower and lower. Calm: broad, gentle waves travel through the
// row; during the last one the wave flattens to nothing, which leaves the robots in the line.
public class ResonanceHeartbeat : SwarmRoutine
{
    public override string Name { get { return "v2 Resonance: heartbeat"; } }
    public override int MinRobots { get { return 2; } }

    // ---- Phrases (change the counts freely; the choreography follows) ----
    const int StrongBeats = 4;
    const int SofteningBeats = 3;
    const int CalmWaves = 2;               // the last one flattens into the line

    // ---- One heartbeat ----
    const float BeatSecs = 1.4f;           // strong: THUMP, thump, release
    const float SoftSecs = 2.6f;           // the last softening cycle
    const float CalmWaveSecs = 3.6f;
    const float ThumpShare = 0.36f;        // the THUMP lasts this share of a heartbeat...
    const float SecondAt = 0.4f;           // ...the second thump starts here...
    const float SecondShare = 0.3f;        // ...and lasts this long...
    const float SecondSize = 0.75f;        // ...at this share of the THUMP; the rest is the release
    const float ThumpHeight = 34f;         // mat units away from the row
    const float SoftHeight = 24f;          // at the end of the softening cycles
    const float CalmHeight = 16f;          // broad, gentle waves, still clearly visible
    const float WaveLag = 0.1f;            // share of a cycle between neighbours once it travels...
    const float CalmLag = 0.07f;           // ...and less (a broader wave) in the calm waves

    // ---- Joining the row ----
    const float EnterSecs = 3.5f;          // a robot out of the row (the messenger) slides along below it...
    const float UpDelay = 1.4f;            // ...and steps up into it this much later
    const int MaxSpeed = 115;              // the THUMP needs the robots' full speed

    // Robots in the row beat straight up and down, holding their heading on that line
    public override IEnumerator Run(RoutineContext ctx) { return Show.Run(ctx, start => new Plan(start), MaxSpeed, axisDeg: 90f); }

    public class Plan : Show.IPlan
    {
        readonly Vector2[] start, spot;
        readonly int[] rank;
        readonly float[] upDelay;
        readonly Show.Paced beats;
        // Everything below changes with the number of heartbeats done, not with the clock
        const float SoftFrom = StrongBeats, CalmFrom = StrongBeats + SofteningBeats;
        const float Beats = StrongBeats + SofteningBeats + CalmWaves;
        static readonly Show.Eased cycleSecs = new Show.Eased(SoftFrom, BeatSecs, CalmFrom, SoftSecs, CalmFrom + 1f, CalmWaveSecs);
        static readonly Show.Eased rounding = new Show.Eased(SoftFrom, 0f, CalmFrom, 1f);
        static readonly Show.Eased height = new Show.Eased(SoftFrom, ThumpHeight, CalmFrom, SoftHeight, CalmFrom + 1f, CalmHeight, Beats - 1f, CalmHeight, Beats, 0f);
        static readonly Show.Eased lag = new Show.Eased(SoftFrom, 0f, CalmFrom, WaveLag, CalmFrom + 1f, CalmLag);
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
            beats = new Show.Paced(done => 1f / cycleSecs.At(done), Beats);
            Total = beats.Duration;
        }

        // One heartbeat (x from 0 to 1): THUMP, thump, release
        static float Beat(float x)
        {
            return Show.Bump(x / ThumpShare) + SecondSize * Show.Bump((x - SecondAt) / SecondShare);
        }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            float done = beats.At(t);
            float phase = done - rank[i] * lag.At(done);
            float x = phase - Mathf.Floor(phase);
            // Away from the participant, so beats never reach toward a robot still joining the row
            float lift = height.At(done) * Mathf.Lerp(Beat(x), Show.Bump(x), rounding.At(done));
            if (upDelay[i] < 0.3f)
            {
                // In the row (or straight below its spot): beats from the first moment
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
