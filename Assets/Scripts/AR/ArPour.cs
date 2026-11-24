using UnityEngine;

// Pours soda while the can is tilted past an angle, and stops when it is set upright again.
public class ArPour : MonoBehaviour
{
    [SerializeField] LineRenderer stream;
    [SerializeField] ParticleSystem drops;
    [SerializeField] GameObject opening;

    [SerializeField] float startTilt = 60f;
    [SerializeField] float stopTilt = 48f;
    [SerializeField] float rimRadius = 0.0275f;
    [SerializeField] float rimHeight = 0.0585f;
    [SerializeField] float exitSpeed = 0.16f;
    [SerializeField] float gravity = 5f;
    [SerializeField] float flowSeconds = 0.32f;
    [SerializeField] float streamWidth = 0.009f;
    [SerializeField] float dropsPerSecond = 45f;

    const int Points = 18;

    readonly Vector3[] points = new Vector3[Points];
    MaterialPropertyBlock block;
    Vector3 side = Vector3.back;
    float head, tail, dropDebt;
    bool pouring;

    public bool Pouring => pouring;

    void Awake()
    {
        block = new MaterialPropertyBlock();
        stream.positionCount = Points;
        stream.useWorldSpace = true;
    }

    void OnEnable()
    {
        pouring = false;
        head = tail = 0f;
        stream.enabled = false;
    }

    // Runs after the target pose and ArUpright.
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 up = ArGravity.Up(cam);
        float tilt = Vector3.Angle(transform.up, up);
        bool wasPouring = pouring;
        pouring = tilt > (pouring ? stopTilt : startTilt);

        if (pouring && !wasPouring)
        {
            head = tail = 0f;
            opening.SetActive(true);
            if (HapticManager.Instance != null)
                HapticManager.Instance.PlayTap();
        }

        if (pouring)
            head = Mathf.Min(head + Time.deltaTime, flowSeconds);
        else
            tail += Time.deltaTime;

        bool visible = pouring || tail < head;
        stream.enabled = visible;
        if (visible)
            Flow(up);
    }

    void Flow(Vector3 up)
    {
        float scale = transform.lossyScale.x;

        // The lowest point of the rim; kept from before when the can is fully upside down.
        Vector3 lowest = Vector3.ProjectOnPlane(-up, transform.up);
        if (lowest.sqrMagnitude > 0.02f)
            side = transform.InverseTransformDirection(lowest.normalized);

        Vector3 outward = transform.TransformDirection(side);
        Vector3 lip = transform.TransformPoint(side * rimRadius + Vector3.up * rimHeight);
        Vector3 velocity = (transform.up * 0.8f + outward * 0.6f) * exitSpeed * scale;
        Vector3 fall = -up * gravity * scale;

        for (int i = 1; i < Points; i++)
        {
            float t = Mathf.Lerp(tail, head, (i - 1f) / (Points - 2f));
            points[i] = lip + velocity * t + 0.5f * t * t * fall;
        }
        // While it flows the stream starts inside the opening; afterwards it lets go of the can.
        points[0] = pouring ? Vector3.Lerp(opening.transform.position, lip, 0.35f) : points[1];

        stream.SetPositions(points);
        stream.widthMultiplier = streamWidth * scale;
        block.SetVector("_BaseMap_ST", new Vector4(1f, 1f, -Time.time * 4f % 1f, 0f));
        stream.SetPropertyBlock(block);

        if (pouring)
            EmitDrops(lip, velocity, fall, scale);
    }

    void EmitDrops(Vector3 lip, Vector3 velocity, Vector3 fall, float scale)
    {
        ParticleSystem.ForceOverLifetimeModule force = drops.forceOverLifetime;
        force.x = fall.x;
        force.y = fall.y;
        force.z = fall.z;

        dropDebt += dropsPerSecond * Time.deltaTime;
        for (; dropDebt >= 1f; dropDebt -= 1f)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = lip + Random.insideUnitSphere * 0.003f * scale,
                velocity = velocity + Random.insideUnitSphere * 0.07f * scale,
                startSize = Random.Range(0.0015f, 0.004f) * scale
            };
            drops.Emit(emit, 1);
        }
    }
}
