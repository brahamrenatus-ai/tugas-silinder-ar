using UnityEngine;

// Sways and floats the logo, with one full turn at a regular interval.
public class ArLogoMotion : MonoBehaviour
{
    [SerializeField] float swayDegrees = 24f;
    [SerializeField] float swaySpeed = 1.3f;
    [SerializeField] float floatHeight = 0.003f;
    [SerializeField] float turnInterval = 6f;
    [SerializeField] float turnDuration = 1.4f;

    Vector3 startPosition;
    float elapsed;

    void Awake()
    {
        startPosition = transform.localPosition;
    }

    void OnEnable()
    {
        elapsed = 0f;
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        float cycle = elapsed % turnInterval;
        float turn = Mathf.SmoothStep(0f, 360f, cycle / turnDuration);
        float sway = swayDegrees * Mathf.Sin(elapsed * swaySpeed);
        transform.localRotation = Quaternion.Euler(0f, sway + turn, 0f);

        float lift = floatHeight * Mathf.Sin(elapsed * swaySpeed * 1.7f);
        transform.localPosition = startPosition + Vector3.up * lift;
    }
}
