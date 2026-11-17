using System.Collections;
using System.Linq;
using UnityEngine;

// v2 Cheerleading (C22): the ring turns slowly while the robots go in and out in two groups (four in, four out, then swap), then the
// ending of the C13 breathing circle without the breaths. See V2CheerPlan.
public class V2Cheerleading : SwarmRoutine
{
    public override string Name { get { return "v2 Cheerleading"; } }
    public override string Code { get { return "C22"; } }
    public override string Note { get { return "Ring turns, four in four out, then closes"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        bool native = ctx.Cubes.All(c => V2StressRestless.SupportsTargetMove(c));
        Plan plan = null;
        yield return V2Show.Run(ctx, start => { plan = new Plan(start); plan.Native = native; return plan; }, 100,
            skip: (i, t) => plan != null && plan.Driving(t),
            extra: t => { if (plan != null) { plan.Drive(ctx, t); } });
    }

    public class Plan : V2CheerPlan { public Plan(Vector2[] start) : base(start, false) { } }
}
