using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// v2 Social Dance. Four partner dances that become one dance round the campfire.
//   Pairs:    neighbours in the line pair up; each pair takes one corner of the central dance
//             area, with its own centre and its own mirror axis (pointing at the mat centre).
//             The partners come in from opposite sides and trace mirrored butterfly wings: one
//             the left wing, the other the right. From the pair's centre they open away from
//             each other, curve round and back in, and come together side by side with
//             clearance before the next sweep. Each pair dances a little out of step with the
//             others, so the four pairs read as four dances.
//   Gather:   the last inward sweep carries on past the pair's centre straight toward the mat
//             centre, into the nearest two places of a compact ring. The ring starts its slow
//             turn as the first pair arrives; the order around the ring is the order the pairs
//             come in.
//   Campfire: one turning ring, evenly spaced. It opens out gradually to its final size, keeps
//             turning at that size, and peels into the line.
// Recovered: pairing neighbours and choosing corners by least travel, the walk-in blended with
// the dance, and the return to the line come from CheeringSocialDance. New: the mirrored wing
// loops and the straight carry-on into the ring.
public abstract class V2SocialDanceBase : SwarmRoutine
{
    protected abstract float Minutes { get; }
    public override string Name { get { return "v2 Social Dance (" + V2Show.Label(Minutes) + ")"; } }
    public override int MinRobots { get { return 2; } }
    public override IEnumerator Run(RoutineContext ctx) { return V2Show.Run(ctx, start => V2SocialDance.For(start, Minutes), maxSpeed: 70); }
}
public class V2SocialDanceNatural : V2SocialDanceBase { protected override float Minutes { get { return 0f; } } }
public class V2SocialDance3Min : V2SocialDanceBase { protected override float Minutes { get { return 3f; } } }
public class V2SocialDance5Min : V2SocialDanceBase { protected override float Minutes { get { return 5f; } } }

public static class V2SocialDance
{
    // ---- Partner dance ----
    const float ArriveSecs = 4.5f;         // from the line to the pair's corner, the dance starting on the way
    const float PhraseSecs = 3.6f;         // one butterfly sweep: out, round and back together
    const float OutOfStep = 0.1f;         // each pair this share of a sweep ahead of the one before
    const float StartOpen = 0.5f;          // the walk in ends on the open wing, this far into a sweep, not where the partners meet
    static readonly Vector2[] AllCorners = { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };
    const float CornerOffset = 85f;        // pair centre from the mat centre, along x and along y
    const float PassGap = 54f;             // partners' distance when they come together
    const float WingWidth = 50f;           // how far a wing opens away from the mirror axis
    const float WingLength = 28f;          // how far a wing reaches along the axis, each way

    // ---- Ring ----
    const float JoinSecs = 3f;             // the carried-on sweep -> a place in the turning ring
    const float JoinRadius = 70f;          // compact ring
    const float FinalRadius = 128f;        // diameter 256 = 62% of the mat's usable 410
    const float CompactSecs = 3f;          // the compact ring turns before it opens
    const float OpenSecs = 10f;            // opening out and evening the spacing
    const float WideSecs = 8f;             // dancing on at the final size (natural length)
    const float RingSpeed = 28f;           // mat units per second along the ring, whatever its size
    const float QuarterTurnSecs = Mathf.PI / 2f * FinalRadius / RingSpeed;

    static float Duration(int phrases, float wideSecs, int pairs)
    {
        return NodeTime(phrases, 0) + JoinSecs + CompactSecs + OpenSecs + wideSecs + Show.ReturnSecs;
    }

    public static Plan For(Vector2[] start, float minutes)
    {
        int phrases = 4; float wide = WideSecs;
        if (minutes <= 0f) { return new Plan(start, phrases, wide); }
        int pairs = (start.Length + 1) / 2;
        float budget = V2Show.Budget(minutes);
        bool added = true;
        while (added)
        {
            added = false;
            // partner dances for a little under half the time, the campfire for the rest
            if (phrases * PhraseSecs < 0.45f * budget && Duration(phrases + 1, wide, pairs) <= budget) { phrases++; added = true; }
            else if (Duration(phrases, wide + QuarterTurnSecs, pairs) <= budget) { wide += QuarterTurnSecs; added = true; }
        }
        return new Plan(start, phrases, wide + Mathf.Max(0f, budget - Duration(phrases, wide, pairs)));
    }

    // The partner dance's clock: still for the first part of the walk in, then easing up to full
    // pace a little after arriving, so the walk flows into the first sweep
    const float RampFrom = ArriveSecs * 0.6f, RampTo = ArriveSecs + 1.5f;
    static float Clock(float t) { return (RampTo - RampFrom) * Shapes.EaseIntegral((t - RampFrom) / (RampTo - RampFrom)); }

    // When pair p comes together for the last time (the end of its last sweep)
    static float NodeTime(int phrases, int p) { return (phrases - StartOpen - p * OutOfStep) * PhraseSecs + (RampFrom + RampTo) / 2f; }

    public class Plan : V2Show.IPlan
    {
        readonly int n, phrases;
        readonly Vector2[] start, spot;
        readonly int[] pair, side;
        Vector2[] corner;                   // each pair's corner
        readonly float[] arrive, even;      // place round the ring on arriving, and evenly spaced
        readonly float allJoined, exitFrom;
        readonly Show.Eased radius;
        readonly Show.Integrated turned;
        public float Total { get; private set; }

        public Plan(Vector2[] start, int phrases, float wideSecs)
        {
            this.start = start; this.phrases = phrases;
            n = start.Length;
            int pairs = (n + 1) / 2;

            allJoined = NodeTime(phrases, 0) + JoinSecs;
            float openFrom = allJoined + CompactSecs;
            exitFrom = openFrom + OpenSecs + wideSecs;
            Total = exitFrom + Show.ReturnSecs;
            radius = new Show.Eased(0f, JoinRadius, openFrom, JoinRadius, openFrom + OpenSecs, FinalRadius);
            float firstNode = NodeTime(phrases, pairs - 1);
            turned = new Show.Integrated(s => Shapes.Ease((s - firstNode) / (allJoined - firstNode)) * RingSpeed / radius.At(s)
                                             * (1f - Shapes.Ease((s - exitFrom) / Show.ReturnSecs)), Total + 1f);

            // Partners: neighbours in the line, left to right (the messenger counts at its own line
            // spot); with an odd number the last robot dances alone. Each pair takes the corner
            // that, with the others, makes the shortest walk in.
            var inLine = Show.KeepLineSpots(start);
            var byX = Enumerable.Range(0, n).OrderBy(i => inLine[i].x).ToArray();
            pair = new int[n]; side = new int[n];
            float best = float.MaxValue; Vector2[] bestCorners = null;
            foreach (var choice in Permutations(new[] { 0, 1, 2, 3 }))
            {
                corner = choice.Take(pairs).Select(c => AllCorners[c]).ToArray();
                float cost = 0f;
                for (int k = 0; k < pairs; k++) { cost += PlacePair(k, byX, false); }
                if (cost < best - 0.01f) { best = cost; bestCorners = corner; }
            }
            corner = bestCorners;
            for (int k = 0; k < pairs; k++) { PlacePair(k, byX, true); }

            // Ring places. Arriving: where the carried-on sweep brings each robot, seen in the
            // turning ring. Even: the same order round the ring, evenly spaced, as near as possible.
            arrive = new float[n]; even = new float[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 d = Wing(pair[i], side[i], Total) - Shapes.Centre;
                arrive[i] = Mathf.Atan2(d.y, d.x) - turned.At(NodeTime(phrases, pair[i]) + 0.6f * JoinSecs);
            }
            var order = Enumerable.Range(0, n).OrderBy(i => Mathf.Repeat(arrive[i], 2f * Mathf.PI)).ToArray();
            float sx = 0f, sy = 0f;
            for (int k = 0; k < n; k++) { float a = arrive[order[k]] - 2f * Mathf.PI * k / n; sx += Mathf.Cos(a); sy += Mathf.Sin(a); }
            float a0 = Mathf.Atan2(sy, sx);
            for (int k = 0; k < n; k++)
            {
                int i = order[k];
                even[i] = arrive[i] + Mathf.DeltaAngle(arrive[i] * Mathf.Rad2Deg, (a0 + 2f * Mathf.PI * k / n) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            }

            var leaving = Enumerable.Range(0, n).Select(i => Ring(i, exitFrom + Show.ReturnSecs / 2f)).ToArray();
            spot = Show.LineSpots(leaving);
        }

        static IEnumerable<int[]> Permutations(int[] items)
        {
            if (items.Length <= 1) { yield return items; yield break; }
            for (int i = 0; i < items.Length; i++)
            {
                var rest = items.Where((_, j) => j != i).ToArray();
                foreach (var tail in Permutations(rest)) { yield return new[] { items[i] }.Concat(tail).ToArray(); }
            }
        }

        // Pair k (robots byX[2k], byX[2k+1]) at its corner, whichever way round is the shorter
        // walk; returns that walk (squared)
        float PlacePair(int k, int[] byX, bool keep)
        {
            int a = byX[2 * k];
            Vector2 plus = Wing(k, 1, ArriveSecs), minus = Wing(k, -1, ArriveSecs);
            if (2 * k + 1 >= n)
            {
                if (keep) { pair[a] = k; side[a] = 1; }
                return (start[a] - plus).sqrMagnitude;
            }
            int b = byX[2 * k + 1];
            float straight = (start[a] - plus).sqrMagnitude + (start[b] - minus).sqrMagnitude;
            float swapped = (start[a] - minus).sqrMagnitude + (start[b] - plus).sqrMagnitude;
            if (keep) { pair[a] = pair[b] = k; side[a] = straight <= swapped ? 1 : -1; side[b] = -side[a]; }
            return Mathf.Min(straight, swapped);
        }

        // A partner on its butterfly wing. In the pair's own frame the mirror axis points at the
        // mat centre; partner s (+1 / -1) is the mirror image of the other across it. One sweep:
        // from beside the axis (together, PassGap apart) forward and out, round the far side of
        // the wing, and back in from behind to come together again.
        // After the last sweep the partners carry straight on along the axis, side by side,
        // slowing into their two places of the compact ring.
        Vector2 Wing(int p, int s, float t)
        {
            Vector2 axis = -corner[p].normalized;               // toward the mat centre
            Vector2 across = new Vector2(-axis.y, axis.x);
            Vector2 centre = Shapes.Centre + corner[p] * CornerOffset;
            float sweeps = Clock(t) / PhraseSecs + StartOpen + p * OutOfStep;
            float u, v;
            if (sweeps < phrases)
            {
                float open = Mathf.Sin(Mathf.PI * sweeps);
                u = PassGap / 2f + WingWidth * open * open;
                v = WingLength * Mathf.Sin(2f * Mathf.PI * sweeps);
            }
            else
            {
                // straight on toward the centre, at the sweep's speed at first, easing to a stop
                // on the ring
                float reach = CornerOffset * Mathf.Sqrt(2f) - Mathf.Sqrt(JoinRadius * JoinRadius - PassGap * PassGap / 4f);
                u = PassGap / 2f;
                v = reach * (1f - Mathf.Exp(-2f * Mathf.PI * WingLength * (sweeps - phrases) / reach));
            }
            return centre + s * u * across + v * axis;
        }

        float Place(int i, float t)
        {
            // Evenly spaced from the moment of joining: the ring is already turning while later
            // pairs come in, so places that followed each pair's own arrival would close the gaps
            // between pairs. With 8 robots the even spacing (47) is close to the partners' own gap.
            return even[i];
        }

        Vector2 Ring(int i, float t) { return Show.OnRing(Place(i, t) + turned.At(t), radius.At(t)); }

        public float NodeAt(int p) { return NodeTime(phrases, p); }
        public float AllJoined { get { return allJoined; } }
        public float ExitFrom { get { return exitFrom; } }
        public int PairOf(int i) { return pair[i]; }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            int p = pair[i];
            // Walk in from the line, the dance already starting on the way
            // (toward or away from the participant first, sideways a little later, so neighbours in
            // the line that head for different corners part before they travel)
            Vector2 wing = Wing(p, side[i], t);
            Vector2 q = new Vector2(Mathf.Lerp(start[i].x, wing.x, Shapes.Ease(t / ArriveSecs)),
                                    Mathf.Lerp(start[i].y, wing.y, Shapes.Ease(t / (ArriveSecs * 0.7f))));
            // The carried-on sweep becomes a place in the turning ring
            q = Vector2.Lerp(q, Ring(i, t), Shapes.Ease((t - NodeTime(phrases, p)) / JoinSecs));
            return Show.Return(q, spot[i], t, exitFrom);
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
        public bool InLane(int i, float t) { return false; }
    }
}
