using System.Collections;
using UnityEngine;

// Resonance - Heartbeat. After the Messenger's introduction the robots hold a centred row (the
// line) and beat together, all on one rhythm: a large, quick THUMP away from the row and back, a
// smaller but strong thump, a short release, repeated about three times in the first five
// seconds. The double beat then rounds into a smooth wave that starts to travel along the row and
// grows broader, slower and shallower until the row is almost straight at 20 s. A faint, slow
// ripple carries on and fades into the line.
public class ResonanceHeartbeat : SwarmRoutine
{
    public override string Name { get { return "v2 Resonance: heartbeat"; } }
    public override int MinRobots { get { return 2; } }

    // ---- Timing (seconds after the introduction; the return to the line starts at 30) ----
    const float EnterSecs = 3f;      // a robot out of the row (the messenger) slides along below it...
    const float UpDelay = 1.5f;      // ...and steps up into it this much later, clear of the others
    const float BeatEaseSecs = 1f;   // robots already in the row reach full beats this fast
    const float RoundFrom = 5f;      // the double beat starts to round off...
    const float RoundTo = 13f;       // ...is a smooth travelling wave by here...
    const float CalmBy = 20f;        // ...and nearly flat by here

    // ---- One heartbeat ----
    const float BeatSecs = 1.5f;       // THUMP, thump, release at the start
    const float CalmBeatSecs = 4.5f;   // one wave at 20 s
    const float RippleSecs = 5f;       // one ripple at 30 s
    const float ThumpShare = 0.32f;    // the THUMP lasts this share of a heartbeat...
    const float SecondAt = 0.36f;      // ...the thump starts here...
    const float SecondShare = 0.26f;   // ...and lasts this long...
    const float SecondSize = 0.6f;     // ...at this share of the THUMP's height; the rest is the release
    const float BeatHeight = 34f;      // mat units away from the row at the THUMP
    const float CalmHeight = 8f;
    const float RippleHeight = 6f;     // fades to nothing during the return
    const float TravelLag = 0.12f;     // share of a wave between neighbours once it travels...
    const float BroadLag = 0.07f;      // ...and less (a broader wave) by 20 s
    const int MaxSpeed = 115;          // the THUMP needs the robots' full speed

    public override IEnumerator Run(RoutineContext ctx) { return Show.Run(ctx, start => new Plan(start), MaxSpeed); }

    public class Plan : Show.IPlan
    {
        readonly Vector2[] start, spot;
        readonly int[] rank;          // place along the row, left to right
        readonly float[] upDelay;
        readonly Show.Eased height = new Show.Eased(0f, BeatHeight, RoundFrom, BeatHeight, CalmBy, CalmHeight, Show.PerformSecs, RippleHeight, Show.PerformSecs + Show.ReturnSecs * 0.6f, 0f);
        readonly Show.Eased hz = new Show.Eased(0f, 1f / BeatSecs, RoundFrom, 1f / BeatSecs, CalmBy, 1f / CalmBeatSecs, Show.PerformSecs, 1f / RippleSecs);
        readonly Show.Eased round = new Show.Eased(0f, 0f, RoundFrom, 0f, RoundTo, 1f);
        readonly Show.Eased lag = new Show.Eased(0f, 0f, RoundFrom, 0f, RoundTo, TravelLag, CalmBy, BroadLag);

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
        }

        // One heartbeat (x from 0 to 1): THUMP, thump, release
        static float Beat(float x)
        {
            return Show.Bump(x / ThumpShare) + SecondSize * Show.Bump((x - SecondAt) / SecondShare);
        }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Show.Total);
            float phase = hz.Integral(t) - rank[i] * lag.At(t);
            float x = phase - Mathf.Floor(phase);
            // Away from the participant, so beats never reach toward a robot still entering the row
            float y = spot[i].y - height.At(t) * Mathf.Lerp(Beat(x), Show.Bump(x), round.At(t));
            float along = Shapes.Ease(t / EnterSecs);
            float up = Shapes.Ease((t - upDelay[i]) / (upDelay[i] > 0f ? EnterSecs : BeatEaseSecs));
            var p = new Vector2(Mathf.Lerp(start[i].x, spot[i].x, along), Mathf.Lerp(start[i].y, y, up));
            return Show.Return(p, spot[i], t);
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
    }
}
