using UnityEngine;

// Turns the content the right way up once, on the first frame after the target is found.
public class ArUpright : MonoBehaviour
{
    bool pending;

    void OnEnable()
    {
        pending = true;
    }

    // The target pose is written in Update, so the direction is only known here.
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (!pending || cam == null || transform.parent == null)
            return;

        pending = false;
        bool upsideDown = Vector3.Dot(transform.parent.up, cam.transform.up) < 0f;
        transform.localRotation = Quaternion.Euler(0f, 0f, upsideDown ? 180f : 0f);
    }
}
