using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

// Pasang script ini di GameObject kosong bernama "Merapi" (anak dari image target Zappar).
// Model gunung, lava, magma, pohon, dan partikel erupsi dibuat otomatis saat Play.
public class MerapiBuilder : MonoBehaviour
{
    [Header("Bentuk")]
    public float tinggi = 1.2f;
    public int segmen = 56;
    public int cincin = 30;

    [Header("Material dasar (WAJIB diisi, supaya shader ikut ter-build)")]
    public Material matDasar;      // material URP Lit biasa
    public Material matPartikel;   // material URP Particles/Unlit

    [Header("Visibilitas")]
    public bool mulaiTersembunyi = false;   // centang kalau model harus muncul hanya saat marker terlihat

    [Header("UI dan audio (isi dari Inspector)")]
    public GameObject panelInfo;
    public TMP_Text teksJudul;
    public TMP_Text teksIsi;
    public AudioSource sfxLetusan;   // seret letusan.wav
    public AudioSource narasi;       // seret rekaman narasi

    ParticleSystem lava, asap, abu;
    Quaternion rotAwal;
    Material matMagma;

    static readonly Dictionary<string, string[]> Info = new Dictionary<string, string[]>
    {
        { "umum",   new[]{ "Gunung Merapi", "Gunung Merapi (sekitar 2.930 mdpl) berada di perbatasan Daerah Istimewa Yogyakarta dan Jawa Tengah. Merapi bertipe stratovolcano dan termasuk salah satu gunung api paling aktif di Indonesia." } },
        { "kawah",  new[]{ "Kawah", "Lubang di puncak gunung tempat magma, gas, dan abu keluar saat erupsi." } },
        { "lereng", new[]{ "Lereng", "Badan gunung yang tersusun dari lapisan lava dan material piroklastik hasil erupsi sebelumnya." } },
        { "magma",  new[]{ "Magma", "Batuan cair panas di dalam bumi yang naik ke atas melalui saluran gunung." } },
        { "lava",   new[]{ "Lava", "Magma yang sudah keluar ke permukaan bumi dan mengalir di lereng gunung." } },
        { "erupsi", new[]{ "Erupsi!", "Erupsi adalah keluarnya magma, gas, dan abu vulkanik. Bahayanya antara lain awan panas, lahar, dan hujan abu." } },
    };

    // Profil cekung ala stratovolcano, sama dengan versi web
    static float Profil(float a, float t)
    {
        float r = 0.1f + 0.5f * Mathf.Pow(1f - t, 1.6f);
        return r * (1f + 0.06f * Mathf.Sin(a * 5f + t * 4f) + 0.04f * Mathf.Sin(a * 9f - t * 6f) * (0.4f + t));
    }

    Material Mat(Color c, bool emisi = false)
    {
        var m = new Material(matDasar);
        m.color = c;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);   // dua sisi
        if (emisi) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 1.5f); }
        return m;
    }

    // Bentuk dasar dibuat sendiri (tanpa CreatePrimitive) supaya tidak butuh Collider di build Android
    static Mesh Revolusi(List<Vector2> profil, int seg, string nama)
    {
        var v = new Vector3[profil.Count * (seg + 1)];
        var tri = new List<int>();
        for (int j = 0; j < profil.Count; j++)
            for (int i = 0; i <= seg; i++)
            {
                float a = i * 2f * Mathf.PI / seg;
                v[j * (seg + 1) + i] = new Vector3(Mathf.Cos(a) * profil[j].x, profil[j].y, Mathf.Sin(a) * profil[j].x);
            }
        for (int j = 0; j < profil.Count - 1; j++)
            for (int i = 0; i < seg; i++)
            {
                int a = j * (seg + 1) + i, b = a + 1, c = a + seg + 1, d = c + 1;
                tri.AddRange(new[] { a, c, b, b, c, d });
            }
        var m = new Mesh { name = nama, vertices = v };
        m.SetTriangles(tri, 0);
        m.RecalculateNormals();
        return m;
    }

    Mesh meshSilinder, meshBola;

    Mesh AmbilMesh(PrimitiveType tipe)
    {
        if (tipe == PrimitiveType.Cylinder)
        {
            if (meshSilinder == null)   // jari-jari 0.5, tinggi 2 (sama seperti primitive Unity)
                meshSilinder = Revolusi(new List<Vector2> { new Vector2(0, -1), new Vector2(0.5f, -1), new Vector2(0.5f, 1), new Vector2(0, 1) }, 24, "Silinder");
            return meshSilinder;
        }
        if (meshBola == null)           // jari-jari 0.5
        {
            var pr = new List<Vector2>();
            for (int k = 0; k <= 12; k++) { float ang = Mathf.PI * k / 12f; pr.Add(new Vector2(0.5f * Mathf.Sin(ang), -0.5f * Mathf.Cos(ang))); }
            meshBola = Revolusi(pr, 24, "Bola");
        }
        return meshBola;
    }

    GameObject Primitif(PrimitiveType tipe, string nama, Vector3 pos, Vector3 skala, Material mat)
    {
        var g = new GameObject(nama);
        g.transform.SetParent(transform, false);
        g.transform.localPosition = pos; g.transform.localScale = skala;
        g.AddComponent<MeshFilter>().sharedMesh = AmbilMesh(tipe);
        g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        return g;
    }

    void Start()
    {
        rotAwal = transform.localRotation;
        if (matDasar == null || matPartikel == null)
        {
            Debug.LogError("MerapiBuilder: isi Mat Dasar dan Mat Partikel di Inspector.");
            return;
        }
        BangunLereng();
        BangunLava();
        BangunLainnya();
        BangunPartikel();
        if (panelInfo) panelInfo.SetActive(false);
        if (mulaiTersembunyi) Sembunyikan();
    }

    void BangunLereng()
    {
        var v = new Vector3[(segmen + 1) * (cincin + 1)];
        for (int j = 0; j <= cincin; j++)
            for (int i = 0; i <= segmen; i++)
            {
                float t = j / (float)cincin, a = i * 2f * Mathf.PI / segmen, r = Profil(a, t);
                v[j * (segmen + 1) + i] = new Vector3(Mathf.Cos(a) * r, t * tinggi, Mathf.Sin(a) * r);
            }
        var tri = new List<int>[4];
        for (int k = 0; k < 4; k++) tri[k] = new List<int>();
        for (int j = 0; j < cincin; j++)
        {
            float tm = (j + 0.5f) / cincin;
            int band = tm < 0.15f ? 0 : tm < 0.45f ? 1 : tm < 0.85f ? 2 : 3;
            for (int i = 0; i < segmen; i++)
            {
                int a = j * (segmen + 1) + i, b = a + 1, c = a + segmen + 1, d = c + 1;
                tri[band].AddRange(new[] { a, c, b, b, c, d });
            }
        }
        var mesh = new Mesh { name = "Lereng", subMeshCount = 4 };
        mesh.vertices = v;
        for (int k = 0; k < 4; k++) mesh.SetTriangles(tri[k], k);
        mesh.RecalculateNormals();

        var g = new GameObject("Lereng");
        g.transform.SetParent(transform, false);
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mrL = g.AddComponent<MeshRenderer>();
        mrL.sharedMaterials = new[]
        {
            Mat(new Color32(0x4f, 0x7a, 0x3a, 255)), Mat(new Color32(0x6b, 0x5a, 0x3a, 255)),
            Mat(new Color32(0x7a, 0x6e, 0x64, 255)), Mat(new Color32(0x3a, 0x33, 0x30, 255))
        };
        Daftar("lereng", mrL);
    }

    void BangunLava()
    {
        var matLava = Mat(new Color(1f, 0.29f, 0.08f), true);
        foreach (float a0 in new[] { 0.3f, 2.2f, 4.1f, 5.3f })
        {
            var g = new GameObject("AliranLava");
            g.transform.SetParent(transform, false);
            var lr = g.AddComponent<LineRenderer>();
            lr.useWorldSpace = false; lr.sharedMaterial = matLava;
            lr.startWidth = lr.endWidth = 0.04f; lr.numCornerVertices = 3;
            lr.positionCount = 11;
            Daftar("lava", lr);
            for (int k = 0; k <= 10; k++)
            {
                float t = 0.97f - k * 0.06f, a = a0 + 0.1f * Mathf.Sin(k * 1.3f), r = Profil(a, t) + 0.02f;
                lr.SetPosition(k, new Vector3(Mathf.Cos(a) * r, t * tinggi, Mathf.Sin(a) * r));
            }
        }
    }

    void BangunLainnya()
    {
        matMagma = Mat(new Color(1f, 0.35f, 0.12f), true);
        Daftar("magma", Primitif(PrimitiveType.Cylinder, "Magma", new Vector3(0, 0.45f, 0), new Vector3(0.14f, 0.45f, 0.14f), matMagma).GetComponent<Renderer>());
        Daftar("magma", Primitif(PrimitiveType.Sphere, "DapurMagma", new Vector3(0, 0.08f, 0), Vector3.one * 0.4f, matMagma).GetComponent<Renderer>());
        Primitif(PrimitiveType.Cylinder, "Tanah", new Vector3(0, -0.015f, 0), new Vector3(1.9f, 0.015f, 1.9f), Mat(new Color32(0x5c, 0x9a, 0x45, 255)));

        // Kawah: cincin gelap di puncak
        var k = new GameObject("Kawah"); k.transform.SetParent(transform, false);
        var lr = k.AddComponent<LineRenderer>();
        lr.useWorldSpace = false; lr.loop = true; lr.sharedMaterial = Mat(new Color(0.17f, 0.17f, 0.17f));
        lr.startWidth = lr.endWidth = 0.06f; lr.positionCount = 24;
        Daftar("kawah", lr);
        for (int i = 0; i < 24; i++)
        {
            float a = i * 2f * Mathf.PI / 24f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.11f, tinggi, Mathf.Sin(a) * 0.11f));
        }

        // Pohon kecil
        var matPohon = Mat(new Color32(0x2f, 0x7d, 0x32, 255));
        for (int i = 0; i < 9; i++)
        {
            float a = i * 0.7f + 0.3f, r = 0.7f + (i % 3) * 0.08f;
            Primitif(PrimitiveType.Sphere, "Pohon", new Vector3(Mathf.Cos(a) * r, 0.08f, Mathf.Sin(a) * r),
                     new Vector3(0.09f, 0.08f + (i % 2) * 0.03f, 0.09f), matPohon);
        }
    }

    ParticleSystem BuatPartikel(string nama, Color warna, float ukuran, float laju, float umur, float kecepatan, float gravitasi)
    {
        var g = new GameObject(nama);
        g.transform.SetParent(transform, false);
        g.transform.localPosition = new Vector3(0, tinggi, 0);
        g.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // memancar ke atas
        var ps = g.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false; main.loop = true;
        main.startColor = warna; main.startSize = ukuran; main.startLifetime = umur;
        main.startSpeed = kecepatan; main.gravityModifier = gravitasi;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var em = ps.emission; em.rateOverTime = laju;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 22f; sh.radius = 0.03f;
        var rend = g.GetComponent<ParticleSystemRenderer>();
        var m = new Material(matPartikel); m.color = warna;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", warna);
        rend.sharedMaterial = m;
        return ps;
    }

    void BangunPartikel()
    {
        lava = BuatPartikel("PartikelLava", new Color(1f, 0.3f, 0.05f), 0.04f, 25f, 1.2f, 1.3f, 0.5f);
        asap = BuatPartikel("PartikelAsap", new Color(0.45f, 0.45f, 0.45f, 0.7f), 0.15f, 12f, 3f, 0.35f, 0f);
        abu  = BuatPartikel("PartikelAbu",  new Color(0.2f, 0.2f, 0.2f, 0.9f), 0.03f, 30f, 2.5f, 0.6f, 0.05f);
    }

    // Bisa dihubungkan ke event "marker terlihat / hilang" milik Zappar
    public void Tampilkan() { AturTampil(true); }
    public void Sembunyikan() { AturTampil(false); }
    void AturTampil(bool tampil)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = tampil;
    }

    // ---------- Highlight bagian gunung ----------
    struct Asli { public Color warna, emisi; public bool emisiAktif; }
    readonly Dictionary<string, List<Renderer>> bagian = new Dictionary<string, List<Renderer>>();
    readonly Dictionary<Material, Asli> asli = new Dictionary<Material, Asli>();
    static readonly Color Kuning = new Color(1f, 0.88f, 0.3f);

    void Daftar(string kunci, Renderer r)
    {
        if (!bagian.ContainsKey(kunci)) bagian[kunci] = new List<Renderer>();
        bagian[kunci].Add(r);
    }

    void Sorot(string kunci)
    {
        BersihSorot();
        if (!bagian.ContainsKey(kunci)) return;
        foreach (var r in bagian[kunci])
            foreach (var m in r.sharedMaterials)
            {
                if (m == null || asli.ContainsKey(m)) continue;
                bool pakaiBase = m.HasProperty("_BaseColor");
                asli[m] = new Asli
                {
                    warna = pakaiBase ? m.GetColor("_BaseColor") : m.color,
                    emisi = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black,
                    emisiAktif = m.IsKeywordEnabled("_EMISSION")
                };
                Color baru = Color.Lerp(asli[m].warna, Kuning, 0.7f);
                m.color = baru;
                if (pakaiBase) m.SetColor("_BaseColor", baru);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Kuning * 1.6f);
            }
    }

    void BersihSorot()
    {
        foreach (var kv in asli)
        {
            var m = kv.Key;
            if (m == null) continue;
            m.color = kv.Value.warna;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", kv.Value.warna);
            m.SetColor("_EmissionColor", kv.Value.emisi);
            if (!kv.Value.emisiAktif) m.DisableKeyword("_EMISSION");
        }
        asli.Clear();
    }

    // ---------- Dipanggil dari tombol UI (OnClick) ----------
    public void TampilInfo(string kunci)
    {
        if (!Info.ContainsKey(kunci)) return;
        Sorot(kunci);
        if (teksJudul) teksJudul.text = Info[kunci][0];
        if (teksIsi) teksIsi.text = Info[kunci][1];
        if (panelInfo) panelInfo.SetActive(true);
    }

    public void MulaiErupsi()
    {
        if (lava == null) return;
        lava.Play(); asap.Play(); abu.Play();
        if (sfxLetusan) sfxLetusan.Play();
        TampilInfo("erupsi");
    }

    public void ToggleNarasi()
    {
        if (!narasi) return;
        if (narasi.isPlaying) narasi.Stop(); else { narasi.Play(); TampilInfo("umum"); }
    }

    public void ResetSemua()
    {
        if (lava != null)
        {
            lava.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            asap.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            abu.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (sfxLetusan) sfxLetusan.Stop();
        if (narasi) narasi.Stop();
        if (panelInfo) panelInfo.SetActive(false);
        BersihSorot();
        transform.localRotation = rotAwal;
    }

    // Geser satu jari untuk memutar model (Input System baru)
    void Update()
    {
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed)
            transform.Rotate(0f, -ts.primaryTouch.delta.ReadValue().x * 0.3f, 0f, Space.Self);
#if UNITY_EDITOR
        var ms = Mouse.current;
        if (ms != null && ms.leftButton.isPressed)
            transform.Rotate(0f, -ms.delta.ReadValue().x * 0.3f, 0f, Space.Self);
#endif
    }
}
