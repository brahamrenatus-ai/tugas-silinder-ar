using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CanInteraction : MonoBehaviour
{
    // Sentuhan yang lebih singkat dari ini dihitung sebagai tap
    [SerializeField] private float holdSeconds = 0.25f;

    // Kaleng terangkat setinggi ini saat mulai dipegang
    [SerializeField] private float liftHeight = 0.12f;
    [SerializeField] private float followSpeed = 14f;

    // Batas gerak kaleng di atas meja (posisi dunia)
    [SerializeField] private Vector3 minPosition = new Vector3(-0.45f, 1.01f, -0.25f);
    [SerializeField] private Vector3 maxPosition = new Vector3(0.45f, 1.5f, 0.25f);

    private Camera mainCamera;
    private Renderer objectRenderer;

    private bool isPressed = false;
    private bool isGrabbed = false;
    private float pressTime;

    private float grabDistance;
    private Vector3 grabOffset;

    private Color originalColor;
    private Color grabColor = new Color(1.5f, 1.45f, 1.1f);

    // Dapat dibaca oleh script Member 3 nantinya
    public bool IsGrabbed => isGrabbed;

    // Kaleng diketuk: disentuh lalu dilepas sebelum holdSeconds
    public event Action Tapped;

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
            TryPress(pointerPosition);
        }

        if (isPressed && !isGrabbed && pointerHeld && Time.time - pressTime >= holdSeconds)
        {
            Grab(pointerPosition);
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

    void TryPress(Vector2 pointerPosition)
    {
        if (mainCamera == null || IsOverUI(pointerPosition)) return;

        Ray ray = mainCamera.ScreenPointToRay(pointerPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform)
            {
                isPressed = true;
                pressTime = Time.time;

                if (objectRenderer != null)
                {
                    objectRenderer.material.color = grabColor;
                }
            }
        }
    }

    // Sentuhan pada tombol atau panel tidak diteruskan ke kaleng
    bool IsOverUI(Vector2 pointerPosition)
    {
        if (EventSystem.current == null) return false;

        var pointer = new PointerEventData(EventSystem.current) { position = pointerPosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);
        return results.Count > 0;
    }

    void Grab(Vector2 pointerPosition)
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

        grabOffset = transform.position - pointerWorldPosition + Vector3.up * liftHeight;
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

        Vector3 target = pointerWorldPosition + grabOffset;
        target = Vector3.Max(minPosition, Vector3.Min(maxPosition, target));

        // Bergerak halus ke tujuan, jadi kaleng terlihat terangkat
        transform.position = Vector3.Lerp(
            transform.position,
            target,
            1f - Mathf.Exp(-followSpeed * Time.deltaTime)
        );
    }

    void ReleaseObject()
    {
        if (!isPressed) return;

        bool wasTap = !isGrabbed;
        isPressed = false;
        isGrabbed = false;

        if (objectRenderer != null)
        {
            objectRenderer.material.color = originalColor;
        }

        if (wasTap)
        {
            Tapped?.Invoke();
        }
    }
}