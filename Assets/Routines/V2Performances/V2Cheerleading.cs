using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using toio;
using toio.Navigation;

// v2 Cheerleading / happiness (C22). The robots circle along a wavy circle, as in the happiness behaviour of
// "From Motions to Emotions" (Santos & Egerstedt, Appendix A.1, Fig. 15a): robot i is at angle theta = theta_i + w t
// on the path r(theta) = R + A cos(f theta), the theta_i evenly spaced round the circle. The wave is on the path the
// robots follow (it stays where it is while the robots travel round it); the ring is not a perfect circle, and it
// does not expand or contract as a whole.
//   1. After the standard opening the robots form an evenly spaced ring round the middle of the mat.
//   2. THE INNER PART: they travel round the wavy circle (Radius, Ripples, Amplitude, TurnRate below).
//   3. The ripples fade and the turning slows to a stop, the ring settles, then opens into the line.
// Steps 1 and 3 steer the usual way (V2Show.Flow). In step 2, when the robots have TargetMove (firmware 2.1.0 or
// newer), nothing is steered: each robot gets one move per waypoint (every SegDeg degrees round the wavy circle) and
// is only given the next when it has arrived and its time has come, so there are no per-frame corrections.
// Robots without TargetMove are steered through step 2 like the rest.
public class V2Cheerleading : SwarmRoutine
{
    public override string Name { get { return "v2 Cheerleading"; } }
    public override string Code { get { return "C22"; } }
    public override string Note { get { return "Ring travels a wavy circle, then slows"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        bool native = ctx.Cubes.All(c => V2StressRestless.SupportsTargetMove(c));
        Plan plan = null;
        yield return V2Show.Run(ctx, start => { plan = new Plan(start); plan.Native = native; return plan; }, 100,
            skip: (i, t) => plan != null && plan.Driving(t),
            extra: t => { if (plan != null) { plan.Drive(ctx, t); } });
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading, V2Show.ISections
    {
        // ---- the wavy circle (settings to calibrate)
        const float RingR = 100f;           // R: main circle radius
        const float Ripple = 16f;           // A: ripple amplitude (mat units)
        const float Ripples = 4f;           // f: ripples per revolution. 4 with the phase below puts neighbouring robots on opposite sides: 4 out, 4 in, one in and one out alternately
        const float TurnRate = 0.14f;       // w: how fast the robots travel round (rad/s; one lap in about 45 s)
        const float MiddleSecs = 72f;       // length of the wavy-circle part
        const float TurnUpSecs = 5f;       // the turn eases in over this long
        const float RampSecs = 8f;          // the ripples grow in over this long when the part starts
        const float SegDeg = 15f;           // a waypoint every this many degrees (24 per lap)
        // ---- the start and the end (as planned before)
        const float SlowSecs = 16f, ReturnSecs = 12f, ExitRadius = 106f;
        static readonly Vector2 Centre = new Vector2(250f, 250f);

        readonly Vector2[] start, line;
        readonly float[] sectorTo = new float[8];
        readonly float opensAt;
        V2Show.WayToLine way;
        float FormEnd = 14f, midEnd, slowEnd, ringStop, extraTurn;
        public bool Native;
        public float Total { get; private set; }

        public IEnumerable<KeyValuePair<float, string>> Sections
        {
            get
            {
                yield return new KeyValuePair<float, string>(0f, "ring forms");
                yield return new KeyValuePair<float, string>(FormEnd, "ring travels a wavy circle");
                yield return new KeyValuePair<float, string>(midEnd, "ripples fade and the ring slows");
                yield return new KeyValuePair<float, string>(slowEnd, "back to the line");
            }
        }

        public Plan(Vector2[] start)
        {
            this.start = start;
            line = V2Show.OwnSpots(start.Length);
            var order = V2Show.RingOrder(8);
            for (int i = 0; i < 8; i++) { sectorTo[i] = (135f + 45f * order[i]) * Mathf.Deg2Rad; }
            float unused; opensAt = V2Show.BestAngle(order, 1, ExitRadius, line, out unused);
            var ideal = Enumerable.Range(0, 8).Select(i => i == V2Show.MessengerSlot(8) ? (Vector2)V2Show.TalkSpot : line[i]).ToArray();
            var targets = Enumerable.Range(0, 8).Select(i => Ring(i, RingR, 0f)).ToArray();
            foreach (float keep in new[] { 58f, 50f, 42f })
            {
                foreach (float speed in new[] { 22f, 26f, 30f, 36f, 42f, 50f })
                {
                    var w = new V2Show.WayToLine(ideal, targets, speed, keep);
                    if (w.Clear && w.Total <= FormEnd) { way = w; break; }
                }
                if (way != null) { break; }
            }
            if (way != null) { FormEnd = way.Total + 0.3f; }
            midEnd = FormEnd + MiddleSecs;
            slowEnd = midEnd + SlowSecs;
            ringStop = slowEnd + ReturnSecs;
            Total = ringStop + 0.3f;
            // the turn is eased to a stop at exactly the angle the ring needs to flatten into the line without crossing
            float reached = TurnRate * (MiddleSecs - 0.5f * TurnUpSecs + 0.5f * SlowSecs);
            extraTurn = Mathf.DeltaAngle(reached * Mathf.Rad2Deg, (opensAt - 135f * Mathf.Deg2Rad) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        }

        Vector2 Ring(int slot, float r, float turn)
        {
            float a = sectorTo[slot] + turn;
            return Centre + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        // how far the robots have travelled round: steady while in the wavy-circle part, easing to a stop in the slow part
        float Turn(float t)
        {
            float u = Mathf.Clamp01((t - midEnd) / SlowSecs);
            float run = Mathf.Clamp(t - FormEnd, 0f, MiddleSecs);
            run = TurnUpSecs * Shapes.EaseIntegral(run / TurnUpSecs);        // the turn eases in: no jump from rest to full speed when the ring has formed
            return TurnRate * (run + SlowSecs * (u - Shapes.EaseIntegral(u))) + extraTurn * Shapes.Ease(u);
        }

        // 1 while the wave is on: grows in at the start of the wavy-circle part, fades during the slow part
        float Wave(float t) { return Shapes.Ease((t - FormEnd - TurnUpSecs) / RampSecs) * (1f - Shapes.Ease((t - midEnd) / SlowSecs)); }

        // r(theta) = R + A cos(f theta), theta = theta_i + the turn: the path itself does not move, the robots travel round it
        Vector2 OnWave(int slot, float t)
        {
            float turn = Turn(t);
            float theta = sectorTo[slot] + turn;
            float r = RingR + Ripple * Wave(t) * Mathf.Cos(Ripples * theta);
            return Ring(slot, r, turn);
        }

        public Vector2 Position(int slot, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            if (t < FormEnd) { return way != null ? way.Position(slot, t) : Vector2.Lerp(start[slot], Ring(slot, RingR, 0f), Shapes.Ease(t / FormEnd)); }
            if (t <= slowEnd) { return OnWave(slot, t); }
            float e = Shapes.Ease((t - slowEnd) / ReturnSecs);
            Vector2 open = Ring(slot, Mathf.Lerp(RingR, ExitRadius, Shapes.Ease((t - slowEnd) / (ReturnSecs * 0.5f))), Turn(slowEnd));
            return Vector2.Lerp(open, line[slot], e);
        }

        public float Heading(int slot, float t) { return 270f; }
        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int slot, float t) { return false; }

        // ---------------------------------------------------------------- the wavy-circle part, one move per waypoint
        float SegSecs { get { return SegDeg * Mathf.Deg2Rad / TurnRate; } }
        bool begun; int[] next, lastId; readonly Dictionary<Cube, int> arrived = new Dictionary<Cube, int>(); bool unhooked;
        Action<Cube, int, Cube.TargetMoveRespondType> onDone;

        // true while the robots are driven by Drive and not steered
        public bool Driving(float t) { return Native && t >= FormEnd && t < midEnd; }

        public void Drive(RoutineContext ctx, float t)
        {
            int n = ctx.Count;
            if (!Native) { return; }
            if (t >= midEnd) { Unhook(ctx); return; }
            if (t < FormEnd) { return; }
            if (!begun)
            {
                begun = true; next = Enumerable.Repeat(1, n).ToArray(); lastId = new int[n];
                onDone = (c, id, resp) => { arrived[c] = id; };
                foreach (var c in ctx.Cubes) { c.targetMoveCallback.AddListener("c22", onDone); }
            }
            float dt = SegSecs;
            int lastWp = Mathf.FloorToInt(MiddleSecs / dt);
            for (int i = 0; i < n; i++)
            {
                int k = next[i];
                if (k > lastWp) { continue; }
                bool free = lastId[i] == 0 || (arrived.ContainsKey(ctx.Cubes[i]) && arrived[ctx.Cubes[i]] == lastId[i]);
                if (!free) { continue; }
                if (t < FormEnd + (k - 1) * dt - 0.15f) { continue; }                 // not before its time
                int due = Mathf.FloorToInt((t - FormEnd) / dt) + 1;
                if (k < due - 1) { k = Mathf.Min(due - 1, lastWp); }                    // a robot that fell far behind skips ahead
                Vector2 from = OnWave(i, FormEnd + (k - 1) * dt), to = OnWave(i, FormEnd + k * dt);
                float unitsPerSec = Vector2.Distance(from, to) / dt;
                int speed = Mathf.Clamp(Mathf.RoundToInt(unitsPerSec / (float)CubeHandle.VDotOverU * 1.05f), 10, 100);
                int id = (k % 250) + 1;
                Vector2 d = to - from;
                int angle = Mathf.RoundToInt(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                ctx.Cubes[i].TargetMove(Mathf.RoundToInt(to.x), Mathf.RoundToInt(to.y), angle, id, 6,
                    Cube.TargetMoveType.RotatingMove, speed, Cube.TargetSpeedType.UniformSpeed, Cube.TargetRotationType.NotRotate);
                lastId[i] = id; next[i] = k + 1;
            }
        }

        void Unhook(RoutineContext ctx)
        {
            if (unhooked || !begun) { return; }
            unhooked = true;
            foreach (var c in ctx.Cubes) { c.targetMoveCallback.RemoveListener("c22"); }
        }
    }
}
