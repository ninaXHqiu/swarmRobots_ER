using System.Collections;
using System.Linq;
using UnityEngine;

// v2 motion. Mirroring: irregularly scattered pairs move apart and together as mirror images,
// drift into a loose group in the middle, a compact rotating ring opens out of the group, and
// the ring widens while it turns, settling as a still, medium-sized circle.
// Starts from the shared line. One continuous movement: every phase fades in while the previous
// one fades out, and nothing stops until the circle comes to rest.
public class MirrorPairsV2 : SwarmRoutine
{
    public override string Name { get { return "v2 motion: Mirror pairs -> circle"; } }
    public override int MinRobots { get { return 2; } }

    // ---- Timing (seconds), in order ----
    const float EnterSecs = 5f;        // robots leave the line for their pair spots, already moving
    const float PairsSecs = 9f;        // A: scattered mirror pairs
    const float GatherSecs = 7f;       // B: pairs drift to a loose group in the middle, pair motion fades
    const float RingOverlapSecs = 3f;  //    C starts this long before B ends
    const float RingSecs = 6f;         // C: compact ring opens out of the group, rotation starts
    const float CompactSecs = 2.5f;    // compact ring readable (still turning) before it widens
    const float ExpandSecs = 11f;      // D: ring widens while turning
    const float SettleSecs = 5f;       // E: last part of D, widening and turning ease to a stop together

    // ---- Scattered pairs ----
    // Pair centres as offsets from the mat centre, deliberately uneven (one near the middle, one
    // lower right, one lower left, one upper left, all at different distances). 2 robots use the
    // first pair, 8 robots all four; with an odd number the last robot moves alone.
    static readonly Vector2[] PairCentre = { new Vector2(15, -10), new Vector2(110, 95), new Vector2(-110, 60), new Vector2(-60, -115) };
    static readonly float[] PairAxis = { 15, 140, 50, 160 };     // direction partners move along (deg)
    static readonly float[] PairReach = { 18, 14, 21, 16 };      // how far each partner moves in and out
    static readonly float[] PairPeriod = { 2.4f, 3.1f, 2.7f, 3.4f }; // seconds per breath
    static readonly float[] PairPhase = { 0f, 0.55f, 0.3f, 0.8f };   // where in the breath each pair starts (0-1)
    const float HalfGap = 40f;         // partner distance from the pair centre at rest (inner gap >= 38)

    // ---- Gathering and circle ----
    const float GatherScale = 0.55f;          // loose group = pair layout shrunk to 55% around the centre
    const float CompactSpacing = 40f;         // neighbour distance in the compact ring
    const float CompactMinRadius = 30f;
    const float FinalDiameterShare = 0.63f;   // final circle diameter as a share of the usable mat
    const float RingSpeed = 20f;              // mat units per second along the ring: calm, and the same
                                              // whatever the radius (so widening doesn't speed it up)

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return Swarm.StartInLine(ctx);
        var plan = new Plan(ctx);
        yield return Swarm.Flow(ctx, plan.Position, plan.Total, maxSpeed: 100);
    }

    class Plan
    {
        readonly int n;
        readonly float tA, tB, tC, tD, tS;
        public readonly float Total;
        readonly Vector2[] start;
        readonly int[] group, side;      // side +1/-1 for partners, 0 for a robot on its own
        readonly float[] ringAngle;      // radians, fixed per robot
        readonly float compactRadius, finalRadius;
        const float Step = 0.02f;        // rotation table resolution (seconds)
        readonly float[] turned, turnRate;

        public Plan(RoutineContext ctx)
        {
            n = ctx.Count;
            tA = EnterSecs;
            tB = tA + PairsSecs;
            tC = tB + GatherSecs - RingOverlapSecs;
            tD = tC + RingSecs + CompactSecs;
            Total = tD + ExpandSecs;
            tS = Total - SettleSecs;
            compactRadius = Mathf.Max(CompactMinRadius, CompactSpacing / (2f * Mathf.Sin(Mathf.PI / n)));
            finalRadius = FinalDiameterShare * (Swarm.MatMax - Swarm.MatMin) / 2f;

            // Pair spots, and which robot takes which: least total travel from the line
            var slotGroup = new int[n]; var slotSide = new int[n];
            for (int s = 0; s < n; s++)
            {
                slotGroup[s] = s / 2;
                slotSide[s] = s % 2 == 0 ? 1 : -1;
                if (s == n - 1 && n % 2 == 1) { slotSide[s] = 0; }
            }
            start = new Vector2[n];
            for (int i = 0; i < n; i++) { start[i] = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y); }
            var spots = Enumerable.Range(0, n).Select(s => Spot(slotGroup[s], slotSide[s], tA, 1f, 1f)).ToArray();
            var slot = Swarm.LeastTravel(start, spots);
            group = new int[n]; side = new int[n];
            for (int i = 0; i < n; i++) { group[i] = slotGroup[slot[i]]; side[i] = slotSide[slot[i]]; }

            // Ring places: the rotation and assignment with the least total travel from the loose
            // group, so robots open outward along short, non-crossing paths. Fixed from here on.
            var gathered = Enumerable.Range(0, n).Select(i => PairPosition(i, tB + GatherSecs)).ToArray();
            ringAngle = new float[n];
            float bestCost = float.MaxValue;
            for (int k = 0; k < 36; k++)
            {
                float a0 = 2f * Mathf.PI / n * k / 36f;
                var places = Enumerable.Range(0, n).Select(p => RingPoint(a0 + 2f * Mathf.PI * p / n, compactRadius)).ToArray();
                var place = Swarm.LeastTravel(gathered, places);
                float cost = Enumerable.Range(0, n).Sum(i => (gathered[i] - places[place[i]]).sqrMagnitude);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    for (int i = 0; i < n; i++) { ringAngle[i] = a0 + 2f * Mathf.PI * place[i] / n; }
                }
            }

            // Shared rotation, worked out once: how far the ring has turned at each moment
            int steps = Mathf.CeilToInt(Total / Step) + 1;
            turned = new float[steps + 1]; turnRate = new float[steps + 1];
            for (int s = 0; s <= steps; s++) { turnRate[s] = TurnRate(s * Step); }
            for (int s = 1; s <= steps; s++) { turned[s] = turned[s - 1] + (turnRate[s - 1] + turnRate[s]) / 2f * Step; }
        }

        static Vector2 RingPoint(float angle, float radius)
        {
            return Shapes.Centre + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        // A pair member's spot at time t. scale shrinks the layout toward the centre, breath
        // scales the in-and-out movement. Partners are always mirror images around their centre.
        static Vector2 Spot(int g, int s, float t, float scale, float breath)
        {
            float a = PairAxis[g] * Mathf.Deg2Rad;
            Vector2 axis = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            float move = breath * PairReach[g] * Mathf.Sin(2f * Mathf.PI * (t / PairPeriod[g] + PairPhase[g]));
            Vector2 offset = s == 0 ? move * axis : s * (HalfGap + move) * axis;
            return Shapes.Centre + scale * (PairCentre[g] + offset);
        }

        // Pairs, gathering into a loose group in the middle during B
        Vector2 PairPosition(int i, float t)
        {
            float g = Shapes.Ease((t - tB) / GatherSecs);
            return Spot(group[i], side[i], t, 1f - (1f - GatherScale) * g, 1f - g);
        }

        float Radius(float t)
        {
            return Mathf.Lerp(compactRadius, finalRadius, Shapes.Ease((t - tD) / ExpandSecs));
        }

        // Rotation speed (radians per second): emerges while the ring forms, keeps the same speed
        // along the ring as it widens, and eases to zero together with the widening
        float TurnRate(float t)
        {
            return Shapes.Ease((t - tC) / RingSecs) * RingSpeed / Radius(t) * (1f - Shapes.Ease((t - tS) / SettleSecs));
        }

        float Turned(float t)
        {
            float x = Mathf.Clamp(t / Step, 0f, turned.Length - 1.001f);
            int s = (int)x; float u = x - s;
            // Hermite between table steps, so rotation speed stays continuous
            float u2 = u * u, u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * turned[s] + (u3 - 2f * u2 + u) * turnRate[s] * Step
                 + (-2f * u3 + 3f * u2) * turned[s + 1] + (u3 - u2) * turnRate[s + 1] * Step;
        }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            Vector2 p = Vector2.Lerp(start[i], PairPosition(i, t), Shapes.Ease(t / EnterSecs));
            Vector2 ring = RingPoint(ringAngle[i] + Turned(t), Radius(t));
            return Vector2.Lerp(p, ring, Shapes.Ease((t - tC) / RingSecs));
        }
    }
}
