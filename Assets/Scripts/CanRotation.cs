using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanInteraction))]
public class CanRotation : MonoBehaviour
{
    [SerializeField] float degreesPerScreenWidth = 360f;

    CanInteraction interaction;

    void Awake()
    {
        interaction = GetComponent<CanInteraction>();
    }

    void Update()
    {
        if (!interaction.IsGrabbed || Screen.width <= 0)
            return;

        float deltaX = ReadDeltaX();
        if (deltaX == 0f)
            return;

        float angle = -deltaX / Screen.width * degreesPerScreenWidth;
        transform.Rotate(0f, angle, 0f, Space.World);
    }

    static float ReadDeltaX()
    {
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.isPressed)
            return touch.primaryTouch.delta.ReadValue().x;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
            return mouse.delta.ReadValue().x;

        return 0f;
    }
}
