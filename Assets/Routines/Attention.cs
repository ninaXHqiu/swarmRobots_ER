using System.Collections;

// Attention / acknowledgement: every robot turns, where it stands, to face the participant
// (who sits at the bottom-centre edge of the mat, see Swarm.Participant).
public class Attention : SwarmRoutine
{
    public override string Name { get { return "Attention: face participant"; } }
    public override int MinRobots { get { return 2; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return Swarm.FacePoint(ctx, Swarm.Participant, timeoutMs: 4000);
    }
}
