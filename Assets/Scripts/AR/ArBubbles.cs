using UnityEngine;

// Keeps the bubbles rising against gravity however the can is tilted. They start at the lowest point of the can.
[RequireComponent(typeof(ParticleSystem))]
public class ArBubbles : MonoBehaviour
{
    [SerializeField] float halfHeight = 0.055f;
    [SerializeField] float radius = 0.033f;

    void LateUpdate()
    {
        Camera cam = Camera.main;
        Transform can = transform.parent;
        if (cam == null || can == null)
            return;

        Vector3 up = ArGravity.Up(cam);
        float along = Mathf.Abs(Vector3.Dot(can.up, up));
        float depth = halfHeight * along + radius * Mathf.Sqrt(Mathf.Max(0f, 1f - along * along));

        // The cone of the particle system points along +Z.
        transform.rotation = Quaternion.LookRotation(up, cam.transform.right);
        transform.position = can.position - up * depth * can.lossyScale.x;
    }
}
