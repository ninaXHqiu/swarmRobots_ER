using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// v2 Heart Resonance (C21): a second version of Heart Ring. The ring does not beat with every heartbeat but
// resonates with the participant's heart rate at a factor (Resonance below): 0.5 = one swell for every two
// heartbeats (a calm echo), 2 = two swells per heartbeat. The pace is the heart rate x the factor, kept between
// 30 and 90 swells a minute so the robots can follow, and it changes smoothly as the heart rate changes.
//   1. After the standard opening the robots form one ring round the middle of the mat.
//   2. The ring swells at the resonant pace and turns slowly (the faster the heart, the slower the turn).
//      With no sensor the heart rate is taken as 72, so the routine still runs.
//   3. The ring then opens and flattens into the line.
public class V2HeartResonance : SwarmRoutine
{
    public override string Name { get { return "v2 Heart Resonance"; } }
    public override string Code { get { return "C21"; } }
    public override string Note { get { return "Ring beats at a multiple of your heart rate"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return V2Show.Run(ctx, start => new Plan(start), 100);
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading, V2Show.ISections
    {
        const float BeatSecs = 100f, ReturnSecs = 12f, ExitRadius = 106f, FallbackBpm = 72f;
        const float RingR = 100f, SwellBy = 10f, UpPart = 0.2f, DownPart = 0.5f;   // shape of one swell, as parts of its length
        const float Resonance = 0.5f, MinPace = 30f, MaxPace = 90f;                // swells per minute = heart rate x Resonance, kept in this range
        const float RateAt60 = 0.14f, RatePerBpm = 0.0008f, RateMin = 0.05f, LockSecs = 22f;
        static readonly Vector2 Centre = new Vector2(250f, 250f);

        readonly Vector2[] start, line;
        readonly float[] sectorTo = new float[8];
        float beatEnd, ringStop;
        readonly float opensAt;
        V2Show.WayToLine way;
        float FormEnd = 14f;
        float lastT = -1f, angle, lockedAngle, extraTurn, swellPhase; bool locked;
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

        // the ring's turn: follows the heart (slower when it is faster) until the settle time, then eases round to the
        // angle from which the ring flattens into the line without crossing
        float Turn(float t)
        {
            if (t < lastT - 1f) { lastT = -1f; angle = 0f; locked = false; extraTurn = 0f; swellPhase = 0f; }   // a new run through the plan (checks play it several times)
            if (t > lastT + 1e-4f)
            {
                float dt = lastT < 0f ? 0f : Mathf.Min(0.2f, t - lastT);
                lastT = t;
                float pace = Mathf.Clamp(Physio.HeartRateOr(FallbackBpm) * Resonance, MinPace, MaxPace);
                swellPhase = Mathf.Repeat(swellPhase + dt * pace / 60f, 1f);   // 0..1 through one swell
                float settleFrom = beatEnd - LockSecs;
                if (t <= settleFrom && t > FormEnd)
                {
                    float rate = Mathf.Clamp(RateAt60 - RatePerBpm * (Physio.HeartRateOr(FallbackBpm) - 60f), RateMin, RateAt60);
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
            float up = Shapes.Ease(swellPhase / UpPart);
            float down = Shapes.Ease((swellPhase - UpPart) / DownPart);
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
