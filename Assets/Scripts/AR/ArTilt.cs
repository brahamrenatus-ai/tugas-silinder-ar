using UnityEngine;

// How the can is tilted as seen on the screen, and an upright frame for things that must stay readable.
public static class ArTilt
{
    const float MaxPitch = 35f;

    // Direction of the can's top on the screen: y = 1 upright, x = 1 lying with the top to the right.
    public static Vector2 Lean(Transform can, Camera cam)
    {
        return cam.transform.InverseTransformDirection(can.up);
    }

    // 0 while the can is near upright, 1 when it lies with its top to the positive side.
    public static float Side(float lean)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.9f, lean));
    }

    // 1 while the top of the can points at the viewer, -1 while the bottom does, 0 otherwise.
    public static float EndOn(Transform can, Camera cam)
    {
        float depth = cam.transform.InverseTransformDirection(can.up).z;
        return -Mathf.Sign(depth) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.85f, 0.97f, Mathf.Abs(depth)));
    }

    // Up on the screen, following how far the can leans towards or away from the viewer.
    public static Vector3 Up(Transform can, Camera cam)
    {
        Vector3 top = cam.transform.InverseTransformDirection(can.up);
        float along = new Vector2(top.x, top.y).magnitude;
        float pitch = Mathf.Atan2(top.z * Mathf.Clamp(top.y / 0.3f, -1f, 1f), along) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, -MaxPitch, MaxPitch) * Mathf.Deg2Rad;
        return cam.transform.TransformDirection(new Vector3(0f, Mathf.Cos(pitch), Mathf.Sin(pitch)));
    }
}
