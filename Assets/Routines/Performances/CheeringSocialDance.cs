using System.Collections;
using System.Linq;
using UnityEngine;

// Cheering - Social dance. After the Messenger's introduction the robots pair up and each pair
// goes to its own corner of the dance area, meeting at a shared point there. From that point the
// partners sweep out in mirrored curves, like butterfly wings, and arc back toward each other,
// over and over. On the last inward arc they are already heading for the centre: each pair feeds
// into the nearby part of a small ring, which starts turning as it forms. Then a campfire dance:
// the ring keeps turning slowly while it widens, dances on at its new size, and flows into the line.
public class CheeringSocialDance : SwarmRoutine
{
    public override string Name { get { return "v2 Cheering: social dance"; } }
    public override int MinRobots { get { return 2; } }

    // ---- Timing (seconds after the introduction; the return to the line starts at 30) ----
    const float ArriveSecs = 3f;   // partners reach their meeting point, already moving into the first sweep
    const float PhraseSecs = 2.8f; // one butterfly phrase: out, apart, back together
    const int Phrases = 2;         // full phrases before the last one becomes the way to the ring
    const float JoinSecs = 5f;     // last inward arc -> small ring, turning as it forms
    const float WidenSecs = 9f;    // the campfire ring widens...
    // ...then dances on at its full size until 30 s

    // ---- Butterfly pairs ----
    // Corners in the order pairs use them (2-3 robots use the first one or two)
    static readonly Vector2[] Corners = { new Vector2(-1, -1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1) };
    const float CornerOffset = 110f;  // meeting point from the mat centre, along x and along y
    const float MeetGap = 52f;        // partners' distance at the meeting point (they cut in a little on the real robots)
    const float WingOpen = 28f;       // each partner sweeps this much further out to the side...
    const float WingReach = 25f;      // ...and this far toward the corner and back toward the centre

    // ---- Campfire ring ----
    const float CompactSpacing = 48f;        // neighbour distance in the small ring
    const float CompactMinRadius = 55f;
    const float FinalDiameterShare = 0.63f;  // final ring diameter, share of the dance area
    const float RingSpeed = 20f;             // mat units per second along the ring, whatever its size

    // When the last inward arc starts: partners at their widest, after the full phrases
    const float JoinFrom = ArriveSecs + PhraseSecs * (Phrases + 0.5f);

    public override IEnumerator Run(RoutineContext ctx) { return Show.Run(ctx, start => new Plan(start)); }

    public class Plan : Show.IPlan
    {
        readonly int n;
        readonly Vector2[] start, spot;
        readonly int[] pair, side;   // side +1/-1 for partners, 0 for a robot dancing alone
        readonly float[] place;      // radians around the ring
        readonly float compactRadius, finalRadius;
        readonly Show.Integrated turned;
        // The butterfly keeps its pace until the last inward arc, which slows to a finish as the
        // partners come together on the way to the ring
        static readonly Show.Eased wingPace = new Show.Eased(0f, 1f, JoinFrom, 1f, JoinFrom + PhraseSecs, 0f);

        public Plan(Vector2[] start)
        {
            this.start = start;
            n = start.Length;
            compactRadius = Mathf.Max(CompactMinRadius, CompactSpacing / (2f * Mathf.Sin(Mathf.PI / n)));
            float dance = Swarm.MatMax - Swarm.MatMin - 2f * (Show.DanceMargin - 16.5f);
            finalRadius = FinalDiameterShare * dance / 2f;
            turned = new Show.Integrated(TurnRate, Show.Total + 1f);

            // Partners and corners: the meeting places with the least total travel from the line
            var slotPair = new int[n]; var slotSide = new int[n];
            for (int s = 0; s < n; s++)
            {
                slotPair[s] = s / 2;
                slotSide[s] = (n % 2 == 1 && s == n - 1) ? 0 : (s % 2 == 0 ? 1 : -1);
            }
            var meet = Enumerable.Range(0, n).Select(s => Wing(slotPair[s], slotSide[s], ArriveSecs)).ToArray();
            var slot = Swarm.LeastTravel(start, meet);
            pair = new int[n]; side = new int[n];
            for (int i = 0; i < n; i++) { pair[i] = slotPair[slot[i]]; side[i] = slotSide[slot[i]]; }

            // Ring places: each pair feeds into the part of the ring nearest its corner
            var joining = Enumerable.Range(0, n).Select(i => Wing(pair[i], side[i], JoinFrom + PhraseSecs)).ToArray();
            place = Show.RingPlaces(joining, compactRadius, turned.At(JoinFrom + JoinSecs));

            var leaving = Enumerable.Range(0, n).Select(i => Ring(i, Show.PerformSecs + Show.ReturnSecs / 2f)).ToArray();
            spot = Show.LineSpots(leaving);
        }

        // A partner's place in its pair's butterfly dance. Partners mirror each other across the
        // line from the corner to the mat centre: together at the meeting point, out toward the
        // corner and apart, back toward the centre and together again.
        static Vector2 Wing(int p, int s, float t)
        {
            Vector2 toCorner = Corners[p].normalized;
            Vector2 across = new Vector2(-toCorner.y, toCorner.x);
            float a = 2f * Mathf.PI / PhraseSecs * (wingPace.Integral(t) - ArriveSecs);
            float apart = Mathf.Sin(a / 2f);
            return Shapes.Centre + Corners[p] * CornerOffset
                 + toCorner * WingReach * Mathf.Sin(a)
                 + across * s * (MeetGap / 2f + WingOpen * apart * apart);
        }

        float Radius(float t)
        {
            float widenFrom = JoinFrom + JoinSecs;
            return Mathf.Lerp(compactRadius, finalRadius, Shapes.Ease((t - widenFrom) / WidenSecs));
        }

        // Turning speed (radians per second): starts as the ring forms, keeps the same speed along
        // the ring as it widens, and eases away during the return
        float TurnRate(float t)
        {
            return Shapes.Ease((t - JoinFrom) / JoinSecs) * RingSpeed / Radius(t)
                 * (1f - Shapes.Ease((t - Show.PerformSecs) / Show.ReturnSecs));
        }

        Vector2 Ring(int i, float t) { return Show.OnRing(place[i] + turned.At(t), Radius(t)); }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Show.Total);
            Vector2 p = Vector2.Lerp(start[i], Wing(pair[i], side[i], t), Shapes.Ease(t / ArriveSecs));
            p = Vector2.Lerp(p, Ring(i, t), Shapes.Ease((t - JoinFrom) / JoinSecs));
            return Show.Return(p, spot[i], t);
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
    }
}
