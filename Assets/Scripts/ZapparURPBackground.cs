using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Zappar;

// Menampilkan gambar kamera HP di belakang objek AR saat memakai URP.
//
// ZapparCameraBackground bawaan Zappar menggambar background lewat OnPostRender (tidak pernah
// dipanggil di URP) atau lewat GL di endCameraRendering (tidak bekerja dengan RenderGraph di
// Unity 6). Akibatnya layar hanya berisi buffer yang tidak dibersihkan (warna kuning/sampah).
// Script ini menggambar tekstur kamera Zappar sendiri dan merapikan setup kamera.
//
// Komponen ini dipasang otomatis pada ZapparCamera saat scene dimuat, jadi tidak perlu
// ditambahkan manual ke scene.
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class ZapparURPBackground : MonoBehaviour
{
    public enum KoreksiWarna { Otomatis, Aktif, Mati }

    [Tooltip("Konversi warna kamera dari sRGB ke Linear. Otomatis = aktif jika project memakai Linear color space.")]
    public KoreksiWarna koreksiWarna = KoreksiWarna.Otomatis;

    private const string NamaShader = "ZapparURPCameraBackground";

    private ZapparCamera zapparCamera;
    private Camera kamera;
    private Material material;
    private Mesh quad;
    private float[] elemenMatriks = new float[16];
    private Matrix4x4 matriksTekstur = Matrix4x4.identity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void PasangOtomatis()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => PasangKeZapparCamera();
        PasangKeZapparCamera();
    }

    private static void PasangKeZapparCamera()
    {
        ZapparCamera zc = ZapparCamera.Instance;
        if (zc != null && zc.GetComponent<ZapparURPBackground>() == null)
            zc.gameObject.AddComponent<ZapparURPBackground>();
    }

    private void Awake()
    {
        zapparCamera = GetComponent<ZapparCamera>();
        kamera = GetComponent<Camera>();

        Shader shader = Resources.Load<Shader>(NamaShader);
        if (shader == null)
        {
            Debug.LogError("[ZapparURPBackground] Shader Resources/" + NamaShader + " tidak ditemukan.");
            enabled = false;
            return;
        }
        material = new Material(shader) { mainTexture = Texture2D.blackTexture };

        quad = new Mesh { name = "ZapparBackgroundQuad" };
        quad.vertices = new[] { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0) };
        quad.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        quad.bounds = new Bounds(Vector3.zero, Vector3.one * 1000000f);

        PerbaikiSetupKamera();
    }

    // Satu kamera Base yang membersihkan layar ke hitam; kamera background tambahan dimatikan.
    private void PerbaikiSetupKamera()
    {
        kamera.clearFlags = CameraClearFlags.SolidColor;
        kamera.backgroundColor = Color.black;

        UniversalAdditionalCameraData data = kamera.GetUniversalAdditionalCameraData();
        if (data != null)
        {
            data.renderType = CameraRenderType.Base;
            data.cameraStack?.Clear();
        }

        foreach (ZapparCameraBackground bg in GetComponentsInChildren<ZapparCameraBackground>(true))
        {
            Camera kameraLain = bg.GetComponent<Camera>();
            if (kameraLain != null && kameraLain != kamera)
                kameraLain.enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (zapparCamera == null || !zapparCamera.PipelineIsInitialized || !zapparCamera.CameraSourceInitialized)
            return;

        System.IntPtr pipeline = zapparCamera.GetPipeline;

        if (!zapparCamera.CameraSourcePaused)
        {
            Texture2D teksturKamera = Z.PipelineCameraFrameTexture(pipeline);
            if (teksturKamera == null)
                return;

            Z.PipelineCameraFrameTextureMatrix(pipeline, ref elemenMatriks, Screen.width, Screen.height, zapparCamera.MirrorCameraFeed);
            for (int i = 0; i < 16; i++)
                matriksTekstur[i] = elemenMatriks[i];

            material.mainTexture = teksturKamera;
            material.SetMatrix("_nativeTextureMatrix", matriksTekstur);
            material.SetFloat("_LinearizeInput", PerluLinearize() ? 1f : 0f);
        }

        if (material.mainTexture == Texture2D.blackTexture)
            return;

        RenderParams rp = new RenderParams(material)
        {
            camera = kamera,
            layer = LayerPertamaYangTerlihat(),
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false,
            worldBounds = new Bounds(kamera.transform.position, Vector3.one * 1000000f)
        };
        Graphics.RenderMesh(rp, quad, 0, Matrix4x4.identity);
    }

    private bool PerluLinearize()
    {
        switch (koreksiWarna)
        {
            case KoreksiWarna.Aktif: return true;
            case KoreksiWarna.Mati: return false;
            default: return QualitySettings.activeColorSpace == ColorSpace.Linear;
        }
    }

    private int LayerPertamaYangTerlihat()
    {
        int mask = kamera.cullingMask;
        for (int i = 0; i < 32; i++)
            if ((mask & (1 << i)) != 0)
                return i;
        return 0;
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (quad != null) Destroy(quad);
    }
}
