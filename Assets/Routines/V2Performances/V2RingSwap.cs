using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// v2 Ring Swap (C17): two rings, one inside the other, turning in opposite directions.
//   1. After the standard opening the robots go straight to an inner ring and an outer ring, four each:
//      one ring stands on the four "+" points, the other on the four "x" points (45 degrees between
//      the two rings' robots). The rings turn against each other, slowly. No pauses.
//   2. While they keep turning, all inner robots move out and all outer robots move in at the same
//      time, each carrying on at the turning speed it had and taking on the direction of its new ring
//      once it is nearly there. Four times.
//   3. The two rings draw together into one ring which keeps turning, breathes (large, slow) several
//      times and opens into the line.
// The turning angles are worked out backwards from the final ring (every robot in its own place of
// V2Show.RingOrder, so that it flattens into the line without crossing). The swaps are timed so that
// the two rings are always 45 degrees apart (interleaved) when robots cross between them.
public class V2RingSwap : SwarmRoutine
{
    public override string Name { get { return "v2 Ring Swap"; } }
    public override string Code { get { return "C17"; } }
    public override string Note { get { return "Counter rings swap, breathing"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return V2Show.Run(ctx, start => new Plan(start), 100);
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading
    {
        const float W = 0.25f;                                   // turning speed of each ring, radians per second (about 14 degrees)
        const float InnerR = 58f, OuterR = 122f, RingR = 118f;
        const float FormEnd = 16f;                                // the rings start turning when the robots have arrived
        const float SwapSecs = 7f, LagSecs = 4.2f;                // a robot keeps its old turning speed for the first 60% of its move
        const int Swaps = 4;
        const float WideRadius = 128f, SmallRadius = 52f, WidenSecs = 12f, BreathSecs = 20f;
        const int Breaths = 4;
        const float ReturnSecs = 12f, ExitRadius = 106f, RingRate0 = 0.11f;
        static readonly Vector2 Centre = new Vector2(250f, 250f);
        const float Dt = 0.02f, Half = Mathf.PI / 2f;

        readonly Vector2[] start, line;
        readonly float[] baseAngle = new float[8];
        readonly bool[] groupA = new bool[8];                    // even ring places
        bool aOutside;                                            // which group starts on the outer ring
        readonly float[] swapAt = new float[Swaps];
        float mergeFrom, mergeTo, breatheFrom, stopFrom, ringStop;
        readonly List<float>[] ang = new List<float>[8];
        readonly float[] phi = new float[8];
        Show.Eased turnRate;
        float phase0;
        V2Show.WayToLine way;
        public float Total { get; private set; }
        public float RingAt { get { return FormEnd + mergeTo; } }
        public float BreatheAt { get { return FormEnd + breatheFrom; } }
        public float ReturnAt { get { return FormEnd + stopFrom; } }

        // times inside the plan are counted from FormEnd ("tau"); the formation is the time before it

        static float Wrap(float x) { return Mathf.Repeat(x + Half / 2f, Half) - Half / 2f; }

        // how far outside (0 inner .. 1 outer) the robots of group A are at tau, using the first `swaps` swaps
        float AOut(float tau, int swaps)
        {
            int state = 0;
            for (int k = 0; k < swaps; k++)
            {
                if (tau >= swapAt[k] + SwapSecs) { state = 1 - state; continue; }
                if (tau > swapAt[k])
                {
                    float e = Shapes.Ease((tau - swapAt[k]) / SwapSecs);
                    return state == 0 ? 1f - e : e;
                }
                break;
            }
            return state == 0 ? 1f : 0f;     // (group A is the outside one at the start of the turning, counted in this frame)
        }

        // inside/outside amount of a robot (1 = outer ring)
        float Outside(int slot, float tau, int swaps = Swaps)
        {
            float a = AOut(tau, swaps);
            float v = groupA[slot] ? a : 1f - a;
            return aOutside ? v : 1f - v;
        }

        // turning rate of a robot: its ring's, taken on late (the lag) when it changes ring
        float Rate(int slot, float tau, int swaps = Swaps)
        {
            float u = Outside(slot, tau - LagSecs, swaps);
            return W * (1f - 2f * u);
        }

        public Plan(Vector2[] start)
        {
            this.start = start;
            line = V2Show.OwnSpots(start.Length);
            var order = V2Show.RingOrder(8);
            for (int i = 0; i < 8; i++) { baseAngle[i] = (135f + 45f * order[i]) * Mathf.Deg2Rad; groupA[i] = order[i] % 2 == 0; }

            // swap times: each swap starts when the angle between the two rings is again a multiple of 90 degrees
            // (so they are interleaved), found by following the angle between them forward in time
            swapAt[0] = 2f * Mathf.PI;     // W-W turning for 2 pi seconds: 180 degrees apart
            aOutside = true;
            float dAng = 0f, t0 = 0f;      // dAng: angle of group B minus that of group A
            int known = 1;
            for (int k = 1; k < Swaps; k++)
            {
                // integrate from t0 to the earliest next start
                float earliest = swapAt[k - 1] + SwapSecs + LagSecs + 2f, t = t0;
                bool found = false;
                for (; t < earliest + 12f; t += Dt)
                {
                    float ua = AOut(t - LagSecs, known);
                    dAng += (W * (1f - 2f * (1f - ua)) - W * (1f - 2f * ua)) * Dt;
                    if (t >= earliest && Mathf.Abs(Wrap(dAng)) < 0.012f) { swapAt[k] = t; found = true; t += Dt; break; }
                }
                if (!found) { swapAt[k] = t; }
                t0 = t; known = k + 1;
            }
            // keep integrating through the last swap to the start of the merge
            mergeFrom = swapAt[Swaps - 1] + SwapSecs + LagSecs;
            for (float t = t0; t < mergeFrom; t += Dt)
            {
                float ua = AOut(t - LagSecs, Swaps);
                dAng += (W * (1f - 2f * (1f - ua)) - W * (1f - 2f * ua)) * Dt;
            }
            // merge length: the angle between the rings must again be a multiple of 90 degrees when it ends
            // (during the merge it shrinks by 0.25 per second of merge length; both groups end at one turning rate)
            float mg = 11f;
            for (int k = -10; k < 10; k++)
            {
                float cand = 4f * (dAng - k * Half) * -1f;
                if (cand >= 8f && cand <= 14f) { mg = cand; break; }
            }
            mergeTo = mergeFrom + mg;
            breatheFrom = mergeTo + WidenSecs;
            stopFrom = breatheFrom + Breaths * BreathSecs;
            ringStop = stopFrom + ReturnSecs;

            // each robot's angle history, worked out backwards from its final place on the ring (angle c + its ring place)
            var rel = new List<float>[8];
            for (int i = 0; i < 8; i++) { rel[i] = new List<float>(); rel[i].Add(0f); }
            var sum = new float[8];
            for (float t = 0f; t < mergeTo; t += Dt)
            {
                for (int i = 0; i < 8; i++)
                {
                    float r = Rate(i, t);
                    if (t >= mergeFrom) { float m = Shapes.Ease((t - mergeFrom) / mg); r = Mathf.Lerp(r, RingRate0, m); }
                    sum[i] += r * Dt; rel[i].Add(sum[i]);
                }
            }
            var idealStart = Enumerable.Range(0, 8).Select(i => i == V2Show.MessengerSlot(8) ? (Vector2)V2Show.TalkSpot : line[i]).ToArray();
            // which group starts outside and the turn c of the whole figure that make the trips from the line shortest
            float bestC = 0f, bestCost = float.MaxValue; bool bestAOut = true;
            foreach (bool aOut in new[] { true, false })
            {
                for (float c = 0f; c < 2f * Mathf.PI; c += 0.05f)
                {
                    float cost = 0f;
                    var tgt = new Vector2[8];
                    for (int i = 0; i < 8; i++)
                    {
                        float a = baseAngle[i] + c - sum[i];
                        bool outer = groupA[i] == aOut;
                        float r = outer ? OuterR : InnerR;
                        tgt[i] = Centre + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        float dist = (tgt[i] - idealStart[i]).magnitude;
                        cost += dist * dist / 100f;
                    }
                    // straight trips that cross each other (or pass close to another robot's start) are what makes the start messy
                    for (int i = 0; i < 8; i++)
                    {
                        for (int j = i + 1; j < 8; j++)
                        {
                            if (Crosses(idealStart[i], tgt[i], idealStart[j], tgt[j])) { cost += 4000f; }
                        }
                    }
                    if (cost < bestCost) { bestCost = cost; bestC = c; bestAOut = aOut; }
                }
            }
            if (bestAOut != aOutside)
            {
                // recompute the angle histories with the other group outside first
                aOutside = bestAOut;
                for (int i = 0; i < 8; i++) { rel[i].Clear(); rel[i].Add(0f); sum[i] = 0f; }
                for (float t = 0f; t < mergeTo; t += Dt)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        float r = Rate(i, t);
                        if (t >= mergeFrom) { float m = Shapes.Ease((t - mergeFrom) / mg); r = Mathf.Lerp(r, RingRate0, m); }
                        sum[i] += r * Dt; rel[i].Add(sum[i]);
                    }
                }
            }
            phase0 = bestC;
            for (int i = 0; i < 8; i++) { phi[i] = baseAngle[i] + bestC - sum[i]; ang[i] = rel[i]; }

            float tMid = stopFrom + ReturnSecs / 2f;
            Show.Eased RateFor(float c) { return new Show.Eased(mergeTo, RingRate0, breatheFrom, c, stopFrom, c, ringStop, 0f); }
            float unused; float opensAt = V2Show.BestAngle(order, 1, ExitRadius, line, out unused);
            float needed = Mathf.Repeat(opensAt - 135f * Mathf.Deg2Rad - phase0, 2f * Mathf.PI);
            float omega0 = RateFor(0f).Integral(tMid) - RateFor(0f).Integral(mergeTo);
            float perUnit = RateFor(1f).Integral(tMid) - RateFor(1f).Integral(mergeTo) - omega0;
            float chosen = 7f * Mathf.Deg2Rad;
            for (int k = 0; k < 40; k++)
            {
                float c = (needed + 2f * Mathf.PI * k - omega0) / perUnit;
                if (c >= 4f * Mathf.Deg2Rad && c <= 9f * Mathf.Deg2Rad) { chosen = c; break; }
            }
            turnRate = RateFor(chosen);
            Total = FormEnd + ringStop + 0.3f;
            mergeLen = mg;

            // the way from where the opening leaves the robots to their places on the two rings: planned paths that keep clear of each other
            var targets = Enumerable.Range(0, 8).Select(i => OnRings(i, FormEnd)).ToArray();
            // the formation: clear paths planned from where the opening leaves the robots (the line, with the messenger at
            // its talking spot), at the slowest speed that still arrives in time. Planned from those ideal spots, not from the
            // exact positions of the moment, so the plan is the same every time
            var ideal = Enumerable.Range(0, 8).Select(i => i == V2Show.MessengerSlot(8) ? (Vector2)V2Show.TalkSpot : line[i]).ToArray();
            way = null;
            foreach (float speed in new[] { 22f, 26f, 30f, 36f, 42f, 50f })
            {
                var w = new V2Show.WayToLine(ideal, targets, speed);
                if (w.Clear && w.Total <= FormEnd) { way = w; break; }
            }
            float formEnd = way != null ? way.Total : -1f;
            Debug.Log("RingSwap formation takes " + formEnd + " swaps at " + string.Join(",", swapAt.Select(x => x.ToString("0.0")).ToArray()) + " merge " + mg + " aOutside=" + aOutside);
        }
        float mergeLen;

        static bool Crosses(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float d1 = Cross(b - a, c - a), d2 = Cross(b - a, d - a), d3 = Cross(d - c, a - c), d4 = Cross(d - c, b - c);
            return d1 * d2 < 0f && d3 * d4 < 0f;
        }
        static float Cross(Vector2 u, Vector2 v) { return u.x * v.y - u.y * v.x; }

        float Angle(int slot, float tau)
        {
            float x = Mathf.Clamp(tau, 0f, mergeTo) / Dt; int i = Mathf.Min((int)x, ang[slot].Count - 2);
            return phi[slot] + Mathf.Lerp(ang[slot][i], ang[slot][i + 1], x - i);
        }

        float Radius(float tau)
        {
            float r = Mathf.Lerp(RingR, WideRadius, Shapes.Ease((tau - mergeTo) / WidenSecs));
            for (int b = 0; b < Breaths; b++) { r -= (WideRadius - SmallRadius) * Show.Bump((tau - breatheFrom - b * BreathSecs) / BreathSecs); }
            return r + (ExitRadius - WideRadius) * Shapes.Ease((tau - stopFrom) / (ReturnSecs * 0.5f));
        }

        Vector2 OnRings(int slot, float t)
        {
            float tau = Mathf.Max(0f, t - FormEnd);
            if (tau >= mergeTo)
            {
                float a2 = baseAngle[slot] + phase0 + turnRate.Integral(tau) - turnRate.Integral(mergeTo);
                return Centre + Radius(tau) * new Vector2(Mathf.Cos(a2), Mathf.Sin(a2));
            }
            float r = Mathf.Lerp(InnerR, OuterR, Outside(slot, tau));
            if (tau >= mergeFrom)
            {
                // the radii draw together only after the rings have passed through each other's angles
                float x = (tau - mergeFrom) / mergeLen;
                r = Mathf.Lerp(r, RingR, Shapes.Ease((x - 0.6f) / 0.4f));
            }
            float a = Angle(slot, tau);
            return Centre + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        public Vector2 Position(int slot, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            Vector2 p = OnRings(slot, t);
            if (t < FormEnd) { p = way != null ? way.Position(slot, t) : Vector2.Lerp(start[slot], p, Shapes.Ease(t / FormEnd)); }
            if (t - FormEnd > stopFrom) { p = Vector2.Lerp(p, line[slot], Shapes.Ease((t - FormEnd - stopFrom) / ReturnSecs)); }
            return p;
        }

        public float Heading(int slot, float t) { return 270f; }
        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int slot, float t) { return false; }
    }
}
