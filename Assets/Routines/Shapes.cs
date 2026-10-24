using UnityEngine;

// Shapes shared by routines. Angles in degrees, positions in mat coordinates.
public static class Shapes
{
    public static readonly Vector2 Centre = new Vector2(Swarm.Centre, Swarm.Centre);

    // Point on a ring around the mat centre
    public static Vector2 OnCircle(float angleDeg, float radius)
    {
        float a = angleDeg * Mathf.Deg2Rad;
        return Centre + radius * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    // Point on a flower outline: the radius swells into `petals` petals around the ring.
    // A robot whose angle keeps increasing travels along the petal outline (in and out of
    // each petal) instead of circling at a fixed distance. bloom 0 = plain circle, 1 = full flower.
    public static Vector2 OnFlower(float angleDeg, float radius, float bloom, int petals = 5)
    {
        float a = angleDeg * Mathf.Deg2Rad;
        float r = radius * (1f + bloom * 0.4f * Mathf.Cos(petals * a));
        return Centre + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
    }

    // Small repeatable "random" number in [0, 1) for robot/pair i and a salt
    public static float Hash(int i, float salt)
    {
        float v = Mathf.Sin(i * 12.9898f + salt * 78.233f) * 43758.5453f;
        return v - Mathf.Floor(v);
    }
}
