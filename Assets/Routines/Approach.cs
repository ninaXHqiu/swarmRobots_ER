using System.Collections;
using UnityEngine;

// Approach: the whole swarm slowly moves closer to the participant, keeping its shape
// (slightly tightened), then faces them.
public class Approach : SwarmRoutine
{
    public override string Name { get { return "Approach: move closer"; } }
    public override int MinRobots { get { return 2; } }

    const float StopDistance = 120f; // how far in from the participant's edge the swarm's centre stops
    const float Tighten = 0.8f;      // spacing between robots shrinks to this fraction
    const int Speed = 45;            // slow, calm approach

    public override IEnumerator Run(RoutineContext ctx)
    {
        int n = ctx.Count;
        Vector2 centre = Vector2.zero;
        for (int i = 0; i < n; i++) { centre += new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y); }
        centre /= n;

        Vector2 goal = Swarm.InFrontOfParticipant((int)StopDistance);
        var targets = new Vector2Int[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 offset = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y) - centre;
            targets[i] = Vector2Int.RoundToInt(goal + offset * Tighten);
        }
        yield return Swarm.MoveTo(ctx, targets, maxSpeed: Speed, timeoutMs: 20000);
        yield return Swarm.FacePoint(ctx, Swarm.Participant, timeoutMs: 4000);
    }
}
