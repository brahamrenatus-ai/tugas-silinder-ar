using UnityEngine;

// Keeps an object facing the camera and upright on the screen, at a fixed offset from the can.
// While the can lies on its side it moves to a second offset, clear of the can and of what it pours.
public class ArBillboard : MonoBehaviour
{
    [SerializeField] Vector3 offset;
    [SerializeField] Vector3 leanOffset;    // x is measured away from the top end of the can
    [SerializeField] float swing = 0.03f;   // how far it comes forward on the way there
    [SerializeField] float smoothing = 8f;

    Vector2 lean;
    float side = 1f;
    bool overturned;
    bool placed;

    void OnEnable()
    {
        placed = false;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        Transform pivot = transform.parent;
        if (cam == null || pivot == null)
            return;

        Vector3 up = ArTilt.Up(pivot, cam);
        Vector3 away = Vector3.ProjectOnPlane(pivot.position - cam.transform.position, up);
        if (away.sqrMagnitude < 1e-8f)
            return;

        Vector2 now = ArTilt.Lean(pivot, cam);
        overturned = now.y < (overturned ? 0f : -0.35f);
        float k = placed ? 1f - Mathf.Exp(-smoothing * Time.deltaTime) : 1f;
        lean = Vector2.Lerp(lean, now, k);
        side = Mathf.Lerp(side, overturned ? -1f : 1f, k);
        placed = true;

        float lying = ArTilt.Side(Mathf.Abs(lean.x));
        Vector3 standing = new Vector3(offset.x * side, offset.y, offset.z);
        Vector3 aside = new Vector3(-Mathf.Sign(lean.x) * leanOffset.x, leanOffset.y, leanOffset.z);
        Vector3 at = Vector3.Lerp(standing, aside, lying);
        at.z -= swing * 4f * lying * (1f - lying);

        Quaternion facing = Quaternion.LookRotation(away, up);
        transform.rotation = facing;
        transform.position = pivot.position + facing * Vector3.Scale(at, pivot.lossyScale);
    }
}
