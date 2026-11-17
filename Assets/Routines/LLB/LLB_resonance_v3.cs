using System.Collections;
using UnityEngine;

// LLB_resonance_v3 (D03), 3:20. Introduction: the messenger (LLBPlan). Then:
//   1. The messenger steps back into its place and the whole line glides straight back to the middle of
//      the mat (a little beyond it, away from the participant, so it does not crowd them), already
//      breaking into a heart trace: peaks run along it, beat after beat, at the participant's heart rate
//      (within LLBPlan.HeartMargin of the script: 90 beats a minute, easing to 62; never above 92).
//   2. It beats there, the peaks growing a little as they soften.
//   3. The beat slows and the peaks soften into a gentle swell running along the line, which dies away.
//   4. The line curls round into a ring, which becomes a flower that opens and closes twice with calm
//      breathing (in 4 s, out 6 s).
//   Closing (the same in every routine): slowly back into the line; everyone looks at the participant.
// Robots only move toward and away from the participant until the line curls (in lanes), so the
// heart rate changes how fast they peak, never where they go.
public class LLB_resonance_v3 : SwarmRoutine
{
    public override string Name { get { return "LLB_resonance_v3"; } }
    public override string Code { get { return "D03"; } }
    public override string Note { get { return "Heart trace, swell, flower"; } }
    public override int MinRobots { get { return 8; } }   // planned and checked for 8

    public override IEnumerator Run(RoutineContext ctx) { return LLBShow.Run(ctx, start => new Plan(start), 100, followHeart: true); }

    public class Plan : LLBPlan
    {
        const float ReturnSecs = 2.5f, PeakAmp = 18f, HighAmp = 28f, BeatDelay = 0.09f, PeakShare = 0.6f, MaxBpm = 92f;
        const float GlideSecs = 7f, RowY = 225f, SmoothSecs = 40f, FadeSecs = 14f, CurlSecs = 12f, PetalOut = 20f, PetalExtra = 20f;
        const int FlowerBreaths = 2;
        const float ScriptHigh = 90f, ScriptLow = 62f;

        readonly int messenger;
        readonly float returnAt, backAt, smoothAt, fadeAt, curlAt, flowerAt;
        readonly LiveTempo tempo;

        public Plan(Vector2[] start) : base(start, Intro.Messenger)
        {
            messenger = LLB.MessengerSlot(n);
            returnAt = MiddleAt;
            backAt = returnAt + ReturnSecs;
            smoothAt = backAt + GlideSecs + 18f;
            flowerAt = CloseAt - FlowerBreaths * LLB.BreathSecs;
            curlAt = flowerAt - CurlSecs;
            fadeAt = curlAt - FadeSecs;
            tempo = new LiveTempo(this, t => Mathf.Lerp(ScriptHigh, ScriptLow, Shapes.Ease((t - backAt - 10f) / (fadeAt - backAt - 10f))), MiddleAt - 1f, MaxBpm);
            for (int i = 0; i < n; i++) { InLaneFor(i, i == messenger ? backAt : MiddleAt, curlAt, 90f); }   // (the others stand still in their lanes until it is back)
            Section(MiddleAt, "back into the line");
            Section(backAt, "the line glides to the middle, a heart trace running along it (tempo follows the heart rate)");
            Section(smoothAt, "the beat slows, peaks soften into a swell");
            Section(fadeAt, "the swell dies away");
            Section(curlAt, "the line curls into a ring");
            Section(flowerAt, "a flower opens and closes with calm breathing (2 breaths)");
        }

        // How far robot i is pushed toward the participant by the beat
        float Beat(int i, float t)
        {
            float beats = tempo.Beats(t - BeatDelay * i);
            float u = beats - Mathf.Floor(beats);
            float peak = Show.Bump(u / PeakShare) - 0.25f * Show.Bump((u - PeakShare) / (1f - PeakShare));
            float swell = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * u);
            float soft = Shapes.Ease((t - smoothAt) / SmoothSecs);
            float shape = Mathf.Lerp(peak, swell, soft);
            float from = backAt + 0.4f * GlideSecs;   // once the glide is under way, growing in gently
            float amp = Mathf.Lerp(PeakAmp, HighAmp, soft)   // the peaks grow only as they soften
                      * Shapes.Ease((t - from) / 4f) * (1f - Shapes.Ease((t - fadeAt) / FadeSecs));
            return amp * shape;
        }

        protected override Vector2 Middle(int i, float t)
        {
            if (t >= flowerAt)
            {
                float petal = Mathf.Cos(4f * (RingDeg(i) - RingDeg(0)) * Mathf.Deg2Rad);
                return LLB.On(LLB.Centre, RingDeg(i), CloseR + LLB.Breath(t - flowerAt) * (PetalOut + PetalExtra * petal));
            }
            if (t >= curlAt) { return Flatten(i, LLB.Centre, RingDeg(i), CloseR, 1f - (t - curlAt) / CurlSecs, RowY, LLB.Spacing); }
            float y = Mathf.Lerp(LLB.LineY, RowY, Shapes.Ease((t - backAt) / GlideSecs));
            Vector2 p = new Vector2(line[i].x, y + Beat(i, t));
            if (i == messenger && t < backAt)
            {
                // back into its place from straight below it, clear of its neighbours
                float u = Shapes.Ease((t - returnAt) / ReturnSecs), v = 1f - u;
                Vector2 a = MiddleStart(i), c = new Vector2(line[i].x, a.y);
                p = v * v * a + 2f * u * v * c + u * u * line[i];
            }
            return p;
        }
    }
}
