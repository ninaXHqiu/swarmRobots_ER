using System.Collections;
using UnityEngine;

// Wave: robots stand in a row and move up and down, each at its own speed and distance.
// Gradually they fall into step until the row moves as one travelling wave. Then they gather
// into a ring, the ring turns, and it opens into a turning flower.
public class WaveFlower : SwarmRoutine
{
    public override string Name { get { return "Wave -> flower"; } }
    public override int MinRobots { get { return 3; } }

    const float SettleSecs = 14f;   // how long until everyone moves as one wave
    const float HoldSecs = 6f;      // how long the finished wave keeps going
    const float WaveHeight = 60f;   // up/down distance of the finished wave
    const float WaveHz = 0.25f;     // finished wave speed (cycles per second)
    const float WaveStep = 0.7f;    // phase step between neighbours (how "wavy" the row is)

    public override IEnumerator Run(RoutineContext ctx)
    {
        int n = ctx.Count;

        // Start: a row across the middle of the mat
        var row = new Vector2Int[n];
        for (int i = 0; i < n; i++) { row[i] = new Vector2Int(Mathf.RoundToInt(RowX(i, n)), Swarm.Centre); }
        Swarm.AssignNearest(ctx, row);
        yield return Swarm.MoveTo(ctx, row, maxSpeed: 90, timeoutMs: 10000);

        // Everyone at their own speed and distance, settling into one wave
        float total = SettleSecs + HoldSecs;
        yield return Swarm.Follow(ctx, (i, t) => new Vector2(RowX(i, n), Swarm.Centre + Height(i, t)), (int)(total * 1000), maxSpeed: 80);

        yield return Shapes.CircleToFlower(ctx);
    }

    static float RowX(int i, int n)
    {
        return n == 1 ? Swarm.Centre : Mathf.Lerp(100, 400, i / (float)(n - 1));
    }

    // Up/down offset of robot i at time t: its own rhythm blending into the shared wave
    static float Height(int i, float t)
    {
        // Each robot's own speed and distance (fixed per robot, varied across robots)
        float ownHz = 0.15f + 0.3f * Frac(i * 0.618f);
        float ownHeight = 25f + 60f * Frac(i * 0.414f + 0.3f);
        float ownPhase = 6.28f * Frac(i * 0.271f);
        float own = ownHeight * Mathf.Sin(2 * Mathf.PI * ownHz * t + ownPhase);

        float wave = WaveHeight * Mathf.Sin(2 * Mathf.PI * WaveHz * t - WaveStep * i);
        float settle = Mathf.SmoothStep(0, 1, t / SettleSecs);
        return Mathf.Lerp(own, wave, settle);
    }

    static float Frac(float v) { return v - Mathf.Floor(v); }
}
