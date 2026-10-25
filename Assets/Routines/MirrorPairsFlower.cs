using System.Collections;
using System.Linq;
using UnityEngine;

// Mirroring: unorganized at first - robots scattered in pairs, partners pushing apart and back
// like mirror images while each pair drifts and turns at its own pace. Then the robots ease
// into a row moving in a small wave, and the wave grows into big waves.
public class MirrorPairsFlower : SwarmRoutine
{
    public override string Name { get { return "Mirror pairs -> waves"; } }
    public override int MinRobots { get { return 2; } }

    // Timing (seconds)
    const float ChaosSecs = 12f;    // unorganized pairs
    const float BlendSecs = 10f;    // pairs ease into a row moving in a small wave
    const float GrowSecs = 8f;      // small wave grows into big waves
    const float HoldSecs = 18f;     // big waves keep going

    // Pairs (sizes for up to 8 robots; shrunk automatically for more)
    const float BaseGap = 75f;      // distance between partners (wide enough that they never catch each other)
    const float PushGap = 45f;      // extra distance at the strongest push
    const float Wander = 20f;       // how far a pair drifts around its spot
    const float AreaMin = 90f, AreaMax = 410f;

    // Waves (same rhythm as the Wave routine)
    const float WaveHz = 0.25f;     // wave speed, steady throughout
    const float SmallHeight = 35f, BigHeight = 110f;
    const float SmallStep = 0.9f, BigStep = 0.45f; // phase step between neighbours

    public override IEnumerator Run(RoutineContext ctx)
    {
        int n = ctx.Count;

        // Scattered pairs
        var start = new Vector2Int[n];
        for (int i = 0; i < n; i++) { start[i] = Vector2Int.RoundToInt(Chaos(i, n, 0)); }
        Swarm.AssignNearest(ctx, start);
        yield return Swarm.MoveTo(ctx, start, maxSpeed: 90, timeoutMs: 10000);

        // Unorganized: pairs repel, drift and turn
        yield return Swarm.Follow(ctx, (i, t) => Chaos(i, n, t), (int)(ChaosSecs * 1000), maxSpeed: 110);

        // Row slots in left-to-right order of where robots are, so nobody crosses the mat
        var order = Enumerable.Range(0, n).OrderBy(i => Chaos(i, n, ChaosSecs).x).ToArray();
        var slot = new int[n];
        for (int rank = 0; rank < n; rank++) { slot[order[rank]] = rank; }

        // Ease from the pairs into a row moving in a small wave, then grow into big waves.
        // Robots spread sideways into their row places first and settle up/down later, so
        // partners standing one above the other never meet.
        var from = new Vector2[n];
        for (int i = 0; i < n; i++) { from[i] = Chaos(i, n, ChaosSecs); }
        float total = BlendSecs + GrowSecs + HoldSecs;
        yield return Swarm.Follow(ctx, (i, t) =>
        {
            Vector2 wave = Wave(slot[i], n, t);
            float e = Mathf.Clamp01(t / BlendSecs);
            if (e >= 1f) { return wave; }
            float sideways = 1f - Mathf.Pow(1f - e, 3f);
            float upDown = Mathf.Pow(e, 3f);
            return new Vector2(Mathf.Lerp(from[i].x, wave.x, sideways), Mathf.Lerp(from[i].y, wave.y, upDown));
        }, (int)(total * 1000), maxSpeed: 110);

        yield return Closing.Play(ctx);
    }

    // Robot in row slot s at time t (t from the start of the easing)
    static Vector2 Wave(int s, int n, float t)
    {
        float grow = Mathf.SmoothStep(0, 1, (t - BlendSecs) / GrowSecs);
        float height = Mathf.Lerp(SmallHeight, BigHeight, grow);
        float step = Mathf.Lerp(SmallStep, BigStep, grow);
        float x = n == 1 ? Swarm.Centre : Mathf.Lerp(100, 400, s / (float)(n - 1));
        float y = Swarm.Centre + height * Mathf.Sin(2 * Mathf.PI * WaveHz * t - step * s);
        return new Vector2(x, y);
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
        float spin = (Shapes.Hash(pair, 6) - 0.5f) * 30f; // deg per second, either direction (slow enough to follow)
        float angle = (startAngle + spin * t) * Mathf.Deg2Rad;

        float period = 1.4f + 1.4f * Shapes.Hash(pair, 7);
        float push = Mathf.Max(0f, Mathf.Sin(2 * Mathf.PI * t / period + 6.28f * Shapes.Hash(pair, 8)));
        float gap = (BaseGap + PushGap * push) * scale;

        Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float side = i % 2 == 0 ? 1f : -1f;
        return centre + side * axis * gap / 2f;
    }

    // Scattered spots for the pairs: one per grid cell, cells in shuffled order, jittered
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
}
