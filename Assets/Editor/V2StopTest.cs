using System.Collections;
using UnityEngine;

// Editor-only check (it is not in the built app): starts the Social Dance, stops it part way
// through, as the Stop button does, and starts Vibration. Shows that the robots go straight
// back to their own slots from wherever they were stopped. Record it with SimRecorder:
//   -routine "zz test: stop part way, start another"
public class V2StopTest : SwarmRoutine
{
    public override string Name { get { return "zz test: stop part way, start another"; } }
    public override int MinRobots { get { return 2; } }

    const float StopAfterSecs = 40f;   // the pairs have just gathered into the ring: every robot away from its slot

    public override IEnumerator Run(RoutineContext ctx)
    {
        var host = Object.FindObjectOfType<SimRoutinePlayer>();
        var first = host.StartCoroutine(new V2SocialDanceNatural().Run(ctx));
        yield return new WaitForSeconds(StopAfterSecs);
        host.StopCoroutine(first);
        Swarm.StopAll(ctx);
        Show.StartedAt = -1f;
        yield return new WaitForSeconds(1f);
        yield return new V2VibrationNatural().Run(ctx);
    }
}
