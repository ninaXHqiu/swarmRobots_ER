using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Shared pieces of the LLB routines (LLB_resonance_v1..v3, LLB_cheering_v1..v3) that only plan
// paths: no robots, no Unity time, so the same code runs in the routines and in the checks
// (Editor/LLBCheck.cs). The robot side (forming the line, following the plan, reading the
// heart rate) is in LLBShow.cs.
//
// Every LLB routine is one plan of exactly LLBPlan.TotalSecs (3:00), the same three parts:
//   1. an introduction (LLBPlan.Intro: Messenger or Huddle), the same in every routine that uses it;
//   2. the routine's own middle part;
//   3. the closing, identical in all six: the robots slowly go back from a ring into the line, and
//      everyone turns to look at the participant.
//
// Mat coordinates as everywhere: x to the right, y down, the participant at the bottom-centre edge.
public static class LLB
{
    // The line every LLB routine starts in and ends in: a quarter of the mat in from the
    // participant's edge (455 - 410 / 4), robots close together (a cube is about 23 units wide).
    public const float LineY = 352f;
    public const float Spacing = 36f;

    // The messenger: robot 1, the second from the left (robot i owns line slot i, counted from 0).
    public const int Messenger = 1;
    public static int MessengerSlot(int n) { return Mathf.Min(Messenger, n - 1); }

    // Which hand wears the Shimmer, seen from the participant: -1 = their left (smaller x on the
    // mat), +1 = their right. The messenger stops a little toward that side and looks at it.
    public const int HandSide = -1;
    public const float HandOffset = 110f;   // mat units along the participant's edge from its centre
    public static Vector2 Hand { get { return Swarm.Participant + new Vector2(HandSide * HandOffset, 0f); } }

    // Where the messenger stops to look (in front of the participant, toward the hand), and where it
    // talks to the team: straight back from there, just in front of the line, facing it
    public static Vector2 VisitSpot { get { return Swarm.Participant + new Vector2(HandSide * 35f, -29f); } }
    public static Vector2 TalkSpot { get { return new Vector2(VisitSpot.x, LineY + 42f); } }

    // Robot i's own spot in the line
    public static Vector2[] OwnSpots(int n)
    {
        var s = new Vector2[n];
        for (int i = 0; i < n; i++) { s[i] = new Vector2(Swarm.Centre + (i - (n - 1) / 2f) * Spacing, LineY); }
        return s;
    }

    // Centre of the dance: the mat centre, a little away from the participant
    public static readonly Vector2 Centre = new Vector2(250f, 240f);

    // Planned robots in open formations keep at least Keep apart (centre to centre); in the line and
    // at its start and end they are Spacing apart.
    public const float Keep = 44f;

    // Breathing guided at 6 a minute: in for 4 s, out for 6 s. 0 = breathed out, 1 = breathed in.
    public const float BreathIn = 4f, BreathOut = 6f, BreathSecs = BreathIn + BreathOut;
    public static float Breath(float sinceStart)
    {
        if (sinceStart <= 0f) { return 0f; }
        float u = Mathf.Repeat(sinceStart, BreathSecs);
        return u < BreathIn ? Shapes.Ease(u / BreathIn) : 1f - Shapes.Ease((u - BreathIn) / BreathOut);
    }

    // Smooth wandering in about [-1, 1] on each axis, its own for every robot and salt: three
    // sines a direction, frequencies between lo and hi (Hz) at clock t. Sharper with a higher hi.
    public static Vector2 Wander(int i, float salt, float t, float lo, float hi)
    {
        Vector2 v = Vector2.zero;
        for (int k = 0; k < 3; k++)
        {
            float fx = Mathf.Lerp(lo, hi, Shapes.Hash(i, salt + 11.1f * k));
            float fy = Mathf.Lerp(lo, hi, Shapes.Hash(i, salt + 23.7f * k + 5f));
            float px = 6.283f * Shapes.Hash(i, salt + 31.3f * k + 2f), py = 6.283f * Shapes.Hash(i, salt + 47.9f * k + 3f);
            v.x += Mathf.Sin(6.283f * fx * t + px) / 2f;
            v.y += Mathf.Sin(6.283f * fy * t + py) / 2f;
        }
        return v / 1.5f;
    }

    // Rest-to-rest paths that keep the robots apart (V2Show.WayToLine, its waits and bows), the
    // roomiest and calmest that is clear. Paths that swing out toward the participant past
    // NearParticipantY are only taken if nothing else is clear. Falls back to the plain trip if no
    // plan is clear (the checks report it).
    public static V2Show.WayToLine Way(Vector2[] from, Vector2[] to, float unitsPerSec)
    {
        V2Show.WayToLine last = null, firstClear = null;
        float limit = NearParticipantY;
        for (int k = 0; k < from.Length; k++) { limit = Mathf.Max(limit, Mathf.Max(from[k].y, to[k].y)); }
        foreach (float keep in new[] { 52f, 47f, Keep, 40f })
        {
            foreach (float pace in new[] { 1f, 0.85f })   // never faster than asked: long trips are what the motors are loudest on
            {
                last = new V2Show.WayToLine(from, to, unitsPerSec * pace, keep);
                if (!last.Clear) { continue; }
                if (firstClear == null) { firstClear = last; }
                if (LowestY(last, from.Length) <= limit) { return last; }
            }
        }
        if (firstClear != null) { return firstClear; }
        UnclearWays++;
        Debug.LogWarning("LLB: no clear paths found for " + from.Length + " robots; they go straight");
        return last;
    }
    public const float NearParticipantY = 410f;   // planned paths stay above this (the participant's edge is at 455)
    public static int UnclearWays;                // how many times Way found no clear paths (read by the checks)

    static float LowestY(V2Show.WayToLine way, int n)
    {
        float y = 0f;
        for (float t = 0f; t <= way.Total; t += 0.1f) { for (int i = 0; i < n; i++) { y = Mathf.Max(y, way.Position(i, t).y); } }
        return y;
    }

    public static Vector2 Dir(float deg) { return new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)); }
    public static float AngleOf(Vector2 v) { return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; }

    // Point on a circle round `c` (angle in degrees, increasing = clockwise on the mat)
    public static Vector2 On(Vector2 c, float deg, float r) { return c + r * Dir(deg); }

    // Smallest ring that keeps n robots `gap` apart
    public static float RingFor(int n, float gap) { return n < 2 ? 0f : gap / (2f * Mathf.Sin(Mathf.PI / n)); }

    // Rises from 0 at `from` over `rise`, stays at 1, falls back to 0 from `until` over `fall`
    public static float Envelope(float t, float from, float rise, float until, float fall)
    {
        return Shapes.Ease((t - from) / rise) * (1f - Shapes.Ease((t - until) / fall));
    }

    // Each of `from` takes one of `to`: the least total squared travel (straight trips don't cross)
    public static Vector2[] Assign(Vector2[] from, Vector2[] to)
    {
        var slot = Swarm.LeastTravel(from, to);
        return Enumerable.Range(0, from.Length).Select(i => to[slot[i]]).ToArray();
    }

    // What a plan wants of robots that stand still and turn: while InPlace is true the robot is not
    // driven along its path (its planned position stands still) but turned: toward faceDeg (mat
    // degrees, NaN = no particular way) plus spin (wheel speed, + = clockwise, 0 = none).
    public interface IInPlace { bool InPlace(int i, float t, out float spin, out float faceDeg); }

    // The participant's heart rate, filtered against noise (LLBShow.PhysioHeart on the robots, made-up
    // heart rates in the checks): beats a minute, or NaN when there is no trustworthy reading.
    public interface IHeart { float Bpm(float planSecs); }
}

// Base of the LLB plans. A plan starts with every robot on its own line spot facing the participant,
// runs the introduction, the routine's middle (Middle), and the shared closing, and ends with every
// robot at rest on its own spot, facing the participant, after exactly TotalSecs.
public abstract class LLBPlan : V2Show.IPlan, V2Show.IHeading, V2Show.ISections, LLB.IInPlace
{
    public const float TotalSecs = 180f;   // 3:00, every LLB routine

    protected readonly int n;
    protected readonly Vector2[] start, line;
    readonly List<KeyValuePair<float, string>> sections = new List<KeyValuePair<float, string>>();
    readonly List<Act> acts = new List<Act>();
    readonly List<Lane> lanes = new List<Lane>();
    public float Total { get; private set; }

    public enum Intro { Messenger, Huddle }

    protected LLBPlan(Vector2[] start, Intro intro)
    {
        this.start = start;
        n = start.Length;
        line = LLB.OwnSpots(n);
        Total = TotalSecs;
        if (intro == Intro.Messenger) { BuildMessengerIntro(); } else { BuildHuddleIntro(); }
        BuildClosing();
        AddClosingSections();
        middleAt = introEnd;
    }

    // ---------------- The parts ----------------

    // The middle runs from MiddleAt to CloseAt. It starts where the introduction leaves the robots
    // (MiddleStart) and must bring every robot to rest on its RingSpot by CloseAt.
    protected abstract Vector2 Middle(int i, float t);
    readonly float middleAt;
    protected float MiddleAt { get { return middleAt; } }
    protected float CloseAt { get { return closeAt; } }
    protected float MiddleSecs { get { return closeAt - middleAt; } }
    protected Vector2 MiddleStart(int i) { return introEndAt[i]; }
    protected Vector2 RingSpot(int i) { return LLB.On(LLB.Centre, ringDeg[i], CloseR); }
    protected float RingDeg(int i) { return ringDeg[i]; }
    protected Vector2[] RingSpots() { return Enumerable.Range(0, n).Select(RingSpot).ToArray(); }
    protected Vector2[] MiddleStarts() { return (Vector2[])introEndAt.Clone(); }

    Vector2 PathAll(int i, float t)
    {
        if (t < introEnd) { return IntroPath(i, t); }
        if (t >= closeAt) { return Closing(i, t); }
        return Middle(i, t);
    }

    // As planned, plus the small difference between where the robot really stands at the start and
    // its line spot, faded out over the first 1.5 s
    public Vector2 Position(int i, float t)
    {
        t = Mathf.Clamp(t, 0f, Total);
        Vector2 off = start[i] - line[i];
        if (off.magnitude > 20f) { off = off.normalized * 20f; }
        return PathAll(i, t) + off * (1f - Shapes.Ease(t / 1.5f));
    }

    public Vector2 LineSpot(int i) { return line[i]; }

    // ---------------- Introduction 1: the messenger ----------------
    // Robot 1 steps out and walks over to the participant, looks at them, at their hand, back at them,
    // then turns to the team and goes straight back to just in front of the line, facing it, and
    // "talks" (shakes). The others answer each in its own way and at its own moment: a nod, two nods,
    // a little shake back, a turn toward the messenger and back.
    const float ApproachSecs = 3.6f, LookP = 1.8f, LookHand = 1.8f, LookBack = 1.3f, LookTeam = 1.0f, WalkBackSecs = 2.2f, TalkSecs = 3.6f;
    const float Nod = 7f, NodSecs = 0.8f;
    float introEnd;
    Vector2[] introEndAt;
    int messenger;
    float approachAt, looksAt, walkBackAt, talkAt;
    Vector2 visit, talk;
    float[] nodAt; int[] nods;

    void BuildMessengerCommon()
    {
        messenger = LLB.MessengerSlot(n);
        visit = LLB.VisitSpot; talk = LLB.TalkSpot;
        approachAt = 1.0f;
        looksAt = approachAt + ApproachSecs + 0.2f;
        float t = looksAt;
        FacePoint(messenger, t, LookP, visit, Swarm.Participant); t += LookP;
        FacePoint(messenger, t, LookHand, visit, LLB.Hand); t += LookHand;
        FacePoint(messenger, t, LookBack, visit, Swarm.Participant); t += LookBack;
        walkBackAt = t + LookTeam;
        Face(messenger, t, LookTeam, LLB.AngleOf(talk - visit));
        Section(0f, "intro: the line");
        Section(approachAt, "intro: robot 1 walks over to the participant");
        Section(looksAt, "intro: looks at them, at their hand, back at them");
        Section(looksAt + LookP + LookHand + LookBack, "intro: turns to the team");
    }

    void BuildMessengerIntro()
    {
        BuildMessengerCommon();
        talkAt = walkBackAt + WalkBackSecs;
        introEnd = talkAt + TalkSecs;
        InLaneFor(messenger, walkBackAt, talkAt, LLB.AngleOf(talk - visit));
        Face(messenger, talkAt, TalkSecs, LLB.AngleOf(talk - visit));
        Wiggle(messenger, talkAt + 0.2f, TalkSecs - 0.4f, 24f, 0.27f);
        // the others answer, each its own way and moment
        nodAt = new float[n]; nods = new int[n];
        for (int j = 0; j < n; j++)
        {
            if (j == messenger) { continue; }
            float at = talkAt + 0.3f + 1.4f * Shapes.Hash(j, 13f);   // every answer is over before the talk ends
            int kind = Mathf.FloorToInt(4f * Shapes.Hash(j, 17f));
            if (kind == 0 || kind == 1) { nodAt[j] = at; nods[j] = kind + 1; }
            else if (kind == 2) { Wiggle(j, at, 0.7f, 22f, 0.22f); }
            else { FacePoint(j, at, 0.9f, line[j], talk); Face(j, at + 0.9f, 0.7f, 90f); }
            InLaneFor(j, 0f, introEnd, 90f);
        }
        introEndAt = (Vector2[])line.Clone();
        introEndAt[messenger] = talk;
        Section(walkBackAt, "intro: back to the team, talks to them");
        Section(talkAt, "intro: the others answer, each in its own way");
    }

    // ---------------- Introduction 2: the huddle ----------------
    // The messenger walks over and looks as in the first; then everyone gathers in a little huddle
    // round it, as if to hear it, and they argue among themselves, ignoring the participant. Two or
    // three turn to look at the participant, turn back to the huddle, and argue a little more.
    const float HuddleR = 68f, HuddleWalkSecs = 3.5f, CurlSecs = 6f, Argue1 = 3.0f, LookOut = 1.8f, Argue2 = 2.4f;
    static readonly Vector2 HuddleCentre = new Vector2(250f, 272f);
    float huddleWalkAt, curlAt;
    float[] huddleDeg;
    bool huddle;

    void BuildHuddleIntro()
    {
        BuildMessengerCommon();
        huddle = true;
        // the messenger walks back into its place in the line, then the whole line curls up into a tight
        // circle, everyone facing in: the huddle
        huddleWalkAt = walkBackAt;
        curlAt = huddleWalkAt + HuddleWalkSecs;
        huddleDeg = FlatRingDeg(HuddleCentre, HuddleR);
        var to = Enumerable.Range(0, n).Select(i => LLB.On(HuddleCentre, huddleDeg[i], HuddleR)).ToArray();
        introEndAt = to;
        float a1 = curlAt + CurlSecs + 0.2f;
        float lookAt = a1 + Argue1, a2 = lookAt + LookOut + 1.0f;
        introEnd = a2 + Argue2;
        // the two or three nearest the participant look at them
        var lookers = Enumerable.Range(0, n).OrderByDescending(j => to[j].y).Take(n >= 7 ? 3 : 2).ToArray();
        for (int j = 0; j < n; j++)
        {
            float faceIn = LLB.AngleOf(HuddleCentre - to[j]);
            int k = System.Array.IndexOf(lookers, j);
            if (k >= 0)
            {
                Face(j, a1, lookAt + 0.5f * k - a1, faceIn);
                FacePoint(j, lookAt + 0.5f * k, LookOut, to[j], Swarm.Participant);
                Face(j, lookAt + 0.5f * k + LookOut, introEnd - (lookAt + 0.5f * k + LookOut), faceIn);
            }
            else { Face(j, a1, introEnd - a1, faceIn); }
            float w1 = a1 + 0.9f * Shapes.Hash(j, 21f), w2 = a2 + 0.6f * Shapes.Hash(j, 23f);
            Wiggle(j, w1, 1.2f + 1.0f * Shapes.Hash(j, 22f), 24f, 0.24f + 0.08f * Shapes.Hash(j, 24f));
            if (k < 0) { Wiggle(j, lookAt + 0.3f + Shapes.Hash(j, 25f), 0.8f, 18f, 0.25f); }
            Wiggle(j, w2, 1.0f + 0.8f * Shapes.Hash(j, 26f), 24f, 0.24f + 0.08f * Shapes.Hash(j, 27f));
            if (j != messenger) { InLaneFor(j, 0f, curlAt, 90f); }
        }
        Section(huddleWalkAt, "intro: back to its place; the line curls into a huddle");
        Section(a1, "intro: they argue among themselves");
        Section(lookAt, "intro: two or three look at the participant");
        Section(a2, "intro: back to arguing");
    }

    Vector2 IntroPath(int i, float t)
    {
        if (huddle && t >= curlAt) { return Flatten(i, HuddleCentre, huddleDeg[i], HuddleR, 1f - (t - curlAt) / CurlSecs); }
        if (i != messenger)
        {
            if (nods == null || nods[i] == 0) { return line[i]; }
            float y = 0f;
            for (int k = 0; k < nods[i]; k++) { y += Nod * Show.Bump((t - nodAt[i] - k * 0.95f) / NodSecs); }
            return line[i] + new Vector2(0f, y);   // a nod: a little step toward the messenger and back
        }
        Vector2 home = line[messenger];
        if (t < approachAt) { return home; }
        if (huddle && t >= huddleWalkAt)
        {
            // back into its place from straight below it
            float u = Shapes.Ease((t - huddleWalkAt) / HuddleWalkSecs), v = 1f - u;
            Vector2 c = new Vector2(home.x, visit.y);
            return v * v * visit + 2f * u * v * c + u * u * home;
        }
        if (t < walkBackAt)
        {
            float u = Shapes.Ease((t - approachAt) / ApproachSecs), v = 1f - u;
            Vector2 c = new Vector2(home.x, visit.y - 6f);   // straight out of the line first, then over
            return v * v * home + 2f * u * v * c + u * u * visit;
        }
        return Vector2.Lerp(visit, talk, Shapes.Ease((t - walkBackAt) / WalkBackSecs));
    }

    // ---------------- The closing, the same in every routine ----------------
    // From a ring round LLB.Centre (each routine's middle ends there, at rest; its places open into the
    // line without crossing) the robots slowly go back into the line, and everyone turns to look at
    // the participant.
    public const float CloseR = 80f;
    const float OpenSecs = 13f, FaceSecs = 4f;
    public const float ClosingSecs = OpenSecs + FaceSecs;
    float closeAt, faceAt;
    float[] ringDeg;

    void BuildClosing()
    {
        closeAt = Total - ClosingSecs;
        faceAt = closeAt + OpenSecs;
        ringDeg = FlatRingDeg(LLB.Centre, CloseR);
        for (int i = 0; i < n; i++) { FacePoint(i, faceAt, Total - faceAt + 1f, line[i], Swarm.Participant); }
    }

    Vector2 Closing(int i, float t)
    {
        return t < faceAt ? Flatten(i, LLB.Centre, ringDeg[i], CloseR, (t - closeAt) / OpenSecs) : line[i];
    }

    void AddClosingSections()
    {
        Section(closeAt, "closing: slowly back into the line");
        Section(faceAt, "closing: everyone looks at the participant");
    }

    // ---------------- Sections (markers in the sensor recording) ----------------
    protected void Section(float t, string name) { sections.Add(new KeyValuePair<float, string>(t, name)); }
    public IEnumerable<KeyValuePair<float, string>> Sections { get { return sections.OrderBy(s => s.Key); } }

    // ---------------- Lanes: robot i only drives forward and back, facing `deg` ----------------
    struct Lane { public int i; public float from, to, deg; }
    protected void InLaneFor(int i, float from, float to, float deg = 90f) { lanes.Add(new Lane { i = i, from = from, to = to, deg = deg }); }
    public bool InLane(int i, float t)
    {
        foreach (var l in lanes) { if (l.i == i && t >= l.from && t < l.to) { return true; } }
        return false;
    }
    public float Heading(int i, float t)
    {
        foreach (var l in lanes) { if (l.i == i && t >= l.from && t < l.to) { return l.deg; } }
        return float.NaN;
    }

    // ---------------- Turning on the spot ----------------
    enum Kind { Spin, Wiggle, Face, Custom }
    class Act { public int i; public float from, to, speed, period, deg; public Kind kind; public System.Func<float, float> custom; }

    // Spin on the spot at `speed` (wheel speed, + clockwise), easing in and out over `ease` s
    protected void Spin(int i, float from, float secs, float speed, float ease = 0.7f)
    {
        acts.Add(new Act { i = i, from = from, to = from + secs, speed = speed, period = ease, kind = Kind.Spin });
    }
    // Spin on the spot at a speed that changes: speedAt(t) (wheel speed, + clockwise, t in plan seconds)
    protected void SpinBy(int i, float from, float to, System.Func<float, float> speedAt)
    {
        acts.Add(new Act { i = i, from = from, to = to, kind = Kind.Custom, custom = speedAt });
    }
    // A shake on the spot, as when talking: left and right at `speed`, one swing a `period`
    protected void Wiggle(int i, float from, float secs, float speed = 25f, float period = 0.32f)
    {
        acts.Add(new Act { i = i, from = from, to = from + secs, speed = speed, period = period, kind = Kind.Wiggle });
    }
    // Turn on the spot to face `deg` and hold it
    protected void Face(int i, float from, float secs, float deg)
    {
        acts.Add(new Act { i = i, from = from, to = from + secs, deg = deg, kind = Kind.Face });
    }
    protected void FacePoint(int i, float from, float secs, Vector2 standing, Vector2 point)
    {
        Face(i, from, secs, LLB.AngleOf(point - standing));
    }

    public bool InPlace(int i, float t, out float spin, out float faceDeg)
    {
        spin = 0f; faceDeg = float.NaN;
        bool any = false;
        foreach (var a in acts)
        {
            if (a.i != i || t < a.from || t >= a.to) { continue; }
            any = true;
            float u = t - a.from, left = a.to - t;
            switch (a.kind)
            {
                case Kind.Spin: spin += a.speed * Shapes.Ease(u / a.period) * Shapes.Ease(left / a.period); break;
                case Kind.Wiggle: spin += a.speed * Mathf.Sin(2f * Mathf.PI * u / (2f * a.period)) * Shapes.Ease(u / 0.15f) * Shapes.Ease(left / 0.15f); break;
                case Kind.Face: faceDeg = a.deg; break;
                case Kind.Custom: spin += a.custom(t); break;
            }
        }
        return any;
    }

    // ---------------- The heart rate, for routines that follow it ----------------
    // Null: no sensor (the script alone). Set by LLBShow on the robots, by the checks in tests.
    public LLB.IHeart Heart;
    public const float HeartMargin = 10f;      // the heart rate moves a routine's tempo at most this far from its script
    public const float HeartSlewPerSec = 1.5f; // and the difference changes at most this fast (beats a minute per second)

    // A tempo that follows the script (beats a minute at plan time t), moved toward the participant's
    // heart rate by at most HeartMargin, the move changing slowly; Beats(t) is its running count of
    // beats (from `from`). Robots' positions never depend on it, only how fast they go round.
    protected class LiveTempo
    {
        const float Step = 0.02f;
        readonly LLBPlan plan; readonly System.Func<float, float> script; readonly float from, cap;
        readonly List<float> beats = new List<float>(), used = new List<float>();
        float offset;
        public LiveTempo(LLBPlan plan, System.Func<float, float> script, float from, float cap = 1000f)
        {
            this.plan = plan; this.script = script; this.from = from; this.cap = cap;
            beats.Add(0f); used.Add(Mathf.Min(cap, script(from)));
        }
        void Extend(float t)
        {
            int need = Mathf.CeilToInt((t - from) / Step) + 1;
            while (beats.Count <= need)
            {
                float tt = from + beats.Count * Step;
                float s = script(tt), want = 0f;
                if (plan.Heart != null)
                {
                    float live = plan.Heart.Bpm(tt);
                    if (!float.IsNaN(live)) { want = Mathf.Clamp(live - s, -HeartMargin, HeartMargin); }
                }
                offset += Mathf.Clamp(want - offset, -HeartSlewPerSec * Step, HeartSlewPerSec * Step);
                float u = Mathf.Min(cap, s + offset);
                used.Add(u);
                beats.Add(beats[beats.Count - 1] + u / 60f * Step);
            }
        }
        // beats counted since `from`
        public float Beats(float t)
        {
            if (t <= from) { return 0f; }
            Extend(t);
            float x = (t - from) / Step; int j = Mathf.FloorToInt(x);
            return Mathf.Lerp(beats[j], beats[j + 1], x - j);
        }
        // the tempo used at t, beats a minute
        public float Bpm(float t)
        {
            if (t <= from) { return used[0]; }
            Extend(t);
            return used[Mathf.Min(used.Count - 1, Mathf.RoundToInt((t - from) / Step))];
        }
        // how much faster than the script (1 = as scripted)
        public float Factor(float t) { return Bpm(t) / Mathf.Max(1f, Mathf.Min(cap, script(t))); }
    }

    // The running total of rate(t) from `from` on, worked out as far as asked (so the rate may follow the
    // live heart rate, which is only known as time goes by)
    protected class Running
    {
        const float Step = 0.02f;
        readonly System.Func<float, float> rate; readonly float from;
        readonly List<float> sum = new List<float> { 0f };
        public Running(System.Func<float, float> rate, float from) { this.rate = rate; this.from = from; }
        public float At(float t)
        {
            if (t <= from) { return 0f; }
            float x = (t - from) / Step; int j = Mathf.FloorToInt(x);
            while (sum.Count <= j + 1) { float tt = from + (sum.Count - 0.5f) * Step; sum.Add(sum[sum.Count - 1] + rate(tt) * Step); }
            return Mathf.Lerp(sum[j], sum[j + 1], x - j);
        }
    }

    // ---------------- Small path helpers ----------------

    // How far robot i may stray from a base plan without coming closer than `keep` to anybody: half
    // of its margin to its nearest neighbour (both may stray toward each other), at most `max`. It is
    // the lowest margin within 1.5 s either side, averaged three times over 1 s, so it changes gently and
    // never exceeds the margin at that moment. Use it to scale loops, wiggles and waves added on top.
    protected class Room
    {
        const float Step = 0.05f;
        readonly float from; readonly float[][] room;
        public Room(int n, System.Func<int, float, Vector2> basePos, float from, float to, float keep, float max)
        {
            this.from = from;
            int k = Mathf.Max(2, Mathf.CeilToInt((to - from) / Step) + 1);
            room = new float[n][];
            var raw = new float[n][];
            for (int i = 0; i < n; i++) { raw[i] = new float[k]; }
            var p = new Vector2[n];
            for (int j = 0; j < k; j++)
            {
                float t = from + j * Step;
                for (int i = 0; i < n; i++) { p[i] = basePos(i, t); }
                for (int i = 0; i < n; i++)
                {
                    float d = float.MaxValue;
                    for (int o = 0; o < n; o++) { if (o != i) { d = Mathf.Min(d, (p[i] - p[o]).magnitude); } }
                    raw[i][j] = Mathf.Clamp((d - keep) / 2f, 0f, max);
                }
            }
            int w = Mathf.RoundToInt(1.5f / Step), h = Mathf.RoundToInt(0.5f / Step);
            for (int i = 0; i < n; i++)
            {
                var low = new float[k];
                for (int j = 0; j < k; j++)
                {
                    float m = max;
                    for (int q = Mathf.Max(0, j - w); q <= Mathf.Min(k - 1, j + w); q++) { m = Mathf.Min(m, raw[i][q]); }
                    low[j] = m;
                }
                room[i] = Average(Average(Average(low, h), h), h);
                room[i][0] = 0f; room[i][k - 1] = 0f;
            }
        }
        static float[] Average(float[] a, int h)
        {
            var b = new float[a.Length];
            for (int j = 0; j < a.Length; j++)
            {
                float s = 0f; int c = 0;
                for (int q = j - h; q <= j + h; q++) { s += a[Mathf.Clamp(q, 0, a.Length - 1)]; c++; }
                b[j] = s / c;
            }
            return b;
        }
        public float At(int i, float t)
        {
            float x = (t - from) / Step;
            var r = room[i];
            if (x <= 0f || x >= r.Length - 1) { return 0f; }
            int j = Mathf.FloorToInt(x);
            return Mathf.Lerp(r[j], r[j + 1], x - j) * Shapes.Ease(x / 10f) * Shapes.Ease((r.Length - 1 - x) / 10f);
        }
    }

    // The running total of rate(t) from `from` on, tabulated every 0.02 s up to `to` (for phases whose
    // rate wanders, so has no formula)
    protected class Accumulated
    {
        const float Step = 0.02f;
        readonly float from; readonly float[] sum;
        public Accumulated(System.Func<float, float> rate, float from, float to)
        {
            this.from = from;
            int k = Mathf.Max(1, Mathf.CeilToInt((to - from) / Step));
            sum = new float[k + 1];
            for (int j = 1; j <= k; j++) { float t = from + (j - 0.5f) * Step; sum[j] = sum[j - 1] + rate(t) * Step; }
        }
        public float At(float t)
        {
            float x = (t - from) / Step;
            if (x <= 0f) { return 0f; }
            int j = Mathf.FloorToInt(x);
            if (j >= sum.Length - 1) { return sum[sum.Length - 1]; }
            return Mathf.Lerp(sum[j], sum[j + 1], x - j);
        }
    }

    protected static Vector2 Morph(Vector2 a, Vector2 b, float t, float from, float secs)
    {
        return Vector2.Lerp(a, b, Shapes.Ease((t - from) / secs));
    }

    // ---------------- A ring that opens into a row ----------------

    // Ring places (degrees round c) in the order that flattens into the line with nobody crossing
    // (V2Show.RingOrder: the left end, the even slots along one side, the right end, the odd slots
    // back along the other), turned to lie as close to the line spots as it can.
    protected float[] FlatRingDeg(Vector2 c, float r)
    {
        var place = V2Show.RingOrder(n);
        float best = float.MaxValue; float[] bestDeg = null;
        foreach (int sense in new[] { 1, -1 })
        {
            for (int k = 0; k < 360; k += 2)
            {
                var deg = new float[n]; float cost = 0f;
                for (int i = 0; i < n; i++) { deg[i] = k + sense * 360f * place[i] / n; cost += (LLB.On(c, deg[i], r) - line[i]).sqrMagnitude; }
                if (cost < best) { best = cost; bestDeg = deg; }
            }
        }
        return bestDeg;
    }

    // Robot i, standing at deg0 on a ring of radius r0 round c0 (a FlatRingDeg place), u of the way
    // (0..1) to its place in a row: the ring squashes into an ever flatter ellipse as wide as the row,
    // which settles onto the row. Robots keep their order round it, so nobody crosses, and the two
    // sides of the ring interleave into the row. By default the row is the line.
    const float FlattenWiden = 1.5f;
    protected Vector2 Flatten(int i, Vector2 c0, float deg0, float r0, float u) { return Flatten(i, c0, deg0, r0, u, LLB.LineY, LLB.Spacing); }
    protected Vector2 Flatten(int i, Vector2 c0, float deg0, float r0, float u, float rowY, float spacing)
    {
        float hw = Mathf.Max(1f, (n - 1) / 2f * spacing);
        float dx = Mathf.Clamp((i - (n - 1) / 2f) * spacing / hw, -1f, 1f);
        float side = Mathf.Sin(deg0 * Mathf.Deg2Rad) >= 0f ? 1f : -1f;
        float toDeg = LLB.AngleOf(new Vector2(dx, side * Mathf.Sqrt(Mathf.Max(0f, 1f - dx * dx))));
        toDeg = deg0 + Mathf.DeltaAngle(deg0, toDeg);
        float e = Shapes.Ease(u), wide = Shapes.Ease(u * FlattenWiden);
        float a = Mathf.Lerp(deg0, toDeg, e) * Mathf.Deg2Rad;
        Vector2 c = Vector2.Lerp(c0, new Vector2(Swarm.Centre, rowY), e);
        return c + new Vector2(Mathf.Lerp(r0, hw, wide) * Mathf.Cos(a), Mathf.Lerp(r0, 0f, e) * Mathf.Sin(a));
    }

    // Positions of every robot at time t (handy for planning the next rest-to-rest step)
    protected Vector2[] All(float t) { var p = new Vector2[n]; for (int i = 0; i < n; i++) { p[i] = PathAll(i, t); } return p; }
}
