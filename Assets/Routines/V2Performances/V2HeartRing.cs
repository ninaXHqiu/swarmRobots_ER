using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// v2 Heart Ring (C19): the first routine that follows the participant's body.
//   1. After the standard opening the robots form one ring round the middle of the mat.
//   2. The ring swells a little at every heartbeat of the participant (from the Shimmer sensor, or from the
//      simulated sensor), and turns at base speed x skin conductance (microsiemens): the more aroused the
//      participant, the faster the ring turns. With no sensor the ring beats like a heart at 72 beats per minute
//      and turns as if the skin conductance were 5, so the routine still runs.
//   3. The ring then opens and flattens into the line.
// The swell is small and slow enough for the robots to follow (about 12 mat units out in a quarter of a second).
public class V2HeartRing : SwarmRoutine
{
    public override string Name { get { return "v2 Heart Ring"; } }
    public override string Code { get { return "C19"; } }
    public override string Note { get { return "Ring beats with your heart"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return V2Show.Run(ctx, start => new Plan(start), 100);
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading, V2Show.ISections
    {
        const float BeatSecs = 100f, ReturnSecs = 12f, ExitRadius = 106f, FallbackBpm = 72f;
        const float RingR = 100f, SwellBy = 10f, AttackSecs = 0.3f, DecaySecs = 0.7f;
        // turn speed (radians per second) = BaseSpeed x skin conductance, kept inside what the robots can follow
        // (0.35 rad/s at the ring's radius of 100 is 35 mat units per second)
        const float BaseSpeed = 0.028f, FallbackEda = 5f, RateMin = 0.05f, RateMax = 0.35f, LockSecs = 22f;
        static readonly Vector2 Centre = new Vector2(250f, 250f);

        readonly Vector2[] start, line;
        readonly float[] sectorTo = new float[8];
        float beatEnd, ringStop;
        readonly float opensAt;
        V2Show.WayToLine way;
        float FormEnd = 14f;
        float lastT = -1f, angle, lockedAngle, extraTurn; bool locked;
        public float Total { get; private set; }

        public IEnumerable<KeyValuePair<float, string>> Sections
        {
            get
            {
                yield return new KeyValuePair<float, string>(0f, "ring forms");
                yield return new KeyValuePair<float, string>(FormEnd, "ring beats with the heart");
                yield return new KeyValuePair<float, string>(beatEnd - LockSecs, "ring settles for the way home");
                yield return new KeyValuePair<float, string>(beatEnd, "back to the line");
            }
        }

        public Plan(Vector2[] start)
        {
            this.start = start;
            line = V2Show.OwnSpots(start.Length);
            var order = V2Show.RingOrder(8);
            for (int i = 0; i < 8; i++) { sectorTo[i] = (135f + 45f * order[i]) * Mathf.Deg2Rad; }
            float unused; opensAt = V2Show.BestAngle(order, 1, ExitRadius, line, out unused);
            // the way from the line to the ring, planned from the ideal start spots so it is the same every time
            var ideal = Enumerable.Range(0, 8).Select(i => i == V2Show.MessengerSlot(8) ? (Vector2)V2Show.TalkSpot : line[i]).ToArray();
            var targets = Enumerable.Range(0, 8).Select(i => RingPoint(i, RingR, 0f)).ToArray();
            // as much room between the robots as can be had: real robots stray a few units from their planned paths
            foreach (float keep in new[] { 58f, 50f, 42f })
            {
                foreach (float speed in new[] { 22f, 26f, 30f, 36f, 42f, 50f })
                {
                    var w = new V2Show.WayToLine(ideal, targets, speed, keep);
                    if (w.Clear && w.Total <= FormEnd) { way = w; break; }
                }
                if (way != null) { break; }
            }
            if (way != null) { FormEnd = way.Total + 0.3f; }   // the ring begins to beat the moment the last robot is there
            beatEnd = FormEnd + BeatSecs;
            ringStop = beatEnd + ReturnSecs;
            Total = ringStop + 0.3f;
        }

        Vector2 RingPoint(int slot, float r, float turn)
        {
            float a = sectorTo[slot] + turn;
            return Centre + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        // the ring's turn: base speed x skin conductance until the settle time, then eases round to the
        // angle from which the ring flattens into the line without crossing
        float Turn(float t)
        {
            if (t < lastT - 1f) { lastT = -1f; angle = 0f; locked = false; extraTurn = 0f; }   // a new run through the plan (checks play it several times)
            if (t > lastT + 1e-4f)
            {
                float dt = lastT < 0f ? 0f : Mathf.Min(0.2f, t - lastT);
                lastT = t;
                float settleFrom = beatEnd - LockSecs;
                if (t <= settleFrom && t > FormEnd)
                {
                    float rate = Mathf.Clamp(BaseSpeed * Physio.EdaOr(FallbackEda), RateMin, RateMax);
                    angle += rate * dt;
                }
                if (t > settleFrom && !locked)
                {
                    locked = true; lockedAngle = angle;
                    float target = opensAt - 135f * Mathf.Deg2Rad;
                    extraTurn = Mathf.Repeat(target - lockedAngle, 2f * Mathf.PI);
                }
            }
            if (!locked) { return angle; }
            float settleFrom2 = beatEnd - LockSecs;
            return lockedAngle + extraTurn * Shapes.Ease((t - settleFrom2) / (LockSecs + ReturnSecs / 2f));
        }

        float Swell(float t)
        {
            float s = Physio.SecondsSinceBeatOr(t, FallbackBpm);
            float up = Shapes.Ease(s / AttackSecs);
            float down = Shapes.Ease((s - AttackSecs) / DecaySecs);
            return up * (1f - down);
        }

        public Vector2 Position(int slot, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            float turn = Turn(t);
            // the swell fades in after the ring is formed and out before the way home
            float w = Shapes.Ease((t - FormEnd) / 4f) * (1f - Shapes.Ease((t - (beatEnd - 4f)) / 4f));
            float r = RingR + SwellBy * w * Swell(t);
            Vector2 p = RingPoint(slot, r, turn);
            if (t < FormEnd) { p = way != null ? way.Position(slot, t) : Vector2.Lerp(start[slot], RingPoint(slot, RingR, 0f), Shapes.Ease(t / FormEnd)); }
            if (t > beatEnd)
            {
                float e = Shapes.Ease((t - beatEnd) / ReturnSecs);
                Vector2 open = RingPoint(slot, Mathf.Lerp(RingR, ExitRadius, Shapes.Ease((t - beatEnd) / (ReturnSecs * 0.5f))), turn);
                p = Vector2.Lerp(open, line[slot], e);
            }
            return p;
        }

        public float Heading(int slot, float t) { return 270f; }
        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int slot, float t) { return false; }
    }
}
