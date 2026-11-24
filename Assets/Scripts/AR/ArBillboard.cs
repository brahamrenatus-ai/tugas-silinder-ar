using UnityEngine;

// Keeps an object facing the camera around its parent's vertical axis, at a fixed offset in view.
public class ArBillboard : MonoBehaviour
{
    [SerializeField] Vector3 offset;

    void LateUpdate()
    {
        Camera cam = Camera.main;
        Transform pivot = transform.parent;
        if (cam == null || pivot == null)
            return;

        Vector3 away = Vector3.ProjectOnPlane(pivot.position - cam.transform.position, pivot.up);
        if (away.sqrMagnitude < 1e-8f)
            return;

        Quaternion facing = Quaternion.LookRotation(away, pivot.up);
        transform.rotation = facing;
        transform.position = pivot.position + facing * Vector3.Scale(offset, pivot.lossyScale);
    }
}
