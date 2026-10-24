using System.Collections;
using UnityEngine;

// Mirroring: robots pair up and push each other away and back like mirror images. The pushing
// fades while every pair slowly turns to the same direction. Then they gather into a ring,
// the ring turns, and it opens into a turning flower.
public class MirrorPairsFlower : SwarmRoutine
{
    public override string Name { get { return "Mirror pairs -> flower"; } }
    public override int MinRobots { get { return 2; } }

    const float SettleSecs = 14f;   // how long the pairs take to calm down and align
    const float BaseGap = 60f;      // distance between partners once calm
    const float PushGap = 80f;      // extra distance at the start of the pushing
    const float PushPeriod = 2.5f;  // seconds per push away and back

    public override IEnumerator Run(RoutineContext ctx)
    {
        int n = ctx.Count;
        int pairs = n / 2;
        var centres = PairCentres(pairs);

        // Start: partners side by side, each pair at its own random-looking angle
        var start = new Vector2Int[n];
        for (int i = 0; i < n; i++) { start[i] = Vector2Int.RoundToInt(Position(i, n, centres, 0)); }
        Swarm.AssignNearest(ctx, start);
        yield return Swarm.MoveTo(ctx, start, maxSpeed: 90, timeoutMs: 10000);

        // Pairs push apart and back, calming down and aligning over SettleSecs
        yield return Swarm.Follow(ctx, (i, t) => Position(i, n, centres, t), (int)(SettleSecs * 1000), maxSpeed: 100);

        yield return Shapes.CircleToFlower(ctx);
    }

    // Robot i at time t: partners mirror each other across their pair centre
    static Vector2 Position(int i, int n, Vector2[] centres, float t)
    {
        int pair = i / 2;
        if (pair >= centres.Length) { return new Vector2(Swarm.Centre, Swarm.Centre); } // odd robot waits in the middle
        float calm = Mathf.SmoothStep(0, 1, t / SettleSecs);
        float startAngle = (pair * 137.5f) % 180f;
        float angle = Mathf.Lerp(startAngle, 0f, calm) * Mathf.Deg2Rad;
        float push = Mathf.Max(0f, Mathf.Sin(2 * Mathf.PI * t / PushPeriod + pair));
        float gap = BaseGap + PushGap * push * (1f - calm);
        Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        float side = i % 2 == 0 ? 1f : -1f;
        return centres[pair] + side * axis * gap / 2f;
    }

    // Pair centres spread over the mat in a grid
    static Vector2[] PairCentres(int pairs)
    {
        var centres = new Vector2[pairs];
        int cols = Mathf.CeilToInt(Mathf.Sqrt(pairs));
        int rows = Mathf.CeilToInt(pairs / (float)cols);
        for (int p = 0; p < pairs; p++)
        {
            int c = p % cols; int r = p / cols;
            float x = Mathf.Lerp(130, 370, cols == 1 ? 0.5f : c / (float)(cols - 1));
            float y = Mathf.Lerp(150, 350, rows == 1 ? 0.5f : r / (float)(rows - 1));
            centres[p] = new Vector2(x, y);
        }
        return centres;
    }
}
