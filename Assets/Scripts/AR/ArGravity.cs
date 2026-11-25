using UnityEngine;
using UnityEngine.InputSystem;

// Real-world up, read from the phone accelerometer. The AR camera does not turn with the phone,
// so the Y axis of the scene is only the top of the screen.
public static class ArGravity
{
    const float Smoothing = 8f;

    static Vector3 upInView = Vector3.up;
    static float unrest;
    static bool hasReading;
    static int readFrame = -1;

    public static Vector3 Up(Camera cam)
    {
        Read();
        return cam.transform.TransformDirection(upInView);
    }

    // How far the reading is from 1 g, which grows when the phone itself is being shaken.
    public static float Unrest
    {
        get
        {
            Read();
            return unrest;
        }
    }

    static void Read()
    {
        if (readFrame == Time.frameCount)
            return;
        readFrame = Time.frameCount;

        Accelerometer sensor = Accelerometer.current;
        if (sensor == null)
        {
            upInView = Vector3.up;
            unrest = 0f;
            hasReading = false;
            return;
        }
        if (!sensor.enabled)
            InputSystem.EnableDevice(sensor);

        Vector3 g = sensor.acceleration.ReadValue();
        if (g.sqrMagnitude < 0.01f)
            return;

        unrest = Mathf.Abs(g.magnitude - 1f);

        // Screen axes to camera axes: the back camera looks along -Z of the screen.
        Vector3 up = new Vector3(-g.x, -g.y, g.z).normalized;
        upInView = hasReading
            ? Vector3.Slerp(upInView, up, 1f - Mathf.Exp(-Smoothing * Time.deltaTime)).normalized
            : up;
        hasReading = true;
    }
}
