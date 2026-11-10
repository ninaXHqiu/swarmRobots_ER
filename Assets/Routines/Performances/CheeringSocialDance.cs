using System.Collections;
using System.Linq;
using UnityEngine;

// Cheering - Social dance. Four close partner dances that become one turning group dance.
// After the Messenger's introduction the robots pair up, each pair in one corner area of the
// compact performance area, around a shared dance centre. Butterfly: the partners curve toward the
// centre and pass it on opposite sides, sweep out along mirrored wing curves, turn back toward
// each other and come close again, over and over. Joining: on its last inward sweep each pair
// carries on toward the nearest part of a small central ring; the ring starts turning as the
// first pair arrives and the others slip into it as it turns. Campfire: the whole ring keeps
// turning while it draws in together and opens out again, a little wider each time, turns on at
// its widest and flows into the line.
public class CheeringSocialDance : SwarmRoutine
{
    public override string Name { get { return "v2 Cheering: social dance"; } }
    public override int MinRobots { get { return 2; } }

    // ---- Phrases (change the counts freely; the choreography follows) ----
    const int ButterflyPhrases = 3;        // the last one's inward sweep becomes the way to the ring
    static readonly float[] CampfireIn = { 55f, 60f, 66f };    // radius drawn in to, per campfire phrase
    static readonly float[] CampfireOut = { 88f, 100f, 112f }; // radius opened out to (widest: 60% of the dance area)

    // ---- Butterfly ----
    const float ArriveSecs = 3f;           // from the line to the dance centres, arriving in the first sweep
    const float PhraseSecs = 2.6f;         // one butterfly phrase
    const float JoinStagger = 0.5f;        // each pair dances this far behind the one before, so pairs join in turn
    // Corner dance centres, in the order pairs use them (2-3 robots use the first one or two)
    static readonly Vector2[] Corners = { new Vector2(-1, -1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 1) };
    const float CornerOffset = 68f;        // dance centre from the mat centre, along x and along y:
                                           // the corners of the compact central area, not of the mat
    const float PassGap = 44f;             // partners' distance as they pass the centre
    const float WingOpen = 10f;            // each partner sweeps this much further out to the side...
    const float WingReach = 24f;           // ...and this far along the diagonal and back
                                           // (partners stay nearer each other than to the next pair)

    // ---- Ring ----
    const float JoinSecs = 3f;             // last inward sweep -> place in the turning ring
    const float JoinRadius = 70f;
    const float InSecs = 1.6f, OutSecs = 1.8f, WideSecs = 2f;  // one campfire phrase
    const float FinalSecs = 1f;            // turning at the final size before the line
    const float RingSpeed = 22f;           // mat units per second along the ring, whatever its size

    public override IEnumerator Run(RoutineContext ctx) { return Show.Run(ctx, start => new Plan(start)); }

    public class Plan : Show.IPlan
    {
        readonly int n;
        readonly Vector2[] start, spot;
        readonly int[] pair, side;   // side +1/-1 for partners, 0 for a robot dancing alone
        readonly float[] place;      // radians around the ring
        readonly float exitFrom;
        readonly Show.Eased radius;
        readonly Show.Integrated turned;
        public float Total { get; private set; }

        public Plan(Vector2[] start)
        {
            this.start = start;
            n = start.Length;
            int pairs = (n + 1) / 2;

            // Campfire: draw in, open out, turn wide; repeat a little wider; then the line
            float allJoined = JoinFrom(pairs - 1) + JoinSecs;
            var keys = new System.Collections.Generic.List<float> { 0f, JoinRadius, allJoined, JoinRadius };
            float t = allJoined;
            for (int k = 0; k < CampfireOut.Length; k++)
            {
                t += InSecs; keys.Add(t); keys.Add(CampfireIn[k]);
                t += OutSecs; keys.Add(t); keys.Add(CampfireOut[k]);
                t += WideSecs; keys.Add(t); keys.Add(CampfireOut[k]);
            }
            exitFrom = t + FinalSecs;
            Total = exitFrom + Show.ReturnSecs;
            radius = new Show.Eased(keys.ToArray());
            float firstJoin = JoinFrom(0);
            turned = new Show.Integrated(s => Shapes.Ease((s - firstJoin) / JoinSecs) * RingSpeed / radius.At(s)
                                             * (1f - Shapes.Ease((s - exitFrom) / Show.ReturnSecs)), Total + 1f);

            // Partners and corners: the dance places with the least total travel from the line
            var slotPair = new int[n]; var slotSide = new int[n];
            for (int s = 0; s < n; s++)
            {
                slotPair[s] = s / 2;
                slotSide[s] = (n % 2 == 1 && s == n - 1) ? 0 : (s % 2 == 0 ? 1 : -1);
            }
            var meet = Enumerable.Range(0, n).Select(s => Wing(slotPair[s], slotSide[s], Arrive(slotPair[s]))).ToArray();
            var slot = Swarm.LeastTravel(start, meet);
            pair = new int[n]; side = new int[n];
            for (int i = 0; i < n; i++) { pair[i] = slotPair[slot[i]]; side[i] = slotSide[slot[i]]; }

            // Ring places: evenly spaced, each robot in the turning ring's place nearest to where
            // its last inward sweep brings it
            var want = new float[n];
            for (int i = 0; i < n; i++)
            {
                float joined = JoinFrom(pair[i]) + JoinSecs;
                Vector2 d = Wing(pair[i], side[i], JoinFrom(pair[i]) + PhraseSecs) - Shapes.Centre;
                want[i] = Mathf.Atan2(d.y, d.x) - turned.At(joined);
            }
            var order = Enumerable.Range(0, n).OrderBy(i => Mathf.Repeat(want[i], 2f * Mathf.PI)).ToArray();
            float sx = 0f, sy = 0f;
            for (int k = 0; k < n; k++) { float a = want[order[k]] - 2f * Mathf.PI * k / n; sx += Mathf.Cos(a); sy += Mathf.Sin(a); }
            float a0 = Mathf.Atan2(sy, sx);
            place = new float[n];
            for (int k = 0; k < n; k++) { place[order[k]] = a0 + 2f * Mathf.PI * k / n; }

            var leaving = Enumerable.Range(0, n).Select(i => Ring(i, exitFrom + Show.ReturnSecs / 2f)).ToArray();
            spot = Show.LineSpots(leaving);
        }

        static float Arrive(int p) { return ArriveSecs + p * JoinStagger; }
        // When pair p's last inward sweep starts: partners at their widest, after the full phrases
        static float JoinFrom(int p) { return Arrive(p) + PhraseSecs * (ButterflyPhrases - 0.5f); }

        // A partner's place in its pair's butterfly. Partners mirror each other across the line
        // from the corner to the mat centre: passing the dance centre side by side, out along a
        // wing toward the corner and apart, round and back toward the mat centre and each other.
        // The last inward sweep slows to a finish as the pair heads into the ring.
        static Vector2 Wing(int p, int s, float t)
        {
            float end = JoinFrom(p);
            float done = Mathf.Min(t, end) - Arrive(p);
            if (t > end)
            {
                float u = Mathf.Min(1f, (t - end) / PhraseSecs);
                done += PhraseSecs * (u - Shapes.EaseIntegral(u));   // speed eases from full to zero
            }
            float a = 2f * Mathf.PI * done / PhraseSecs;
            Vector2 toCorner = Corners[p].normalized;
            Vector2 across = new Vector2(-toCorner.y, toCorner.x);
            float apart = Mathf.Sin(a / 2f);
            return Shapes.Centre + Corners[p] * CornerOffset
                 + toCorner * WingReach * Mathf.Sin(a)
                 + across * s * (PassGap / 2f + WingOpen * apart * apart);
        }

        Vector2 Ring(int i, float t) { return Show.OnRing(place[i] + turned.At(t), radius.At(t)); }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            int p = pair[i];
            Vector2 q = Vector2.Lerp(start[i], Wing(p, side[i], t), Shapes.Ease(t / Arrive(p)));
            q = Vector2.Lerp(q, Ring(i, t), Shapes.Ease((t - JoinFrom(p)) / JoinSecs));
            return Show.Return(q, spot[i], t, exitFrom);
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
    }
}
