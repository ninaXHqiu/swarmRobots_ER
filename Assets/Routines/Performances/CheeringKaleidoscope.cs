using System.Collections;
using UnityEngine;

// Cheering - Kaleidoscope. After the Messenger's introduction the robots swing onto a centred
// circle that eases into a slow, shared turn. Still turning, it grows into a triangle (the three-
// petal flower of the original Mirror pairs routine, with robots on the three tips), then into a
// square with four equal sides and robots on its corners. Robots keep their order around the
// outline, so every change is a short shift to a nearby place, and each finished shape turns for
// several seconds. The turn ends with the square upright; at 30 s it opens like a book: its left
// and right halves sweep out from the middle and straighten into the two halves of the line.
public class CheeringKaleidoscope : SwarmRoutine
{
    public override string Name { get { return "v2 Cheering: kaleidoscope"; } }
    public override int MinRobots { get { return 2; } }

    // ---- Timing (seconds after the introduction; the book opens at 30) ----
    const float FormSecs = 4f;        // line -> circle
    const float TriangleFrom = 7f;    // circle grows three corners...
    const float TriangleTo = 10.5f;   // ...a finished triangle until...
    const float SquareFrom = 17f;     // ...it turns into a square...
    const float SquareTo = 20.5f;     // ...which turns, finished, until the book opens
    const float TurnUpSecs = 4f;      // turning eases in...
    const float TurnDownSecs = 4f;    // ...and out after 30 s, carried into the opening
    const float AboutDeg = 360f;      // about one full turn, adjusted so the square ends upright

    // ---- Shapes (compact, centred) ----
    const float CircleRadius = 85f;
    const float TriangleRadius = 82f;  // flower radius: tips at 1.4x, between tips down to 0.6x
    const float Petal = 0.4f;          // how far the flower swells into petals (Shapes.OnFlower)
    const float SquareHalfSide = 75f;  // corners 106 from the centre

    public override IEnumerator Run(RoutineContext ctx) { return Show.Run(ctx, start => new Plan(start)); }

    public class Plan : Show.IPlan
    {
        readonly int n;
        readonly Vector2[] start, spot;
        readonly int[] slot;            // order around the outline
        readonly float first;           // angle of slot 0 (radians)
        readonly float[] fromAngle, fromRadius, swing;
        readonly Show.Eased turnRate;   // radians per second

        public Plan(Vector2[] start)
        {
            this.start = start;
            n = start.Length;
            // The turn eases in, holds and eases out. Places on the circle are picked for where it
            // will be once formed, then the total turn is set so the square ends with its sides
            // square to the line (corners at 45 degrees)
            float turnSecs = TurnUpSecs / 2f + (Show.PerformSecs - TurnUpSecs) + TurnDownSecs / 2f;
            float guess = AboutDeg / turnSecs * Mathf.Deg2Rad * TurnUpSecs / 2f;
            slot = Show.RingSlots(start, CircleRadius, guess, out first);
            float total = AboutDeg - 30f + Mathf.Repeat(45f - first * Mathf.Rad2Deg - (AboutDeg - 30f), 90f);
            float rate = total / turnSecs * Mathf.Deg2Rad;
            turnRate = new Show.Eased(0f, 0f, TurnUpSecs, rate, Show.PerformSecs, rate, Show.PerformSecs + TurnDownSecs, 0f);

            var atEnd = new float[n];
            for (int i = 0; i < n; i++) { atEnd[i] = first + 2f * Mathf.PI * slot[i] / n + Turned(FormSecs); }
            Show.SwingOnto(start, atEnd, out fromAngle, out fromRadius, out swing);

            // Line spots for the book: from the upright square, a little further along the turn,
            // so robots on the spine go the way the turn is already carrying them
            var square = new Vector2[n];
            for (int i = 0; i < n; i++) { square[i] = Point(slot[i], Shape(2, slot[i]), Turned(Show.Total) + 3f * Mathf.Deg2Rad); }
            spot = Show.LineSpots(square);
        }

        float Turned(float t) { return turnRate.Integral(t); }

        Vector2 Point(int k, Vector2 polar, float turned)
        {
            float a = first + polar.x + turned;
            return Shapes.Centre + polar.y * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        static int Round(float v) { return Mathf.FloorToInt(v + 0.5f); }

        // Place k of shape 0 (circle), 1 (triangle) or 2 (square), as (angle from slot 0's
        // circle place, distance from the centre). Angles grow with k, so robots keep their order.
        Vector2 Shape(int shape, int k)
        {
            if (shape == 1 && n >= 3)
            {
                // Tips at 0, 120 and 240 degrees, each taken by a robot; the others spread evenly
                // between them. Radius as the original flower: r = R (1 + 0.4 cos 3a)
                int[] tip = { 0, Round(n / 3f), Round(2f * n / 3f), n };
                int j = k >= tip[2] ? 2 : (k >= tip[1] ? 1 : 0);
                float a = 2f * Mathf.PI / 3f * (j + (k - tip[j]) / (float)(tip[j + 1] - tip[j]));
                return new Vector2(a, TriangleRadius * (1f + Petal * Mathf.Cos(3f * a)));
            }
            if (shape == 2 && n >= 4)
            {
                // Corners at 0, 90, 180 and 270 degrees, each taken by a robot; the others spread
                // evenly along the straight sides between them
                int[] corner = { 0, Round(n / 4f), Round(n / 2f), Round(3f * n / 4f), n };
                int j = 0;
                while (j < 3 && k >= corner[j + 1]) { j++; }
                float c = SquareHalfSide * Mathf.Sqrt(2f);
                float a0 = Mathf.PI / 2f * j, a1 = Mathf.PI / 2f * (j + 1);
                Vector2 p = Vector2.Lerp(c * new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)), c * new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)), (k - corner[j]) / (float)(corner[j + 1] - corner[j]));
                float a = Mathf.Atan2(p.y, p.x);
                if (a < a0 - 0.001f) { a += 2f * Mathf.PI; }
                return new Vector2(a, p.magnitude);
            }
            return new Vector2(2f * Mathf.PI * k / n, CircleRadius);
        }

        // The outline at time t, part way through a change if one is under way
        Vector2 ShapeAt(int k, float t)
        {
            if (t < TriangleFrom) { return Shape(0, k); }
            if (t < SquareFrom) { return Vector2.Lerp(Shape(0, k), Shape(1, k), Shapes.Ease((t - TriangleFrom) / (TriangleTo - TriangleFrom))); }
            return Vector2.Lerp(Shape(1, k), Shape(2, k), Shapes.Ease((t - SquareFrom) / (SquareTo - SquareFrom)));
        }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Show.Total);
            Vector2 polar = ShapeAt(slot[i], t);
            Vector2 p;
            if (t < FormSecs)
            {
                // Forming: swing round the centre onto the circle, turning with it from the start
                float e = Shapes.Ease(t / FormSecs);
                float angle = fromAngle[i] + e * (swing[i] + Turned(t) - Turned(FormSecs));
                p = Shapes.Centre + Mathf.Lerp(fromRadius[i], polar.y, e) * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            else { p = Point(slot[i], polar, Turned(t)); }
            // The book: each half moves out to its own side of the line while the last of the
            // turn carries on, so the square sweeps open from the middle and straightens
            return Show.Return(p, spot[i], t);
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
    }
}
