using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// LLB_resonance_v2 (D02), 3:20. Introduction: the messenger (LLBPlan). Then:
//   1. Panic at once: everyone rushes to its own patch and panics there, each its own way: some run in
//      circles, some spin on the spot, two "bounce" along the top edge, turning back at the mat's edge or
//      at each other. How fast they go follows the participant's heart rate, within LLBPlan.HeartMargin
//      of the script (92 beats a minute, easing to 70).
//   2. Calming, all pairs at the same time, each pair in its own way: one of each pair (the helper) stops,
//      goes to its partner and circles round it, slower and slower, while the partner spins on the spot,
//      slower and slower. The circles differ: size, direction, one swells in and out, one wobbles.
//   3. Each pair dances together, turning round each other.
//   4. Slowly they all join into one circle and dance in it, gently, every other robot swaying in and out.
//   5. One step further, toward the participant: they turn to look at them, come a little closer as a
//      flower, and the flower opens and closes with calm breathing (in 4 s, out 6 s), three times, as if
//      inviting the participant to breathe with them; then back to the middle.
//   Closing (the same in every routine): slowly back into the line; everyone looks at the participant.
// Planned for 8 robots (4 pairs); with fewer, the first pairs are used, and an odd robot calms on its own.
public class LLB_resonance_v2 : SwarmRoutine
{
    public override string Name { get { return "LLB_resonance_v2"; } }
    public override string Code { get { return "D02"; } }
    public override string Note { get { return "Panic, calming, flower breathes with you"; } }
    public override int MinRobots { get { return 8; } }   // planned and checked for 8

    public override IEnumerator Run(RoutineContext ctx) { return LLBShow.Run(ctx, start => new Plan(start), 100, followHeart: true); }

    public class Plan : LLBPlan
    {
        enum Role { Circle, Spin, Bounce }
        class Station
        {
            public Role role; public Vector2 centre, inner; public float radius;
            public Station(Role role, Vector2 centre, float radius = 0f) { this.role = role; this.centre = centre; this.radius = radius; }
            public Station(Vector2 rest, Vector2 inner) { role = Role.Bounce; centre = rest; this.inner = inner; }
            public Vector2 Start { get { return role == Role.Circle ? centre + new Vector2(0f, radius) : centre; } }
        }
        // Everything kept toward the middle of the mat: the two bouncers share a track across the middle,
        // meeting in the centre, between the top and bottom patches.
        const float TrackY = 230f;
        static readonly Station[] Helped =
        {
            new Station(Role.Circle, new Vector2(170f, 150f), 24f),
            new Station(Role.Spin, new Vector2(330f, 310f)),
            new Station(Role.Spin, new Vector2(330f, 150f)),
            new Station(Role.Circle, new Vector2(170f, 310f), 24f),
        };
        static readonly Station[] Helpers =
        {
            new Station(Role.Spin, new Vector2(250f, 112f)),
            new Station(Role.Circle, new Vector2(250f, 345f), 18f),
            new Station(new Vector2(380f, TrackY), new Vector2(274f, TrackY)),
            new Station(new Vector2(120f, TrackY), new Vector2(226f, TrackY)),
        };
        // each pair calms its own way: a moment's delay, circle size, direction, and a touch of its own
        static readonly float[] PairDelay = { 0f, 0.7f, 0.3f, 1.1f };
        static readonly float[] OrbitR = { 46f, 48f, 46f, 48f };
        static readonly float[] OrbitDir = { 1f, -1f, 1f, -1f };
        static readonly float[] OrbitSwell = { 0f, 4f, 0f, 0f }, OrbitWobble = { 0f, 0f, 3f, 0f };

        const float RushUnitsPerSec = 48f, ShakeMax = 9f;
        const float CircleHz = 0.55f, BounceHz = 0.27f, SpinSpeed = 48f, Erratic = 0.35f;
        const float HelperCalm = 2.5f, PartnerCalmFrom = 0.5f, PartnerCalm = 4f, ApproachFrom = 2.5f, ApproachSecs = 3f, OrbitFrom = 5.5f;
        const float OrbitSecs = 18f, InnerSpinFast = 34f, InnerSpinSlow = 8f, DanceSecs = 14f, DanceSway = 4f;
        const float GatherUnitsPerSec = 34f, DanceR = 104f, RingSway = 12f, RingSwayPeriod = 6f;
        // 5. toward the participant: a flower that breathes calmly
        const float LookSecs = 1.5f, ApproachSecs2 = 4f, FlowerR = 80f, PetalOut = 20f, PetalExtra = 20f, BackSecs = 5f;
        const int FlowerBreaths = 3;
        static readonly Vector2 FlowerCentre = new Vector2(250f, 268f);
        const float ScriptHigh = 92f, ScriptLow = 70f;

        readonly Station[] station;
        readonly int[] pairOf; readonly bool[] isHelper, alone;
        readonly float[] arriveAt, calmAt, calmSecs, orbitFromDeg, orbitSweep, side;
        readonly Running[] phase;
        readonly V2Show.WayToLine rush, gather;
        readonly Room rushRoom;
        readonly LiveTempo tempo;
        readonly Show.Eased[] orbitRate;
        readonly Vector2[] besideSpot;
        readonly float rushAt, panicEnd, calmStart, danceEnd, gatherAt, ringAt, lookAt, approachAt, breatheAt, backAt;
        readonly Show.Eased ringTurn;
        readonly int[] parity;

        public Plan(Vector2[] start) : base(start, Intro.Messenger)
        {
            int pairs = n / 2;
            bool solo = n % 2 == 1;
            var used = new List<Station>(); var usedPair = new List<int>(); var usedHelper = new List<bool>();
            for (int k = 0; k < pairs; k++) { used.Add(Helped[k]); usedPair.Add(k); usedHelper.Add(false); used.Add(Helpers[k]); usedPair.Add(k); usedHelper.Add(true); }
            if (solo) { used.Add(Helped[pairs]); usedPair.Add(pairs); usedHelper.Add(false); }
            var from = MiddleStarts();
            var slot = Swarm.LeastTravel(from, used.Select(s => s.Start).ToArray());
            station = new Station[n]; pairOf = new int[n]; isHelper = new bool[n]; alone = new bool[n];
            for (int i = 0; i < n; i++) { station[i] = used[slot[i]]; pairOf[i] = usedPair[slot[i]]; isHelper[i] = usedHelper[slot[i]]; alone[i] = solo && pairOf[i] == pairs; }

            // 1. rush and panic, at once: each robot starts panicking when it gets to its patch
            rushAt = MiddleAt;
            rush = LLB.Way(from, station.Select(s => s.Start).ToArray(), RushUnitsPerSec);
            arriveAt = Enumerable.Range(0, n).Select(i => rushAt + rush.ArrivesAt(i)).ToArray();
            rushRoom = new Room(n, (i, t) => rush.Position(i, t - rushAt), rushAt, rushAt + rush.Total + 0.5f, LLB.Keep, ShakeMax);

            // the time left: panic gets half of what the calming, the dances and the gathering leave
            float pairsSecs = OrbitFrom + OrbitSecs + DanceSecs + PairDelay.Max();
            var ringDeg = Enumerable.Range(0, n).Select(RingDeg).ToArray();
            // (the gathering's length depends on where the pairs end, which does not depend on the panic's length)
            besideSpot = new Vector2[n]; side = new float[n]; orbitFromDeg = new float[n]; orbitSweep = new float[n];
            for (int i = 0; i < n; i++)
            {
                besideSpot[i] = station[i].centre;
                if (!isHelper[i]) { continue; }
                int k = pairOf[i];
                Vector2 partner = Helped[k].centre, rest = station[i].centre;
                side[i] = Mathf.Sign(rest.x - partner.x);
                orbitFromDeg[i] = LLB.AngleOf(rest - partner);
                float endDeg = side[i] > 0f ? 0f : 180f;
                float sweep = Mathf.Repeat(OrbitDir[k] * (endDeg - orbitFromDeg[i]), 360f) + 360f;
                orbitSweep[i] = OrbitDir[k] * sweep;
                besideSpot[i] = LLB.On(partner, endDeg, OrbitR[k]);
            }
            gather = LLB.Way(besideSpot, ringDeg.Select(d => LLB.On(LLB.Centre, d, DanceR)).ToArray(), GatherUnitsPerSec);
            backAt = CloseAt - BackSecs;
            breatheAt = backAt - FlowerBreaths * LLB.BreathSecs;
            approachAt = breatheAt - ApproachSecs2;
            lookAt = approachAt - LookSecs;
            float free = lookAt - (rushAt + rush.Total) - pairsSecs - gather.Total;
            panicEnd = rushAt + rush.Total + 0.55f * free;
            calmStart = panicEnd;
            danceEnd = calmStart + pairsSecs;
            gatherAt = danceEnd;
            ringAt = gatherAt + gather.Total;

            calmAt = new float[n]; calmSecs = new float[n];
            for (int i = 0; i < n; i++)
            {
                float s = calmStart + PairDelay[pairOf[i] % 4];
                calmAt[i] = isHelper[i] || alone[i] ? s : s + PartnerCalmFrom;
                calmSecs[i] = isHelper[i] ? HelperCalm : PartnerCalm;
            }

            tempo = new LiveTempo(this, t => Mathf.Lerp(ScriptHigh, ScriptLow, Shapes.Ease((t - rushAt - 10f) / (panicEnd - rushAt))), rushAt);
            phase = new Running[n];
            for (int i = 0; i < n; i++)
            {
                int me = i;
                float hz = station[i].role == Role.Bounce ? BounceHz : CircleHz;
                phase[i] = new Running(t => 2f * Mathf.PI * hz * Level(me, t) * tempo.Factor(t) * (1f + Erratic * LLB.Wander(me, 5f, t, 0.2f, 0.6f).x), arriveAt[i]);
            }

            // each helper's circle round its partner: quick at first, slower and slower
            orbitRate = new Show.Eased[n];
            for (int i = 0; i < n; i++)
            {
                if (!isHelper[i]) { continue; }
                float o0 = calmStart + PairDelay[pairOf[i] % 4] + OrbitFrom, o1 = o0 + OrbitSecs;
                var unit = new Show.Eased(o0, 0f, o0 + 1.5f, 1f, o0 + 0.45f * OrbitSecs, 0.55f, o1 - 2f, 0.2f, o1, 0f, o1 + 10000f, 0f);
                float scale = 1f / (unit.Integral(o1) - unit.Integral(o0));
                orbitRate[i] = new Show.Eased(o0, 0f, o0 + 1.5f, scale, o0 + 0.45f * OrbitSecs, 0.55f * scale, o1 - 2f, 0.2f * scale, o1, 0f, o1 + 10000f, 0f);
            }

            // the ring dance turns once, gently, so it ends on the closing ring's places
            var ru = new Show.Eased(ringAt, 0f, ringAt + 4f, 1f, lookAt - 4f, 1f, lookAt, 0f, lookAt + 10000f, 0f);
            float rr = 360f / (ru.Integral(lookAt) - ru.Integral(ringAt));
            ringTurn = new Show.Eased(ringAt, 0f, ringAt + 4f, rr, lookAt - 4f, rr, lookAt, 0f, lookAt + 10000f, 0f);
            parity = V2Show.RingOrder(n).Select(p => p % 2 == 0 ? 1 : -1).ToArray();

            // turning on the spot, lanes
            for (int i = 0; i < n; i++)
            {
                int me = i;
                if (station[i].role == Role.Spin)
                {
                    float ph = 6.283f * Shapes.Hash(i, 9f);
                    SpinBy(i, arriveAt[i], calmAt[i] + calmSecs[i], t => SpinSpeed * Level(me, t) * tempo.Factor(t)
                        * Mathf.Clamp(2.2f * Mathf.Sin(2f * Mathf.PI * 0.16f * (t - arriveAt[me]) + ph), -1f, 1f) * (1f + Erratic * LLB.Wander(me, 7f, t, 0.3f, 0.8f).y));
                }
                if (station[i].role == Role.Bounce) { InLaneFor(i, arriveAt[i], calmAt[i] + calmSecs[i], 0f); }
                if (!isHelper[i])
                {
                    // the partner spins on the spot while its helper circles it, slower and slower
                    float o0 = calmStart + PairDelay[pairOf[i] % 4] + OrbitFrom, o1 = o0 + OrbitSecs;
                    float dir = -OrbitDir[pairOf[i] % 4];
                    SpinBy(i, o0, o1, t => dir * Mathf.Lerp(InnerSpinFast, InnerSpinSlow, Shapes.Ease((t - o0) / OrbitSecs)) * Shapes.Ease((t - o0) / 1.5f) * Shapes.Ease((o1 - t) / 1.5f));
                }
            }
            Section(rushAt, "panic: circles, spins, bouncing (pace follows the heart rate)");
            Section(calmStart, "calming: helpers circle their partners, partners spin, slower and slower");
            Section(calmStart + OrbitFrom + OrbitSecs, "each pair dances together");
            Section(gatherAt, "all join into one circle");
            Section(ringAt, "dancing gently in the circle");
            Section(lookAt, "they turn to the participant");
            Section(approachAt, "a little closer: a flower");
            Section(breatheAt, "the flower opens and closes with calm breathing (3 breaths)");
            Section(backAt, "back to the middle");
            for (int i = 0; i < n; i++) { FacePoint(i, lookAt, LookSecs, LLB.On(LLB.Centre, RingDeg(i), DanceR), Swarm.Participant); }
        }

        float Level(int i, float t) { return Shapes.Ease((t - arriveAt[i]) / 0.6f) * (1f - Shapes.Ease((t - calmAt[i]) / calmSecs[i])); }

        Vector2 Panicking(int i, float t)
        {
            var s = station[i];
            float level = 1f - Shapes.Ease((t - calmAt[i]) / calmSecs[i]);
            float a = phase[i].At(t);
            switch (s.role)
            {
                case Role.Circle: return s.centre + s.radius * level * new Vector2(Mathf.Sin(a), Mathf.Cos(a));
                case Role.Bounce:
                    const float k = 0.5f;   // a rounded turn at each end, not a sharp one
                    float b = (Mathf.Asin(k * Mathf.Sin(a - Mathf.PI / 2f)) / Mathf.Asin(k) + 1f) / 2f;
                    return Vector2.Lerp(s.centre, s.inner, b * level);
                default: return s.centre;
            }
        }

        // The pair's dance: both turn round the middle between them, once, a little in and out
        Vector2 Dancing(int i, float t)
        {
            int k = pairOf[i] % 4;
            float d0 = calmStart + PairDelay[k] + OrbitFrom + OrbitSecs;
            if (alone[i])
            {
                float a0 = 360f * Shapes.Ease((t - d0) / DanceSecs);
                return besideSpot[i] + 12f * Show.Bump((t - d0) / DanceSecs) * (LLB.Dir(a0 - 90f) - LLB.Dir(-90f));
            }
            Vector2 me = besideSpot[i];
            Vector2 other = isHelper[i] ? Helped[k].centre : HelperSpot(k);
            Vector2 mid = (me + other) / 2f;
            float u = Shapes.Ease((t - d0) / DanceSecs);
            float r = (me - mid).magnitude + DanceSway * Mathf.Sin(2f * Mathf.PI * (t - d0) / 5f) * Show.Bump((t - d0) / DanceSecs);
            return LLB.On(mid, LLB.AngleOf(me - mid) + OrbitDir[k] * 360f * u, r);
        }

        Vector2 HelperSpot(int k) { for (int j = 0; j < n; j++) { if (isHelper[j] && pairOf[j] == k) { return besideSpot[j]; } } return Helped[k].centre; }

        protected override Vector2 Middle(int i, float t)
        {
            if (t < arriveAt[i])
            {
                Vector2 q = rush.Position(i, t - rushAt);
                float r = rushRoom.At(i, t) * (1f - Shapes.Ease((t - (arriveAt[i] - 0.8f)) / 0.8f));
                return q + r * LLB.Wander(i, 2f, t, 0.8f, 1.6f);   // nobody stands still, even while waiting its turn
            }
            if (t < gatherAt)
            {
                int k = pairOf[i] % 4;
                float pairAt = calmStart + PairDelay[k];
                if (t >= pairAt + OrbitFrom + OrbitSecs) { return Dancing(i, t); }
                Vector2 p = Panicking(i, t);
                if (isHelper[i] && t > pairAt + ApproachFrom)
                {
                    Vector2 partner = Helped[k].centre;
                    p = Morph(station[i].centre, LLB.On(partner, orbitFromDeg[i], OrbitR[k]), t, pairAt + ApproachFrom, ApproachSecs);
                    if (t > pairAt + OrbitFrom)
                    {
                        float u = orbitRate[i].Integral(t) - orbitRate[i].Integral(pairAt + OrbitFrom);
                        float tt = t - pairAt - OrbitFrom;
                        float r = OrbitR[k] + OrbitSwell[k] * Mathf.Sin(2f * Mathf.PI * tt / 5f) * Show.Bump(tt / OrbitSecs)
                                            + OrbitWobble[k] * Mathf.Sin(2f * Mathf.PI * tt * 1.1f) * Show.Bump(tt / OrbitSecs);
                        p = LLB.On(partner, orbitFromDeg[i] + orbitSweep[i] * u, r);
                    }
                }
                return p;
            }
            if (t < ringAt) { return gather.Position(i, t - gatherAt); }
            if (t < lookAt)
            {
                float turned = ringTurn.Integral(t) - ringTurn.Integral(ringAt) - 360f;
                float sway = RingSway * LLB.Envelope(t, ringAt, 3f, lookAt - 5f, 5f) * Mathf.Sin(2f * Mathf.PI * (t - ringAt) / RingSwayPeriod);
                return LLB.On(LLB.Centre, RingDeg(i) + turned, DanceR + parity[i] * sway);
            }
            if (t < approachAt) { return LLB.On(LLB.Centre, RingDeg(i), DanceR); }
            // the flower: every other robot is a petal tip, the rest the hollows between; it opens as they breathe in
            float petal = Mathf.Cos(4f * (RingDeg(i) - RingDeg(0)) * Mathf.Deg2Rad);
            if (t < breatheAt) { float u = Shapes.Ease((t - approachAt) / ApproachSecs2); return LLB.On(Vector2.Lerp(LLB.Centre, FlowerCentre, u), RingDeg(i), Mathf.Lerp(DanceR, FlowerR, u)); }
            if (t < backAt) { return LLB.On(FlowerCentre, RingDeg(i), FlowerR + LLB.Breath(t - breatheAt) * (PetalOut + PetalExtra * petal)); }
            float b = Shapes.Ease((t - backAt) / BackSecs);
            return LLB.On(Vector2.Lerp(FlowerCentre, LLB.Centre, b), RingDeg(i), Mathf.Lerp(FlowerR, CloseR, b));
        }
    }
}
