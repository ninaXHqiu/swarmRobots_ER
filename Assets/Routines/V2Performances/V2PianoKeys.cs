using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// v2 Piano Keys (Test). Eight robots are eight piano keys playing "Twinkle, Twinkle, Little Star".
// They stand in the line, left to right C D E F G A B high-C, all facing the participant, and
// keep those places. A key only ever moves along its own lane, straight toward the participant
// and back, with its heading held on the lane (V2Show's lane steering), so it never swivels.
// One keystroke: a quick stroke forward, easing at the front, then a lighter return to its place.
// A held note stays forward longer. Two notes in a row on one key are two strokes with the key
// back in its place between them. The next key may start while the last one is still returning.
// Everything else stands still. The two keys the melody never uses have a small part: B moves
// once, less far, with the held G that ends the first phrase, and high-C plays the last note
// together with low C; those two release together and all eight are back in the line.
// It runs on its own: no Messenger introduction.
public class V2PianoKeys : SwarmRoutine
{
    public override string Name { get { return "v2 Piano Keys (Test)"; } }
    public override int MinRobots { get { return 8; } }

    const float BeatsPerMinute = 96f;
    const float LeadInSecs = 1f;           // stillness in the line before the first note
    const float AfterSecs = 0.6f;

    // ---- One keystroke ----
    const float Depth = 34f;               // mat units toward the participant (about 4.6 cm, 1.4 robot lengths)
    const float HarmonyDepth = 20f;        // B's quieter accent
    const float StrokeSecs = 0.25f;        // forward: crisp
    const float FrontSecs = 0.05f;         // at the front before a one-beat note returns
    const float HeldFrontBeats = 0.8f;     // a held (two-beat) note stays forward this many beats
    const float QuickReturnSecs = 0.27f;   // back in place in time for the same key's next note
    const float LightReturnSecs = 0.45f;   // otherwise: an unhurried, light return
    const float Crisp = 1.3f;              // > 1 puts the stroke's speed early, so it eases into the front

    // C D E F G A B c = keys 0..7, left to right
    const int C = 0, D = 1, E = 2, F = 3, G = 4, A = 5, B = 6, HighC = 7;
    static readonly int[][] Melody =
    {
        new[] { C, C, G, G, A, A, G },
        new[] { F, F, E, E, D, D, C },
        new[] { G, G, F, F, E, E, D },
        new[] { G, G, F, F, E, E, D },
        new[] { C, C, G, G, A, A, G },
        new[] { F, F, E, E, D, D, C },
    };

    struct Stroke { public int key; public float at, depth, strokeSecs, frontSecs, returnSecs; }

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return V2Show.ToLine(ctx);
        Show.StartedAt = Time.time;   // read by the simulator recorder
        var plan = new Plan(ctx.Count);
        yield return V2Show.Flow(ctx, plan, plan.Total, 110);
    }

    public class Plan : V2Show.IPlan
    {
        readonly Vector2[] spot;
        readonly List<Stroke>[] strokes;
        public float Total { get; private set; }

        public Plan(int n)
        {
            spot = V2Show.OwnSpots(n);
            strokes = new List<Stroke>[n];
            for (int i = 0; i < n; i++) { strokes[i] = new List<Stroke>(); }
            float beat = 60f / BeatsPerMinute;
            float t = LeadInSecs, lastEnd = t;
            for (int line = 0; line < Melody.Length; line++)
            {
                for (int k = 0; k < Melody[line].Length; k++)
                {
                    int key = Melody[line][k];
                    bool held = k == Melody[line].Length - 1;   // the last note of each line takes two beats
                    bool sameKeyNext = !held && Melody[line][k + 1] == key;
                    var s = new Stroke { key = key, at = t, depth = Depth, strokeSecs = StrokeSecs };
                    s.frontSecs = held ? HeldFrontBeats * beat : FrontSecs;
                    s.returnSecs = sameKeyNext ? QuickReturnSecs : LightReturnSecs;
                    Add(s);
                    // B with the held G that ends the first phrase: once, softer and less far
                    if (line == 0 && held) { s.key = B; s.depth = HarmonyDepth; s.strokeSecs = StrokeSecs * 1.4f; Add(s); }
                    // high-C with the last note: the closing octave, released together
                    if (line == Melody.Length - 1 && held) { s.key = HighC; Add(s); }
                    lastEnd = t + s.strokeSecs + s.frontSecs + s.returnSecs;
                    t += (held ? 2f : 1f) * beat;
                }
            }
            Total = lastEnd + AfterSecs;
        }

        void Add(Stroke s) { if (s.key < strokes.Length) { strokes[s.key].Add(s); } }

        // How far key i is pressed (toward the participant) at t
        public float Pressed(int i, float t)
        {
            foreach (var s in strokes[i])
            {
                float u = t - s.at;
                if (u <= 0f || u >= s.strokeSecs + s.frontSecs + s.returnSecs) { continue; }
                if (u < s.strokeSecs)
                {
                    // crisp: most of the speed early, easing into the front
                    float x = 1f - Mathf.Pow(1f - u / s.strokeSecs, Crisp);
                    return s.depth * (0.5f - 0.5f * Mathf.Cos(Mathf.PI * x));
                }
                if (u < s.strokeSecs + s.frontSecs) { return s.depth; }
                float back = (u - s.strokeSecs - s.frontSecs) / s.returnSecs;
                return s.depth * (0.5f + 0.5f * Mathf.Cos(Mathf.PI * back));   // light: an even ease back
            }
            return 0f;
        }

        public Vector2 Position(int i, float t)
        {
            return spot[i] - Swarm.Inward * Pressed(i, Mathf.Clamp(t, 0f, Total));
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
        public bool InLane(int i, float t) { return true; }
    }
}
