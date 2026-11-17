using System.Collections;
using System.Linq;
using UnityEngine;

// v2 Cheerleading + Breathing (C24): C22, then the breathing circle of C13 (the ring widens and breathes four times, large and slow)
// before it opens into the line. See V2CheerPlan.
public class V2CheerBreathing : SwarmRoutine
{
    public override string Name { get { return "v2 Cheerleading + Breathing"; } }
    public override string Code { get { return "C24"; } }
    public override string Note { get { return "Four in four out, then breathing circle"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        bool native = ctx.Cubes.All(c => V2StressRestless.SupportsTargetMove(c));
        Plan plan = null;
        yield return V2Show.Run(ctx, start => { plan = new Plan(start); plan.Native = native; return plan; }, 100,
            skip: (i, t) => plan != null && plan.Driving(t),
            extra: t => { if (plan != null) { plan.Drive(ctx, t); } });
    }

    public class Plan : V2CheerPlan { public Plan(Vector2[] start) : base(start, true) { } }
}
