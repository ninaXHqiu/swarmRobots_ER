using System.Collections;

// The study's final routines (v5, 2:20 each, 8 robots), designed in the swarm simulator ("rutinas
// finales v5"). One button per routine; Resonance 1 and 2 have one button per starting intensity,
// the level 1, 2 or 3 that the experiment page gives from the skin-conductance rise (1 = low,
// 2 = medium, 3 = high). Only their chaos changes with it.
//
//   E01-E03  Resonance 1: wide chaos in the middle of the mat, a racing heart that slows, three breaths, calm heart
//   E04-E06  Resonance 2: chaos, a flower, round the petals, a heart that beats and turns
//   E07      Resonance 3: line to the middle, "here we are", pairs, turning circle, spinning, a heart that beats
//   E08      Cheerleading: line to the middle, the book, two fireworks of four, the finale
//
// Every routine: the messenger (robot 1) walks to the participant looking at them, looks at their
// hand, goes back and talks to the team; the middle; everyone turns slowly to the participant and the
// messenger asks "better?"; slowly back into the line, everyone looking at the participant.
public abstract class FinalRoutine : SwarmRoutine
{
    protected abstract string Data { get; }
    public override int MinRobots { get { return 8; } }   // planned and checked for 8
    public override IEnumerator Run(RoutineContext ctx) { return FinalShow.Run(ctx, FinalPlan.Load(Data), 100); }
}

public class Final_Resonance1_Level1 : FinalRoutine
{
    public override string Name { get { return "Final Resonance 1, level 1"; } }
    public override string Code { get { return "E01"; } }
    public override string Note { get { return "Racing heart, breathe (low)"; } }
    protected override string Data { get { return "res_heart_level1"; } }
}

public class Final_Resonance1_Level2 : FinalRoutine
{
    public override string Name { get { return "Final Resonance 1, level 2"; } }
    public override string Code { get { return "E02"; } }
    public override string Note { get { return "Racing heart, breathe (medium)"; } }
    protected override string Data { get { return "res_heart_level2"; } }
}

public class Final_Resonance1_Level3 : FinalRoutine
{
    public override string Name { get { return "Final Resonance 1, level 3"; } }
    public override string Code { get { return "E03"; } }
    public override string Note { get { return "Racing heart, breathe (high)"; } }
    protected override string Data { get { return "res_heart_level3"; } }
}

public class Final_Resonance2_Level1 : FinalRoutine
{
    public override string Name { get { return "Final Resonance 2, level 1"; } }
    public override string Code { get { return "E04"; } }
    public override string Note { get { return "Flower, turning heart (low)"; } }
    protected override string Data { get { return "res_flower_level1"; } }
}

public class Final_Resonance2_Level2 : FinalRoutine
{
    public override string Name { get { return "Final Resonance 2, level 2"; } }
    public override string Code { get { return "E05"; } }
    public override string Note { get { return "Flower, turning heart (medium)"; } }
    protected override string Data { get { return "res_flower_level2"; } }
}

public class Final_Resonance2_Level3 : FinalRoutine
{
    public override string Name { get { return "Final Resonance 2, level 3"; } }
    public override string Code { get { return "E06"; } }
    public override string Note { get { return "Flower, turning heart (high)"; } }
    protected override string Data { get { return "res_flower_level3"; } }
}

public class Final_Resonance3 : FinalRoutine
{
    public override string Name { get { return "Final Resonance 3"; } }
    public override string Code { get { return "E07"; } }
    public override string Note { get { return "Circle, spinning, beating heart"; } }
    protected override string Data { get { return "res_circle"; } }
}

public class Final_Cheerleading : FinalRoutine
{
    public override string Name { get { return "Final Cheerleading"; } }
    public override string Code { get { return "E08"; } }
    public override string Note { get { return "Book, fireworks, finale"; } }
    protected override string Data { get { return "cl_fireworks"; } }
}
