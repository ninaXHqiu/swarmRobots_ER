using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// v2 Vibration. One ring that turns as a whole. Every robot keeps one evenly spaced slot around
// the mat centre; its position is always (slot angle + one shared rotation, one shared radius),
// so nothing moves on its own: no shake, no swivel, no random offsets.
//   Stress:  the shared rotation surges briskly and releases, again and again, never stopping or
//            reversing. In each block, after a few surges, the whole ring draws in, opens to a
//            visibly wider ring and draws back to the compact size while it keeps surging.
//   Settle:  the surges come back weaker and longer each time until the turn is slow and even.
//   Calm:    the ring turns on at a slow, steady pace (in longer versions it also breathes in
//            and out slowly), then the robots peel out of the turning ring into the line.
// Recovered: the ring forming from the line, the surge shape and the return to the line are the
// ones from ResonanceVibration; the draw in - open - draw back phrase is the shared-radius
// phrase of the Social Dance campfire (commit 0349aea). New: surges during the radius phrase,
// the calm breathing phrase and the timed versions.
public abstract class V2VibrationBase : SwarmRoutine
{
    protected abstract float Minutes { get; }   // 0 = natural length
    public override string Name { get { return "v2 Vibration (" + V2Show.Label(Minutes) + ")"; } }
    public override int MinRobots { get { return 2; } }
    public override IEnumerator Run(RoutineContext ctx) { return V2Show.Run(ctx, start => V2Vibration.For(start, Minutes), maxSpeed: 90); }
}
public class V2VibrationNatural : V2VibrationBase { protected override float Minutes { get { return 0f; } } }
public class V2Vibration3Min : V2VibrationBase { protected override float Minutes { get { return 3f; } } }
public class V2Vibration5Min : V2VibrationBase { protected override float Minutes { get { return 5f; } } }

public static class V2Vibration
{
    // ---- One surge ----
    const float SurgeSecs = 1.5f;          // burst + release
    const float SurgeRise = 0.25f;         // share of the surge spent speeding up; the rest eases back
    const float SettleLonger = 0.12f;      // each settling surge lasts this share longer than the last

    // ---- Ring and pace (mat units per second along the ring) ----
    const float FormSecs = 5f;             // line -> ring; the ring is already turning as it forms
    const float BaseSpeed = 25f;           // between surges
    const float SurgeBoost = 85f;          // extra at the top of a surge (peak 110)
    const float CalmSpeed = 16f;

    // ---- Radius: compact, drawn in, wide ----
    const float Compact = 72f, DrawnIn = 56f, Wide = 112f;
    const int SurgesBeforePhrase = 4;      // surges at the compact size before each radius phrase
    const float InSecs = 2.4f, OutSecs = 3.6f, WideSecs = 2.4f, BackSecs = 3.6f;   // 8 surges long
    const float CalmInSecs = 3f, CalmOutSecs = 4.5f, CalmWideSecs = 3f, CalmBackSecs = 4.5f;
    const float CalmWide = 104f, CalmIn = 60f;
    const float CalmQuarterSecs = 7f;      // calm turn before each calm breathing phrase
    const float CalmTurnSecs = 10f;        // the final calm turn before the line

    const float BlockSecs = SurgesBeforePhrase * SurgeSecs + InSecs + OutSecs + WideSecs + BackSecs;
    const float CalmPhraseSecs = CalmQuarterSecs + CalmInSecs + CalmOutSecs + CalmWideSecs + CalmBackSecs;

    static float SettleSecs(int settle)
    {
        float s = 0f;
        for (int k = 1; k <= settle; k++) { s += SurgeSecs * (1f + SettleLonger * k); }
        return s;
    }

    static float Duration(int blocks, int settle, int calmPhrases)
    {
        return FormSecs + blocks * BlockSecs + SurgesBeforePhrase * SurgeSecs + SettleSecs(settle)
             + calmPhrases * CalmPhraseSecs + CalmTurnSecs + Show.ReturnSecs;
    }

    // The plan for a version: natural, or filled with whole phrases up to `minutes` in all
    public static Plan For(Vector2[] start, float minutes)
    {
        int blocks = 1, settle = 5, calm = 0;
        if (minutes <= 0f) { return new Plan(start, blocks, settle, calm, 0f); }
        float budget = V2Show.Budget(minutes);
        bool added = true;
        while (added)
        {
            added = false;
            if (Duration(blocks + 1, settle, calm) <= budget) { blocks++; added = true; }
            if (Duration(blocks, settle, calm + 1) <= budget) { calm++; added = true; }
            if (settle < 8 && blocks % 2 == 0 && Duration(blocks, settle + 1, calm) <= budget) { settle++; added = true; }
        }
        // What is left (less than one phrase) goes to the final calm turn
        return new Plan(start, blocks, settle, calm, Mathf.Max(0f, budget - Duration(blocks, settle, calm)));
    }

    public class Plan : V2Show.IPlan
    {
        readonly Vector2[] start, spot;
        readonly float[] place, fromAngle, fromRadius, swing;
        readonly List<float> surgeStart = new List<float>(), surgeLength = new List<float>(), surgeStrength = new List<float>();
        readonly float exitFrom;
        readonly Show.Eased pace, radius;
        readonly Show.Integrated turned;
        public float Total { get; private set; }

        public Plan(Vector2[] start, int blocks, int settle, int calmPhrases, float extraCalmSecs)
        {
            this.start = start;
            int n = start.Length;
            var keys = new List<float> { 0f, Compact };
            float t = FormSecs;
            // Stress: surges all the way through; in every block the radius phrase runs under them
            for (int b = 0; b < blocks; b++)
            {
                float phrase = t + SurgesBeforePhrase * SurgeSecs;
                keys.AddRange(new[] { phrase, Compact, phrase + InSecs, DrawnIn, phrase + InSecs + OutSecs, Wide,
                                      phrase + InSecs + OutSecs + WideSecs, Wide, phrase + InSecs + OutSecs + WideSecs + BackSecs, Compact });
                t += BlockSecs;
            }
            float settleFrom = t + SurgesBeforePhrase * SurgeSecs;
            for (float s = FormSecs; s < settleFrom - 0.01f; s += SurgeSecs) { surgeStart.Add(s); surgeLength.Add(SurgeSecs); surgeStrength.Add(1f); }
            t = settleFrom;
            for (int k = 1; k <= settle; k++)
            {
                float length = SurgeSecs * (1f + SettleLonger * k);
                surgeStart.Add(t); surgeLength.Add(length); surgeStrength.Add(1f - k / (settle + 1f));
                t += length;
            }
            float calmFrom = t;
            for (int c = 0; c < calmPhrases; c++)
            {
                float phrase = t + CalmQuarterSecs;
                keys.AddRange(new[] { phrase, Compact, phrase + CalmInSecs, CalmIn, phrase + CalmInSecs + CalmOutSecs, CalmWide,
                                      phrase + CalmInSecs + CalmOutSecs + CalmWideSecs, CalmWide, t + CalmPhraseSecs, Compact });
                t += CalmPhraseSecs;
            }
            exitFrom = t + CalmTurnSecs + extraCalmSecs;
            Total = exitFrom + Show.ReturnSecs;
            radius = new Show.Eased(keys.ToArray());
            pace = new Show.Eased(0f, 0f, FormSecs, BaseSpeed, settleFrom, BaseSpeed, calmFrom, CalmSpeed, exitFrom, CalmSpeed, Total, 0f);
            // One shared rotation: speed along the ring / the shared radius
            turned = new Show.Integrated(s => Speed(s) / radius.At(s), Total + 1f);

            place = Show.RingPlaces(start, Compact, turned.At(FormSecs));
            var atEnd = new float[n];
            for (int i = 0; i < n; i++) { atEnd[i] = place[i] + turned.At(FormSecs); }
            Show.SwingOnto(start, atEnd, out fromAngle, out fromRadius, out swing);
            var leaving = new Vector2[n];
            for (int i = 0; i < n; i++) { leaving[i] = Show.OnRing(place[i] + turned.At(exitFrom + Show.ReturnSecs / 2f), Compact); }
            spot = Show.LineSpots(leaving);
        }

        // Speed along the ring: the steady pace plus the current surge
        float Speed(float t)
        {
            float surge = 0f;
            for (int k = 0; k < surgeStart.Count; k++)
            {
                float u = (t - surgeStart[k]) / surgeLength[k];
                if (u > 0f && u < 1f) { surge = SurgeBoost * surgeStrength[k] * Burst(u); break; }
            }
            return pace.At(t) + surge;
        }

        // A sharp rise, then a longer easing back
        static float Burst(float u)
        {
            float s = u < SurgeRise ? Mathf.Sin(Mathf.PI / 2f * u / SurgeRise) : Mathf.Cos(Mathf.PI / 2f * (u - SurgeRise) / (1f - SurgeRise));
            return s * s;
        }

        public float Turned(float t) { return turned.At(t); }
        public float Radius(float t) { return radius.At(t); }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            // Forming: swing round the centre onto the ring, turning with it from the start
            float e = Shapes.Ease(t / FormSecs);
            float angle = fromAngle[i] + e * (swing[i] + turned.At(t) - turned.At(FormSecs));
            float r = Mathf.Lerp(fromRadius[i], radius.At(t), e);
            Vector2 p = Shapes.Centre + r * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            return Show.Return(p, spot[i], t, exitFrom);
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
        public bool InLane(int i, float t) { return false; }
    }
}
