using UnityEngine;
using UnityEngine.InputSystem;

public class CanInteraction : MonoBehaviour
{
    private Camera mainCamera;
    private Renderer objectRenderer;

    private bool isGrabbed = false;

    private float grabDistance;
    private Vector3 grabOffset;

    private Color originalColor;
    private Color grabColor = Color.green;

    // Dapat dibaca oleh script Member 3 nantinya
    public bool IsGrabbed => isGrabbed;

    void Start()
    {
        mainCamera = Camera.main;
        objectRenderer = GetComponent<Renderer>();

        if (objectRenderer != null)
        {
            originalColor = objectRenderer.material.color;
        }
    }

    void Update()
    {
        bool pointerDown = false;
        bool pointerHeld = false;
        bool pointerUp = false;

        Vector2 pointerPosition = Vector2.zero;

        // Input dari layar sentuh Android
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame ||
                touch.press.isPressed ||
                touch.press.wasReleasedThisFrame)
            {
                pointerDown = touch.press.wasPressedThisFrame;
                pointerHeld = touch.press.isPressed;
                pointerUp = touch.press.wasReleasedThisFrame;
                pointerPosition = touch.position.ReadValue();
            }
        }

        // Input mouse untuk pengujian di Unity Editor
        else if (Mouse.current != null)
        {
            pointerDown = Mouse.current.leftButton.wasPressedThisFrame;
            pointerHeld = Mouse.current.leftButton.isPressed;
            pointerUp = Mouse.current.leftButton.wasReleasedThisFrame;
            pointerPosition = Mouse.current.position.ReadValue();
        }

        if (pointerDown)
        {
            TryGrab(pointerPosition);
        }

        if (isGrabbed && pointerHeld)
        {
            DragObject(pointerPosition);
        }

        if (pointerUp)
        {
            ReleaseObject();
        }
    }

    void TryGrab(Vector2 pointerPosition)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform)
            {
                isGrabbed = true;

                grabDistance = Vector3.Distance(
                    mainCamera.transform.position,
                    transform.position
                );

                Vector3 pointerWorldPosition =
                    mainCamera.ScreenToWorldPoint(
                        new Vector3(
                            pointerPosition.x,
                            pointerPosition.y,
                            grabDistance
                        )
                    );

                grabOffset = transform.position - pointerWorldPosition;

                if (objectRenderer != null)
                {
                    objectRenderer.material.color = grabColor;
                }
            }
        }
    }

    void DragObject(Vector2 pointerPosition)
    {
        Vector3 pointerWorldPosition =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    pointerPosition.x,
                    pointerPosition.y,
                    grabDistance
                )
            );

        transform.position = pointerWorldPosition + grabOffset;
    }

    void ReleaseObject()
    {
        if (!isGrabbed) return;

        isGrabbed = false;

        if (objectRenderer != null)
        {
            objectRenderer.material.color = originalColor;
        }
    }
}