using UnityEngine;

// Looping background music that keeps playing across scene changes (AR <-> Virtual).
// Created automatically at startup; the clip is loaded from Assets/Resources/Audio.
public class BackgroundMusic : MonoBehaviour
{
    const string ClipPath = "Audio/BgmChinese";
    const float Volume = 0.35f;

    static BackgroundMusic instance;

    public static AudioSource Source { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (instance != null) return;

        AudioClip clip = Resources.Load<AudioClip>(ClipPath);
        if (clip == null)
        {
            Debug.LogWarning("BackgroundMusic: no clip at Resources/" + ClipPath);
            return;
        }

        GameObject go = new GameObject("BackgroundMusic");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<BackgroundMusic>();

        Source = go.AddComponent<AudioSource>();
        Source.clip = clip;
        Source.loop = true;
        Source.volume = Volume;
        Source.spatialBlend = 0f; // 2D: same loudness wherever the camera is
        Source.playOnAwake = false;
        Source.Play();
    }
}
