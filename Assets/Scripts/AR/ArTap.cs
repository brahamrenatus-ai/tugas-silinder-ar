using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Tapping the can on the screen rouses the dragon, like shaking the can does.
[RequireComponent(typeof(ArShake))]
public class ArTap : MonoBehaviour
{
    [SerializeField] float reach = 0.07f;

    ArShake shake;

    void Awake()
    {
        shake = GetComponent<ArShake>();
    }

    void Update()
    {
        Camera cam = Camera.main;
        Pointer pointer = Pointer.current;
        if (cam == null || pointer == null || !pointer.press.wasPressedThisFrame)
            return;

        Vector2 point = pointer.position.ReadValue();
        if (IsOverUI(point))
            return;

        // Distance from the line of sight to the middle of the can.
        Ray ray = cam.ScreenPointToRay(point);
        float distance = Vector3.Cross(ray.direction, transform.position - ray.origin).magnitude;
        if (distance <= reach * transform.lossyScale.x)
            shake.Rouse();
    }

    static bool IsOverUI(Vector2 point)
    {
        if (EventSystem.current == null)
            return false;

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, results);
        return results.Count > 0;
    }
}
