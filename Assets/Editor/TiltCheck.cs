using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Headless check for "random tilting": runs a simple model of a robot (drives forward or backward along its heading, turns at the commanded rate,
// 0.13 s command lag, position and heading noise like the real sensor) through the steering rule of V2Show.Steer, over each plan, and reports for
// every 5 s window how much the robots turned ("spin") against how much the path itself needs them to turn ("needed"), and how often they flipped
// between driving forward and backward. Spin well above needed is tilting. The rule below is a copy of V2Show.Steer's maths; keep them together.
//   Unity -batchmode -nographics -quit -projectPath <swarm> -executeMethod TiltCheck.Run -logFile <log>
public static class TiltCheck
{
    const float Dt = 1f / 30f, Lag = 0.13f, PosNoise = 1.5f, HeadNoiseDeg = 1.5f;
    const float FlowGain = 3f, SteerTurnSecs = 0.25f, FlipDeg = 110f, MaxSpeedCmd = 100f, VDotOverU = 2.06f;
    static float LowSpeed = 6f, FullTurn = 20f, FlipMinSpeed = 0f;

    public static void Run()
    {
        var line = V2Show.OwnSpots(8);
        var start = Enumerable.Range(0, 8).Select(i => i == V2Show.MessengerSlot(8) ? V2Show.TalkSpot : line[i]).ToArray();
        var plans = new (string, V2Show.IPlan)[]
        {
            ("C22 Cheerleading", new V2Cheerleading.Plan(start)),
            ("C24 CheerBreathing", new V2CheerBreathing.Plan(start)),
        };
        var sb = new StringBuilder("\nTILTCHECK\n");
        // variants of the steering rule: (heading smoothing s, turn gate low/full speed, smallest wanted speed at which forward/backward may flip)
        var variants = new (string, float, float, float, float)[]
        {
            ("DETAIL smooth 0.2", 0.2f, 6f, 20f, 0f),
            ("old rule", 0f, 6f, 20f, 0f),
            ("smooth 0.2", 0.2f, 6f, 20f, 0f),
            ("smooth 0.2 + gate 10/28", 0.2f, 10f, 28f, 0f),
            ("smooth 0.2 + gate 10/28 + flip only above 14", 0.2f, 10f, 28f, 14f),
            ("smooth 0.2 + gate 14/34 + flip only above 18", 0.2f, 14f, 34f, 18f),
        };
        foreach (var (name, plan) in plans)
            foreach (var v in variants)
            {
                LowSpeed = v.Item3; FullTurn = v.Item4; FlipMinSpeed = v.Item5;
                sb.Append(Simulate(name + " | " + v.Item1, plan, v.Item2, v.Item1.StartsWith("DETAIL")));
            }
        Debug.Log(sb.ToString());
    }

    static string Simulate(string name, V2Show.IPlan plan, float smooth, bool rows)
    {
        int n = 8; var rng = new System.Random(7);
        Func<double> noise = () => rng.NextDouble() * 2 - 1;
        var sections = (plan as V2Show.ISections)?.Sections.OrderBy(k => k.Key).ToList();
        var pos = new Vector2[n]; var head = new float[n]; var rev = new bool[n]; var hold = new bool[n]; var hw = new Vector2[n];
        var queue = new System.Collections.Generic.Queue<Vector2>[n];     // delayed commands: (drive, turn rate)
        var lastDir = new float[n]; var lastAxis = new float[n];
        var snap = new System.Collections.Generic.List<(float t, float[] head, Vector2[] pos)>(); int circling = 0, orbit = 0; string circAt = "", orbAt = ""; float nextSnap = 0f;
        for (int i = 0; i < n; i++)
        {
            pos[i] = plan.Position(i, 0f); head[i] = 0f; queue[i] = new System.Collections.Generic.Queue<Vector2>();
            for (int q = 0; q < Mathf.RoundToInt(Lag / Dt); q++) { queue[i].Enqueue(Vector2.zero); }
            var v0 = (plan.Position(i, 0.5f) - plan.Position(i, 0f)); head[i] = v0.sqrMagnitude > 0.01f ? Mathf.Atan2(v0.y, v0.x) : 0f;
            lastDir[i] = head[i]; lastAxis[i] = head[i];
        }
        float win = rows ? 1f : 5f; int winCount = Mathf.CeilToInt(plan.Total / win);
        var spin = new float[winCount]; var need = new float[winCount]; var flips = new int[winCount]; var maxErr = new float[winCount];
        for (float t = 0; t < plan.Total; t += Dt)
        {
            if (t >= nextSnap)
            {
                nextSnap += 0.5f; snap.Add((t, (float[])head.Clone(), (Vector2[])pos.Clone()));
                int back = snap.Count - 13;
                if (back >= 0) for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        float b0 = Mathf.Atan2(snap[back].pos[j].y - snap[back].pos[i].y, snap[back].pos[j].x - snap[back].pos[i].x), b1 = Mathf.Atan2(pos[j].y - pos[i].y, pos[j].x - pos[i].x);
                        if (Vector2.Distance(pos[i], pos[j]) < 80f && Vector2.Distance(snap[back].pos[i], snap[back].pos[j]) < 80f && Mathf.Abs(Mathf.DeltaAngle(b0 * Mathf.Rad2Deg, b1 * Mathf.Rad2Deg)) > 120f) { orbit++; if (orbAt.Length < 60) { orbAt += string.Format(" t={0:0} robots {1},{2};", t, i, j); } }
                    }
                    if (Mathf.Abs(head[i] - snap[back].head[i]) * Mathf.Rad2Deg > 100f && Vector2.Distance(pos[i], snap[back].pos[i]) < 30f) { circling++; if (circAt.Length < 60) { circAt += string.Format(" t={0:0} robot {1};", t, i); } snap.RemoveRange(0, 0); }
                }
            }
            int w = Mathf.Min(winCount - 1, (int)(t / win));
            for (int i = 0; i < n; i++)
            {
                // what the steering sees
                Vector2 here = pos[i] + PosNoise * new Vector2((float)noise(), (float)noise());
                float facing = head[i] + HeadNoiseDeg * Mathf.Deg2Rad * (float)noise();
                float at = t + Lag, h = 0.05f;
                Vector2 target = plan.Position(i, at);
                Vector2 pv = (plan.Position(i, at + h) - plan.Position(i, Mathf.Max(0f, at - h))) / (at + h - Mathf.Max(0f, at - h));
                Vector2 want = pv + FlowGain * (target - here);
                float err = (target - here).magnitude, sp = pv.magnitude;
                if (hold[i]) { if (sp > 4f || err > 9f) { hold[i] = false; } } else if (sp < 2.5f && err < 5f) { hold[i] = true; }
                Vector2 cmd = Vector2.zero;
                if (hold[i]) { hw[i] = Vector2.zero; }
                else
                {
                    float limit = MaxSpeedCmd * VDotOverU;
                    if (want.magnitude > limit) { want = want.normalized * limit; }
                    if (want.magnitude >= 2f)
                    {
                        Vector2 hwant = want;
                        if (smooth > 0f) { hw[i] = hw[i] == Vector2.zero ? want : Vector2.Lerp(hw[i], want, 1f - Mathf.Exp(-Dt / smooth)); hwant = hw[i]; }
                        float wantDeg = Mathf.Atan2(hwant.y, hwant.x) * Mathf.Rad2Deg;
                        float facingDeg = facing * Mathf.Rad2Deg + (rev[i] ? 180f : 0f);
                        float turnDeg = Mathf.DeltaAngle(facingDeg, wantDeg);
                        if (Mathf.Abs(turnDeg) > FlipDeg && want.magnitude >= FlipMinSpeed) { rev[i] = !rev[i]; turnDeg = Mathf.DeltaAngle(facingDeg + 180f, wantDeg); flips[w]++; }
                        float drive = want.magnitude * Mathf.Max(0f, Mathf.Cos(turnDeg * Mathf.Deg2Rad));
                        float believe = Mathf.Clamp01((hwant.magnitude - LowSpeed) / (FullTurn - LowSpeed));
                        cmd = new Vector2(rev[i] ? -drive : drive, believe * turnDeg * Mathf.Deg2Rad / SteerTurnSecs);
                    }
                }
                queue[i].Enqueue(cmd);
                var run = queue[i].Dequeue();
                head[i] += run.y * Dt;
                pos[i] += run.x * Dt * new Vector2(Mathf.Cos(head[i]), Mathf.Sin(head[i]));
                // tilt bookkeeping, on the facing axis (forward and backward are the same axis)
                float axis = Mathf.Repeat(head[i], Mathf.PI);
                float dAxis = Mathf.Abs(Mathf.DeltaAngle(lastAxis[i] * Mathf.Rad2Deg * 2f, axis * Mathf.Rad2Deg * 2f)) / 2f; lastAxis[i] = axis;
                spin[w] += dAxis;
                if (pv.magnitude > 3f)
                {
                    float d = Mathf.Repeat(Mathf.Atan2(pv.y, pv.x), Mathf.PI);
                    need[w] += Mathf.Abs(Mathf.DeltaAngle(lastDir[i] * Mathf.Rad2Deg * 2f, d * Mathf.Rad2Deg * 2f)) / 2f; lastDir[i] = d;
                    float miss = Mathf.Abs(Mathf.DeltaAngle(axis * Mathf.Rad2Deg * 2f, d * Mathf.Rad2Deg * 2f)) / 2f;
                    maxErr[w] = Mathf.Max(maxErr[w], miss);
                }
            }
        }
        var sb = new StringBuilder(string.Format("{0}  (heading smoothing {1:0.0} s)\n", name, smooth));
        for (int w = 0; rows && w < Mathf.Min(winCount, 17); w++)
        {
            float t0 = w * win; string sec = "";
            if (sections != null) { foreach (var k in sections) { if (k.Key <= t0 + 0.01f) { sec = k.Value; } } }
            sb.AppendFormat("  {0,5:0}-{1,5:0} s  spin {2,6:0} deg  needed {3,6:0} deg  flips {4,2}  worst miss {5,4:0} deg   {6}\n", t0, t0 + win, spin[w] / n, need[w] / n, flips[w], maxErr[w], sec);
        }
        { float worst = 0f, startWorst = 0f; var own = V2Show.OwnSpots(n); for (int i = 0; i < n; i++) { worst = Mathf.Max(worst, Vector2.Distance(plan.Position(i, plan.Total), own[i])); } sb.AppendFormat("  ends within {0:0.0} units of the own line spots (the line at y={1})\n", worst, own[0].y); }
        sb.AppendFormat("  circling in place (over 100 degrees within 6 s, moving under 30 units): {0} sightings{1}\n", circling, circAt);
        sb.AppendFormat("  two robots going round each other (close, bearing turned over 120 degrees in 6 s): {0} sightings{1}\n", orbit, orbAt);
        // summary per section: spin against needed, worst miss, flips
        if (sections != null)
        {
            sb.Append("  -- by section: spin/needed, worst miss, flips\n");
            for (int k = 0; k < sections.Count; k++)
            {
                float a = sections[k].Key, b = k + 1 < sections.Count ? sections[k + 1].Key : plan.Total;
                float sp = 0, nd = 0, mx = 0; int fl = 0;
                for (int w = 0; w < winCount; w++) { float t0 = w * win; if (t0 >= a - 0.01f && t0 < b - 0.01f) { sp += spin[w]; nd += need[w]; mx = Mathf.Max(mx, maxErr[w]); fl += flips[w]; } }
                sb.AppendFormat("     {0,-34} spin {1,5:0} / needed {2,5:0} deg   worst miss {3,3:0} deg   flips {4}\n", sections[k].Value, sp / n, nd / n, mx, fl);
            }
        }
        return sb.ToString();
    }
}
