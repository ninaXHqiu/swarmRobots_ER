using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// v2 Small Circles (C16): the optical-illusion feeling of many identical motions at different paces.
//   1. After the standard opening each robot goes to a cell of a 4 x 2 grid and walks a small circle
//      there. Every cell is the same idea; each circle has its own size and speed, neighbours turn
//      opposite ways, so side by side some are fast, some slow, some wide, some tight. No pauses.
//   2. The circles hold for a long stretch, then the cells draw into one ring: the ring turns, breathes
//      (large, slow) several times and then opens and flattens back into the line.
public class V2SmallCircles : SwarmRoutine
{
    public override string Name { get { return "v2 Small Circles"; } }
    public override string Code { get { return "C16"; } }
    public override string Note { get { return "Own small circles, then ring"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return V2Show.Run(ctx, start => new Plan(start), 100);
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading
    {
        const float GoSecs = 9f, RingFrom = 74f;
        const float SectorRadius = 118f, FormSecs = 12f, TurnUpSecs = 6f, RingRate0 = 0.11f;
        const float WideRadius = 128f, SmallRadius = 52f, WidenSecs = 12f, BreathSecs = 20f;
        const int Breaths = 4;
        const float ReturnSecs = 12f, ExitRadius = 106f;
        static readonly Vector2 Centre = new Vector2(250f, 250f);
        // the grid cells: top row left to right, bottom row left to right
        static readonly Vector2[] Cells =
        {
            new Vector2(110f, 185f), new Vector2(203f, 185f), new Vector2(297f, 185f), new Vector2(390f, 185f),
            new Vector2(110f, 315f), new Vector2(203f, 315f), new Vector2(297f, 315f), new Vector2(390f, 315f),
        };
        static readonly float[] CellR = { 16f, 30f, 22f, 30f, 28f, 18f, 30f, 24f };       // circle radius per cell
        static readonly float[] CellP = { 3.2f, 6.5f, 4.4f, 9f, 5f, 3.6f, 7.5f, 4.8f };   // seconds per lap per cell

        readonly Vector2[] start, line;
        readonly float[] sectorTo = new float[8];
        readonly int[] cellOf = new int[8];
        readonly float breatheFrom, stopFrom, ringStop;
        Show.Eased turnRate;
        public float Total { get; private set; }
        public float RingAt { get { return RingFrom; } }
        public float BreatheAt { get { return breatheFrom; } }
        public float ReturnAt { get { return stopFrom; } }

        public Plan(Vector2[] start)
        {
            this.start = start;
            line = V2Show.OwnSpots(start.Length);
            var order = V2Show.RingOrder(8);
            for (int i = 0; i < 8; i++) { sectorTo[i] = (135f + 45f * order[i]) * Mathf.Deg2Rad; }
            // each robot takes the cell that lies in the direction of its place on the ring, so that cells and ring don't cross
            var cellsByAngle = Enumerable.Range(0, 8).OrderBy(c => Mathf.Repeat(Mathf.Atan2(Cells[c].y - Centre.y, Cells[c].x - Centre.x), 2f * Mathf.PI)).ToArray();
            var slotsByAngle = Enumerable.Range(0, 8).OrderBy(s => Mathf.Repeat(sectorTo[s], 2f * Mathf.PI)).ToArray();
            for (int k = 0; k < 8; k++) { cellOf[slotsByAngle[k]] = cellsByAngle[k]; }

            breatheFrom = RingFrom + FormSecs + WidenSecs;
            stopFrom = breatheFrom + Breaths * BreathSecs;
            ringStop = stopFrom + ReturnSecs;
            float tMid = stopFrom + ReturnSecs / 2f;
            Show.Eased RateFor(float c) { return new Show.Eased(RingFrom, 0f, RingFrom + TurnUpSecs, RingRate0, breatheFrom, c, stopFrom, c, ringStop, 0f); }
            float unused; float opensAt = V2Show.BestAngle(order, 1, ExitRadius, line, out unused);
            float needed = Mathf.Repeat(opensAt - 135f * Mathf.Deg2Rad, 2f * Mathf.PI);
            float omega0 = RateFor(0f).Integral(tMid) - RateFor(0f).Integral(RingFrom);
            float perUnit = RateFor(1f).Integral(tMid) - RateFor(1f).Integral(RingFrom) - omega0;
            float chosen = 7f * Mathf.Deg2Rad;
            for (int k = 0; k < 40; k++)
            {
                float c = (needed + 2f * Mathf.PI * k - omega0) / perUnit;
                if (c >= 4f * Mathf.Deg2Rad && c <= 9f * Mathf.Deg2Rad) { chosen = c; break; }
            }
            turnRate = RateFor(chosen);
            Total = ringStop + 0.3f;
        }

        // a robot's own small circle: the circle passes through the cell's centre-line start so the entry is smooth
        Vector2 Circle(int slot, float t)
        {
            int c = cellOf[slot];
            float dir = (c % 4 + c / 4) % 2 == 0 ? 1f : -1f;                  // checkerboard of directions
            float a = dir * 2f * Mathf.PI * t / CellP[c] + 0.9f * c;
            return Cells[c] + CellR[c] * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        float Ring(float t) { return turnRate.Integral(t) - turnRate.Integral(RingFrom); }

        float Radius(float t)
        {
            float r = Mathf.Lerp(SectorRadius, WideRadius, Shapes.Ease((t - RingFrom - FormSecs) / WidenSecs));
            for (int b = 0; b < Breaths; b++) { r -= (WideRadius - SmallRadius) * Show.Bump((t - breatheFrom - b * BreathSecs) / BreathSecs); }
            return r + (ExitRadius - WideRadius) * Shapes.Ease((t - stopFrom) / (ReturnSecs * 0.5f));
        }

        Vector2 Sector(int slot, float t)
        {
            float angle = sectorTo[slot] + Ring(t);
            return Centre + Radius(t) * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        public Vector2 Position(int slot, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            Vector2 p = Circle(slot, t);
            // the circle shrinks toward its cell centre as the ring forms, so the entry into the ring is smooth
            float b = Shapes.Ease((t - RingFrom) / FormSecs);
            if (b > 0f) { p = Cells[cellOf[slot]] + (p - Cells[cellOf[slot]]) * (1f - b); }
            float go = Shapes.Ease((t - 0.6f * slot) / GoSecs);
            if (go < 1f) { p = Vector2.Lerp(start[slot], p, go); }
            if (b > 0f) { p = Vector2.Lerp(p, Sector(slot, t), b); }
            if (t > stopFrom) { p = Vector2.Lerp(p, line[slot], Shapes.Ease((t - stopFrom) / ReturnSecs)); }
            return p;
        }

        public float Heading(int slot, float t) { return 270f; }
        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int slot, float t) { return false; }
    }
}
