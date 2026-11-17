using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using toio;
using toio.Navigation;

// v2 Cheerleading / happiness (C22). The robots travel round a 4-pointed star. It is the zigzag version of the wavy circle of the
// happiness behaviour in "From Motions to Emotions" (Santos & Egerstedt, Appendix A.1, Fig. 15a): 8 corners, alternately 4 tips
// (R + A) and 4 inner corners (R - A), joined by straight edges, with a robot on every corner and all of them travelling round the
// star at the same rate. A robot's path is a zigzag (out along one edge, in along the next: > V \_ ), and neighbours are always on
// opposite sides of a corner. The ring does not expand or contract as a whole.
//   1. After the standard opening the robots form an evenly spaced ring round the middle of the mat.
//   2. They turn on the spot to face the way the ring will move; the ring starts to turn and grows into the star.
//   3. THE INNER PART: they travel round the star (Ripple = how far the tips stand out, TurnRate = how fast).
//   4. The star fades back into the ring and the turning slows to a stop, then the ring opens into the line.
// Steps 1 and 4 steer the usual way (V2Show.Flow). In step 3, when the robots have TargetMove (firmware 2.1.0 or newer), nothing is
// steered: each robot gets one move per edge (from corner to corner), and the next only when it has arrived and its time has come,
// so there are no per-frame corrections. Robots without TargetMove are steered through step 3 like the rest.
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
        const float Ripple = 30f;           // how far the 4 points of the star stand out and the 4 inner corners sit in (mat units): tips at R + this, inner corners at R - this
        // (the star has 8 corners: the 8 robots stand on them, alternately tip and inner corner)
        const float TurnRate = 0.14f;       // w: how fast the robots travel round (rad/s; one lap in about 45 s)
        const float MiddleSecs = 72f;       // length of the wavy-circle part
        const float OrientSecs = 3f;       // after the ring has formed the robots turn on the spot to face the way the ring will move, then it starts
        const float TurnUpSecs = 5f;       // the turn eases in over this long
        const float RampSecs = 8f;          // the ripples grow in over this long when the part starts
        // (one move per edge of the star: each robot drives straight from corner to corner)
        // ---- the start and the end (as planned before)
        const float SlowSecs = 16f, ReturnSecs = 12f, ExitRadius = 106f;
        static readonly Vector2 Centre = new Vector2(250f, 250f);

        readonly Vector2[] start, line;
        readonly float[] sectorTo = new float[8];
        readonly float opensAt;
        V2Show.WayToLine way;
        float FormEnd = 14f, midStart, midEnd, slowEnd, ringStop, extraTurn;
        public bool Native;
        public float Total { get; private set; }

        public IEnumerable<KeyValuePair<float, string>> Sections
        {
            get
            {
                yield return new KeyValuePair<float, string>(0f, "ring forms");
                yield return new KeyValuePair<float, string>(FormEnd, "robots turn to face along the ring");
                yield return new KeyValuePair<float, string>(midStart, "ring travels a wavy circle");
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
            midStart = FormEnd + OrientSecs;
            midEnd = midStart + MiddleSecs;
            slowEnd = midEnd + SlowSecs;
            ringStop = slowEnd + ReturnSecs;
            Total = ringStop + 0.3f;
            // the turn is eased to a stop at exactly the angle the ring needs to flatten into the line without crossing
            float reached = TurnRate * (MiddleSecs - 0.5f * TurnUpSecs + 0.5f * SlowSecs);
            extraTurn = Mathf.DeltaAngle(reached * Mathf.Rad2Deg, (opensAt - 135f * Mathf.Deg2Rad) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            BuildWaypoints();
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
            float run = Mathf.Clamp(t - midStart, 0f, MiddleSecs);
            run = TurnUpSecs * Shapes.EaseIntegral(run / TurnUpSecs);        // the turn eases in: no jump from rest to full speed when the ring has formed
            return TurnRate * (run + SlowSecs * (u - Shapes.EaseIntegral(u))) + extraTurn * Shapes.Ease(u);
        }

        // 1 while the wave is on: grows in at the start of the wavy-circle part, fades during the slow part
        float Wave(float t) { return Shapes.Ease((t - midStart - TurnUpSecs) / RampSecs) * (1f - Shapes.Ease((t - midEnd) / SlowSecs)); }

        // The ring becomes a 4-pointed star: 8 corners (4 tips out, 4 inner corners in, alternately) joined by straight edges. The robots
        // travel round the star corner by corner, so each robot's path is a zigzag (out along one edge, in along the next: > V \_ ), and
        // neighbouring robots are always on opposite sides of a corner. The star grows out of the ring and fades back into it.
        Vector2 StarCorner(int m, float baseR)
        {
            float ang = 135f * Mathf.Deg2Rad + m * Mathf.PI / 4f;
            float r = baseR + (((m % 8) + 8) % 8 % 2 == 0 ? 1f : -1f) * Ripple;
            return Centre + r * new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        }

        Vector2 OnWave(int slot, float t)
        {
            float turn = Turn(t);
            float baseR = RingR;
            float sf = (sectorTo[slot] + turn - 135f * Mathf.Deg2Rad) / (Mathf.PI / 4f);   // where along the star's edges, in edges
            int m = Mathf.FloorToInt(sf);
            Vector2 star = Vector2.Lerp(StarCorner(m, baseR), StarCorner(m + 1, baseR), sf - m);
            return Vector2.Lerp(Ring(slot, baseR, turn), star, Wave(t));
        }

        // when the turn has reached `angle` (it only grows between midStart and midEnd)
        float TimeAtTurn(float angle)
        {
            float lo = midStart, hi = midEnd;
            for (int k = 0; k < 40; k++) { float mid = 0.5f * (lo + hi); if (Turn(mid) < angle) { lo = mid; } else { hi = mid; } }
            return hi;
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
        bool begun; int[] next, lastId; readonly Dictionary<Cube, int> arrived = new Dictionary<Cube, int>(); bool unhooked;
        Action<Cube, int, Cube.TargetMoveRespondType> onDone;
        readonly List<float> wpT = new List<float>();     // times the robots are at a corner of the star (the first is the start, the last the end of the part)

        // a robot is at a corner of the star each time the turn has gone another 45 degrees
        void BuildWaypoints()
        {
            wpT.Clear(); wpT.Add(midStart);
            for (int k = 1; Turn(midEnd) >= k * Mathf.PI / 4f; k++) { wpT.Add(TimeAtTurn(k * Mathf.PI / 4f)); }
            if (wpT[wpT.Count - 1] < midEnd - 0.5f) { wpT.Add(midEnd); }
        }

        // true while the robots are driven by Drive and not steered
        public bool Driving(float t) { return Native && t >= FormEnd && t < midEnd; }

        public void Drive(RoutineContext ctx, float t)
        {
            int n = ctx.Count;
            if (!Native) { return; }
            if (t >= midEnd) { Unhook(ctx); return; }
            if (t < FormEnd) { return; }
            if (t < midStart) { Orient(ctx); return; }
            if (!begun)
            {
                begun = true; next = Enumerable.Repeat(1, n).ToArray(); lastId = new int[n];
                onDone = (c, id, resp) => { arrived[c] = id; };
                foreach (var c in ctx.Cubes) { c.targetMoveCallback.AddListener("c22", onDone); }
            }
            int last = wpT.Count - 1;
            for (int i = 0; i < n; i++)
            {
                int k = next[i];
                if (k > last) { continue; }
                bool free = lastId[i] == 0 || (arrived.ContainsKey(ctx.Cubes[i]) && arrived[ctx.Cubes[i]] == lastId[i]);
                if (!free) { continue; }
                if (t < wpT[k - 1] - 0.15f) { continue; }                                  // not before its time
                int due = 1; while (due < last && wpT[due] <= t) { due++; }
                if (k < due - 1) { k = due - 1; }                                           // a robot that fell far behind skips ahead
                Vector2 from = OnWave(i, wpT[k - 1]), to = OnWave(i, wpT[k]);
                float unitsPerSec = Vector2.Distance(from, to) / Mathf.Max(0.5f, wpT[k] - wpT[k - 1]);
                int speed = Mathf.Clamp(Mathf.RoundToInt(unitsPerSec / (float)CubeHandle.VDotOverU * 1.05f), 10, 100);
                int id = (k % 250) + 1;
                Vector2 d = to - from;
                int angle = Mathf.RoundToInt(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                ctx.Cubes[i].TargetMove(Mathf.RoundToInt(to.x), Mathf.RoundToInt(to.y), angle, id, 8,
                    Cube.TargetMoveType.RotatingMove, speed, Cube.TargetSpeedType.UniformSpeed, Cube.TargetRotationType.NotRotate);
                lastId[i] = id; next[i] = k + 1;
            }
        }

        bool oriented;
        // Once the ring has formed, each robot turns on the spot (one command) to the way the ring will move, so it does not
        // have to swing round while the ring starts to turn.
        void Orient(RoutineContext ctx)
        {
            if (oriented) { return; }
            oriented = true;
            for (int i = 0; i < ctx.Count; i++)
            {
                int deg = Mathf.RoundToInt(Mathf.Repeat(sectorTo[i] * Mathf.Rad2Deg + 90f, 360f));
                ctx.Cubes[i].TargetMove(ctx.Cubes[i].x, ctx.Cubes[i].y, deg, 251, 4,
                    Cube.TargetMoveType.RotatingMove, 40, Cube.TargetSpeedType.UniformSpeed, Cube.TargetRotationType.AbsoluteLeastAngle);
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
