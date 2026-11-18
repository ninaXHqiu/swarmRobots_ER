using System.Collections;

// The study's final routines (1:55 each, 8 robots), designed in the simulator in tools/final-sim
// (open tools/final-sim/preview/index.html). One button per routine; the Resonance routines have
// one button per starting intensity, the level 1, 2 or 3 that the experiment page gives from the
// skin-conductance rise (1 = low, 2 = medium, 3 = high). Only their short chaos changes with it.
//
//   E01-E03  Resonance 1: brief chaos, a racing heart that slows, three breaths
//   E04-E06  Resonance 2: brief chaos, a flower, round the petals, a heart that beats and turns
//   E07      Cheerleading 1: line to the middle, "here we are", pairs, turning heart, circle, spinning
//   E08      Cheerleading 2: line to the middle, the book, two fireworks, the finale
//
// Every routine: the messenger (robot 1) walks to the participant looking at them, looks at their
// hand, goes back and talks to the team; the middle; everyone looks at the participant and the
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

public class Final_Cheerleading1 : FinalRoutine
{
    public override string Name { get { return "Final Cheerleading 1"; } }
    public override string Code { get { return "E07"; } }
    public override string Note { get { return "Turning heart, circle, spinning"; } }
    protected override string Data { get { return "cl_heart"; } }
}

public class Final_Cheerleading2 : FinalRoutine
{
    public override string Name { get { return "Final Cheerleading 2"; } }
    public override string Code { get { return "E08"; } }
    public override string Note { get { return "Book, fireworks, finale"; } }
    protected override string Data { get { return "cl_fireworks"; } }
}
