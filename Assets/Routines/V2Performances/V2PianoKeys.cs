using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// v2 Piano Keys. Eight robots are eight piano keys playing "Twinkle, Twinkle, Little Star".
// They stand in the line, left to right C D E F G A B high-C, all facing the participant, and
// keep those places. A key only ever moves along its own lane, straight out of the line away
// from the participant and back, with its heading held on the lane (V2Show's lane steering),
// so it never swivels: it stays facing the participant and backs out.
// One keystroke: the key jumps out of the line, eases at the top, then returns lightly to its place.
// A held note stays forward longer. Two notes in a row on one key are two strokes with the key
// back in its place between them. The next key may start while the last one is still returning.
// Everything else stands still. The two keys the melody never uses have a small part: B moves
// once, less far, with the held G that ends the first phrase, and high-C plays the last note
// together with low C. Closing wave: those two release together, and their release is the
// start of a smooth ripple that runs in from both ends to the middle and back out again, lower
// each time, until all eight are back in the line. There is no pause before it: each key rises
// as its neighbour falls.
// It runs on its own: no Messenger introduction.
public class V2PianoKeys : SwarmRoutine
{
    public override string Name { get { return "v2 Piano Keys"; } }
    public override string Code { get { return "C10"; } }
    public override string Note { get { return "Piano keys play melody"; } }
    public override int MinRobots { get { return 8; } }

    const float BeatsPerMinute = 96f;
    const float LeadInSecs = 1f;           // stillness in the line before the first note
    const float AfterSecs = 0.6f;

    // ---- One keystroke ----
    const float Depth = 34f;               // mat units out of the line, away from the participant (about 4.6 cm, 1.4 robot lengths)
    const float HarmonyDepth = 20f;        // B's quieter accent
    const float StrokeSecs = 0.22f;        // out: a jump
    const float FrontSecs = 0.05f;         // at the front before a one-beat note returns
    const float HeldFrontBeats = 0.8f;     // a held (two-beat) note stays forward this many beats
    const float QuickReturnSecs = 0.27f;   // back in place in time for the same key's next note
    const float LightReturnSecs = 0.45f;   // otherwise: an unhurried, light return
    const float Crisp = 1.5f;              // > 1 puts the stroke's speed early, so it eases into the front

    // ---- Closing wave ----
    const float WaveSecs = 1f;             // one key's rise and fall
    const float WaveLagSecs = 0.24f;       // each key this long after its neighbour
    static readonly float[] WaveIn = { 0f, 28f, 25f, 23f };    // height going in, by distance from the nearest end (the ends are already out)
    static readonly float[] WaveOut = { 10f, 14f, 18f, 0f };   // height coming back out (the middle turns the wave round in one move)

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
    struct Swell { public float peakAt, height; }

    public override IEnumerator Run(RoutineContext ctx)
    {
        // the fixed opening (the Messenger's introduction) and ending (all in the line, facing the participant)
        yield return V2Show.Run(ctx, start => new Plan(start), 110);
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading
    {
        const float EnterSecs = 2.5f;     // the messenger back into its place in the line
        readonly Vector2[] start, spot;
        readonly List<Stroke>[] strokes;
        readonly List<Swell>[] swells;
        public float Total { get; private set; }

        public Plan(Vector2[] start)
        {
            int n = start.Length;
            this.start = start;
            spot = V2Show.OwnSpots(n);
            strokes = new List<Stroke>[n];
            swells = new List<Swell>[n];
            for (int i = 0; i < n; i++) { strokes[i] = new List<Stroke>(); swells[i] = new List<Swell>(); }
            float beat = 60f / BeatsPerMinute;
            float t = EnterSecs + LeadInSecs, lastEnd = t;
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
                    bool last = line == Melody.Length - 1 && held;
                    if (last)
                    {
                        // high-C with the last note: the closing octave, released together. Their
                        // release is the falling half of the wave's first swell.
                        strokes[key].RemoveAt(strokes[key].Count - 1);
                        s.returnSecs = WaveSecs / 2f;
                        Add(s); s.key = HighC; Add(s);
                        float release = t + s.strokeSecs + s.frontSecs;
                        int middle = (n - 1) / 2;   // distance of the middle keys from the nearest end
                        for (int i = 0; i < n; i++)
                        {
                            int fromEnd = Mathf.Min(Mathf.Min(i, n - 1 - i), WaveIn.Length - 1);
                            if (WaveIn[fromEnd] > 0f) { swells[i].Add(new Swell { peakAt = release + fromEnd * WaveLagSecs, height = WaveIn[fromEnd] }); }
                            if (WaveOut[fromEnd] > 0f) { swells[i].Add(new Swell { peakAt = release + (2 * middle - fromEnd + 1) * WaveLagSecs + WaveSecs / 2f, height = WaveOut[fromEnd] }); }
                        }
                        lastEnd = release + (2 * middle + 1) * WaveLagSecs + WaveSecs;
                        t += 2f * beat;
                        continue;
                    }
                    lastEnd = t + s.strokeSecs + s.frontSecs + s.returnSecs;
                    t += (held ? 2f : 1f) * beat;
                }
            }
            Total = lastEnd + AfterSecs;
        }

        void Add(Stroke s) { if (s.key < strokes.Length) { strokes[s.key].Add(s); } }

        // How far key i is out of the line (away from the participant) at t
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
            // the closing wave: smooth swells, one after another along the row
            float wave = 0f;
            foreach (var w in swells[i]) { wave += w.height * Show.Bump((t - w.peakAt) / WaveSecs + 0.5f); }
            return wave;
        }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            Vector2 inLine = spot[i] + Swarm.Inward * Pressed(i, t);
            return t < EnterSecs ? Vector2.Lerp(start[i], inLine, Shapes.Ease(t / EnterSecs)) : inLine;
        }

        // every key faces the participant, whichever way it is driving
        public float Heading(int i, float t) { return (float)Swarm.FacingParticipant; }

        public Vector2 LineSpot(int i) { return spot[i]; }
        // the messenger comes back to its place steering freely, and only then keeps to its lane
        public bool InLane(int i, float t) { return t >= EnterSecs || Mathf.Abs(start[i].x - spot[i].x) < 3f; }
    }
}
