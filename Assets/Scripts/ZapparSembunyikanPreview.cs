using UnityEngine;
using Zappar;

// "Preview Object" dari Zappar hanya untuk membantu menata konten di editor,
// tapi Zappar tidak menyembunyikannya saat aplikasi berjalan. Script ini mematikannya
// otomatis saat scene dimuat, jadi tidak perlu dipasang manual.
public static class ZapparSembunyikanPreview
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Pasang()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => Sembunyikan(scene);
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            Sembunyikan(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i));
    }

    private static void Sembunyikan(UnityEngine.SceneManagement.Scene scene)
    {
        if (!scene.isLoaded)
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (ZapparImageTrackingTarget target in root.GetComponentsInChildren<ZapparImageTrackingTarget>(true))
                if (target.PreviewImageObject != null)
                    target.PreviewImageObject.SetActive(false);
    }
}
