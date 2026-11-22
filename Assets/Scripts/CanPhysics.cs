using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CanInteraction))]
public class CanPhysics : MonoBehaviour
{
    Rigidbody body;
    CanInteraction interaction;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        interaction = GetComponent<CanInteraction>();
    }

    // Runs after every Update this frame, so the grab state is already current.
    void LateUpdate()
    {
        bool grabbed = interaction.IsGrabbed;
        if (body.isKinematic == grabbed)
            return;

        body.isKinematic = grabbed;
    }
}
