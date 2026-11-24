using UnityEngine;

// Plays the haptic that matches what happens to the can: tap, grab, release, or table hit.
[RequireComponent(typeof(CanInteraction), typeof(CanCollision))]
public class CanHaptics : MonoBehaviour
{
    CanInteraction interaction;
    CanCollision collision;
    bool wasGrabbed;

    void Awake()
    {
        interaction = GetComponent<CanInteraction>();
        collision = GetComponent<CanCollision>();
    }

    void OnEnable()
    {
        interaction.Tapped += OnTapped;
        collision.TableHit += OnTableHit;
    }

    void OnDisable()
    {
        interaction.Tapped -= OnTapped;
        collision.TableHit -= OnTableHit;
    }

    void LateUpdate()
    {
        HapticManager haptics = HapticManager.Instance;
        bool grabbed = interaction.IsGrabbed;
        if (haptics == null)
            return;

        if (grabbed && !wasGrabbed)
            haptics.PlayGrab();
        else if (!grabbed && wasGrabbed)
            haptics.PlayRelease();

        wasGrabbed = grabbed;
    }

    void OnTapped()
    {
        if (HapticManager.Instance != null)
            HapticManager.Instance.PlayTap();
    }

    void OnTableHit(float speed)
    {
        if (HapticManager.Instance != null)
            HapticManager.Instance.PlayCollision();
    }
}
