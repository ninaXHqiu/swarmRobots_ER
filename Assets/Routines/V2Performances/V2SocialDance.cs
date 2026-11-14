using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// v2 Social Dance. Four partner dances that become one dance round the campfire.
//   Pairs:    each pair takes one corner of the central dance area, with its own centre. The
//             partners are always on opposite sides of that centre, moving in opposite
//             directions: when one sweeps out to the top left the other sweeps out to the
//             bottom right. One sweep: from beside each other (PassGap apart) they pass in
//             opposite directions, open away from each other, curve round and come back in to
//             pass again. Each pair dances a little out of step with the others, so the four
//             pairs read as four dances.
//   Gather:   from the last inward sweep each pair curves on into its two places of a compact
//             ring at the mat centre. The ring starts its slow turn as the first pair arrives.
//   Campfire: one turning ring, evenly spaced. It opens out gradually to its final size, keeps
//             turning at that size, and peels into the line, every robot onto its own slot.
// The order round the ring is the one that opens back into the robots' own line slots
// (V2Show.RingOrder), so the pairs are the robots next to each other in that order: with 8
// robots, slots 1+2, 3+5, 7+8 and 4+6 counted from the left.
// Recovered: choosing corners by least travel, the walk-in blended with the dance, and the
// return to the line come from CheeringSocialDance. New: the opposite wing loops, the pairing
// by ring order and the curve into the ring.
public abstract class V2SocialDanceBase : SwarmRoutine
{
    protected abstract float Minutes { get; }
    public override string Name { get { return "v2 Social Dance (" + V2Show.Label(Minutes) + ")"; } }
    public override string Code { get { return "C09" + (Minutes <= 0f ? "n" : (Minutes < 4f ? "3" : "5")); } }
    public override string Note { get { return "Pairs, then campfire ring"; } }
    public override int MinRobots { get { return 2; } }
    public override IEnumerator Run(RoutineContext ctx) { return V2Show.Run(ctx, start => V2SocialDance.For(start, Minutes), maxSpeed: 70); }
}
public class V2SocialDanceNatural : V2SocialDanceBase { protected override float Minutes { get { return 0f; } } }
public class V2SocialDance3Min : V2SocialDanceBase { protected override float Minutes { get { return 3f; } } }
public class V2SocialDance5Min : V2SocialDanceBase { protected override float Minutes { get { return 5f; } } }

public static class V2SocialDance
{
    // ---- Partner dance ----
    const float ArriveSecs = 5.5f;         // from the line to the pair's corner, the dance starting on the way
    const float PhraseSecs = 3.6f;         // one sweep: pass, open away, round and back in
    const float OutOfStep = 0.1f;          // each pair this share of a sweep ahead of the one before
    const float StartOpen = 0.5f;          // the walk in ends on the open wing, this far into a sweep, not where the partners pass
    // The corners of the central dance area, going round
    static readonly Vector2[] Corners = { new Vector2(1, 1), new Vector2(-1, 1), new Vector2(-1, -1), new Vector2(1, -1) };
    const float CornerOffset = 85f;        // pair centre from the mat centre, along x and along y
    const float PassGap = 60f;             // partners' distance as they pass each other
    const float WingWidth = 46f;           // how far a wing opens away from the pair's centre line
    const float WingLength = 28f;          // how far a wing reaches along it, each way

    // ---- Ring ----
    const float JoinSecs = 3.6f;           // the last inward sweep -> a place in the turning ring
    const float JoinEarly = 0.25f;         // ...starting this share of a sweep before the partners would pass again
    const float JoinRadius = 70f;          // compact ring
    const float FinalRadius = 128f;        // diameter 256 = 62% of the mat's usable 410
    const float CompactSecs = 3f;          // the compact ring turns before it opens
    const float OpenSecs = 10f;            // opening out
    const float WideSecs = 6f;             // dancing on at the final size, at least
    const float RingSpeed = 28f;           // mat units per second along the ring, whatever its size
    const float TurnSecs = 2f * Mathf.PI * FinalRadius / RingSpeed;   // one turn at the final size

    static float Duration(int phrases, float wideSecs)
    {
        return JoinFrom(phrases, 0) + JoinSecs + CompactSecs + OpenSecs + wideSecs + Show.ReturnSecs;
    }

    // The ring has to come round to one particular angle to open into the line with every robot
    // on its own slot. The turning at the final size is made as long as that takes (turning
    // either way, whichever needs less), or, in a timed version, as near to the time left as a
    // whole number of turns allows.
    public static Plan For(Vector2[] start, float minutes)
    {
        int phrases = 4;
        float leftover = -1f;
        if (minutes > 0f)
        {
            float budget = V2Show.Budget(minutes);
            // partner dances for a little under half the time, the campfire for the rest
            while ((phrases + 1) * PhraseSecs < 0.45f * budget && Duration(phrases + 1, WideSecs) <= budget) { phrases++; }
            leftover = budget - Duration(phrases, WideSecs);
        }
        float bestSecs = 0f, bestDir = 1f, bestScore = float.MaxValue;
        foreach (float dir in new[] { 1f, -1f })
        {
            float secs = new Plan(start, phrases, WideSecs, dir).NeedTurn / (2f * Mathf.PI) * TurnSecs;
            if (leftover >= 0f) { secs += TurnSecs * Mathf.Max(0f, Mathf.Round((leftover - secs) / TurnSecs)); }
            float score = leftover >= 0f ? Mathf.Abs(leftover - secs) : secs;
            if (score < bestScore) { bestScore = score; bestSecs = secs; bestDir = dir; }
        }
        return new Plan(start, phrases, WideSecs + bestSecs, bestDir);
    }

    // The partner dance's clock: still for the first part of the walk in, then easing up to full
    // pace a little after arriving, so the walk flows into the first sweep
    const float RampFrom = ArriveSecs * 0.6f, RampTo = ArriveSecs + 1.5f;
    static float Clock(float t) { return (RampTo - RampFrom) * Shapes.EaseIntegral((t - RampFrom) / (RampTo - RampFrom)); }

    // When pair p starts curving into the ring: on its last inward sweep
    static float JoinFrom(int phrases, int p)
    {
        return (phrases - JoinEarly - StartOpen - p * OutOfStep) * PhraseSecs + (RampFrom + RampTo) / 2f;
    }

    public class Plan : V2Show.IPlan
    {
        readonly int n, phrases, sense;     // sense: which way round the ring the places are numbered
        readonly float dir;                 // which way the ring turns (+1 / -1)
        public readonly float NeedTurn;     // how much further the ring must turn to open onto the robots' own slots
        readonly Vector2[] start, spot;
        readonly int[] pair, side;
        readonly Vector2[] corner;          // each pair's corner
        readonly float[] place;             // place round the ring, before any turning
        readonly float allJoined, exitFrom;
        readonly Show.Eased radius;
        readonly Show.Integrated turned;
        public float Total { get; private set; }

        public Plan(Vector2[] start, int phrases, float wideSecs, float dir)
        {
            this.start = start; this.phrases = phrases; this.dir = dir;
            n = start.Length;
            int pairs = (n + 1) / 2;

            allJoined = JoinFrom(phrases, 0) + JoinSecs;
            float openFrom = allJoined + CompactSecs;
            exitFrom = openFrom + OpenSecs + wideSecs;
            Total = exitFrom + Show.ReturnSecs;
            radius = new Show.Eased(0f, JoinRadius, openFrom, JoinRadius, openFrom + OpenSecs, FinalRadius);
            float firstJoin = JoinFrom(phrases, pairs - 1);
            turned = new Show.Integrated(s => Shapes.Ease((s - firstJoin) / (allJoined - firstJoin)) * RingSpeed / radius.At(s)
                                             * (1f - Shapes.Ease((s - exitFrom) / Show.ReturnSecs)), Total + 1f);

            // Pairs: the robots next to each other round the ring (two by two). The pairs take
            // the corners in ring order; which corner comes first, and which way round, is the
            // choice with the shortest walk in.
            var order = V2Show.RingOrder(n);
            var round = V2Show.RoundTheRing(n);
            pair = new int[n]; side = new int[n];
            for (int i = 0; i < n; i++) { pair[i] = order[i] / 2; }
            corner = new Vector2[pairs];
            // (the choice that keeps the robots furthest apart on the way in and through the first
            // sweeps; among equally clear ones, the shortest walk)
            float best = float.MaxValue, bestRoom = -1f; int bestFirst = 0, bestSense = 1;
            foreach (int s in new[] { 1, -1 })
            {
                for (int f = 0; f < 4; f++)
                {
                    Arrange(f, s, round);
                    float cost = 0f, room = RoomEnough;
                    for (int i = 0; i < n; i++) { cost += (start[i] - Wing(pair[i], side[i], ArriveSecs)).sqrMagnitude; }
                    for (float t = 0f; t < ArriveSecs + 2f * PhraseSecs; t += 0.1f)
                    {
                        for (int i = 0; i < n; i++)
                        {
                            for (int j = i + 1; j < n; j++) { room = Mathf.Min(room, (WalkIn(i, t) - WalkIn(j, t)).magnitude); }
                        }
                    }
                    if (room > bestRoom + 0.5f || (room > bestRoom - 0.5f && cost < best - 0.01f)) { best = cost; bestRoom = room; bestFirst = f; bestSense = s; }
                }
            }
            sense = bestSense;
            Arrange(bestFirst, sense, round);

            // Ring places: evenly spaced in ring order, turned to where the pairs come in
            float sx = 0f, sy = 0f;
            float gapAngle = Mathf.Asin(PassGap / 2f / JoinRadius);
            for (int i = 0; i < n; i++)
            {
                Vector2 c = corner[pair[i]];
                float comesIn = Mathf.Atan2(c.y, c.x) - side[i] * gapAngle - dir * turned.At(JoinFrom(phrases, pair[i]) + 0.6f * JoinSecs);
                float a = comesIn - sense * 2f * Mathf.PI * order[i] / n;
                sx += Mathf.Cos(a); sy += Mathf.Sin(a);
            }
            float first = Mathf.Atan2(sy, sx);
            place = new float[n];
            for (int i = 0; i < n; i++) { place[i] = first + sense * 2f * Mathf.PI * order[i] / n; }

            // Every robot ends on its own slot: the ring has to be at this angle half way out
            spot = V2Show.OwnSpots(n);
            float unused; float opensAt = V2Show.BestAngle(order, sense, FinalRadius, spot, out unused);
            NeedTurn = Mathf.Repeat(dir * (opensAt - (first + dir * turned.At(exitFrom + Show.ReturnSecs / 2f))), 2f * Mathf.PI);
            if (NeedTurn > 2f * Mathf.PI - 0.02f) { NeedTurn = 0f; }
        }

        // Pair p takes corner (firstCorner + s * p); its robot that comes first round the ring
        // takes the side of the pair that is first round the ring
        void Arrange(int firstCorner, int s, int[] round)
        {
            for (int p = 0; p < corner.Length; p++) { corner[p] = Corners[((firstCorner + s * p) % 4 + 4) % 4]; }
            for (int k = 0; k < n; k++) { side[round[k]] = k % 2 == 0 ? s : -s; }
        }

        // A partner on its wing. In the pair's own frame one axis points at the mat centre.
        // Partner s (+1 / -1) is always exactly opposite the other through the pair's centre,
        // so the two move in opposite directions: one out to one side and forward, the other
        // out to the other side and back. One sweep: from beside each other (PassGap apart)
        // they pass, open away, go round the far end of the wing and come back in to pass again.
        Vector2 Wing(int p, int s, float t)
        {
            Vector2 axis = -corner[p].normalized;               // toward the mat centre
            Vector2 across = new Vector2(-axis.y, axis.x);
            Vector2 centre = Shapes.Centre + corner[p] * CornerOffset;
            float sweeps = Clock(t) / PhraseSecs + StartOpen + p * OutOfStep;
            float open = Mathf.Sin(Mathf.PI * sweeps);
            float u = PassGap / 2f + WingWidth * open * open;
            float v = WingLength * Mathf.Sin(2f * Mathf.PI * sweeps);
            return centre + s * (u * across + v * axis);
        }

        const float RoomEnough = 46f;

        // Walk in from the line, the dance already starting on the way
        // (toward or away from the participant first, sideways a little later, so neighbours in
        // the line that head for different corners part before they travel)
        Vector2 WalkIn(int i, float t)
        {
            Vector2 wing = Wing(pair[i], side[i], t);
            return new Vector2(Mathf.Lerp(start[i].x, wing.x, Shapes.Ease(t / ArriveSecs)),
                               Mathf.Lerp(start[i].y, wing.y, Shapes.Ease(t / (ArriveSecs * 0.75f))));
        }

        Vector2 Ring(int i, float t) { return Show.OnRing(place[i] + dir * turned.At(t), radius.At(t)); }

        public float JoinAt(int p) { return JoinFrom(phrases, p); }
        public float AllJoined { get { return allJoined; } }
        public float ExitFrom { get { return exitFrom; } }
        public int PairOf(int i) { return pair[i]; }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            int p = pair[i];
            Vector2 q = WalkIn(i, t);
            // The last inward sweep curves on into a place in the turning ring
            q = Vector2.Lerp(q, Ring(i, t), Shapes.Ease((t - JoinFrom(phrases, p)) / JoinSecs));
            return Show.Return(q, spot[i], t, exitFrom);
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
        public bool InLane(int i, float t) { return false; }
    }
}
