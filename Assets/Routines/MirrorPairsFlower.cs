using System.Collections;
using System.Linq;
using UnityEngine;

// Mirroring: robots start scattered over the mat in pairs. Partners push apart and back like
// mirror images, while each pair drifts and turns in its own direction at its own pace. Over
// time the pushing and drifting calm down and every pair lines up the same way. Then the robots
// drift gradually into a turning ring, and the ring blooms into a flower whose outline the
// robots travel along.
public class MirrorPairsFlower : SwarmRoutine
{
    public override string Name { get { return "Mirror pairs -> flower"; } }
    public override int MinRobots { get { return 2; } }

    // Timing (seconds)
    const float SettleSecs = 16f;   // pairs calm down and align
    const float BlendSecs = 8f;     // gradual drift from the pairs into the ring
    const float TurnSecs = 8f;      // ring turning
    const float BloomSecs = 6f;     // ring opening into the flower
    const float FlowerSecs = 18f;   // flower turning (includes the bloom)

    // Pairs (sizes for up to 8 robots; shrunk automatically for more so pairs don't touch)
    const float BaseGap = 55f;      // distance between partners once calm
    const float PushGap = 55f;      // extra distance at the strongest push
    const float Wander = 25f;       // how far a pair drifts around its spot before calming
    const float AreaMin = 90f, AreaMax = 410f; // part of the mat the pairs spread over

    // Ring and flower
    const float Radius = 100f;
    const float DegPerSec = 18f;    // turning speed of ring and flower

    public override IEnumerator Run(RoutineContext ctx)
    {
        int n = ctx.Count;

        // Scattered start
        var start = new Vector2Int[n];
        for (int i = 0; i < n; i++) { start[i] = Vector2Int.RoundToInt(Position(i, n, 0)); }
        Swarm.AssignNearest(ctx, start);
        yield return Swarm.MoveTo(ctx, start, maxSpeed: 90, timeoutMs: 10000);

        // Pairs push, drift and turn at their own pace, calming and aligning over SettleSecs
        yield return Swarm.Follow(ctx, (i, t) => Position(i, n, t), (int)(SettleSecs * 1000), maxSpeed: 100);

        // Gradual drift into a ring that is already slowly turning. Each robot takes the ring
        // slot matching its direction from the centre, so paths don't cross.
        var from = new Vector2[n];
        for (int i = 0; i < n; i++) { from[i] = Position(i, n, SettleSecs); }
        var slot = RingSlots(from);
        yield return Swarm.Follow(ctx, (i, t) => Vector2.Lerp(from[i], Shapes.OnCircle(slot[i] + DegPerSec * t, Radius), Mathf.SmoothStep(0, 1, t / BlendSecs)), (int)(BlendSecs * 1000), maxSpeed: 80);

        // Ring turns
        float angle = DegPerSec * BlendSecs;
        yield return Swarm.Follow(ctx, (i, t) => Shapes.OnCircle(slot[i] + angle + DegPerSec * t, Radius), (int)(TurnSecs * 1000), maxSpeed: 80);

        // Ring blooms into a flower; robots keep travelling around, along the petal outline
        angle += DegPerSec * TurnSecs;
        yield return Swarm.Follow(ctx, (i, t) => Shapes.OnFlower(slot[i] + angle + DegPerSec * t, Radius, Mathf.SmoothStep(0, 1, t / BloomSecs)), (int)(FlowerSecs * 1000), maxSpeed: 90);
    }

    // Robot i at time t. Partners (robots 0+1, 2+3, ...) mirror each other across their pair's
    // centre; a leftover odd robot wanders alone.
    static Vector2 Position(int i, int n, float t)
    {
        int pair = i / 2;
        float calm = Mathf.SmoothStep(0, 1, t / SettleSecs);
        float scale;
        var centres = PairCentres((n + 1) / 2, out scale);

        // Each pair drifts around its spot at its own pace and direction
        float pace = 0.25f + 0.5f * Shapes.Hash(pair, 3);
        float phase = 6.28f * Shapes.Hash(pair, 4);
        Vector2 drift = Wander * scale * (1f - calm) * new Vector2(Mathf.Sin(pace * t + phase), Mathf.Cos(pace * 1.3f * t + phase * 0.7f));
        Vector2 centre = centres[pair] + drift;
        if (pair * 2 + 1 >= n) { return centre; } // odd robot out

        // Pair axis: own starting angle, turning at its own speed, ending aligned (0 deg)
        float startAngle = 180f * Shapes.Hash(pair, 5);
        float spin = (Shapes.Hash(pair, 6) - 0.5f) * 50f; // deg per second, either direction
        float angle = (startAngle + spin * t) * (1f - calm) * Mathf.Deg2Rad;

        // Repel: partners push apart and come back, each pair with its own rhythm
        float period = 1.8f + 1.7f * Shapes.Hash(pair, 7);
        float push = Mathf.Max(0f, Mathf.Sin(2 * Mathf.PI * t / period + 6.28f * Shapes.Hash(pair, 8)));
        float gap = (BaseGap + PushGap * push * (1f - calm)) * scale;

        Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float side = i % 2 == 0 ? 1f : -1f;
        return centre + side * axis * gap / 2f;
    }

    // Scattered spots for the pairs: one per grid cell, cells in shuffled order, each spot
    // jittered inside its cell. `scale` shrinks the pairs when cells get small (many robots).
    static Vector2[] PairCentres(int count, out float scale)
    {
        int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt(count / (float)cols);
        float cellW = (AreaMax - AreaMin) / cols, cellH = (AreaMax - AreaMin) / rows;
        scale = Mathf.Min(1f, Mathf.Min(cellW, cellH) / 160f);
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

    // Ring angle (deg) per robot: robots keep their order around the centre, evenly spaced
    static float[] RingSlots(Vector2[] positions)
    {
        int n = positions.Length;
        var order = Enumerable.Range(0, n)
            .OrderBy(i => Mathf.Atan2(positions[i].y - Swarm.Centre, positions[i].x - Swarm.Centre))
            .ToArray();
        float first = Mathf.Atan2(positions[order[0]].y - Swarm.Centre, positions[order[0]].x - Swarm.Centre) * Mathf.Rad2Deg;
        var slot = new float[n];
        for (int rank = 0; rank < n; rank++) { slot[order[rank]] = first + 360f * rank / n; }
        return slot;
    }
}
