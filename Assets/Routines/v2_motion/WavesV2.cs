using System.Collections;
using System.Linq;
using UnityEngine;

// v2 motion. Busy, irregular movement slowly organises into tight, rapid travelling waves,
// which broaden and calm down, then settle into a still, straight line.
// Starts from the shared line and ends on it. One continuous movement: every phase fades in
// while the previous one fades out, and nothing stops until the robots come to rest in the line.
public class WavesV2 : SwarmRoutine
{
    public override string Name { get { return "v2 motion: Busy -> waves -> line"; } }
    public override int MinRobots { get { return 2; } }

    // ---- Timing (seconds), in order ----
    const float EnterSecs = 4f;      // robots spread out of the line, already busy
    const float BusySecs = 7f;       // A: busy, irregular movement
    const float OrganizeSecs = 8f;   // B: busy movement fades, row and tight wave emerge
    const float BusyFadeSecs = 5f;   //    (busy wiggles are gone this far into B, before the row closes up)
    const float QuickSecs = 5f;      // tight, rapid wave on its own
    const float CalmSecs = 9f;       // C: wave broadens and slows
    const float BroadSecs = 5f;      // broad, calm wave on its own
    const float SettleSecs = 7f;     // D: wave height fades to zero, robots still on the line

    // ---- Waves ----
    const float QuickHz = 0.7f;          // rapid wave: cycles per second
    const float CalmHz = 0.22f;          // calm wave
    const float SettleHz = 0.15f;        // keeps slowing a little while it settles
    const float QuickWavelength = 140f;  // mat units: tight, about 4 robots per wave
    const float CalmWavelength = 520f;   // broad: less than one wave across 8 robots
    const float QuickHeight = 22f;       // mat units up/down from the line
    const float CalmHeight = 32f;        // modest growth, so the broad wave reads clearly

    // ---- Busy opening ----
    // Each robot darts around its own spot in short, unequal moves (rounded corners, no stops).
    // Spots sit roughly above/below its place in the line, spread 1.2x wider; the numbers below
    // make the arrangement irregular (repeat for robots 9+).
    const float AnchorSpread = 1.2f;
    static readonly float[] AnchorY = { 45, -38, 52, -30, 40, -55, 28, -42 };
    static readonly float[] AnchorJitterX = { 3, -4, 2, -3, 4, -2, -3, 2 };
    const int Points = 6;                    // corners per robot's loop
    const float ReachMin = 8f, ReachMax = 20f; // distance of each corner from the robot's spot
    const float MoveMin = 0.55f, MoveMax = 1.1f; // seconds per move
    const float CornerFlow = 0.35f;          // speed kept through a corner (0 = full stop, 1 = round)

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return Swarm.StartInLine(ctx);
        var plan = new Plan(ctx);
        yield return Swarm.Flow(ctx, plan.Position, plan.Total, maxSpeed: 100);
    }

    class Plan
    {
        readonly int n;
        readonly float tB, tC, tBroad, tS;
        public readonly float Total;
        readonly Vector2[] start, anchor;
        readonly float[] rowX;               // offset from the mat centre
        readonly Vector2[][] corner, cornerVelocity;
        readonly float[][] move;

        public Plan(RoutineContext ctx)
        {
            n = ctx.Count;
            tB = EnterSecs + BusySecs;
            tC = tB + OrganizeSecs + QuickSecs;
            tBroad = tC + CalmSecs;
            tS = tBroad + BroadSecs;
            Total = tS + SettleSecs;

            // Robots keep their left-to-right order from the line for the whole routine
            var byX = Enumerable.Range(0, n).OrderBy(i => ctx.Cubes[i].x).ToArray();
            start = new Vector2[n]; anchor = new Vector2[n]; rowX = new float[n];
            corner = new Vector2[n][]; cornerVelocity = new Vector2[n][]; move = new float[n][];
            for (int rank = 0; rank < n; rank++)
            {
                int i = byX[rank];
                start[i] = new Vector2(ctx.Cubes[i].x, ctx.Cubes[i].y);
                rowX[i] = (rank - (n - 1) / 2f) * Swarm.LineSpacing;
                anchor[i] = new Vector2(Swarm.Centre + rowX[i] * AnchorSpread + AnchorJitterX[rank % 8], Swarm.Centre + AnchorY[rank % 8]);
                BusyLoop(rank, out corner[i], out move[i], out cornerVelocity[i]);
            }
        }

        // One robot's busy loop: corners at irregular directions and distances, moves of
        // irregular length (fixed per robot, no new randomness while running)
        static void BusyLoop(int rank, out Vector2[] corners, out float[] moves, out Vector2[] velocities)
        {
            corners = new Vector2[Points]; moves = new float[Points]; velocities = new Vector2[Points];
            for (int k = 0; k < Points; k++)
            {
                int s = rank * Points + k;
                float angle = 2f * Mathf.PI * Frac(0.618034f * s + 0.13f * rank);
                float reach = Mathf.Lerp(ReachMin, ReachMax, Frac(0.7548777f * s + 0.31f));
                corners[k] = reach * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                moves[k] = Mathf.Lerp(MoveMin, MoveMax, Frac(0.5698403f * s + 0.47f));
            }
            for (int k = 0; k < Points; k++)
            {
                int prev = (k + Points - 1) % Points, next = (k + 1) % Points;
                Vector2 vIn = (corners[k] - corners[prev]) / moves[prev];
                Vector2 vOut = (corners[next] - corners[k]) / moves[k];
                velocities[k] = CornerFlow * (vIn + vOut) / 2f;
            }
        }

        static float Frac(float v) { return v - Mathf.Floor(v); }

        // Offset from the robot's spot along its busy loop at time t
        Vector2 Busy(int i, float t)
        {
            float period = move[i].Sum();
            t = Mathf.Repeat(t, period);
            int k = 0;
            while (k < Points - 1 && t > move[i][k]) { t -= move[i][k]; k++; }
            float d = move[i][k], u = Mathf.Clamp01(t / d);
            int next = (k + 1) % Points;
            // Quintic curve: matches position and speed at both corners, zero acceleration there
            float u3 = u * u * u, u4 = u3 * u, u5 = u4 * u;
            float h00 = 1f - 10f * u3 + 15f * u4 - 6f * u5;
            float h10 = u - 6f * u3 + 8f * u4 - 3f * u5;
            float h11 = -4f * u3 + 7f * u4 - 3f * u5;
            return h00 * corner[i][k] + (1f - h00) * corner[i][next] + d * (h10 * cornerVelocity[i][k] + h11 * cornerVelocity[i][next]);
        }

        // Shared wave phase in radians: a continuous accumulator of the (changing) frequency,
        // so slowing the wave never restarts or jumps it
        float Phase(float t)
        {
            float cycles = QuickHz * Mathf.Min(t, tC);
            if (t > tC)
            {
                float u = (Mathf.Min(t, tBroad) - tC) / CalmSecs;
                cycles += CalmSecs * (QuickHz * u + (CalmHz - QuickHz) * Shapes.EaseIntegral(u));
            }
            if (t > tBroad) { cycles += CalmHz * (Mathf.Min(t, tS) - tBroad); }
            if (t > tS)
            {
                float u = (t - tS) / SettleSecs;
                cycles += SettleSecs * (CalmHz * u + (SettleHz - CalmHz) * Shapes.EaseIntegral(u));
            }
            return 2f * Mathf.PI * cycles;
        }

        // Spatial frequency (radians per mat unit): lower = broader waves
        float SpatialFrequency(float t)
        {
            float quick = 2f * Mathf.PI / QuickWavelength, calm = 2f * Mathf.PI / CalmWavelength;
            return Mathf.Lerp(quick, calm, Shapes.Ease((t - tC) / CalmSecs));
        }

        float Height(float t)
        {
            float h = Mathf.Lerp(QuickHeight, CalmHeight, Shapes.Ease((t - tC) / CalmSecs));
            return h * (1f - Shapes.Ease((t - tS) / SettleSecs));
        }

        public Vector2 Position(int i, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            // Busy around its spot (wiggles fade out early in B), spreading out of the line at the start
            Vector2 busy = anchor[i] + (1f - Shapes.Ease((t - tB) / BusyFadeSecs)) * Busy(i, t);
            Vector2 p = Vector2.Lerp(start[i], busy, Shapes.Ease(t / EnterSecs));
            // The row and its travelling wave take over during B
            float y = Height(t) * Mathf.Sin(Phase(t) - SpatialFrequency(t) * rowX[i]);
            Vector2 wave = new Vector2(Swarm.Centre + rowX[i], Swarm.Centre + y);
            return Vector2.Lerp(p, wave, Shapes.Ease((t - tB) / OrganizeSecs));
        }
    }
}
