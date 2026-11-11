using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// v2 Kaleidoscope. One outline that never stops turning: circle -> triangle -> square, again and
// again, ending on the square. Every robot has a fixed place in perimeter order. Each shape is a
// set of points around the same centre, in a frame that turns with one shared rotation angle;
// a change of shape moves each robot, in that turning frame, straight from its point on one
// outline to its nearby point on the next, so the next shape appears while the group keeps turning.
// Triangle: the tips are the ones of the first flower (commit ccb858b, Shapes.Flower): robots on
// the turning ring move out to 1.45x the ring radius, blooming over 5 s. In that flower the robots
// between the tips moved in to 0.55x, which made the sides bow inward; here they go onto the
// straight sides between the tips instead (new), so the triangle has three corners and straight sides.
// Square: robots on the four corners and along four equal straight sides.
// Ending: the square's last part of a turn brings its robots evenly either side of the middle.
// The square folds into two close, parallel columns at the centre of the mat, their inner ends
// at the midpoint of the final line: the closed book ||. Both halves then sweep outward around
// those inner ends, through a V with its point at the centre, until they lie left and right as
// one straight line __ on the line spots. The fold runs straight into the sweep.
public abstract class V2KaleidoscopeBase : SwarmRoutine
{
    protected abstract float Minutes { get; }
    public override string Name { get { return "v2 Kaleidoscope (" + V2Show.Label(Minutes) + ")"; } }
    public override int MinRobots { get { return 2; } }
    public override IEnumerator Run(RoutineContext ctx) { return V2Show.Run(ctx, start => V2Kaleidoscope.For(start, Minutes), maxSpeed: 70); }
}
public class V2KaleidoscopeNatural : V2KaleidoscopeBase { protected override float Minutes { get { return 0f; } } }
public class V2Kaleidoscope3Min : V2KaleidoscopeBase { protected override float Minutes { get { return 3f; } } }
public class V2Kaleidoscope5Min : V2KaleidoscopeBase { protected override float Minutes { get { return 5f; } } }

public static class V2Kaleidoscope
{
    // ---- Shapes ----
    const float Radius = 78f;              // circle
    const float Tip = 1.45f;               // triangle tips at 1.45x the circle (the first flower)
    const float SquareHalfSide = 74f;      // corners 105 from the centre

    // ---- Turning and timing ----
    const float TurnDegPerSec = 18f;       // the first flower turned at 20
    const float FormSecs = 5f;             // line -> circle, turning as it forms
    const float CircleSecs = 4.5f, TriangleSecs = 7f, SquareSecs = 6f;   // each finished shape, turning
    const float BloomSecs = 5f;            // circle -> triangle (the first flower's bloom time)
    const float ChangeSecs = 4.5f;         // triangle -> square, square -> circle

    // ---- The book ----
    const float CloseSecs = 4.5f;          // square -> closed book, while the turn dies away
    const float OpenSecs = 6f;             // the halves sweep open into the line...
    const float OpenOverlap = 1f;          // ...starting this long before the book has quite closed

    const float CycleSecs = CircleSecs + BloomSecs + TriangleSecs + ChangeSecs + SquareSecs;

    static float Duration(int cycles)
    {
        // the exit turn adds up to (360 / robots) degrees; count half of the 8 robot case
        return FormSecs + cycles * CycleSecs + (cycles - 1) * ChangeSecs + 1.25f + CloseSecs - OpenOverlap + OpenSecs;
    }

    public static Plan For(Vector2[] start, float minutes)
    {
        if (minutes <= 0f) { return new Plan(start, 2, 0f); }
        float budget = V2Show.Budget(minutes);
        int cycles = 2;
        while (Duration(cycles + 1) <= budget) { cycles++; }
        // What is left (less than a cycle) is shared out over the finished shapes' turning time
        return new Plan(start, cycles, Mathf.Max(0f, budget - Duration(cycles)) / (3f * cycles));
    }

    public class Plan : V2Show.IPlan
    {
        readonly int n;
        readonly Vector2[] start, spot;
        readonly int[] slot;                // place in perimeter order
        readonly float first;               // world angle of place 0 before any turning
        readonly float[] fromAngle, fromRadius, swing;
        readonly Show.Eased turnRate;       // radians per second
        readonly Vector2[][] shape = new Vector2[3][];   // per shape: each place's point in the turning frame
        readonly List<float[]> parts = new List<float[]>();   // from, to (seconds), shape before, shape after
        readonly float bookFrom, openFrom;
        readonly float[] armX, armReach, armSide;   // the book: inner end, distance along the half, left/right
        public float Total { get; private set; }

        public Plan(Vector2[] start, int cycles, float longerHold)
        {
            this.start = start;
            n = start.Length;
            float rate = TurnDegPerSec * Mathf.Deg2Rad;
            float formed = rate * FormSecs / 2f;   // turned while forming (the turn eases in)
            slot = Show.RingSlots(start, Radius, formed, out first);

            shape[0] = Enumerable.Range(0, n).Select(k => Radius * Unit(2f * Mathf.PI * k / n)).ToArray();
            shape[1] = n < 3 ? shape[0] : Nearest(3, Radius * Tip, shape[0]);
            shape[2] = n < 3 ? shape[0] : Nearest(4, SquareHalfSide * Mathf.Sqrt(2f), shape[1]);

            float t = FormSecs;
            for (int c = 0; c < cycles; c++)
            {
                t = Part(t, CircleSecs + longerHold, 0, 0); t = Part(t, BloomSecs, 0, 1);
                t = Part(t, TriangleSecs + longerHold, 1, 1); t = Part(t, ChangeSecs, 1, 2);
                t = Part(t, SquareSecs + longerHold, 2, 2);
                if (c < cycles - 1) { t = Part(t, ChangeSecs, 2, 0); }
            }
            // The square's last part of a turn lines up the exit: a gap between two robots at the
            // top, so the robots stand evenly either side of the middle (with an odd number, one
            // at the bottom, on the middle). The turn also carries on a little while the book closes.
            float afterBook = rate * CloseSecs / 2f;
            float turnedSoFar = formed + rate * (t - FormSecs);
            float extra = Mathf.Repeat(-Mathf.PI / 2f - Mathf.PI / n - (first + turnedSoFar + afterBook), 2f * Mathf.PI / n);
            t = Part(t, extra / rate, 2, 2);
            bookFrom = t;
            openFrom = bookFrom + CloseSecs - OpenOverlap;
            Total = openFrom + OpenSecs;
            turnRate = new Show.Eased(0f, 0f, FormSecs, rate, bookFrom, rate, bookFrom + CloseSecs, 0f);

            var atEnd = new float[n];
            for (int i = 0; i < n; i++) { atEnd[i] = first + 2f * Mathf.PI * slot[i] / n + turnRate.Integral(FormSecs); }
            Show.SwingOnto(start, atEnd, out fromAngle, out fromRadius, out swing);

            // The book. The robots left of the middle (as the square stops) make the left half, the
            // others the right half. In each half they keep their order: the lowest robot takes
            // the inner end (the line spot next to the middle) and the highest the far end.
            var square = new Vector2[n];
            for (int i = 0; i < n; i++) { square[i] = Shapes.Centre + Turn(shape[2][slot[i]], first + turnRate.Integral(Total)); }
            var line = LineUp.Spots(n).Select(s => (Vector2)s).ToArray();
            var byX = Enumerable.Range(0, n).OrderBy(i => square[i].x).ToArray();
            spot = new Vector2[n]; armX = new float[n]; armReach = new float[n]; armSide = new float[n];
            int half = n / 2;
            for (int side = -1; side <= 1; side += 2)
            {
                var robots = (side < 0 ? byX.Take(half) : byX.Skip(n - half)).OrderByDescending(i => square[i].y).ToArray();
                var spots = (side < 0 ? line.Take(half).Reverse() : line.Skip(n - half)).ToArray();   // inner end first
                for (int k = 0; k < robots.Length; k++)
                {
                    int i = robots[k];
                    spot[i] = spots[k]; armX[i] = spots[0].x; armReach[i] = Mathf.Abs(spots[k].x - spots[0].x); armSide[i] = side;
                }
            }
            if (n % 2 == 1) { int i = byX[half]; spot[i] = line[half]; armX[i] = line[half].x; }
        }

        float Part(float from, float secs, int before, int after)
        {
            parts.Add(new[] { from, from + secs, before, after });
            return from + secs;
        }

        static Vector2 Unit(float a) { return new Vector2(Mathf.Cos(a), Mathf.Sin(a)); }
        static Vector2 Turn(Vector2 v, float a)
        {
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
        }

        // A polygon with `corners` corners at `reach` from the centre, turned (within the frame) to
        // where the robots have the least to travel from the outline they are on
        Vector2[] Nearest(int corners, float reach, Vector2[] from)
        {
            Vector2[] best = null; float bestCost = float.MaxValue;
            for (int step = 0; step < 360; step++)
            {
                var p = Polygon(corners, reach, step * Mathf.Deg2Rad);
                float cost = 0f;
                for (int k = 0; k < n; k++) { cost += (p[k] - from[k]).sqrMagnitude; }
                if (cost < bestCost) { bestCost = cost; best = p; }
            }
            return best;
        }

        // Each place's point on the polygon, in perimeter order. With enough robots, `corners` of
        // them stand on the corners and the others evenly along the straight sides between them.
        Vector2[] Polygon(int corners, float reach, float turned)
        {
            var p = new Vector2[n];
            System.Func<int, Vector2> corner = j => reach * Unit(turned + 2f * Mathf.PI * j / corners);
            for (int k = 0; k < n; k++)
            {
                int j; float u;
                if (n < corners) { float along = k * corners / (float)n; j = Mathf.FloorToInt(along); u = along - j; }
                else
                {
                    j = corners - 1;
                    while (CornerPlace(j, corners) > k) { j--; }
                    u = (k - CornerPlace(j, corners)) / (float)(CornerPlace(j + 1, corners) - CornerPlace(j, corners));
                }
                p[k] = Vector2.Lerp(corner(j), corner(j + 1), u);
            }
            return p;
        }

        int CornerPlace(int j, int corners) { return Mathf.FloorToInt(j * n / (float)corners + 0.5f); }

        // Robot i's point in the turning frame at t, part way through a change if one is under way
        Vector2 Framed(int i, float t)
        {
            foreach (var p in parts)
            {
                if (t < p[1] || p == parts[parts.Count - 1])
                {
                    float e = p[2] == p[3] ? 0f : Shapes.Ease((t - p[0]) / (p[1] - p[0]));
                    return Vector2.Lerp(shape[(int)p[2]][slot[i]], shape[(int)p[3]][slot[i]], e);
                }
            }
            return shape[0][slot[i]];
        }

        // Which shape is complete at t (0 circle, 1 triangle, 2 square), or -1 during a change
        public int ShapeAt(float t)
        {
            if (t < FormSecs || t > bookFrom) { return -1; }
            foreach (var p in parts) { if (t < p[1]) { return p[2] == p[3] ? (int)p[2] : -1; } }
            return 2;
        }
        public float BookFrom { get { return bookFrom; } }
        public float OpenFrom { get { return openFrom; } }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            Vector2 p;
            if (t < FormSecs)
            {
                // Forming: swing round the centre onto the circle, turning with it from the start
                float e = Shapes.Ease(t / FormSecs);
                float angle = fromAngle[i] + e * (swing[i] + turnRate.Integral(t) - turnRate.Integral(FormSecs));
                p = Shapes.Centre + Mathf.Lerp(fromRadius[i], Radius, e) * Unit(angle);
            }
            else { p = Shapes.Centre + Turn(Framed(i, t), first + turnRate.Integral(t)); }
            if (t > bookFrom)
            {
                // Fold into the closed book, and sweep the halves open around their inner ends
                float open = Mathf.PI / 2f * Shapes.Ease((t - openFrom) / OpenSecs);
                var book = new Vector2(armX[i] + armSide[i] * armReach[i] * Mathf.Sin(open), Swarm.Centre - armReach[i] * Mathf.Cos(open));
                p = Vector2.Lerp(p, book, Shapes.Ease((t - bookFrom) / CloseSecs));
            }
            return p;
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
        public bool InLane(int i, float t) { return false; }
    }
}
