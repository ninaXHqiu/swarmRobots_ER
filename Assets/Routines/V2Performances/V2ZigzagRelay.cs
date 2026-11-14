using System.Collections;
using System.Linq;
using UnityEngine;

// v2 Zigzag Relay (C18).
//   1. After the standard opening four robots go to the top (u1 u2 u3 u4) and four to the bottom (d1 d2 d3 d4), each four
//      evenly spaced, top and bottom staggered: a zigzag u1 d1 u2 d2 u3 d3 u4 d4 over the eight slots.
//   2. A relay runs along the zigzag. The first robot (the runner) goes u1 > d1, then d1 > u2, u2 > d2 ... u4 > d4: at every
//      step it and the robot standing at the point it is going to change places (passing on either side of each other), so
//      everyone it passes moves back one point. At d4 it turns round at once and goes back d4 > u4, u4 > d3 ... d2 > u1 > the
//      way it came, each robot it passes moves forward one point again and the zigzag is as it was. That is one trip there
//      and back; it is done three times, and while it goes on the distance between top and bottom slowly shrinks.
//   3. Top and bottom have come together in the middle; a wave starts to run along the row, several cycles, and then calms and
//      the row goes back to the line. No pauses between the parts.
public class V2ZigzagRelay : SwarmRoutine
{
    public override string Name { get { return "v2 Zigzag Relay"; } }
    public override string Code { get { return "C18"; } }
    public override string Note { get { return "Zigzag relay, then wave"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return V2Show.Run(ctx, start => new Plan(start), 100);
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading, V2Show.ISections
    {
        const float A0 = 70f, A1 = 12f;                          // half the distance between top and bottom: at the start and at the end of the relay
        const float FormSecs = 8f;
        const int Sweeps = 6;                                    // three trips there and back
        const float Speed = 32f, MinHopSecs = 2.6f, Bow = 28f;   // the runner's pace along the zigzag; how far the two robots that change places swing out
        const float WaveAmp = 38f, WavePeriod = 7f, WaveLength = 336f, WaveCycles = 5f, RampSecs = 6f, TaperSecs = 9f, ReturnSecs = 9f;
        const float RowY = 250f;

        struct Hop { public float t0, t1; public int from, to, runner, displaced; public float sweepPos; }

        readonly Vector2[] start, line;
        readonly System.Collections.Generic.List<Hop> hops = new System.Collections.Generic.List<Hop>();
        readonly System.Collections.Generic.List<Hop>[] moves = new System.Collections.Generic.List<Hop>[8];
        readonly float relayFrom, relayTo, waveFrom, waveTo, taperTo;
        V2Show.WayToLine way;
        public float Total { get; private set; }
        public float WaveAt { get { return waveFrom; } }
        public float ReturnAt { get { return taperTo; } }

        public System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<float, string>> Sections
        {
            get
            {
                yield return new System.Collections.Generic.KeyValuePair<float, string>(0f, "zigzag forms");
                yield return new System.Collections.Generic.KeyValuePair<float, string>(relayFrom, "relay along the zigzag");
                yield return new System.Collections.Generic.KeyValuePair<float, string>(waveFrom, "wave");
                yield return new System.Collections.Generic.KeyValuePair<float, string>(taperTo, "calm, back to the line");
            }
        }

        public Plan(Vector2[] start)
        {
            this.start = start;
            line = V2Show.OwnSpots(start.Length);
            for (int i = 0; i < 8; i++) { moves[i] = new System.Collections.Generic.List<Hop>(); }
            relayFrom = FormSecs;
            // the order of robots along the zigzag: order[k] = the robot standing at point k
            var order = Enumerable.Range(0, 8).ToArray();
            float t = relayFrom;
            for (int s = 0; s < Sweeps; s++)
            {
                bool forward = s % 2 == 0;
                int runner = order[forward ? 0 : 7];
                for (int h = 0; h < 7; h++)
                {
                    int a = forward ? h : 7 - h, b = forward ? h + 1 : 6 - h;
                    float sweepPos = s + h / 7f;
                    float amp = Mathf.Lerp(A0, A1, sweepPos / Sweeps);
                    float len = Mathf.Sqrt(48f * 48f + 4f * amp * amp);
                    float dur = Mathf.Max(MinHopSecs, len / Speed);
                    int displaced = order[b];
                    var hop = new Hop { t0 = t, t1 = t + dur, from = a, to = b, runner = runner, displaced = displaced, sweepPos = sweepPos };
                    hops.Add(hop);
                    moves[runner].Add(new Hop { t0 = hop.t0, t1 = hop.t1, from = a, to = b, runner = runner, displaced = displaced, sweepPos = sweepPos });
                    moves[displaced].Add(new Hop { t0 = hop.t0, t1 = hop.t1, from = b, to = a, runner = runner, displaced = displaced, sweepPos = sweepPos });
                    order[a] = displaced; order[b] = runner;
                    t += dur;
                }
            }
            relayTo = t;
            waveFrom = relayTo - 3f;
            waveTo = waveFrom + RampSecs + WaveCycles * WavePeriod;
            taperTo = waveTo + TaperSecs;
            Total = taperTo + ReturnSecs + 0.3f;
            way = V2Show.Approach(Enumerable.Range(0, 8).Select(i => Point(i, relayFrom)).ToArray(), relayFrom);
        }

        // half the distance between the rows at t: it shrinks with the sweeps, then to nothing as the wave takes over
        float Amp(float t)
        {
            float sweepPos = Sweeps * Mathf.Clamp01((t - relayFrom) / Mathf.Max(0.01f, relayTo - relayFrom));
            float a = Mathf.Lerp(A0, A1, sweepPos / Sweeps);
            return a * (1f - Shapes.Ease((t - (relayTo - 4f)) / 7f));
        }

        // point k of the zigzag at time t: even points on top, odd points at the bottom
        Vector2 Point(int k, float t) { return new Vector2(line[k].x, RowY + (k % 2 == 0 ? -1f : 1f) * Amp(t)); }

        float Wave(int slot, float t)
        {
            float k = 2f * Mathf.PI / WaveLength;
            float x = line[slot].x - 250f;
            float own = Shapes.Ease((t - waveFrom - 0.5f * slot) / RampSecs);
            float taper = 1f - Shapes.Ease((t - waveTo) / TaperSecs);
            return WaveAmp * own * taper * Mathf.Sin(k * x - 2f * Mathf.PI * (t - waveFrom) / WavePeriod);
        }

        // where a robot is during the relay: standing at its point, or on a step from one point to the next
        Vector2 Relay(int slot, float t)
        {
            int at = slot;                                          // the point it stands at between steps
            foreach (var m in moves[slot])
            {
                if (t >= m.t1) { at = m.to; continue; }
                if (t > m.t0)
                {
                    float u = (t - m.t0) / (m.t1 - m.t0);
                    bool runner = m.runner == slot;
                    float e = runner ? Mathf.Lerp(u, Shapes.Ease(u), 0.35f) : Shapes.Ease(u);
                    Vector2 a = Point(m.from, t), b = Point(m.to, t);
                    Vector2 dir = (b - a).normalized, right = new Vector2(dir.y, -dir.x);
                    return Vector2.Lerp(a, b, e) + right * Bow * Mathf.Sin(Mathf.PI * u);
                }
                break;
            }
            return Point(at, t);
        }

        public Vector2 Position(int slot, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            if (t < relayFrom)
            {
                if (way != null) { return way.Position(slot, t); }
                return Vector2.Lerp(start[slot], Point(slot, relayFrom), Shapes.Ease((t - 0.3f * slot) / (FormSecs - 2.4f)));
            }
            Vector2 p = t < relayTo ? Relay(slot, t) : Point(slot, t);
            if (t > waveFrom)
            {
                float e = Shapes.Ease((t - waveFrom) / 4f);
                p = new Vector2(Mathf.Lerp(p.x, line[slot].x, e), Mathf.Lerp(p.y, RowY, e) + Wave(slot, t));
            }
            if (t > taperTo)
            {
                p = Vector2.Lerp(new Vector2(line[slot].x, RowY), line[slot], Shapes.Ease((t - taperTo) / ReturnSecs));
            }
            return p;
        }

        public float Heading(int slot, float t) { return 270f; }
        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int slot, float t) { return false; }
    }
}
