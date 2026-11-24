using UnityEngine;

// A dragon coiled around the can. The body mesh is rebuilt every frame along a spiral, so it can
// crawl in, ripple and raise its neck; the head, legs and tail tuft are models placed on it.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ArDragon : MonoBehaviour
{
    [SerializeField] Transform head;
    [SerializeField] Transform jaw;
    [SerializeField] Transform tail;
    [SerializeField] Transform[] legs;
    [SerializeField] ParticleSystem flames;
    [SerializeField] ParticleSystem flameCore;
    [SerializeField] ParticleSystem embers;
    [SerializeField] AudioSource voice;

    [SerializeField] float appearDelay = 0.5f;
    [SerializeField] float appearSeconds = 2.4f;
    [SerializeField] float replayAfter = 1.5f;
    [SerializeField] float roarSeconds = 1.5f;
    [SerializeField] float fireFrom = 0.18f;
    [SerializeField] float fireUntil = 1.15f;

    const int Rings = 156;
    const int Sides = 14;
    const int FinEvery = 2;
    const int Fins = (Rings - 12) / FinEvery;
    const int Table = 300;
    const float TileLength = 0.01225f;

    // Spiral: degrees around the can (0 is towards the viewer, + is to the right), radius, height.
    static readonly Vector3[] Route =
    {
        new Vector3(-1005f, 0.0600f, -0.0470f), new Vector3(-975f, 0.0500f, -0.0545f),
        new Vector3(-935f, 0.0440f, -0.0550f), new Vector3(-835f, 0.0437f, -0.0475f),
        new Vector3(-700f, 0.0437f, -0.0352f), new Vector3(-550f, 0.0437f, -0.0210f),
        new Vector3(-400f, 0.0437f, -0.0068f), new Vector3(-250f, 0.0437f, 0.0075f),
        new Vector3(-150f, 0.0437f, 0.0165f), new Vector3(-108f, 0.0445f, 0.0215f),
        new Vector3(-78f, 0.0490f, 0.0320f), new Vector3(-55f, 0.0575f, 0.0440f),
        new Vector3(-43f, 0.0700f, 0.0500f)
    };

    // Where each leg sits on the spiral, in the same degrees, and on which side of the body.
    static readonly float[] LegDegrees = { -748f, -702f, -392f, -340f };
    static readonly float[] LegSide = { 1f, -1f, 1f, -1f };

    readonly Vector3[] route = new Vector3[Table];
    readonly Vector3[] center = new Vector3[Rings];
    readonly Vector3[] forward = new Vector3[Rings];
    readonly Vector3[] back = new Vector3[Rings];
    readonly float[] radius = new float[Rings];
    readonly float[] place = new float[Rings];
    readonly float[] legAt = new float[LegDegrees.Length];

    Vector3[] vertices, normals;
    Vector4[] tangents;
    Mesh mesh;
    float length;
    float grown;
    float shownFor;
    float hiddenAt = float.NegativeInfinity;
    float roarTime = float.PositiveInfinity;
    float flameDebt, emberDebt;
    float yaw;
    bool aimed;

    public bool Roaring => roarTime < roarSeconds;
    public float Grown => grown;
    public float JawDegrees { get; private set; }
    public int FlamesAlive => flames.particleCount;

    void Awake()
    {
        BuildRoute();
        BuildMesh();
    }

    void OnEnable()
    {
        if (Time.time - hiddenAt > replayAfter)
            shownFor = 0f;
        roarTime = float.PositiveInfinity;
        aimed = false;
    }

    void OnDisable()
    {
        hiddenAt = Time.time;
    }

    void OnDestroy()
    {
        Destroy(mesh);
    }

    public void Roar()
    {
        if (Roaring || grown < 1f)
            return;

        roarTime = 0f;
        voice.pitch = Random.Range(0.94f, 1.06f);
        voice.Play();
    }

    // Runs after the target pose has been written in Update.
    void LateUpdate()
    {
        shownFor += Time.deltaTime;
        roarTime += Time.deltaTime;
        float t = Mathf.Clamp01((shownFor - appearDelay) / appearSeconds);
        grown = 1f - (1f - t) * (1f - t);

        FaceCamera();
        ShapeBody();
        WriteMesh();
        PlaceParts();
        Breathe();
    }

    // 0 at rest, 1 in the middle of a roar.
    float RoarAmount()
    {
        if (!Roaring)
            return 0f;

        float rise = Mathf.SmoothStep(0f, 1f, roarTime / 0.22f);
        float fall = Mathf.SmoothStep(0f, 1f, (roarSeconds - roarTime) / 0.4f);
        return Mathf.Min(rise, fall);
    }

    void FaceCamera()
    {
        Camera cam = Camera.main;
        Transform pivot = transform.parent;
        if (cam == null || pivot == null)
            return;

        Vector3 eye = pivot.InverseTransformPoint(cam.transform.position);
        if (eye.x * eye.x + eye.z * eye.z < 1e-8f)
            return;

        float wanted = Mathf.Atan2(-eye.x, -eye.z) * Mathf.Rad2Deg;
        yaw = aimed ? Mathf.LerpAngle(yaw, wanted, 1f - Mathf.Exp(-7f * Time.deltaTime)) : wanted;
        aimed = true;
        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void ShapeBody()
    {
        float time = Time.time;
        float roar = RoarAmount();

        for (int i = 0; i < Rings; i++)
        {
            float u = i / (Rings - 1f);
            float p = u - (1f - grown);
            place[i] = p;
            Vector3 c = Along(p);

            float neck = Mathf.SmoothStep(0f, 1f, (p - 0.86f) / 0.14f);
            float tailEnd = Mathf.SmoothStep(0f, 1f, (0.09f - p) / 0.09f);
            float wave = p * 26f - time * 2.1f;
            c.x += 3.5f * Mathf.Sin(time * 0.6f) + 5f * neck * Mathf.Sin(time * 0.9f) - 3f * neck * roar
                + 7f * tailEnd * Mathf.Sin(time * 1.7f);
            c.y += 0.0008f * (1f + Mathf.Sin(wave * 0.7f)) + 0.002f * neck * Mathf.Sin(time * 1.1f);
            c.z += 0.0020f * Mathf.Sin(wave) + 0.0025f * neck * Mathf.Sin(time * 1.5f) + 0.009f * neck * roar
                + 0.004f * tailEnd * Mathf.Sin(time * 2.3f);

            center[i] = Point(c);
            radius[i] = p < 0f ? 0f : Thickness(u) * Mathf.Clamp01(p * 40f)
                * (1f + 0.03f * Mathf.Sin(time * 1.8f + u * 5f));
            back[i] = Vector3.Lerp(Point(new Vector3(c.x, 0.6f, 0.8f)), Vector3.up, neck);
        }

        for (int i = 0; i < Rings; i++)
        {
            Vector3 f = center[Mathf.Min(i + 1, Rings - 1)] - center[Mathf.Max(i - 1, 0)];
            forward[i] = f.sqrMagnitude > 1e-14f ? f.normalized : i > 0 ? forward[i - 1] : Vector3.right;
            back[i] = Vector3.ProjectOnPlane(back[i], forward[i]).normalized;
        }
    }

    void WriteMesh()
    {
        int n = 0;
        for (int i = 0; i < Rings; i++)
        {
            Vector3 side = Vector3.Cross(back[i], forward[i]);
            for (int j = 0; j <= Sides; j++, n++)
            {
                float a = 2f * Mathf.PI * j / Sides;
                float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                Vector3 outward = back[i] * cos + side * sin;
                vertices[n] = center[i] + outward * radius[i];
                normals[n] = outward;
                Vector3 around = side * cos - back[i] * sin;
                tangents[n] = new Vector4(around.x, around.y, around.z, 1f);
            }
        }

        for (int k = 0; k < Fins; k++)
        {
            int i = 4 + k * FinEvery;
            Vector3 side = Vector3.Cross(back[i], forward[i]);
            float size = radius[i];
            float tall = size * (k % 2 == 0 ? 0.95f : 0.75f);
            Vector3 foot = center[i] + back[i] * (size * 0.9f);
            Vector3 front = foot + forward[i] * (size * 0.42f);
            Vector3 rear = foot - forward[i] * (size * 0.48f);
            Vector3 left = foot - side * (size * 0.2f);
            Vector3 right = foot + side * (size * 0.2f);
            Vector3 tip = foot + back[i] * tall - forward[i] * (size * 0.75f);
            Facet(ref n, front, right, tip);
            Facet(ref n, right, rear, tip);
            Facet(ref n, rear, left, tip);
            Facet(ref n, left, front, tip);
        }

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTangents(tangents);
    }

    void Facet(ref int n, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
        vertices[n] = a;
        vertices[n + 1] = b;
        vertices[n + 2] = c;
        for (int i = 0; i < 3; i++, n++)
            normals[n] = normal;
    }

    void PlaceParts()
    {
        float time = Time.time;
        float roar = RoarAmount();
        int last = Rings - 1;

        // Looks up and towards the side of the screen while it breathes fire.
        Quaternion rest = Quaternion.LookRotation(forward[last], Vector3.up);
        Quaternion look = Quaternion.Euler(4f * Mathf.Sin(time * 1.3f) - 42f * roar,
            9f * Mathf.Sin(time * 0.55f) * (1f - roar) + 20f * roar,
            4f * Mathf.Sin(time * 0.8f) + 2.5f * roar * Mathf.Sin(time * 55f));
        head.localPosition = center[last] - forward[last] * 0.001f;
        head.localRotation = rest * look;
        head.localScale = Vector3.one * Mathf.Clamp01(grown * 12f);

        JawDegrees = 3.5f + 2.5f * Mathf.Sin(time * 1.8f) + roar * (31f + 3f * Mathf.Sin(time * 40f));
        jaw.localRotation = Quaternion.Euler(JawDegrees, 0f, 0f);

        tail.localPosition = center[0];
        tail.localRotation = Quaternion.LookRotation(-forward[0], back[0]);
        tail.localScale = Vector3.one * (1.15f * Mathf.Clamp01((grown - 0.9f) * 10f));

        for (int k = 0; k < legs.Length; k++)
        {
            float at = legAt[k] * last;
            int i = Mathf.FloorToInt(at);
            float f = at - i;
            Vector3 ahead = Vector3.Slerp(forward[i], forward[i + 1], f);
            Vector3 along = Vector3.ProjectOnPlane(Vector3.up, ahead).normalized * LegSide[k];
            float swing = 5f * Mathf.Sin(time * 1.4f + k * 1.9f);
            legs[k].localPosition = Vector3.Lerp(center[i], center[i + 1], f);
            legs[k].localRotation = Quaternion.LookRotation(ahead, along) * Quaternion.Euler(swing, 0f, 0f);

            // Claws point at the can on both sides, so one side is mirrored.
            float size = Mathf.Clamp01(place[i] * 14f);
            legs[k].localScale = new Vector3(1.08f * LegSide[k], 0.74f, 0.78f) * size;
        }
    }

    void Breathe()
    {
        if (!Roaring || roarTime < fireFrom || roarTime > fireUntil)
        {
            flameDebt = emberDebt = 0f;
            return;
        }

        float scale = transform.lossyScale.x;
        Vector3 mouth = head.TransformPoint(new Vector3(0f, -0.0045f, 0.024f));
        Vector3 ahead = head.TransformDirection(new Vector3(0f, -0.10f, 1f)).normalized;

        flameDebt += 95f * Time.deltaTime;
        for (; flameDebt >= 1f; flameDebt -= 1f)
        {
            Emit(flames, mouth, ahead, 0.21f, 0.15f, 0.013f, 0.021f, scale);
            Emit(flameCore, mouth, ahead, 0.18f, 0.07f, 0.006f, 0.011f, scale);
        }
        emberDebt += 45f * Time.deltaTime;
        for (; emberDebt >= 1f; emberDebt -= 1f)
            Emit(embers, mouth, ahead, 0.24f, 0.3f, 0.0012f, 0.0026f, scale);
    }

    static void Emit(ParticleSystem system, Vector3 at, Vector3 ahead, float speed, float spread,
        float small, float large, float scale)
    {
        Vector3 aim = (ahead + Random.insideUnitSphere * spread).normalized;
        var particle = new ParticleSystem.EmitParams
        {
            position = at + Random.insideUnitSphere * 0.0015f * scale,
            velocity = aim * (speed * Random.Range(0.75f, 1.2f) * scale),
            startSize = Random.Range(small, large) * scale
        };
        system.Emit(particle, 1);
    }

    static Vector3 Point(Vector3 c)
    {
        float a = c.x * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(a) * c.y, c.z, -Mathf.Cos(a) * c.y);
    }

    static float Thickness(float u)
    {
        float behind = Mathf.Lerp(0.0016f, 0.0074f, Mathf.Sqrt(Mathf.Clamp01(u / 0.42f)));
        return Mathf.Lerp(behind, 0.0064f, Mathf.SmoothStep(0f, 1f, (u - 0.78f) / 0.22f));
    }

    Vector3 Along(float p)
    {
        float x = Mathf.Clamp01(p) * (Table - 1);
        int i = Mathf.Min((int)x, Table - 2);
        return Vector3.LerpUnclamped(route[i], route[i + 1], x - i);
    }

    // Resamples the route so that equal steps are equal distances along the body.
    void BuildRoute()
    {
        const int Fine = 1200;
        var fine = new Vector3[Fine];
        var walked = new float[Fine];
        for (int k = 0; k < Fine; k++)
        {
            fine[k] = CatmullRom(k / (Fine - 1f) * (Route.Length - 1));
            if (k > 0)
                walked[k] = walked[k - 1] + Vector3.Distance(Point(fine[k]), Point(fine[k - 1]));
        }
        length = walked[Fine - 1];

        int at = 0;
        for (int k = 0; k < Table; k++)
        {
            float wanted = length * k / (Table - 1);
            while (at < Fine - 2 && walked[at + 1] < wanted)
                at++;
            route[k] = Vector3.Lerp(fine[at], fine[at + 1], Mathf.InverseLerp(walked[at], walked[at + 1], wanted));
        }

        for (int k = 0; k < legAt.Length; k++)
        {
            int i = 0;
            while (i < Table - 1 && route[i].x < LegDegrees[k])
                i++;
            legAt[k] = i / (Table - 1f);
        }
    }

    static Vector3 CatmullRom(float x)
    {
        int i = Mathf.Min((int)x, Route.Length - 2);
        float t = x - i;
        Vector3 b = Route[i], c = Route[i + 1];
        Vector3 a = i > 0 ? Route[i - 1] : 2f * b - c;
        Vector3 d = i + 2 < Route.Length ? Route[i + 2] : 2f * c - b;
        return 0.5f * (2f * b + (c - a) * t + (2f * a - 5f * b + 4f * c - d) * t * t
            + (3f * b - a - 3f * c + d) * t * t * t);
    }

    void BuildMesh()
    {
        int ringVertices = Rings * (Sides + 1);
        int count = ringVertices + Fins * 12;
        vertices = new Vector3[count];
        normals = new Vector3[count];
        tangents = new Vector4[count];
        var uv = new Vector2[count];
        var skin = new int[(Rings - 1) * Sides * 6];
        var fins = new int[Fins * 12];

        for (int i = 0, n = 0, k = 0; i < Rings; i++)
        {
            float v = (1f - i / (Rings - 1f)) * length / TileLength;
            for (int j = 0; j <= Sides; j++, n++)
            {
                uv[n] = new Vector2(j / (float)Sides, v);
                if (i == Rings - 1 || j == Sides)
                    continue;

                int up = n + Sides + 1;
                skin[k++] = n;
                skin[k++] = up;
                skin[k++] = up + 1;
                skin[k++] = n;
                skin[k++] = up + 1;
                skin[k++] = n + 1;
            }
        }

        for (int n = 0; n < Fins * 12; n++)
        {
            int at = ringVertices + n;
            fins[n] = at;
            uv[at] = new Vector2(0.5f, n % 3 == 2 ? 1f : 0.1f);
            tangents[at] = new Vector4(1f, 0f, 0f, 1f);
        }

        mesh = new Mesh { name = "Dragon body" };
        mesh.MarkDynamic();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(skin, 0, false);
        mesh.SetTriangles(fins, 1, false);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 0.3f);
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}
