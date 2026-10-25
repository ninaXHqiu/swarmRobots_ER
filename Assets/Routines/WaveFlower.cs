using System.Collections;
using UnityEngine;

// Wave: robots stand in a row and move up and down, each at its own speed and distance.
// Gradually they fall into step as one travelling wave, then the wave grows into larger,
// longer waves that keep a steady speed.
public class WaveFlower : SwarmRoutine
{
    public override string Name { get { return "Wave -> large waves"; } }
    public override int MinRobots { get { return 2; } }

    // Timing (seconds)
    const float SettleSecs = 14f;   // everyone falls into one wave
    const float GrowSecs = 8f;      // the wave grows larger
    const float HoldSecs = 20f;     // large waves keep going

    // Wave shape
    const float WaveHz = 0.3f;      // wave speed (cycles per second), the same throughout
    const float SmallHeight = 50f;  // up/down distance of the first, small wave
    const float LargeHeight = 120f; // up/down distance of the large waves
    const float SmallStep = 0.8f;   // phase step between neighbours: small, tight waves
    const float LargeStep = 0.45f;  // phase step between neighbours: longer, larger waves

    public override IEnumerator Run(RoutineContext ctx)
    {
        int n = ctx.Count;

        // Start: a row across the middle of the mat
        var row = new Vector2Int[n];
        for (int i = 0; i < n; i++) { row[i] = new Vector2Int(Mathf.RoundToInt(RowX(i, n)), Swarm.Centre); }
        Swarm.AssignNearest(ctx, row);
        yield return Swarm.GlideTo(ctx, row, maxSpeed: 90);

        float total = SettleSecs + GrowSecs + HoldSecs;
        yield return Swarm.Follow(ctx, (i, t) => new Vector2(RowX(i, n), Swarm.Centre + Height(i, t)), (int)(total * 1000), maxSpeed: 110);

        yield return Closing.Play(ctx);
    }

    static float RowX(int i, int n)
    {
        return n == 1 ? Swarm.Centre : Mathf.Lerp(100, 400, i / (float)(n - 1));
    }

    // Up/down offset of robot i at time t
    static float Height(int i, float t)
    {
        // Each robot's own rhythm (fixed per robot, varied across robots)
        float ownHz = 0.15f + 0.3f * Shapes.Hash(i, 1);
        float ownHeight = 25f + 60f * Shapes.Hash(i, 2);
        float own = ownHeight * Mathf.Sin(2 * Mathf.PI * ownHz * t + 6.28f * Shapes.Hash(i, 3));

        // The shared wave: same speed throughout, growing larger and longer after settling
        float grow = Mathf.SmoothStep(0, 1, (t - SettleSecs) / GrowSecs);
        float height = Mathf.Lerp(SmallHeight, LargeHeight, grow);
        float step = Mathf.Lerp(SmallStep, LargeStep, grow);
        float wave = height * Mathf.Sin(2 * Mathf.PI * WaveHz * t - step * i);

        float settle = Mathf.SmoothStep(0, 1, t / SettleSecs);
        return Mathf.Lerp(own, wave, settle);
    }
}
