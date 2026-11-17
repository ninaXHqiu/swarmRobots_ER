using System.Collections;
using System.Linq;
using UnityEngine;

// LLB_cheering_v1 (D04), 3:20. Introduction: the messenger (LLBPlan). Then, as the messenger finishes:
//   1. The others take a quick step back in surprise, not quite together.
//   2. They set off walking erratically, looping in little circles, and gather into two circles, a big
//      one round a small one.
//   3. Fireworks, three times: both circles burst outward, sparkle, and drift back in, the two twisting
//      opposite ways.
//   4. They regroup into one circle, which turns, opening and closing cheerfully as it goes, first one
//      way round, then the other.
//   5. Everyone turns gently on the spot, and the circle draws in.
//   Closing (the same in every routine): slowly back into the line; everyone looks at the participant.
public class LLB_cheering_v1 : SwarmRoutine
{
    public override string Name { get { return "LLB_cheering_v1"; } }
    public override string Code { get { return "D04"; } }
    public override string Note { get { return "Surprise, fireworks, circle"; } }
    public override int MinRobots { get { return 8; } }   // planned and checked for 8

    public override IEnumerator Run(RoutineContext ctx) { return LLBShow.Run(ctx, start => new Plan(start), 100); }

    public class Plan : LLBPlan
    {
        const float StepBack = 26f, StepSecs = 0.5f, StepSpread = 0.7f, SurpriseSecs = 1.6f;
        const float WalkUnitsPerSec = 26f, LoopMax = 14f, LoopHz = 0.65f, WalkTail = 3f, InnerR = 44f;
        const int Bursts = 3;
        const float BurstOut = 1.1f, Sparkle = 1.4f, FallBack = 4.2f, BurstRest = 1.6f;
        const float InnerBurst = 1.9f, OuterBurst = 1.45f, BurstTwistDeg = 14f;
        const float RegroupSecs = 4f, RingR = 100f, Open = 24f, OpenPeriod = 3.2f, TurnDegPerSec = 26f;
        const float SpinSecs = 5f, SpinSpeed = 18f, ShrinkSecs = 5f;

        readonly Vector2[] stepped, burstSpot;
        readonly bool[] inner;
        readonly V2Show.WayToLine walk;
        readonly Room room;
        readonly float walkAt, burstAt, regroupAt, ringAt, switchAt, spinAt, shrinkAt;
        readonly int[] parity;
        readonly Show.Eased turn;
        readonly int messenger;

        public Plan(Vector2[] start) : base(start, Intro.Messenger)
        {
            messenger = LLB.MessengerSlot(n);
            stepped = MiddleStarts();
            for (int i = 0; i < n; i++) { if (i != messenger) { stepped[i] += new Vector2(0f, -StepBack); } }

            // One circle (the closing ring's places, wider) split in two: every few places a robot stands
            // further in (the small circle), so regrouping is just those stepping straight out into the gaps.
            var place = V2Show.RingOrder(n);
            parity = place.Select(p => p % 2 == 0 ? 1 : -1).ToArray();
            int k = Mathf.Max(1, Mathf.RoundToInt(n * 0.375f));
            var innerPlaces = Enumerable.Range(0, k).Select(j => Mathf.RoundToInt(j * n / (float)k)).ToArray();
            inner = Enumerable.Range(0, n).Select(i => innerPlaces.Contains(place[i])).ToArray();
            burstSpot = Enumerable.Range(0, n).Select(i => LLB.On(LLB.Centre, RingDeg(i), inner[i] ? InnerR : RingR)).ToArray();

            walkAt = MiddleAt + SurpriseSecs;
            walk = LLB.Way(stepped, burstSpot, WalkUnitsPerSec);
            burstAt = walkAt + walk.Total + WalkTail;
            room = new Room(n, (i, t) => walk.Position(i, t - walkAt), walkAt, burstAt, LLB.Keep + 2f, LoopMax);
            regroupAt = burstAt + Bursts * (BurstOut + Sparkle + FallBack + BurstRest);
            ringAt = regroupAt + RegroupSecs;
            shrinkAt = CloseAt - ShrinkSecs;
            spinAt = shrinkAt - SpinSecs;
            // clockwise, then back the other way: the two halves the same, so it ends where it started
            switchAt = (ringAt + spinAt) / 2f;
            float v = TurnDegPerSec;
            turn = new Show.Eased(ringAt, 0f, ringAt + 3f, v, switchAt - 2f, v, switchAt + 2f, -v, spinAt - 3f, -v, spinAt, 0f, spinAt + 10000f, 0f);

            for (int i = 0; i < n; i++)
            {
                if (i != messenger) { InLaneFor(i, MiddleAt, walkAt, 90f); }
                Spin(i, spinAt, SpinSecs, (i % 2 == 0 ? 1f : -1f) * SpinSpeed, 1f);
            }
            float burstSecs = BurstOut + Sparkle + FallBack + BurstRest;
            Section(MiddleAt, "surprise: a step back");
            Section(walkAt, "erratic walk with little loops into two circles");
            for (int b = 0; b < Bursts; b++) { Section(burstAt + b * burstSecs, "fireworks " + (b + 1)); }
            Section(regroupAt, "regroup into one circle");
            Section(ringAt, "the circle turns, opening and closing");
            Section(switchAt, "and back the other way");
            Section(spinAt, "turning gently on the spot");
        }

        Vector2 Surprise(int i, float t)
        {
            float at = MiddleAt + StepSpread * Shapes.Hash(i, 2f);
            return Vector2.Lerp(MiddleStart(i), stepped[i], Shapes.Ease((t - at) / StepSecs));
        }

        Vector2 Bursting(int i, float t)
        {
            float secs = BurstOut + Sparkle + FallBack + BurstRest;
            float u = t - burstAt, scale = 1f, twist = 0f;
            if (u > 0f && u < Bursts * secs)
            {
                float v = Mathf.Repeat(u, secs);
                float k = inner[i] ? InnerBurst : OuterBurst;
                float out1 = Shapes.Ease(v / BurstOut) * (1f - Shapes.Ease((v - BurstOut - Sparkle) / FallBack));
                float sparkle = 0.04f * Mathf.Sin(2f * Mathf.PI * (v - BurstOut) / 0.5f) * Show.Bump((v - BurstOut) / Sparkle);
                scale = 1f + (k - 1f) * out1 + sparkle;
                twist = BurstTwistDeg * Mathf.Sin(2f * Mathf.PI * v / secs) * Show.Bump(v / secs) * 2f;
            }
            return LLB.On(LLB.Centre, RingDeg(i) + twist * (inner[i] ? 1f : -1f), (inner[i] ? InnerR : RingR) * scale);
        }

        protected override Vector2 Middle(int i, float t)
        {
            if (t < walkAt) { return Surprise(i, t); }
            if (t < burstAt)
            {
                Vector2 p = walk.Position(i, t - walkAt);
                float r = room.At(i, t) * (1f - Shapes.Ease((t - (burstAt - 1.2f)) / 1.2f));
                float a = 2f * Mathf.PI * LoopHz * (t - walkAt) + 6.283f * Shapes.Hash(i, 3f);
                Vector2 loop = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (i % 2 == 0 ? 1f : -1f);
                return p + r * (0.75f * loop + 0.25f * LLB.Wander(i, 3f, t, 0.3f, 0.8f));
            }
            if (t < regroupAt) { return Bursting(i, t); }
            if (t < ringAt) { return LLB.On(LLB.Centre, RingDeg(i), Mathf.Lerp(inner[i] ? InnerR : RingR, RingR, Shapes.Ease((t - regroupAt) / RegroupSecs))); }
            float turned = turn.Integral(t) - turn.Integral(ringAt);
            float open = Open * LLB.Envelope(t, ringAt, 3f, spinAt - 3f, 3f) * (0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * (t - ringAt) / OpenPeriod));
            float r0 = Mathf.Lerp(RingR, CloseR, Shapes.Ease((t - shrinkAt) / ShrinkSecs));
            return LLB.On(LLB.Centre, RingDeg(i) + turned, r0 + open);
        }
    }
}
