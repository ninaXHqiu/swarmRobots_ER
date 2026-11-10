using System;
using System.Collections;
using System.Linq;
using UnityEngine;

// Shared frame of the performance routines (Resonance and Cheering): the Messenger's full
// introduction, then a performance planned as one continuous path per robot, then the return to
// LineUp's line, facing the participant. A performance is built from repeated movement phrases
// (counts set in each routine), so its length follows from the phrases, not from a clock. It
// starts when the introduction has finished, from wherever the robots actually are.
public static class Show
{
    public const float ReturnSecs = 8f;   // the last phrase flows into the line over this long
    const float HoldSecs = 1f;            // robots hold their line spots this long after the path ends

    // Robot centres stay at least this far inside the mat's edge (Swarm.MatMin/MatMax): half of
    // the space a cube sweeps when it turns (32 mm cube: 16.5 mat units) plus room for tracking
    // and stopping error. The dance area for the robots' bodies is the mat inset by 18.5.
    public const float DanceMargin = 35f;

    // A performance: robot i's planned mat position t seconds after the introduction (it starts
    // where the robot is, and ends at rest on LineSpot(i) at Total)
    public interface IPlan
    {
        float Total { get; }
        Vector2 Position(int i, float t);
        Vector2 LineSpot(int i);
    }

    // When the current performance started (Time.time), for recordings; -1 before
    public static float StartedAt = -1f;

    public static IEnumerator Run(RoutineContext ctx, Func<Vector2[], IPlan> makePlan, int maxSpeed = 60, float axisDeg = float.NaN)
    {
        yield return Messenger.Introduction(ctx);
        StartedAt = Time.time;
        int n = ctx.Count;
        var start = new Vector2[n];
        for (int i = 0; i < n; i++) { start[i] = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y); }
        var plan = makePlan(start);
        // The path ends at rest on the line spots; keep following it a moment longer so every
        // robot settles onto its spot with the same steering (no separate correction that would
        // turn robots sideways), then face the participant, as LineUp does
        yield return Swarm.Flow(ctx, plan.Position, plan.Total + HoldSecs, maxSpeed, axisDeg);
        yield return Swarm.TurnTo(ctx, Swarm.FacingParticipant, timeoutMs: 4000, rotateTimeMs: 1500);
    }

    // Robots at `from` the least total travel away from LineUp's spots: which spot each takes
    public static Vector2[] LineSpots(Vector2[] from)
    {
        var line = LineUp.Spots(from.Length).Select(s => (Vector2)s).ToArray();
        var slot = Swarm.LeastTravel(from, line);
        return Enumerable.Range(0, from.Length).Select(i => line[slot[i]]).ToArray();
    }

    // Line spots for robots that are mostly in the line already: each robot standing on a spot
    // keeps it, and the others (the messenger) take the spots left free
    public static Vector2[] KeepLineSpots(Vector2[] from)
    {
        int n = from.Length;
        var line = LineUp.Spots(n).Select(s => (Vector2)s).ToArray();
        var spot = new Vector2[n];
        var taken = new bool[n]; var placed = new bool[n];
        for (int i = 0; i < n; i++)
        {
            for (int k = 0; k < n; k++)
            {
                if (!taken[k] && (from[i] - line[k]).magnitude < 12f) { spot[i] = line[k]; taken[k] = placed[i] = true; break; }
            }
        }
        var rest = Enumerable.Range(0, n).Where(i => !placed[i]).ToArray();
        var free = Enumerable.Range(0, n).Where(k => !taken[k]).Select(k => line[k]).ToArray();
        var slot = Swarm.LeastTravel(rest.Select(i => from[i]).ToArray(), free);
        for (int r = 0; r < rest.Length; r++) { spot[rest[r]] = free[slot[r]]; }
        return spot;
    }

    // The shared return, from `from` on: the performance keeps going while each robot eases onto
    // its line spot
    public static Vector2 Return(Vector2 performing, Vector2 spot, float t, float from)
    {
        return Vector2.Lerp(performing, spot, Shapes.Ease((t - from) / ReturnSecs));
    }

    // Point on a ring around the mat centre (angle in radians, increasing = clockwise on the mat)
    public static Vector2 OnRing(float angle, float radius)
    {
        return Shapes.Centre + radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    // Evenly spaced ring places (radians, before `turned`) for robots at `from`: the rotation and
    // assignment with the least total travel, so robots take short, non-crossing paths onto it
    public static float[] RingPlaces(Vector2[] from, float radius, float turned)
    {
        float first;
        var slot = RingSlots(from, radius, turned, out first);
        return slot.Select(k => first + 2f * Mathf.PI * k / from.Length).ToArray();
    }

    // Same, as the order around the ring: robot i takes place slot[i], at angle
    // first + 2 pi slot[i] / n (before `turned`)
    public static int[] RingSlots(Vector2[] from, float radius, float turned, out float first)
    {
        int n = from.Length;
        int[] best = null; first = 0f;
        float bestCost = float.MaxValue;
        for (int k = 0; k < 36; k++)
        {
            float a0 = 2f * Mathf.PI / n * k / 36f;
            var spots = Enumerable.Range(0, n).Select(p => OnRing(a0 + 2f * Mathf.PI * p / n + turned, radius)).ToArray();
            var slot = Swarm.LeastTravel(from, spots);
            float cost = Enumerable.Range(0, n).Sum(i => (from[i] - spots[slot[i]]).sqrMagnitude);
            if (cost < bestCost) { bestCost = cost; best = slot; first = a0; }
        }
        return best;
    }

    // Robots at `from` swing round the mat centre onto ring places while moving in or out to the
    // ring, so they keep clear of each other (used to form a ring from the line). Returns, per
    // robot, the start angle and the swing to its place at the end of forming.
    public static void SwingOnto(Vector2[] from, float[] placeAtEnd, out float[] fromAngle, out float[] fromRadius, out float[] swing)
    {
        int n = from.Length;
        fromAngle = new float[n]; fromRadius = new float[n]; swing = new float[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 d = from[i] - Shapes.Centre;
            fromAngle[i] = Mathf.Atan2(d.y, d.x);
            fromRadius[i] = d.magnitude;
            // A robot (almost) on the centre has no direction of its own: it heads straight out
            if (fromRadius[i] < 10f) { fromAngle[i] = placeAtEnd[i]; }
            swing[i] = Mathf.DeltaAngle(fromAngle[i] * Mathf.Rad2Deg, placeAtEnd[i] * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        }
    }

    // Smooth hump from 0 up to 1 and back to 0 as u goes 0 -> 1 (0 outside), starting and
    // ending at zero speed: one beat, one surge
    public static float Bump(float u)
    {
        if (u <= 0f || u >= 1f) { return 0f; }
        float s = Mathf.Sin(Mathf.PI * u);
        return s * s;
    }

    // A value set at keyframes (time, value, time, value, ...), easing from each keyframe's value
    // to the next one's (repeat a value to hold it). Integral gives the amount covered so far:
    // the distance a speed has carried, the cycles a frequency has run, so changing pace never jumps.
    public class Eased
    {
        readonly float[] time, value;

        public Eased(params float[] keys)
        {
            time = new float[keys.Length / 2]; value = new float[keys.Length / 2];
            for (int k = 0; k < time.Length; k++) { time[k] = keys[2 * k]; value[k] = keys[2 * k + 1]; }
        }

        public float At(float t)
        {
            if (t <= time[0]) { return value[0]; }
            for (int k = 0; k + 1 < time.Length; k++)
            {
                if (t < time[k + 1]) { return Mathf.Lerp(value[k], value[k + 1], Shapes.Ease((t - time[k]) / (time[k + 1] - time[k]))); }
            }
            return value[value.Length - 1];
        }

        public float Integral(float t)
        {
            float sum = value[0] * Mathf.Clamp(t, 0f, time[0]);
            for (int k = 0; k + 1 < time.Length && t > time[k]; k++)
            {
                float d = time[k + 1] - time[k];
                float u = (Mathf.Min(t, time[k + 1]) - time[k]) / d;
                sum += d * (value[k] * u + (value[k + 1] - value[k]) * Shapes.EaseIntegral(u));
            }
            int last = time.Length - 1;
            if (t > time[last]) { sum += value[last] * (t - time[last]); }
            return sum;
        }
    }

    // A phrase counter that sets its own pace: phrases (cycles) run at a speed that depends on how
    // many have been done, e.g. each heartbeat a little slower than the last. At(t) gives the
    // phrases done after t seconds; Duration is how long `phrases` take.
    public class Paced
    {
        const float Step = 0.01f;
        readonly float[] done, rate;
        public readonly float Duration;

        public Paced(Func<float, float> phrasesPerSec, float phrases)
        {
            var d = new System.Collections.Generic.List<float> { 0f };
            var r = new System.Collections.Generic.List<float> { phrasesPerSec(0f) };
            while (d[d.Count - 1] < phrases)
            {
                float x = d[d.Count - 1];
                float mid = x + phrasesPerSec(x) * Step / 2f;   // midpoint step
                d.Add(x + phrasesPerSec(mid) * Step);
                r.Add(phrasesPerSec(d[d.Count - 1]));
            }
            done = d.ToArray(); rate = r.ToArray();
            Duration = (done.Length - 1) * Step;
        }

        public float At(float t)
        {
            float x = Mathf.Clamp(t / Step, 0f, done.Length - 1.001f);
            int s = (int)x; float u = x - s;
            float u2 = u * u, u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * done[s] + (u3 - 2f * u2 + u) * rate[s] * Step
                 + (-2f * u3 + 3f * u2) * done[s + 1] + (u3 - u2) * rate[s + 1] * Step;
        }
    }

    // Running total of a rate that has no simple formula (e.g. turning speed that depends on a
    // changing radius), worked out once in small steps
    public class Integrated
    {
        const float Step = 0.02f;
        readonly float[] total, rate;

        public Integrated(Func<float, float> rateAt, float until)
        {
            int steps = Mathf.CeilToInt(until / Step) + 1;
            total = new float[steps + 1]; rate = new float[steps + 1];
            for (int s = 0; s <= steps; s++) { rate[s] = rateAt(s * Step); }
            for (int s = 1; s <= steps; s++) { total[s] = total[s - 1] + (rate[s - 1] + rate[s]) / 2f * Step; }
        }

        public float At(float t)
        {
            float x = Mathf.Clamp(t / Step, 0f, total.Length - 1.001f);
            int s = (int)x; float u = x - s;
            // Hermite between steps, so the rate stays continuous
            float u2 = u * u, u3 = u2 * u;
            return (2f * u3 - 3f * u2 + 1f) * total[s] + (u3 - 2f * u2 + u) * rate[s] * Step
                 + (-2f * u3 + 3f * u2) * total[s + 1] + (u3 - u2) * rate[s + 1] * Step;
        }
    }
}
