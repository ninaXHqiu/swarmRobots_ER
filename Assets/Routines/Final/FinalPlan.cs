using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// One of the final routines, as planned in the simulator (tools/final-sim): every robot's position
// and heading, 10 times a second, read from a data file in Resources/FinalRoutines that
// tools/final-sim/export.js writes. Nothing here decides the choreography; it only plays it back.
//
//   Position(i, t)   where robot i should be t seconds after the start (smooth between samples)
//   Heading(i, t)    which way it should face (mat degrees: 0 right, 90 down toward the participant)
//   InPlace(i, t)    whether robot i is turning where it stands at t (looking at someone, spinning):
//                    then it is turned on the spot toward Heading, with the planned turning speed,
//                    instead of being driven along its path
//
// Robot i is the robot on line slot i (robot 1 is the messenger), as in the LLB routines.
public class FinalPlan : V2Show.IPlan, V2Show.ISections, V2Show.INoSafetyPush
{
    public string Name { get; private set; }
    public int Robots { get; private set; }
    public float Total { get; private set; }
    readonly float fps;
    readonly float[,,] at;      // [frame, robot, 0 = x, 1 = y, 2 = heading deg]
    readonly bool[,] still;     // [frame, robot]: the robot stands (it only turns) around this frame
    readonly List<KeyValuePair<float, string>> sections = new List<KeyValuePair<float, string>>();

    // A robot counts as standing when its planned speed stays under this (mat units a second)
    // over StillWindow seconds each side; the window keeps it from switching back and forth.
    const float StillSpeed = 3f, StillWindow = 0.2f;

    public static FinalPlan Load(string resource)
    {
        var asset = Resources.Load<TextAsset>("FinalRoutines/" + resource);
        if (asset == null) { throw new Exception("Final routine data not found: Resources/FinalRoutines/" + resource + ".txt"); }
        return new FinalPlan(asset.text);
    }

    public FinalPlan(string text)
    {
        var lines = text.Split('\n');
        int k = 0, frames = 0;
        fps = 10f; Robots = 8; Name = "";
        for (; k < lines.Length; k++)
        {
            string line = lines[k].TrimEnd('\r');
            if (line.Length == 0 || line[0] == '#') { continue; }
            int sp = line.IndexOf(' ');
            string key = sp < 0 ? line : line.Substring(0, sp), rest = sp < 0 ? "" : line.Substring(sp + 1);
            if (key == "name") { Name = rest; }
            else if (key == "robots") { Robots = int.Parse(rest, CultureInfo.InvariantCulture); }
            else if (key == "fps") { fps = float.Parse(rest, CultureInfo.InvariantCulture); }
            else if (key == "section")
            {
                int s2 = rest.IndexOf(' ');
                sections.Add(new KeyValuePair<float, string>(float.Parse(rest.Substring(0, s2), CultureInfo.InvariantCulture), rest.Substring(s2 + 1)));
            }
            else if (key == "frames") { frames = int.Parse(rest, CultureInfo.InvariantCulture); k++; break; }
        }
        if (frames < 2) { throw new Exception("Final routine data has no frames"); }
        at = new float[frames, Robots, 3];
        for (int f = 0; f < frames; f++, k++)
        {
            var v = lines[k].Trim().Split(' ');
            if (v.Length < 3 * Robots) { throw new Exception("Final routine data: frame " + f + " is short"); }
            for (int i = 0; i < Robots; i++)
            {
                at[f, i, 0] = int.Parse(v[3 * i], CultureInfo.InvariantCulture) / 10f;
                at[f, i, 1] = int.Parse(v[3 * i + 1], CultureInfo.InvariantCulture) / 10f;
                at[f, i, 2] = int.Parse(v[3 * i + 2], CultureInfo.InvariantCulture) / 10f;
            }
        }
        Total = (frames - 1) / fps;

        // which frames each robot only turns in
        still = new bool[frames, Robots];
        int w = Mathf.Max(1, Mathf.RoundToInt(StillWindow * fps));
        for (int i = 0; i < Robots; i++)
        {
            for (int f = 0; f < frames; f++)
            {
                bool s = true;
                for (int g = Mathf.Max(1, f - w); g <= Mathf.Min(frames - 1, f + w) && s; g++)
                {
                    float dx = at[g, i, 0] - at[g - 1, i, 0], dy = at[g, i, 1] - at[g - 1, i, 1];
                    if (Mathf.Sqrt(dx * dx + dy * dy) * fps > StillSpeed) { s = false; }
                }
                still[f, i] = s;
            }
        }
    }

    int Frames { get { return at.GetLength(0); } }

    void Bracket(float t, out int f0, out float u)
    {
        float x = Mathf.Clamp(t, 0f, Total) * fps;
        f0 = Mathf.Min(Frames - 2, Mathf.FloorToInt(x));
        u = x - f0;
    }

    // Catmull-Rom through the samples: smooth position and speed between them
    public Vector2 Position(int i, float t)
    {
        int f; float u;
        Bracket(t, out f, out u);
        int fa = Mathf.Max(0, f - 1), fb = f, fc = f + 1, fd = Mathf.Min(Frames - 1, f + 2);
        return new Vector2(CatmullRom(at[fa, i, 0], at[fb, i, 0], at[fc, i, 0], at[fd, i, 0], u),
                           CatmullRom(at[fa, i, 1], at[fb, i, 1], at[fc, i, 1], at[fd, i, 1], u));
    }

    static float CatmullRom(float a, float b, float c, float d, float u)
    {
        float u2 = u * u, u3 = u2 * u;
        return 0.5f * (2f * b + (-a + c) * u + (2f * a - 5f * b + 4f * c - d) * u2 + (-a + 3f * b - 3f * c + d) * u3);
    }

    public float Heading(int i, float t)
    {
        int f; float u;
        Bracket(t, out f, out u);
        float h0 = at[f, i, 2];
        return h0 + Mathf.DeltaAngle(h0, at[f + 1, i, 2]) * u;
    }

    // Planned turning speed (deg/s, + = clockwise on the mat) at t
    public float TurnRate(int i, float t)
    {
        const float h = 0.1f;
        return Mathf.DeltaAngle(Heading(i, t - h), Heading(i, t + h)) / (2f * h);
    }

    public bool InPlace(int i, float t)
    {
        int f; float u;
        Bracket(t, out f, out u);
        return still[u < 0.5f ? f : f + 1, i];
    }

    public Vector2 LineSpot(int i) { return Position(i, Total); }
    public bool InLane(int i, float t) { return false; }
    public IEnumerable<KeyValuePair<float, string>> Sections { get { return sections; } }
}
