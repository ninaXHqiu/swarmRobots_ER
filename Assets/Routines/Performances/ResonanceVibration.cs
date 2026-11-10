using System.Collections;
using UnityEngine;

// Resonance - Vibration. One complete ring that turns as a whole: every robot shares the same
// rotation, so the ring stays intact and evenly spaced, and no robot moves on its own.
// After the Messenger's introduction the robots swing onto a compact ring, already turning.
// Agitated: the ring surges forward again and again (a sharp burst of speed, easing back, never
// stopping or reversing). Settling: the same surge phrase repeats, each one weaker and a little
// longer, while the pace calms. Calm: about half a turn at a slow, steady pace, then the robots
// peel out of the turning ring into the line.
public class ResonanceVibration : SwarmRoutine
{
    public override string Name { get { return "v2 Resonance: vibration"; } }
    public override int MinRobots { get { return 2; } }

    // ---- Phrases (change the counts freely; the choreography follows) ----
    const int AgitatedSurges = 4;
    const int SettlingSurges = 4;
    const float CalmTurns = 0.5f;          // turns of the ring at the calm pace before the exit

    // ---- One surge phrase ----
    const float SurgeSecs = 1.2f;          // agitated: burst + ease back
    const float SettleLonger = 0.15f;      // each settling surge lasts this share longer than the one before
    const float SurgeRise = 0.25f;         // share of the phrase spent speeding up (sharp); the rest eases back

    // ---- Ring and pace ----
    const float FormSecs = 2.5f;           // line -> ring (the ring starts turning as it forms)
    const float Radius = 75f;              // compact: neighbours 57 apart with 8 robots
    const float AgitatedSpeed = 50f;       // mat units per second along the ring between bursts
    const float SurgeBoost = 60f;          // extra speed at the top of an agitated burst
    const float CalmSpeed = 22f;

    public override IEnumerator Run(RoutineContext ctx) { return Show.Run(ctx, start => new Plan(start)); }

    public class Plan : Show.IPlan
    {
        readonly Vector2[] start, spot;
        readonly float[] place, fromAngle, fromRadius, swing;
        readonly float[] surgeStart, surgeLength, surgeStrength;
        readonly float settleFrom, calmFrom, exitFrom;
        readonly Show.Eased pace;
        readonly Show.Integrated turned;
        public float Total { get; private set; }

        public Plan(Vector2[] start)
        {
            this.start = start;
            int n = start.Length;
            // Lay the phrases end to end
            int surges = AgitatedSurges + SettlingSurges;
            surgeStart = new float[surges]; surgeLength = new float[surges]; surgeStrength = new float[surges];
            float t = FormSecs;
            for (int k = 0; k < surges; k++)
            {
                int settling = k - AgitatedSurges + 1;   // 1, 2, ... for the settling surges
                surgeStart[k] = t;
                surgeLength[k] = settling > 0 ? SurgeSecs * (1f + SettleLonger * settling) : SurgeSecs;
                surgeStrength[k] = settling > 0 ? 1f - settling / (SettlingSurges + 1f) : 1f;
                t += surgeLength[k];
            }
            settleFrom = FormSecs + AgitatedSurges * SurgeSecs;
            calmFrom = t;
            exitFrom = calmFrom + CalmTurns * 2f * Mathf.PI * Radius / CalmSpeed;
            Total = exitFrom + Show.ReturnSecs;
            // Steady pace: picks up as the ring forms, calms over the settling surges, fades in the exit
            pace = new Show.Eased(0f, 0f, FormSecs, AgitatedSpeed, settleFrom, AgitatedSpeed, calmFrom, CalmSpeed, exitFrom, CalmSpeed, Total, 0f);
            turned = new Show.Integrated(s => Speed(s) / Radius, Total + 1f);

            place = Show.RingPlaces(start, Radius, turned.At(FormSecs));
            var atEnd = new float[n];
            for (int i = 0; i < n; i++) { atEnd[i] = place[i] + turned.At(FormSecs); }
            Show.SwingOnto(start, atEnd, out fromAngle, out fromRadius, out swing);
            var leaving = new Vector2[n];
            for (int i = 0; i < n; i++) { leaving[i] = Show.OnRing(place[i] + turned.At(exitFrom + Show.ReturnSecs / 2f), Radius); }
            spot = Show.LineSpots(leaving);
        }

        // Speed along the ring: the steady pace plus the current surge (zero, with zero change,
        // where one surge hands over to the next)
        float Speed(float t)
        {
            float surge = 0f;
            for (int k = 0; k < surgeStart.Length; k++)
            {
                float u = (t - surgeStart[k]) / surgeLength[k];
                if (u > 0f && u < 1f) { surge = SurgeBoost * surgeStrength[k] * Burst(u); }
            }
            return pace.At(t) + surge;
        }

        // A sharp rise, then a longer easing back
        static float Burst(float u)
        {
            float s = u < SurgeRise ? Mathf.Sin(Mathf.PI / 2f * u / SurgeRise) : Mathf.Cos(Mathf.PI / 2f * (u - SurgeRise) / (1f - SurgeRise));
            return s * s;
        }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            // Forming: swing round the centre onto the ring, turning with it from the start
            float e = Shapes.Ease(t / FormSecs);
            float angle = fromAngle[i] + e * (swing[i] + turned.At(t) - turned.At(FormSecs));
            float radius = Mathf.Lerp(fromRadius[i], Radius, e);
            Vector2 p = Shapes.Centre + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return Show.Return(p, spot[i], t, exitFrom);
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
    }
}
