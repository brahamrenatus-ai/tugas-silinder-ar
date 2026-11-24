using UnityEngine;

// Plays the haptic that matches what happens to the can: tap, grab, release, or table hit.
[RequireComponent(typeof(CanInteraction), typeof(CanCollision))]
public class CanHaptics : MonoBehaviour
{
    [SerializeField] float tapMaxSeconds = 0.2f;

    CanInteraction interaction;
    CanCollision collision;
    bool wasGrabbed;
    bool grabPlayed;
    float grabStart;

    void Awake()
    {
        interaction = GetComponent<CanInteraction>();
        collision = GetComponent<CanCollision>();
    }

    void OnEnable()
    {
        collision.TableHit += OnTableHit;
    }

    void OnDisable()
    {
        collision.TableHit -= OnTableHit;
    }

    void LateUpdate()
    {
        HapticManager haptics = HapticManager.Instance;
        bool grabbed = interaction.IsGrabbed;
        if (haptics == null)
            return;

        if (grabbed && !wasGrabbed)
        {
            grabStart = Time.time;
            grabPlayed = false;
        }
        else if (grabbed && !grabPlayed && Time.time - grabStart >= tapMaxSeconds)
        {
            grabPlayed = true;
            haptics.PlayGrab();
        }
        else if (!grabbed && wasGrabbed)
        {
            if (grabPlayed)
                haptics.PlayRelease();
            else
                haptics.PlayTap();
        }

        wasGrabbed = grabbed;
    }

    void OnTableHit(float speed)
    {
        if (HapticManager.Instance != null)
            HapticManager.Instance.PlayCollision();
    }
}
