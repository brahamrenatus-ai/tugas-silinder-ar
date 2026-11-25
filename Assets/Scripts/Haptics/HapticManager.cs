using UnityEngine;

public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    [Header("Durasi getaran (milidetik)")]
    [SerializeField] private long tapMs = 30;
    [SerializeField] private long grabMs = 40;
    [SerializeField] private long releaseMs = 40;
    [SerializeField] private long collisionMs = 120;

    [Header("Jeda minimal antar getaran (detik)")]
    [SerializeField] private float cooldown = 0.1f;

    private float lastVibrateTime = -10f;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject vibrator;
#endif

    private void Awake()
    {
        Instance = this;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Vibrator tidak tersedia: " + e.Message);
        }
#endif
    }

    public void PlayTap()       { Vibrate(tapMs, "Tap"); }
    public void PlayGrab()      { Vibrate(grabMs, "Grab"); }
    public void PlayRelease()   { Vibrate(releaseMs, "Release"); }
    public void PlayCollision() { Vibrate(collisionMs, "Collision"); }

    private void Vibrate(long milliseconds, string label)
    {
        if (Time.time - lastVibrateTime < cooldown) return;
        lastVibrateTime = Time.time;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator != null)
        {
            vibrator.Call("vibrate", milliseconds);
        }
        else
        {
            Handheld.Vibrate();
        }
#else
        Debug.Log("Vibrate: " + label + " (" + milliseconds + " ms)");
#endif
    }
}