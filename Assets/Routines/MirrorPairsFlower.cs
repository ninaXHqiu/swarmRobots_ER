using System.Collections;
using System.Linq;
using UnityEngine;

// Mirroring: unorganized pairs in the middle of the mat - partners push apart and back like
// mirror images while each pair drifts and turns at its own pace (the "panic"). Then the robots
// ease gradually into a circle, the circle turns, and it opens into a flower that turns as one
// smooth shape, the robots flowing along its outline. Ends with the Closing.
public class MirrorPairsFlower : SwarmRoutine
{
    public override string Name { get { return "Mirror pairs -> flower"; } }
    public override int MinRobots { get { return 2; } }

    // Timing (seconds)
    const float ChaosSecs = 12f;    // unorganized pairs
    const float GatherSecs = 10f;   // gradual move into the circle
    const float TurnSecs = 8f;      // circle turning
    const float BloomSecs = 6f;     // circle opening into the flower
    const float FlowerSecs = 18f;   // flower turning (includes the bloom)

    // Pairs, kept in the middle of the mat
    const float BaseGap = 60f;      // distance between partners
    const float PushGap = 35f;      // extra distance at the strongest push
    const float Wander = 12f;       // how far a pair drifts around its spot
    const float AreaMin = 120f, AreaMax = 380f;

    // Circle and flower
    const float Radius = 90f;
    const float DegPerSec = 18f;    // turning speed of circle and flower

    public override IEnumerator Run(RoutineContext ctx)
    {
        int n = ctx.Count;

        // Unorganized pairs
        var start = new Vector2Int[n];
        for (int i = 0; i < n; i++) { start[i] = Vector2Int.RoundToInt(Chaos(i, n, 0)); }
        Swarm.AssignNearest(ctx, start);
        yield return Swarm.GlideTo(ctx, start, maxSpeed: 90);
        yield return Swarm.Follow(ctx, (i, t) => Chaos(i, n, t), (int)(ChaosSecs * 1000), maxSpeed: 110);

        // Gradual move into a slowly turning circle: each robot spirals to the slot in its own
        // direction from the centre, so nobody crosses
        var fromR = new float[n]; var fromA = new float[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 p = Chaos(i, n, ChaosSecs) - Shapes.Centre;
            fromR[i] = p.magnitude;
            fromA[i] = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
        }
        var order = Enumerable.Range(0, n).OrderBy(i => fromA[i]).ToArray();
        var slot = new float[n];
        for (int rank = 0; rank < n; rank++) { slot[order[rank]] = fromA[order[0]] + 360f * rank / n; }
        yield return Swarm.Follow(ctx, (i, t) =>
        {
            float e = Mathf.SmoothStep(0, 1, t / GatherSecs);
            float a = fromA[i] + Mathf.DeltaAngle(fromA[i], slot[i] + DegPerSec * t) * e;
            return Shapes.OnCircle(a, Mathf.Lerp(fromR[i], Radius, e));
        }, (int)(GatherSecs * 1000), maxSpeed: 90);

        // Circle turns
        float angle = DegPerSec * GatherSecs;
        yield return Swarm.Follow(ctx, (i, t) => Shapes.OnCircle(slot[i] + angle + DegPerSec * t, Radius), (int)(TurnSecs * 1000), maxSpeed: 90);

        // Flower: one petal per two robots, so neighbours sit in and out alternately and the
        // whole shape turns smoothly while each robot flows along the outline
        angle += DegPerSec * TurnSecs;
        int petals = Mathf.Max(2, n / 2);
        yield return Swarm.Follow(ctx, (i, t) => Shapes.OnFlower(slot[i] + angle + DegPerSec * t, Radius, Mathf.SmoothStep(0, 1, t / BloomSecs), petals), (int)(FlowerSecs * 1000), maxSpeed: 100);

        yield return Closing.Play(ctx);
    }

    // Unorganized pairs: partners (0+1, 2+3, ...) mirror each other across their pair's centre
    // while each pair drifts and turns at its own pace; an odd robot out wanders alone.
    static Vector2 Chaos(int i, int n, float t)
    {
        int pair = i / 2;
        float scale;
        var centres = PairCentres((n + 1) / 2, out scale);

        float pace = 0.4f + 0.6f * Shapes.Hash(pair, 3);
        float phase = 6.28f * Shapes.Hash(pair, 4);
        Vector2 drift = Wander * scale * new Vector2(Mathf.Sin(pace * t + phase), Mathf.Cos(pace * 1.3f * t + phase * 0.7f));
        Vector2 centre = centres[pair] + drift;
        if (pair * 2 + 1 >= n) { return centre; }

        float startAngle = 180f * Shapes.Hash(pair, 5);
        float spin = (Shapes.Hash(pair, 6) - 0.5f) * 30f; // deg per second, either direction
        float angle = (startAngle + spin * t) * Mathf.Deg2Rad;

        float period = 1.4f + 1.4f * Shapes.Hash(pair, 7);
        float push = Mathf.Max(0f, Mathf.Sin(2 * Mathf.PI * t / period + 6.28f * Shapes.Hash(pair, 8)));
        float gap = (BaseGap + PushGap * push) * scale;

        Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float side = i % 2 == 0 ? 1f : -1f;
        return centre + side * axis * gap / 2f;
    }

    // Scattered spots for the pairs within the middle area: one per grid cell, cells in
    // shuffled order, jittered; `scale` shrinks pairs a little when cells are small
    static Vector2[] PairCentres(int count, out float scale)
    {
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt(count / (float)cols);
        float cellW = (AreaMax - AreaMin) / cols, cellH = (AreaMax - AreaMin) / rows;
        scale = Mathf.Min(1f, Mathf.Min(cellW, cellH) / 150f);
        var cells = Enumerable.Range(0, cols * rows).OrderBy(k => Shapes.Hash(k, 90)).ToArray();
        var centres = new Vector2[count];
        for (int p = 0; p < count; p++)
        {
            int c = cells[p] % cols, r = cells[p] / cols;
            float x = AreaMin + cellW * (c + 0.5f) + (Shapes.Hash(p, 20) - 0.5f) * 0.3f * cellW;
            float y = AreaMin + cellH * (r + 0.5f) + (Shapes.Hash(p, 21) - 0.5f) * 0.3f * cellH;
            centres[p] = new Vector2(x, y);
        }
        return centres;
    }
}
