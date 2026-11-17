using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// LLB_cheering_v3 (D06), 3:20. Introduction: the huddle (LLBPlan): the messenger visits, everyone huddles
// round it and argues, two or three look at the participant, and they argue again. Then:
//   1. The huddle breaks into two rows that walk off in opposite directions, a wave running along them,
//      and open apart.
//   2. The rows close into one row, which bends round into a circle that opens into a four-pointed star;
//      the star pulses in a steady rhythm and turns clockwise, then comes to rest.
//   3. In little groups of two or three, all in time with each other: they make a figure and turn it, set
//      off fireworks (the group bursts outward and drifts back) three times, spin on the spot, move round
//      small circles together, and turn the figure back the other way.
//   4. They gather into a ring.
//   Closing (the same in every routine): slowly back into the line; everyone looks at the participant.
public class LLB_cheering_v3 : SwarmRoutine
{
    public override string Name { get { return "LLB_cheering_v3"; } }
    public override string Code { get { return "D06"; } }
    public override string Note { get { return "Huddle, rows, star, groups"; } }
    public override int MinRobots { get { return 8; } }   // planned for 8

    public override IEnumerator Run(RoutineContext ctx) { return LLBShow.Run(ctx, start => new Plan(start), 100); }

    public class Plan : LLBPlan
    {
        // 1. two rows
        const float RowAY = 195f, RowBY = 325f, RowsUnitsPerSec = 34f, WalkSecs = 8f, Walk = 36f, OpenA = 50f, OpenB = 12f, WaveAmp = 16f, WavePeriod = 3f;
        // 2. one row, bending into a rhythmic circle
        const float MergeY = 255f, MergeSpacing = 48f, MergeUnitsPerSec = 34f, BendSecs = 10f, BeatAmp = 10f, BeatPeriod = 1.2f, BeatLag = 0.15f;
        const int CircleTurns = 2;
        const float StarSecs = 4f, StarTip = 100f, StarInner = 56f, StarBeat = 0.07f;
        // 3. groups
        const float ToGroupsUnitsPerSec = 34f, GroupR = 30f, BurstR = 62f, BurstOut = 0.8f, BurstHold = 0.6f, BurstBack = 2.0f, BurstRest = 0.6f;
        const int Bursts = 3;
        const float SpinSecs = 3f, SpinSpeed = 30f, CircleR = 16f, CircleLoops = 2f, Figure2Secs = 5f;
        // 4. to the closing ring
        const float HomeUnitsPerSec = 34f;

        readonly bool[] rowA;
        readonly int[] rowIndex, mergeIndex;
        readonly Vector2[] rowStart, rowEnd, merged, groupCentre;
        readonly float[] vertexDeg;
        readonly int[] groupOf;
        readonly V2Show.WayToLine toRows, merge, toGroups, home;
        readonly float rowsAt, walkAt, mergeAt, bendAt, circleEnd, groupsAt, figureAt, burstAt, spinAt, circleAt, figure2At, homeAt;
        readonly float figureSecs, circleSecs, ringR;
        readonly Show.Eased spinRate;

        public Plan(Vector2[] start) : base(start, Intro.Huddle)
        {
            // the two rows: every other line spot's x, one row above the huddle, one below; each robot takes the
            // nearest free place in them, and later the nearest place in the one row
            var rowSpots = Enumerable.Range(0, n).Select(k => new Vector2(line[k].x, k % 2 == 0 ? RowAY : RowBY)).ToArray();
            var rs = Swarm.LeastTravel(MiddleStarts(), rowSpots);
            rowA = new bool[n]; rowStart = new Vector2[n]; rowIndex = new int[n];
            for (int i = 0; i < n; i++) { rowStart[i] = rowSpots[rs[i]]; rowA[i] = rs[i] % 2 == 0; rowIndex[i] = rs[i] / 2; }
            rowEnd = Enumerable.Range(0, n).Select(i => RowWalk(i, 1f)).ToArray();
            var mergeSpots = Enumerable.Range(0, n).Select(k => new Vector2(Swarm.Centre + (k - (n - 1) / 2f) * MergeSpacing, MergeY)).ToArray();
            mergeIndex = Swarm.LeastTravel(rowEnd, mergeSpots);
            merged = Enumerable.Range(0, n).Select(i => mergeSpots[mergeIndex[i]]).ToArray();
            ringR = n * MergeSpacing / (2f * Mathf.PI);

            // the groups: three for 7-8 robots, two for fewer
            var sizes = new List<int>();
            int g = n >= 7 ? 3 : 2, left = n;
            for (int k = 0; k < g; k++) { int s = Mathf.CeilToInt(left / (float)(g - k)); sizes.Add(s); left -= s; }
            var centres = g == 3 ? new[] { new Vector2(150f, 180f), new Vector2(350f, 180f), new Vector2(250f, 312f) }
                                 : new[] { new Vector2(150f, 240f), new Vector2(350f, 240f) };
            var vertices = new List<Vector2>(); var vertexGroup = new List<int>(); var vertexAngle = new List<float>();
            for (int k = 0; k < g; k++)
            {
                for (int j = 0; j < sizes[k]; j++)
                {
                    float a = (sizes[k] == 2 ? 0f : -90f) + 360f * j / sizes[k];
                    vertices.Add(LLB.On(centres[k], a, GroupR)); vertexGroup.Add(k); vertexAngle.Add(a);
                }
            }
            var ringAtRest = Enumerable.Range(0, n).Select(i => StarSpot(i, 1f, 0f)).ToArray();   // where the star comes to rest
            var slot = Swarm.LeastTravel(ringAtRest, vertices.ToArray());
            groupOf = new int[n]; vertexDeg = new float[n]; groupCentre = new Vector2[n];
            for (int i = 0; i < n; i++) { groupOf[i] = vertexGroup[slot[i]]; vertexDeg[i] = vertexAngle[slot[i]]; groupCentre[i] = centres[groupOf[i]]; }
            var vertexOf = Enumerable.Range(0, n).Select(i => vertices[slot[i]]).ToArray();

            toRows = LLB.Way(MiddleStarts(), rowStart, RowsUnitsPerSec);
            merge = LLB.Way(rowEnd, merged, MergeUnitsPerSec);
            toGroups = LLB.Way(ringAtRest, vertexOf, ToGroupsUnitsPerSec);
            home = LLB.Way(vertexOf, RingSpots(), HomeUnitsPerSec);   // the ring the closing starts from

            rowsAt = MiddleAt;
            walkAt = rowsAt + toRows.Total;
            mergeAt = walkAt + WalkSecs + 0.5f;
            bendAt = mergeAt + merge.Total;
            homeAt = CloseAt - home.Total;
            float groupFixed = Bursts * (BurstOut + BurstHold + BurstBack + BurstRest) + SpinSecs + Figure2Secs;
            float free = homeAt - bendAt - BendSecs - toGroups.Total - groupFixed;
            float turning = 0.45f * free;   // the circle's turning; the rest goes to the groups' figure and small circles
            figureSecs = 0.55f * free * 0.5f; circleSecs = 0.55f * free * 0.5f;
            circleEnd = bendAt + BendSecs + turning;
            groupsAt = circleEnd;
            figureAt = groupsAt + toGroups.Total;
            burstAt = figureAt + figureSecs;
            spinAt = burstAt + Bursts * (BurstOut + BurstHold + BurstBack + BurstRest);
            circleAt = spinAt + SpinSecs;
            figure2At = circleAt + circleSecs;
            var unit = new Show.Eased(bendAt + 4f, 0f, bendAt + BendSecs, 1f, circleEnd - 4f, 1f, circleEnd, 0f, circleEnd + 10000f, 0f);
            float rate = CircleTurns * 360f / (unit.Integral(circleEnd) - unit.Integral(bendAt + 4f));
            spinRate = new Show.Eased(bendAt + 4f, 0f, bendAt + BendSecs, rate, circleEnd - 4f, rate, circleEnd, 0f, circleEnd + 10000f, 0f);

            for (int i = 0; i < n; i++)
            {
                Spin(i, spinAt, SpinSecs, (groupOf[i] % 2 == 0 ? 1f : -1f) * SpinSpeed);
            }
            Section(rowsAt, "the huddle breaks into two rows");
            Section(walkAt, "the rows walk apart in a wave");
            Section(mergeAt, "one row");
            Section(bendAt, "the row bends into a circle, which opens into a star: steady pulse, turning clockwise");
            Section(groupsAt, "into little groups");
            Section(figureAt, "figures turning");
            Section(burstAt, "fireworks");
            Section(spinAt, "spinning on the spot");
            Section(circleAt, "small circles together");
            Section(figure2At, "the figures turn back");
            Section(homeAt, "to the closing ring");
        }

        Vector2 RowWalk(int i, float u)
        {
            float e = Shapes.Ease(u);
            return rowA[i] ? rowStart[i] + new Vector2(Walk * e, -OpenA * e) : rowStart[i] + new Vector2(-Walk * e, OpenB * e);
        }

        // The merged row bent `bend` of the way into the circle (0 = straight), with an outward beat of size `beat`
        Vector2 Bent(int i, float bend, float beat)
        {
            float k = bend / ringR;
            float s = (mergeIndex[i] - (n - 1) / 2f) * MergeSpacing, ks = k * s;
            Vector2 along = Mathf.Abs(ks) < 0.1f
                ? new Vector2(s * (1f - ks * ks / 6f), -s * (ks / 2f - ks * ks * ks / 24f))
                : new Vector2(Mathf.Sin(ks) / k, -(1f - Mathf.Cos(ks)) / k);
            Vector2 outward = new Vector2(Mathf.Sin(ks), Mathf.Cos(ks));
            return new Vector2(Swarm.Centre, MergeY) + along + outward * beat;
        }

        // Robot i on the closed circle grown `star` of the way into the star (even places the tips), pushed
        // out by `beat` mat units (before any turning)
        Vector2 StarSpot(int i, float star, float beat)
        {
            Vector2 c = new Vector2(Swarm.Centre, MergeY - ringR);
            Vector2 d = Bent(i, 1f, 0f) - c;
            float r = Mathf.Lerp(ringR, mergeIndex[i] % 2 == 0 ? StarTip : StarInner, star);
            return c + d.normalized * (r + beat);
        }

        Vector2 Circle(int i, float t)
        {
            float bend = Shapes.Ease((t - bendAt) / BendSecs);
            float beatEnv = LLB.Envelope(t, bendAt + 2f, 3f, circleEnd - 5f, 4f);
            float pulse = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * ((t - bendAt) / BeatPeriod - BeatLag * mergeIndex[i]));
            float star = Shapes.Ease((t - bendAt - BendSecs) / StarSecs);
            float beat = beatEnv * pulse * Mathf.Lerp(BeatAmp, StarBeat * (mergeIndex[i] % 2 == 0 ? StarTip : StarInner) * 2f, star);
            Vector2 p = star > 0f ? StarSpot(i, star, beat) : Bent(i, bend, beat);
            // turning clockwise round the circle's centre (it starts once the circle is nearly closed)
            Vector2 c = new Vector2(Swarm.Centre, MergeY - ringR);
            float a = (spinRate.Integral(t) - spinRate.Integral(bendAt + 4f)) * Mathf.Deg2Rad;
            Vector2 d = p - c;
            return c + new Vector2(d.x * Mathf.Cos(a) - d.y * Mathf.Sin(a), d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a));
        }

        Vector2 Group(int i, float t)
        {
            float deg = vertexDeg[i] + 360f * Shapes.Ease((t - figureAt) / figureSecs) - 360f * Shapes.Ease((t - figure2At) / Figure2Secs);
            float r = GroupR;
            float burstSecs = BurstOut + BurstHold + BurstBack + BurstRest, u = t - burstAt;
            if (u > 0f && u < Bursts * burstSecs)
            {
                float v = Mathf.Repeat(u, burstSecs);
                r += (BurstR - GroupR) * Shapes.Ease(v / BurstOut) * (1f - Shapes.Ease((v - BurstOut - BurstHold) / BurstBack));
            }
            Vector2 c = groupCentre[i];
            if (t > circleAt && t < figure2At)
            {
                float a = 2f * Mathf.PI * CircleLoops * Shapes.Ease((t - circleAt) / circleSecs);
                c += CircleR * new Vector2(Mathf.Sin(a), 1f - Mathf.Cos(a)) * (groupCentre[i].y > 250f ? -1f : 1f);
            }
            return LLB.On(c, deg, r);
        }

        protected override Vector2 Middle(int i, float t)
        {
            if (t < walkAt) { return toRows.Position(i, t - rowsAt); }
            if (t < mergeAt)
            {
                float u = (t - walkAt) / WalkSecs;
                Vector2 p = RowWalk(i, u);
                float w = WaveAmp * Show.Bump(Mathf.Clamp01(u)) * 1.3f;
                return p + new Vector2(0f, -w * Mathf.Sin(2f * Mathf.PI * (t - walkAt) / WavePeriod - 0.9f * rowIndex[i] * (rowA[i] ? 1f : -1f)));
            }
            if (t < bendAt) { return merge.Position(i, t - mergeAt); }
            if (t < groupsAt) { return Circle(i, t); }
            if (t < figureAt) { return toGroups.Position(i, t - groupsAt); }
            if (t < homeAt) { return Group(i, t); }
            return home.Position(i, t - homeAt);
        }
    }
}
