using System.Collections;
using UnityEngine;

// Shapes and endings shared by more than one routine
public static class Shapes
{
    static readonly Vector2 Centre = new Vector2(Swarm.Centre, Swarm.Centre);

    // Robot i of n on a ring, turned by turnDeg
    public static Vector2 Circle(int i, int n, float radius, float turnDeg)
    {
        float a = (360f * i / n + turnDeg) * Mathf.Deg2Rad;
        return Centre + radius * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    // Ring that opens into a flower: every other robot moves out (petal tips) and the rest
    // move in. bloom 0 = plain circle, 1 = full flower.
    public static Vector2 Flower(int i, int n, float radius, float bloom, float turnDeg)
    {
        float a = (360f * i / n + turnDeg) * Mathf.Deg2Rad;
        float r = radius * (1f + bloom * 0.45f * (i % 2 == 0 ? 1f : -1f));
        return Centre + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    // Shared ending: gather into a ring, turn it, then bloom into a turning flower
    public static IEnumerator CircleToFlower(RoutineContext ctx, float radius = 100, float degPerSec = 20)
    {
        int n = ctx.Count;
        var ring = new Vector2Int[n];
        for (int i = 0; i < n; i++) { ring[i] = Vector2Int.RoundToInt(Circle(i, n, radius, 0)); }
        Swarm.AssignNearest(ctx, ring);
        yield return Swarm.MoveTo(ctx, ring, maxSpeed: 90, timeoutMs: 10000);

        float turnSecs = 8f;
        yield return Swarm.Follow(ctx, (i, t) => Circle(i, n, radius, t * degPerSec), (int)(turnSecs * 1000), maxSpeed: 60);

        float startAngle = turnSecs * degPerSec;
        float bloomSecs = 5f;
        yield return Swarm.Follow(ctx, (i, t) => Flower(i, n, radius, Mathf.SmoothStep(0, 1, t / bloomSecs), startAngle + t * degPerSec), 15000, maxSpeed: 60);
    }
}
