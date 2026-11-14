using System.Collections;
using System.Linq;
using UnityEngine;

// Cheering - Social dance. Four close partner dances that become one turning group dance.
// After the Messenger's introduction the robots pair up, each pair in one corner area of the
// compact performance area, around a shared dance centre. Partner dance: the two partners always
// move opposite each other around their shared centre: they come close and pass each other on
// opposite sides of it, swing out wide away from each other, and curve back in to pass again,
// over and over. Joining: on the last phrase the pair slows as it comes together lined up along
// the ring and carries on into the nearest part of a small central ring; the ring starts turning
// as the first pair arrives and the others slip into it as it turns. Campfire: the whole ring keeps
// turning while it draws in together and opens out again, a little wider each time, turns on at
// its widest and flows into the line.
public class CheeringSocialDance : SwarmRoutine
{
    public override string Name { get { return "v2 - bad Cheering: social dance"; } }
    public override string Code { get { return "C05"; } }
    public override string Note { get { return "Old social dance attempt"; } }
    public override int MinRobots { get { return 2; } }

    // ---- Phrases (change the counts freely; the choreography follows) ----
    const int ButterflyPhrases = 3;        // the last one's coming together becomes the way to the ring
    static readonly float[] CampfireIn = { 52f, 55f, 58f };    // radius drawn in to, per campfire phrase
    static readonly float[] CampfireOut = { 68f, 76f, 84f };   // radius opened out to (compact; under 60% of the dance area)

    // ---- Partner dance ----
    const float ArriveSecs = 4.5f;         // from the line to the dance centres, gliding, arriving as they pass
    const float PhraseSecs = 3f;           // one phrase: pass, swing out, curve back (half a turn round each other)
    const float JoinStagger = 0.5f;        // each pair dances this far behind the one before, so pairs join in turn
    // Partners are neighbours in the line, pairs numbered left to right. Each pair takes the
    // corner that, together with the others, makes the shortest walk in from where the robots are
    // (so nobody crosses anybody on the way).
    static readonly Vector2[] AllCorners = { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(-1, 1) };
    const float CornerOffset = 54f;        // dance centre from the mat centre, along x and along y:
                                           // the corners of the compact central area, not of the mat
    const float PassGap = 44f;             // partners' distance as they pass each other
    const float SwingGap = 64f;            // partners' distance at the widest part of the swing

    // ---- Ring ----
    const float JoinSecs = 3f;             // last inward sweep -> place in the turning ring
    const float JoinRadius = 60f;
    const float InSecs = 1.6f, OutSecs = 1.8f, WideSecs = 2f;  // one campfire phrase
    const float FinalSecs = 1f;            // turning at the final size before the line
    const float RingSpeed = 16f;           // mat units per second along the ring, whatever its size

    public override IEnumerator Run(RoutineContext ctx) { return Show.Run(ctx, start => new Plan(start)); }

    public class Plan : Show.IPlan
    {
        readonly int n;
        readonly Vector2[] start, spot;
        readonly int[] pair, side;   // side +1/-1 for partners, 0 for a robot dancing alone
        readonly float[] place;      // radians around the ring
        Vector2[] corner;            // each pair's corner
        readonly Vector2[] meet;     // where each robot first meets its partner
        readonly float exitFrom;
        readonly Show.Eased radius;
        readonly Show.Integrated turned;
        public float Total { get; private set; }

        public Plan(Vector2[] start)
        {
            this.start = start;
            n = start.Length;
            int pairs = (n + 1) / 2;
            corner = new Vector2[pairs];

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

            // Partners: neighbours in the line, left to right; within a pair, whichever way round
            // is the shorter walk. With an odd number the rightmost robot dances alone.
            // (the messenger counts at its own spot in the line, not where it stands to talk)
            var inLine = Show.KeepLineSpots(start);
            var byX = Enumerable.Range(0, n).OrderBy(i => inLine[i].x).ToArray();
            pair = new int[n]; side = new int[n]; meet = new Vector2[n];
            float best = float.MaxValue;
            foreach (var choice in Arrangements(pairs))
            {
                corner = choice.Select(c => AllCorners[c]).ToArray();
                float cost = 0f;
                for (int k = 0; k < pairs; k++) { cost += PlacePair(k, byX, false); }
                if (cost < best) { best = cost; bestCorners = (Vector2[])corner.Clone(); }
            }
            corner = bestCorners;
            for (int k = 0; k < pairs; k++) { PlacePair(k, byX, true); }

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

        Vector2[] bestCorners;

        // Every way of giving the pairs different corners
        static System.Collections.Generic.IEnumerable<int[]> Arrangements(int pairs)
        {
            foreach (var p in Permutations(new[] { 0, 1, 2, 3 })) { yield return p.Take(pairs).ToArray(); }
        }

        static System.Collections.Generic.IEnumerable<int[]> Permutations(int[] items)
        {
            if (items.Length <= 1) { yield return items; yield break; }
            for (int i = 0; i < items.Length; i++)
            {
                var rest = items.Where((_, j) => j != i).ToArray();
                foreach (var tail in Permutations(rest)) { yield return new[] { items[i] }.Concat(tail).ToArray(); }
            }
        }

        // Pair k (robots byX[2k], byX[2k+1]) at its corner, whichever way round is the shorter walk;
        // returns that walk (squared). With an odd number the last robot dances alone.
        float PlacePair(int k, int[] byX, bool keep)
        {
            int a = byX[2 * k];
            if (2 * k + 1 >= n)
            {
                Vector2 alone = Wing(k, 0, ArriveSecs);
                if (keep) { pair[a] = k; side[a] = 0; meet[a] = alone; }
                return (start[a] - alone).sqrMagnitude;
            }
            int b = byX[2 * k + 1];
            Vector2 plus = Wing(k, 1, ArriveSecs), minus = Wing(k, -1, ArriveSecs);
            float straight = (start[a] - plus).sqrMagnitude + (start[b] - minus).sqrMagnitude;
            float swapped = (start[a] - minus).sqrMagnitude + (start[b] - plus).sqrMagnitude;
            if (keep)
            {
                pair[a] = pair[b] = k;
                side[a] = straight <= swapped ? 1 : -1; side[b] = -side[a];
                meet[a] = side[a] > 0 ? plus : minus; meet[b] = side[a] > 0 ? minus : plus;
            }
            return Mathf.Min(straight, swapped);
        }

        // The partner dance's clock: still for most of the walk in, then easing up to full pace
        // over the last part of the walk and the first second after arriving
        const float RampFrom = ArriveSecs * 0.75f, RampTo = ArriveSecs + 1f;
        static float Clock(float t)
        {
            return (RampTo - RampFrom) * Shapes.EaseIntegral((t - RampFrom) / (RampTo - RampFrom));
        }
        // Phrases' worth of time pair p has danced at t (each pair a little behind the one before)
        static float Danced(int p, float t) { return Clock(t) - Clock(ArriveSecs) - p * JoinStagger; }
        // When pair p's last phrase starts slowing: partners at their widest, after the full phrases
        static float JoinFrom(int p)
        {
            return PhraseSecs * (ButterflyPhrases - 0.5f) + p * JoinStagger + Clock(ArriveSecs) + RampTo - (RampTo - RampFrom) / 2f;
        }

        // A partner's place in its pair's dance: the two partners on opposite sides of their
        // shared centre, going round it together (so always moving opposite each other), coming
        // close to pass and swinging out wide in turn. The last phrase slows to a finish with the
        // pair lined up along the ring, ready to step into it.
        Vector2 Wing(int p, int s, float t)
        {
            float end = JoinFrom(p);
            // Pairs are offset in their rhythm (not in when they arrive), so they join the ring in turn
            float done = Danced(p, Mathf.Min(t, end));
            if (t > end)
            {
                float u = Mathf.Min(1f, (t - end) / PhraseSecs);
                done += PhraseSecs * (u - Shapes.EaseIntegral(u));   // speed eases from full to zero
            }
            float phrases = done / PhraseSecs;                       // half a turn per phrase
            // Lined up along the ring (tangent at this corner) when the last phrase ends
            float cornerAngle = Mathf.Atan2(corner[p].y, corner[p].x);
            float a = cornerAngle + Mathf.PI / 2f + Mathf.PI * (phrases - ButterflyPhrases);
            float swing = Mathf.Sin(Mathf.PI * phrases);
            float r = (PassGap + (SwingGap - PassGap) * swing * swing) / 2f;
            float sideSign = s == 0 ? 1f : s;
            return Shapes.Centre + corner[p] * CornerOffset + sideSign * r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        Vector2 Ring(int i, float t) { return Show.OnRing(place[i] + turned.At(t), radius.At(t)); }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            int p = pair[i];
            // Walk in to the meeting place; the dance round each other starts on the way
            float e = Shapes.Ease(t / ArriveSecs);
            Vector2 q = Vector2.Lerp(start[i], meet[i], e) + e * (Wing(p, side[i], t) - meet[i]);
            q = Vector2.Lerp(q, Ring(i, t), Shapes.Ease((t - JoinFrom(p)) / JoinSecs));
            return Show.Return(q, spot[i], t, exitFrom);
        }

        public Vector2 LineSpot(int i) { return spot[i]; }
    }
}
