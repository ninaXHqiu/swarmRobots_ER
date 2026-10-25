using System.Collections;
using System.Linq;
using UnityEngine;

// Closing: regroup into a small circle, widen it into a bigger circle, settle into a line,
// then go back to where each robot stood before the routine started.
// Runs on its own, and at the end of the mirroring and wave routines.
public class Closing : SwarmRoutine
{
    public override string Name { get { return "Closing: regroup and return"; } }
    public override int MinRobots { get { return 2; } }

    const float BigScale = 1.5f;    // big circle = 1.5 x the small one (stays in the middle)
    const float LineSpacing = 40f;  // compact line, centred
    const float GatherSecs = 6f;    // spiral in to the small circle
    const float WidenSecs = 5f;     // small circle slowly widens
    const float LineSecs = 6f;      // circle settles into a line
    const float LineY = Swarm.Centre;

    public override IEnumerator Run(RoutineContext ctx) { return Play(ctx); }

    public static IEnumerator Play(RoutineContext ctx)
    {
        int n = ctx.Count;

        // 1. Small circle: robots keep their order around the centre, so nobody crosses.
        //    Radius just big enough that neighbours stay ~45 apart.
        float smallRadius = Mathf.Max(55f, 45f / (2f * Mathf.Sin(Mathf.PI / n)));
        float bigRadius = smallRadius * BigScale;
        var angle = RingAngles(ctx);
        // Spiral in: each robot closes in while turning to its slot, so the order around the
        // centre never changes and nobody crosses the middle.
        var startR = new float[n]; var startA = new float[n];
        for (int i = 0; i < n; i++)
        {
            float dx = ctx.Cubes[i].x - Swarm.Centre, dy = ctx.Cubes[i].y - Swarm.Centre;
            startR[i] = Mathf.Sqrt(dx * dx + dy * dy);
            startA[i] = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        }
        yield return Swarm.Follow(ctx, (i, t) =>
        {
            float e = Mathf.SmoothStep(0, 1, t / GatherSecs);
            float a = startA[i] + Mathf.DeltaAngle(startA[i], angle[i]) * e;
            return Shapes.OnCircle(a, Mathf.Lerp(startR[i], smallRadius, e));
        }, (int)(GatherSecs * 1000), maxSpeed: 80);

        // 2. Widen into a bigger circle
        yield return Swarm.Follow(ctx, (i, t) => Shapes.OnCircle(angle[i], Mathf.Lerp(smallRadius, bigRadius, Mathf.SmoothStep(0, 1, t / WidenSecs))), (int)(WidenSecs * 1000), maxSpeed: 80);

        // 3. Line across the middle: left-to-right in the order robots stand on the circle,
        //    spreading sideways first and settling up/down after, so nobody meets.
        var from = new Vector2[n];
        for (int i = 0; i < n; i++) { from[i] = Shapes.OnCircle(angle[i], bigRadius); }
        var order = Enumerable.Range(0, n).OrderBy(i => from[i].x).ToArray();
        var line = new Vector2[n];
        for (int rank = 0; rank < n; rank++)
        {
            float x = Swarm.Centre + (rank - (n - 1) / 2f) * LineSpacing;
            line[order[rank]] = new Vector2(x, LineY);
        }
        yield return Swarm.Follow(ctx, (i, t) =>
        {
            float e = Mathf.Clamp01(t / LineSecs);
            float sideways = 1f - Mathf.Pow(1f - e, 3f), upDown = Mathf.Pow(e, 3f);
            return new Vector2(Mathf.Lerp(from[i].x, line[i].x, sideways), Mathf.Lerp(from[i].y, line[i].y, upDown));
        }, (int)(LineSecs * 1000), maxSpeed: 80);

        // 4. Back to where each robot started (with collision avoidance, since start spots can
        //    be in any order)
        if (ctx.Cubes.All(c => ctx.Home.ContainsKey(c)))
        {
            var home = ctx.Cubes.Select(c => ctx.Home[c]).ToArray();
            yield return Swarm.MoveToAvoiding(ctx, home, maxSpeed: 60, timeoutMs: 15000);
        }
    }

    // Ring angle (deg) per robot: robots keep their order around the centre, evenly spaced
    static float[] RingAngles(RoutineContext ctx)
    {
        int n = ctx.Count;
        System.Func<int, float> polar = i => Mathf.Atan2(ctx.Cubes[i].y - Swarm.Centre, ctx.Cubes[i].x - Swarm.Centre) * Mathf.Rad2Deg;
        var order = Enumerable.Range(0, n).OrderBy(polar).ToArray();
        float first = polar(order[0]);
        var angle = new float[n];
        for (int rank = 0; rank < n; rank++) { angle[order[rank]] = first + 360f * rank / n; }
        return angle;
    }
}
