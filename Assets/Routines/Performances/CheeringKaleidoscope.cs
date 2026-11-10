using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Cheering - Kaleidoscope. A shape that never stops turning. After the Messenger's introduction
// the robots swing onto a compact circle that turns as one rigid outline. Twice over it holds as a
// circle, grows three corners, turns as a triangle, grows a fourth corner, turns as a square and
// rounds back into the circle; the second time it ends on the square. Every robot keeps its place
// around the centre (same angle in the turning outline), so a change of shape is only each robot
// moving in or out a little: nobody crosses the middle or changes order.
// The triangle is the one from the first Mirror pairs flower (commit ccb858b): robots evenly
// spaced on the turning ring, every other one moving out to 1.45x the radius and the others in
// to 0.55x, which with 6 robots gives three points. With 6 robots it is exactly that; with other
// counts three robots take the same tips (1.45x) and the rest spread along straight sides, each
// shifting a little along the outline.
// The exit is a book closing and opening from the middle. The square's turn brings it round to the
// exit angle and dies away while each half of the square folds into a short column against the
// spine, in the middle of the line: the closed book. The halves then open from the middle, left
// half out to the left and right half out to the right, flattening into the two halves of the
// line. Robots keep their order within each half.
public class CheeringKaleidoscope : SwarmRoutine
{
    public override string Name { get { return "v2 Cheering: kaleidoscope"; } }
    public override int MinRobots { get { return 2; } }

    // ---- Phrases (change the counts freely; the choreography follows) ----
    const int Sequences = 2;               // circle, triangle, square, back to circle; the last ends on the square
    // How far the outline turns in each part of a sequence (degrees)
    const float CircleDeg = 70f;
    const float ChangeDeg = 30f;           // each change of shape: short
    const float TriangleDeg = 100f;
    const float SquareDeg = 100f;

    // ---- Shapes and turning ----
    const float TurnDegPerSec = 16f;       // a little slower than the original flower's 20
    const float FormSecs = 4.5f;           // line -> circle, gliding, turning as it forms
    const float Radius = 58f;              // compact circle (the original used 100)
    const float Petal = 0.45f;             // the original flower: tips out 45%, the rest in 45%
    const float SquareHalfSide = 58f;      // corners 82 from the centre
    const float SideBulge = 8f;            // side robots sit this far outside the straight side: with so few
                                           // robots per side, exactly straight reads as curving in
    const float CloseSecs = 4f;            // square -> closed book, while the turn dies away
    const float OpenSecs = 5f;             // the halves open out into the line...
    const float OpenOverlap = 0.8f;        // ...starting this long before the book has quite closed
    const float BookRowGap = 52f;          // robots' spacing up the closed book's columns

    public override IEnumerator Run(RoutineContext ctx) { return Show.Run(ctx, start => new Plan(start)); }

    public class Plan : Show.IPlan
    {
        readonly int n;
        readonly Vector2[] start, spot;
        readonly int[] slot;            // place around the outline
        readonly float first;           // angle of place 0 (radians)
        readonly float[] fromAngle, fromRadius, swing;
        readonly Show.Eased turnRate;   // radians per second
        readonly float bookFrom, openFrom;
        readonly Vector2[] closed;      // each robot's place in the closed book
        // Shape changes in degrees turned since the circle formed: from, to, shape before, shape after
        readonly List<float[]> parts = new List<float[]>();
        public float Total { get; private set; }

        public Plan(Vector2[] start)
        {
            this.start = start;
            n = start.Length;
            float rate = TurnDegPerSec * Mathf.Deg2Rad;
            float formed = rate * FormSecs / 2f;   // turned while forming (eases in)
            slot = Show.RingSlots(start, Radius, formed, out first);

            // The sequences, end to end (0 circle, 1 triangle, 2 square)
            float d = 0f;
            for (int s = 0; s < Sequences; s++)
            {
                d = Part(d, CircleDeg, 0, 0); d = Part(d, ChangeDeg, 0, 1);
                d = Part(d, TriangleDeg, 1, 1); d = Part(d, ChangeDeg, 1, 2);
                d = Part(d, SquareDeg, 2, 2);
                if (s < Sequences - 1) { d = Part(d, ChangeDeg, 2, 0); }
            }
            // Keep the square turning until it reaches the exit angle: its robots on either side of
            // a vertical spine, evenly (the turn also carries on a little while the book closes)
            float afterBook = rate * CloseSecs / 2f;
            float sideways = Mathf.PI / 2f + Mathf.PI / n;
            float extra = Mathf.Repeat(sideways - (first + formed + d * Mathf.Deg2Rad + afterBook), 2f * Mathf.PI / n);
            d = Part(d, extra * Mathf.Rad2Deg, 2, 2);
            bookFrom = FormSecs + d / TurnDegPerSec;
            openFrom = bookFrom + CloseSecs - OpenOverlap;
            Total = openFrom + OpenSecs;
            turnRate = new Show.Eased(0f, 0f, FormSecs, rate, bookFrom, rate, bookFrom + CloseSecs, 0f);

            var atEnd = new float[n];
            for (int i = 0; i < n; i++) { atEnd[i] = Angle(i, FormSecs); }
            Show.SwingOnto(start, atEnd, out fromAngle, out fromRadius, out swing);

            // The book. Halves: the left robots of the square (at the exit angle) take the left half
            // of the line, the right ones the right half, a robot on the spine the middle spot.
            // Within a half the robots keep their order top to bottom in the closed book's column.
            // Opening, the robots nearest the line take the spots nearest the spine and the column's
            // two ends the far ones, so each half opens out from the middle, evenly above and below.
            var square = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = Angle(i, Total);
                square[i] = Shapes.Centre + RadiusOf(2, Frame(i)) * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            var line = LineUp.Spots(n);
            var byX = Enumerable.Range(0, n).OrderBy(i => square[i].x).ToArray();
            spot = new Vector2[n]; closed = new Vector2[n];
            int halfCount = n / 2;
            for (int side = -1; side <= 1; side += 2)
            {
                var half = (side < 0 ? byX.Take(halfCount) : byX.Skip(n - halfCount)).OrderBy(i => square[i].y).ToArray();
                // this half's spots along the line, outermost first
                var spots = (side < 0 ? line.Take(halfCount) : line.Skip(n - halfCount).Reverse()).ToArray();
                float columnX = spots[spots.Length - 1].x;   // the spot nearest the spine
                for (int k = 0; k < half.Length; k++)
                {
                    int i = half[k];
                    float offset = (k - (half.Length - 1) / 2f) * BookRowGap;
                    closed[i] = new Vector2(columnX, Swarm.Centre + offset);
                }
                // nearest the line first (upper one first on a tie) -> spots from the spine outward
                var outward = Enumerable.Range(0, half.Length).OrderBy(k => Mathf.Abs(k - (half.Length - 1) / 2f)).ThenBy(k => k).ToArray();
                for (int r = 0; r < outward.Length; r++) { spot[half[outward[r]]] = spots[spots.Length - 1 - r]; }
            }
            if (n % 2 == 1) { int i = byX[halfCount]; spot[i] = closed[i] = line[halfCount]; }
        }

        float Part(float from, float deg, int before, int after)
        {
            parts.Add(new[] { from, from + deg, before, after });
            return from + deg;
        }

        float Frame(int i) { return 2f * Mathf.PI * slot[i] / n; }               // angle within the outline
        float Angle(int i, float t) { return first + Frame(i) + turnRate.Integral(t); }

        // Distance from the centre of the outline at angle a (within the outline)
        static float RadiusOf(int shape, float a)
        {
            if (shape == 1) { return Radius * (1f + Petal * Mathf.Cos(3f * a)); }
            if (shape == 2) { return SquareHalfSide / Mathf.Max(Mathf.Abs(Mathf.Cos(a)), Mathf.Abs(Mathf.Sin(a))); }
            return Radius;
        }

        static int Round(float v) { return Mathf.FloorToInt(v + 0.5f); }

        // Place k of a shape within the turning outline, as (angle, distance from the centre)
        Vector2 Place(int shape, int k)
        {
            float a = 2f * Mathf.PI * k / n;
            if (shape != 1 || n == 6 || n < 3) { return new Vector2(a, RadiusOf(shape, a)); }
            // Triangle for counts other than 6: tips taken by three robots, the rest evenly along
            // the straight sides between them
            int[] tip = { 0, Round(n / 3f), Round(2f * n / 3f), n };
            int j = k >= tip[2] ? 2 : (k >= tip[1] ? 1 : 0);
            float tipR = Radius * (1f + Petal), a0 = 2f * Mathf.PI * j / 3f, a1 = 2f * Mathf.PI * (j + 1) / 3f;
            float u = (k - tip[j]) / (float)(tip[j + 1] - tip[j]);
            Vector2 p = Vector2.Lerp(tipR * new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), tipR * new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)), u);
            float mid = (a0 + a1) / 2f;
            p += SideBulge * Mathf.Sin(Mathf.PI * u) * new Vector2(Mathf.Cos(mid), Mathf.Sin(mid));
            float pa = Mathf.Atan2(p.y, p.x);
            while (pa < a0 - 0.001f) { pa += 2f * Mathf.PI; }
            return new Vector2(pa, p.magnitude);
        }

        // Robot i's place within the outline at time t, part way through a change if one is under way
        Vector2 PlaceAt(int i, float t)
        {
            float turnedDeg = (turnRate.Integral(t) - turnRate.Integral(FormSecs)) * Mathf.Rad2Deg;
            foreach (var p in parts)
            {
                if (turnedDeg < p[1] || p == parts[parts.Count - 1])
                {
                    float e = p[2] == p[3] ? 0f : Shapes.Ease((turnedDeg - p[0]) / (p[1] - p[0]));
                    return Vector2.Lerp(Place((int)p[2], slot[i]), Place((int)p[3], slot[i]), e);
                }
            }
            return Place(0, slot[i]);
        }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            Vector2 place = PlaceAt(i, t);
            float angle = first + place.x + turnRate.Integral(t), radius = place.y;
            if (t < FormSecs)
            {
                // Forming: swing round the centre onto the circle, turning with it from the start
                float e = Shapes.Ease(t / FormSecs);
                angle = fromAngle[i] + e * (swing[i] + turnRate.Integral(t) - turnRate.Integral(FormSecs));
                radius = Mathf.Lerp(fromRadius[i], Radius, e);
            }
            Vector2 p = Shapes.Centre + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            if (t > bookFrom)
            {
                // Close into the column against the spine; open out from the middle into the line
                Vector2 book = Vector2.Lerp(closed[i], spot[i], Shapes.Ease((t - openFrom) / OpenSecs));
                p = Vector2.Lerp(p, book, Shapes.Ease((t - bookFrom) / CloseSecs));
            }
            return p;
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
    }
}
