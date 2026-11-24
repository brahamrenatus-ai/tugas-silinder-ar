using UnityEngine;

// Grows an object from nothing to its own size, slightly past it and back, each time it is shown.
public class ArAppear : MonoBehaviour
{
    [SerializeField] float delay;
    [SerializeField] float duration = 0.6f;
    [SerializeField] float overshoot = 1.5f;

    Vector3 fullScale;
    float elapsed;

    void Awake()
    {
        fullScale = transform.localScale;
    }

    void OnEnable()
    {
        elapsed = 0f;
        Apply();
    }

    void Update()
    {
        if (elapsed >= delay + duration)
            return;

        elapsed += Time.deltaTime;
        Apply();
    }

    void Apply()
    {
        float t = Mathf.Clamp01((elapsed - delay) / duration) - 1f;
        float size = 1f + t * t * ((overshoot + 1f) * t + overshoot);
        transform.localScale = fullScale * Mathf.Max(size, 0f);
    }
}
