using UnityEngine;
using Zappar;

// Steadies the tracked pose: strong smoothing while the can is still, almost none while it moves
// (One Euro filter). Sits on the image tracking target and runs after Zappar writes its pose.
[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(ZapparImageTrackingTarget))]
public class ArPoseFilter : MonoBehaviour
{
    // Cutoff in Hz while still, and how much it grows with speed.
    [SerializeField] float sideCutoff = 2f;
    [SerializeField] float sideBeta = 25f;
    [SerializeField] float depthCutoff = 0.8f;
    [SerializeField] float depthBeta = 5f;
    [SerializeField] float turnCutoff = 0.8f;
    [SerializeField] float turnBeta = 0.08f;
    [SerializeField] float speedCutoff = 1.5f;

    // A larger step than this in one update is a new detection, so it is taken over at once.
    [SerializeField] float snapDegrees = 35f;
    [SerializeField] float snapSide = 0.15f;

    ZapparImageTrackingTarget target;
    bool started;
    float rawAge;
    Vector3 rawPosition, outPosition;
    Quaternion rawRotation, outRotation;
    Vector2 side, rawSide, sideSpeed;
    float depth, rawDepth, depthSpeed;
    Quaternion turn;
    Vector3 turnSpeed;

    void Awake()
    {
        target = GetComponent<ZapparImageTrackingTarget>();
    }

    void OnEnable()
    {
        started = false;
        target.OnSeenEvent.AddListener(Restart);
    }

    void OnDisable()
    {
        target.OnSeenEvent.RemoveListener(Restart);
    }

    void Restart()
    {
        started = false;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // Unchanged since the last write: Zappar has no pose now, so the last one is kept.
        if (started && position == outPosition && rotation == outRotation)
            return;

        Transform view = cam.transform;
        Vector3 local = view.InverseTransformPoint(position);
        if (local.z < 1e-4f)
            return;

        Vector2 newSide = new Vector2(local.x, local.y) / local.z;
        Quaternion newTurn = Quaternion.Inverse(view.rotation) * rotation;

        if (!started || Quaternion.Angle(newTurn, turn) > snapDegrees || (newSide - side).magnitude > snapSide)
        {
            started = true;
            rawAge = 0f;
            side = newSide;
            depth = local.z;
            turn = newTurn;
            sideSpeed = Vector2.zero;
            depthSpeed = 0f;
            turnSpeed = Vector3.zero;
            rawSide = newSide;
            rawDepth = local.z;
            rawRotation = newTurn;
            rawPosition = local;
        }
        else
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            rawAge += dt;

            // The camera image can be slower than the screen: speed is measured between new poses only.
            if (local != rawPosition || newTurn != rawRotation)
            {
                float k = Blend(speedCutoff, rawAge);
                sideSpeed = Vector2.Lerp(sideSpeed, (newSide - rawSide) / rawAge, k);
                depthSpeed = Mathf.Lerp(depthSpeed, (local.z - rawDepth) / (rawDepth * rawAge), k);
                turnSpeed = Vector3.Lerp(turnSpeed, TurnVector(rawRotation, newTurn) / rawAge, k);
                rawSide = newSide;
                rawDepth = local.z;
                rawRotation = newTurn;
                rawPosition = local;
                rawAge = 0f;
            }

            side = Vector2.Lerp(side, newSide, Blend(sideCutoff + sideBeta * sideSpeed.magnitude, dt));
            depth = Mathf.Lerp(depth, local.z, Blend(depthCutoff + depthBeta * Mathf.Abs(depthSpeed), dt));
            turn = Quaternion.Slerp(turn, newTurn, Blend(turnCutoff + turnBeta * turnSpeed.magnitude, dt));
        }

        transform.SetPositionAndRotation(
            view.TransformPoint(new Vector3(side.x * depth, side.y * depth, depth)),
            view.rotation * turn);
        outPosition = transform.position;
        outRotation = transform.rotation;
    }

    static float Blend(float cutoff, float dt)
    {
        return 1f - Mathf.Exp(-2f * Mathf.PI * cutoff * dt);
    }

    // Rotation between two orientations as axis times degrees.
    static Vector3 TurnVector(Quaternion from, Quaternion to)
    {
        (to * Quaternion.Inverse(from)).ToAngleAxis(out float degrees, out Vector3 axis);
        if (degrees > 180f)
            degrees -= 360f;
        return float.IsInfinity(axis.x) ? Vector3.zero : axis * degrees;
    }
}
