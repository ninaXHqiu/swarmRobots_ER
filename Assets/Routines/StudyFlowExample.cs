using System.Collections;
using UnityEngine;

// Example routine: the study flow
// Line in front of the participant -> half circle toward them -> head shake ->
// circle in the middle -> back to the line, slowly.
// Copy this file to make a new routine; change Name and the steps in Run().
public class StudyFlowExample : SwarmRoutine
{
    public override string Name { get { return "Study flow (example)"; } }
    public override int MinRobots { get { return 2; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        // 1. Idle: line up in front of the participant, facing them
        yield return Swarm.GlideTo(ctx, Line(ctx, 90), maxSpeed: 90);
        yield return Swarm.TurnTo(ctx, Swarm.FacingParticipant);
        yield return Swarm.Wait(5000);

        // 2. Approach: half circle opening toward the participant
        yield return Swarm.GlideTo(ctx, HalfCircle(ctx, 110), maxSpeed: 75);
        yield return Swarm.TurnTo(ctx, Swarm.FacingParticipant);

        // 3. Acknowledge: small head shake, 3 times
        for (int k = 0; k < 3; k++)
        {
            yield return Swarm.Wheels(ctx, 25, -25, 150);
            yield return Swarm.Wheels(ctx, -25, 25, 150);
        }

        // 4. Expressive: circle in the middle of the mat
        yield return Swarm.GlideTo(ctx, Circle(ctx, 80), maxSpeed: 90);
        yield return Swarm.Wait(10000);

        // 5. Exit: slowly back to the line
        yield return Swarm.GlideTo(ctx, Line(ctx, 90), unitsPerSec: 50f, maxSpeed: 60);
        yield return Swarm.TurnTo(ctx, Swarm.FacingParticipant);
    }

    // Robots side by side, `distance` mat units in from the participant's (bottom) edge
    Vector2Int[] Line(RoutineContext ctx, int distance)
    {
        int n = ctx.Count;
        float spacing = n > 1 ? Mathf.Min(45f, 300f / (n - 1)) : 0f;
        Vector2 centre = Swarm.InFrontOfParticipant(distance);
        Vector2 along = Swarm.Along;
        var points = new Vector2Int[n];
        for (int i = 0; i < n; i++)
        {
            points[i] = Vector2Int.RoundToInt(centre + along * (i - (n - 1) / 2f) * spacing);
        }
        return points;
    }

    // Arc bulging into the mat, open toward the participant
    Vector2Int[] HalfCircle(RoutineContext ctx, int radius)
    {
        int n = ctx.Count;
        Vector2 centre = Swarm.InFrontOfParticipant(40);
        Vector2 along = Swarm.Along;
        Vector2 inward = Swarm.Inward;
        var points = new Vector2Int[n];
        for (int i = 0; i < n; i++)
        {
            float t = n > 1 ? Mathf.PI * i / (n - 1) : Mathf.PI / 2f;
            points[i] = Vector2Int.RoundToInt(centre + radius * (Mathf.Cos(t) * along + Mathf.Sin(t) * inward));
        }
        return points;
    }

    // Ring around the middle of the mat
    Vector2Int[] Circle(RoutineContext ctx, int radius)
    {
        int n = ctx.Count;
        var points = new Vector2Int[n];
        for (int i = 0; i < n; i++)
        {
            float a = 2f * Mathf.PI * i / n;
            points[i] = new Vector2Int(Swarm.Centre + Mathf.RoundToInt(radius * Mathf.Cos(a)), Swarm.Centre + Mathf.RoundToInt(radius * Mathf.Sin(a)));
        }
        return points;
    }
}
