using System.Collections;
using System.Linq;
using UnityEngine;

// LLB_cheering_v2 (D05), 3:20. Introduction: the messenger (LLBPlan). Then:
//   1. In twos, neighbours turn to face each other and shake, as if talking.
//   2. Each pair goes off to its own place on the mat; both spin on the spot, then circle round each
//      other, dancing (a little in and out as they go).
//   3. The pairs split and swap partners (one of each pair moves on to the next pair's place), and the
//      new pairs dance a little again, the other way round, ending with a spin.
//   4. Everyone gathers into a heart (upright as the participant sees it), which beats softly, sways a
//      little, and then opens into a circle.
//   Closing (the same in every routine): slowly back into the line; everyone looks at the participant.
// With an odd number of robots, the last one dances on its own in the middle.
public class LLB_cheering_v2 : SwarmRoutine
{
    public override string Name { get { return "LLB_cheering_v2"; } }
    public override string Code { get { return "D05"; } }
    public override string Note { get { return "Pairs dance, swap, beating heart"; } }
    public override int MinRobots { get { return 8; } }   // planned and checked for 8

    public override IEnumerator Run(RoutineContext ctx) { return LLBShow.Run(ctx, start => new Plan(start), 100); }

    public class Plan : LLBPlan
    {
        const float TalkSecs = 4.5f, BackSecs = 2.5f, RiseSecs = 5f, RowY = 200f;
        const float TravelSecs = 7f, Half = 28f, SpinSecs = 3f, SpinSpeed = 32f;
        const float DanceSecs = 16f, DanceTurns = 3f, DanceInOut = 5f, DanceInOutSecs = 3f, SoloCircle = 14f;
        const float SwapUnitsPerSec = 34f, Dance2Secs = 12f, Dance2Turns = 2f, Spin2Secs = 2.5f;
        const float GatherUnitsPerSec = 34f, HeartScale = 6.6f, BeatBy = 0.08f, BeatSecs = 1.25f, SwayDeg = 5f, SwaySecs = 9f, OpenSecs = 7f;
        static readonly Vector2 HeartCentre = new Vector2(250f, 236f);

        static readonly Vector2[][] PairPlaces =
        {
            null, null,
            new[] { new Vector2(140f, 240f), new Vector2(360f, 240f) },
            new[] { new Vector2(140f, 150f), new Vector2(360f, 150f), new Vector2(250f, 300f) },
            new[] { new Vector2(145f, 125f), new Vector2(355f, 125f), new Vector2(355f, 278f), new Vector2(145f, 278f) },
        };
        static readonly Vector2 SoloPlace2 = new Vector2(250f, 240f), SoloPlace3 = new Vector2(250f, 215f);

        readonly int pairs;
        readonly int[] pairOf, placeOfPair; readonly bool[] isB;
        readonly Vector2[] place, dance1At, dance2At;
        readonly V2Show.WayToLine swap, gather;
        readonly Vector2[] rowSpot;
        readonly int messenger;
        readonly float backAt, riseAt, travelAt, spinAt, danceAt, swapAt, dance2At0, spin2At, gatherAt, heartAt, openAt;
        readonly Vector2[] heartSpot;   // each robot's place on the heart (offset from its centre, before beating)

        public Plan(Vector2[] start) : base(start, Intro.Messenger)
        {
            pairs = Mathf.Min(4, n / 2);
            bool solo = n % 2 == 1;
            place = PairPlaces[pairs];
            pairOf = new int[n]; isB = new bool[n];
            for (int i = 0; i < n; i++) { pairOf[i] = i / 2; isB[i] = i % 2 == 1; }
            if (solo) { pairOf[n - 1] = -1; }
            var from = MiddleStarts();
            messenger = LLB.MessengerSlot(n);
            // after talking the messenger steps back into its place and the line glides up to the middle of the
            // mat, from where each pair goes straight up or down to its place without crossing another
            var row = line.Select(q => new Vector2(q.x, RowY)).ToArray();
            var middles = Enumerable.Range(0, pairs).Select(k => (row[2 * k] + row[2 * k + 1]) / 2f).ToArray();
            placeOfPair = Swarm.LeastTravel(middles, place);
            Vector2 soloPlace = pairs == 3 ? SoloPlace3 : SoloPlace2;
            // In each pair, B is the one that stands on the side toward the next place round (the shorter trips)
            for (int k = 0; k < pairs; k++)
            {
                int pp = placeOfPair[k], qq = (pp + 1) % pairs;
                Vector2 toward = (place[qq] - place[pp]).normalized;
                Vector2 sa = place[pp] - Half * toward, sb = place[pp] + Half * toward;
                Vector2 r0 = row[2 * k], r1 = row[2 * k + 1];
                // the two go straight to their sides without crossing each other (and, if both ways are clear, the shorter)
                bool crossKeep = Crosses(r0, sa, r1, sb), crossFlip = Crosses(r0, sb, r1, sa);
                bool flip = crossKeep != crossFlip ? crossKeep
                          : (r0 - sb).sqrMagnitude + (r1 - sa).sqrMagnitude < (r0 - sa).sqrMagnitude + (r1 - sb).sqrMagnitude;
                isB[2 * k] = flip; isB[2 * k + 1] = !flip;
            }
            // First dance: each pair lies along the way to the next place round, B on the side toward it.
            // The swap: every B goes straight on to the next place and stands on the side it came from; A swings round.
            dance1At = new Vector2[n]; dance2At = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                if (pairOf[i] < 0) { dance1At[i] = dance2At[i] = soloPlace; continue; }
                int p = placeOfPair[pairOf[i]], q = (p + 1) % pairs;
                Vector2 toNext = (place[q] - place[p]).normalized, fromPrev = (place[(p - 1 + pairs) % pairs] - place[p]).normalized;
                dance1At[i] = place[p] + (isB[i] ? Half : -Half) * toNext;
                dance2At[i] = isB[i] ? place[q] - Half * toNext : place[p] - Half * fromPrev;
            }

            backAt = MiddleAt + TalkSecs;
            riseAt = backAt + BackSecs;
            travelAt = riseAt + RiseSecs;
            rowSpot = row;
            spinAt = travelAt + TravelSecs;
            danceAt = spinAt + SpinSecs;
            swapAt = danceAt + DanceSecs;
            swap = LLB.Way(dance1At, dance2At, SwapUnitsPerSec);
            dance2At0 = swapAt + swap.Total;
            spin2At = dance2At0 + Dance2Secs;
            gatherAt = spin2At + Spin2Secs;
            // the heart: n places evenly spaced along its outline, from the dip at the top round to the tip;
            // each robot takes the place nearest its spot in the closing ring, so the heart opens straight into it
            var outline = HeartOutline(n);
            var ring = RingSpots();
            var hslot = Swarm.LeastTravel(ring, outline.Select(o => HeartCentre + o).ToArray());
            heartSpot = Enumerable.Range(0, n).Select(i => outline[hslot[i]]).ToArray();
            gather = LLB.Way(dance2At, heartSpot.Select(o => HeartCentre + o).ToArray(), GatherUnitsPerSec);
            heartAt = gatherAt + gather.Total;
            openAt = CloseAt - OpenSecs;

            for (int i = 0; i < n; i++)
            {
                float faceDeg = 90f;
                if (pairOf[i] >= 0) { int partner = i % 2 == 1 ? i - 1 : i + 1; faceDeg = LLB.AngleOf(from[partner] - from[i]); }
                Face(i, MiddleAt, TalkSecs, faceDeg);
                Wiggle(i, MiddleAt + 0.9f + 0.4f * Shapes.Hash(i, 3f), TalkSecs - 1.6f, 24f, 0.28f);
                InLaneFor(i, riseAt, travelAt, 90f);
                Spin(i, spinAt, SpinSecs, (isB[i] ? -1f : 1f) * SpinSpeed);
                Spin(i, spin2At, Spin2Secs, (isB[i] ? 1f : -1f) * SpinSpeed);
            }
            Section(MiddleAt, "pairs face each other and talk");
            Section(riseAt, "the line glides to the middle");
            Section(travelAt, "each pair to its own place");
            Section(spinAt, "spinning on the spot");
            Section(danceAt, "dancing round each other");
            Section(swapAt, "swapping partners");
            Section(dance2At0, "the new pairs dance");
            Section(gatherAt, "everyone gathers into a heart");
            Section(heartAt, "the heart beats softly");
            Section(openAt, "the heart opens into a circle");
        }

        // n points evenly spaced along a heart's outline (mat units, y toward the participant), starting at the
        // dip between the two lobes; the tip points at the participant
        static bool Crosses(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            System.Func<Vector2, Vector2, Vector2, float> side = (p, q, r) => (q.x - p.x) * (r.y - p.y) - (q.y - p.y) * (r.x - p.x);
            return side(a, b, c) * side(a, b, d) < 0f && side(c, d, a) * side(c, d, b) < 0f;
        }

        // For 8 robots the places are set by hand so the heart reads clearly: the tip toward the participant,
        // two round lobes with a shallow dip between them, and the sides
        static readonly Vector2[] Heart8 =
        {
            new Vector2(0f, -42f), new Vector2(55f, -82f), new Vector2(100f, -32f), new Vector2(62f, 38f),
            new Vector2(0f, 96f), new Vector2(-62f, 38f), new Vector2(-100f, -32f), new Vector2(-55f, -82f),
        };
        static Vector2[] HeartOutline(int n)
        {
            if (n == 8) { return (Vector2[])Heart8.Clone(); }
            const int Fine = 2000;
            var pts = new Vector2[Fine + 1]; var len = new float[Fine + 1];
            for (int k = 0; k <= Fine; k++)
            {
                float a = 2f * Mathf.PI * k / Fine;
                float x = 16f * Mathf.Pow(Mathf.Sin(a), 3f);
                float y = 13f * Mathf.Cos(a) - 5f * Mathf.Cos(2f * a) - 2f * Mathf.Cos(3f * a) - Mathf.Cos(4f * a);
                pts[k] = new Vector2(x, -(y - 2.5f)) * HeartScale;
                if (k > 0) { len[k] = len[k - 1] + (pts[k] - pts[k - 1]).magnitude; }
            }
            var o = new Vector2[n]; int j = 0;
            for (int m = 0; m < n; m++)
            {
                float want = len[Fine] * m / n;
                while (j < Fine && len[j + 1] < want) { j++; }
                o[m] = pts[j];
            }
            return o;
        }

        // A soft heartbeat: a swell and a smaller second one, each beat
        float Beat(float t)
        {
            float u = Mathf.Repeat(t - heartAt, BeatSecs) / BeatSecs;
            return Show.Bump(u / 0.45f) + 0.5f * Show.Bump((u - 0.35f) / 0.45f);
        }

        Vector2 Dance(int i, Vector2[] at, float t, float from, float secs, float turns, float dir)
        {
            if (pairOf[i] < 0)
            {
                float a0 = 2f * Mathf.PI * turns * Shapes.Ease((t - from) / secs) * dir;
                float rr = SoloCircle * Show.Bump((t - from) / secs) * 2f;
                return at[i] + rr * new Vector2(Mathf.Sin(a0), -1f + Mathf.Cos(a0)) * 0.5f;
            }
            int partner = FindPartner(i, at);
            Vector2 mid = (at[i] + at[partner]) / 2f, side = at[i] - mid;
            float u = Shapes.Ease((t - from) / secs);
            float r = side.magnitude + DanceInOut * Mathf.Sin(2f * Mathf.PI * (t - from) / DanceInOutSecs) * Show.Bump((t - from) / secs);
            return LLB.On(mid, LLB.AngleOf(side) + dir * 360f * turns * u, r);
        }

        int FindPartner(int i, Vector2[] at)
        {
            int best = i; float d = float.MaxValue;
            for (int j = 0; j < n; j++) { if (j != i && pairOf[j] >= 0 && (at[j] - at[i]).magnitude < d) { d = (at[j] - at[i]).magnitude; best = j; } }
            return best;
        }

        protected override Vector2 Middle(int i, float t)
        {
            if (t < backAt) { return MiddleStart(i); }
            if (t < riseAt)
            {
                if (i != messenger) { return MiddleStart(i); }
                float u = Shapes.Ease((t - backAt) / BackSecs), v = 1f - u;
                Vector2 a = MiddleStart(i), c = new Vector2(line[i].x, a.y);
                return v * v * a + 2f * u * v * c + u * u * line[i];
            }
            if (t < travelAt) { return Vector2.Lerp(line[i], new Vector2(line[i].x, RowY), Shapes.Ease((t - riseAt) / RiseSecs)); }
            if (t < spinAt)
            {
                // straight up or down out of the row first (pairs alternate up and down), then over to its place
                float u = Shapes.Ease((t - travelAt) / TravelSecs), v = 1f - u;
                Vector2 a = rowSpot[i], b = dance1At[i], c = new Vector2(a.x, b.y);
                return v * v * a + 2f * u * v * c + u * u * b;
            }
            if (t < danceAt) { return dance1At[i]; }
            if (t < swapAt) { return Dance(i, dance1At, t, danceAt, DanceSecs, DanceTurns, 1f); }
            if (t < dance2At0) { return swap.Position(i, t - swapAt); }
            if (t < spin2At) { return Dance(i, dance2At, t, dance2At0, Dance2Secs, Dance2Turns, -1f); }
            if (t < gatherAt) { return dance2At[i]; }
            if (t < heartAt) { return gather.Position(i, t - gatherAt); }
            float env = LLB.Envelope(t, heartAt, 2f, openAt - 3f, 3f);
            float scale = 1f + BeatBy * env * Beat(t);
            float sway = SwayDeg * env * Mathf.Sin(2f * Mathf.PI * (t - heartAt) / SwaySecs) * Mathf.Deg2Rad;
            Vector2 o = heartSpot[i] * scale;
            Vector2 h = HeartCentre + new Vector2(o.x * Mathf.Cos(sway) - o.y * Mathf.Sin(sway), o.x * Mathf.Sin(sway) + o.y * Mathf.Cos(sway));
            return Morph(h, RingSpot(i), t, openAt, OpenSecs);
        }
    }
}
