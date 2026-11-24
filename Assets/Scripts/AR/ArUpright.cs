using UnityEngine;

// Turns the content the right way up on the first frame after the target is found.
// A short loss of tracking keeps the choice, so a can that is being poured is not turned over.
public class ArUpright : MonoBehaviour
{
    [SerializeField] float chooseAgainAfter = 4f;

    bool pending;
    float hiddenAt = float.NegativeInfinity;

    void OnEnable()
    {
        if (Time.time - hiddenAt > chooseAgainAfter)
            pending = true;
    }

    void OnDisable()
    {
        hiddenAt = Time.time;
    }

    // The target pose is written in Update, so the direction is only known here.
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (!pending || cam == null || transform.parent == null)
            return;

        pending = false;
        bool upsideDown = Vector3.Dot(transform.parent.up, ArGravity.Up(cam)) < 0f;
        transform.localRotation = Quaternion.Euler(0f, 0f, upsideDown ? 180f : 0f);
    }
}
