using System.Collections;
using UnityEngine;

// Resonance - Vibration. After the Messenger's introduction the robots swing onto one compact
// ring around the mat centre. As soon as it reads as a circle, the whole ring surges: quick,
// synchronised bursts of speed while it keeps turning one way (every robot shares the same
// rotation, so spacing holds; no robot shakes on its own). The surges keep coming but weaken and
// spread out, and the turning slows to a calm, even pace; from 20 s the ring circles calmly and
// at 30 s it flows into the line.
public class ResonanceVibration : SwarmRoutine
{
    public override string Name { get { return "v2 Resonance: vibration"; } }
    public override int MinRobots { get { return 2; } }

    // ---- Timing (seconds after the introduction; the return to the line starts at 30) ----
    const float FormSecs = 3f;      // line -> ring, already turning
    const float SurgeFrom = 2.5f;   // first surge, once the ring reads as a circle
    const float SettleFrom = 5f;    // surges start to weaken and the turning to slow...
    const float SettleTo = 20f;     // ...until the ring turns calmly with no surges

    // ---- Ring ----
    const float Radius = 80f;           // compact: neighbours 61 apart with 8 robots
    const float BriskSpeed = 45f;       // mat units per second along the ring at the start
    const float CalmSpeed = 14f;        // from 20 s on, easing to a stop during the return
    const float SurgeBoost = 60f;       // extra speed at the top of a surge, at the start
    const float SurgeSecs = 1f;         // one surge phrase (burst + coast) at the start...
    const float CalmSurgeSecs = 2.6f;   // ...stretching to this as the surges fade
    const float SurgeShare = 0.45f;     // share of the phrase spent in the burst

    public override IEnumerator Run(RoutineContext ctx) { return Show.Run(ctx, start => new Plan(start)); }

    public class Plan : Show.IPlan
    {
        readonly Vector2[] start, spot;
        readonly float[] place, fromAngle, fromRadius, swing;
        readonly Show.Eased baseSpeed = new Show.Eased(0f, BriskSpeed, SettleFrom, BriskSpeed, SettleTo, CalmSpeed, Show.PerformSecs, CalmSpeed, Show.Total, 0f);
        readonly Show.Eased boost = new Show.Eased(SurgeFrom, SurgeBoost, SettleFrom, SurgeBoost, SettleTo, 0f);
        readonly Show.Eased surgeHz = new Show.Eased(SurgeFrom, 1f / SurgeSecs, SettleFrom, 1f / SurgeSecs, SettleTo, 1f / CalmSurgeSecs);
        readonly Show.Integrated turned;

        public Plan(Vector2[] start)
        {
            this.start = start;
            int n = start.Length;
            turned = new Show.Integrated(t => Speed(t) / Radius, Show.Total + 1f);
            place = Show.RingPlaces(start, Radius, turned.At(FormSecs));
            var atEnd = new float[n];
            for (int i = 0; i < n; i++) { atEnd[i] = place[i] + turned.At(FormSecs); }
            Show.SwingOnto(start, atEnd, out fromAngle, out fromRadius, out swing);
            var leaving = new Vector2[n];
            for (int i = 0; i < n; i++) { leaving[i] = Show.OnRing(place[i] + turned.At(Show.PerformSecs + Show.ReturnSecs / 2f), Radius); }
            spot = Show.LineSpots(leaving);
        }

        // Speed along the ring: a steady pace plus the shared surges
        float Speed(float t)
        {
            float surge = 0f;
            if (t > SurgeFrom)
            {
                float cycles = surgeHz.Integral(t) - surgeHz.Integral(SurgeFrom);
                surge = boost.At(t) * Show.Bump((cycles - Mathf.Floor(cycles)) / SurgeShare);
            }
            return baseSpeed.At(t) + surge;
        }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Show.Total);
            // Forming: swing round the centre onto the ring, turning with it from the start
            float e = Shapes.Ease(t / FormSecs);
            float angle = fromAngle[i] + e * (swing[i] + turned.At(t) - turned.At(FormSecs));
            float radius = Mathf.Lerp(fromRadius[i], Radius, e);
            Vector2 p = Shapes.Centre + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return Show.Return(p, spot[i], t);
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
    }
}
