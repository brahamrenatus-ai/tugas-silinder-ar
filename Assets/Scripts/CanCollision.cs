using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CanInteraction))]
public class CanCollision : MonoBehaviour
{
    [SerializeField] string tableName = "Table";
    [SerializeField] float minImpactSpeed = 0.5f;
    [SerializeField] float respawnBelowY = 0.2f;

    Rigidbody body;
    CanInteraction interaction;
    Vector3 startPosition;
    Quaternion startRotation;

    // Raised with the impact speed (m/s) when the released can hits the table.
    public event Action<float> TableHit;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        interaction = GetComponent<CanInteraction>();
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (interaction.IsGrabbed || collision.gameObject.name != tableName)
            return;

        float speed = collision.relativeVelocity.magnitude;
        if (speed >= minImpactSpeed)
            TableHit?.Invoke(speed);
    }

    void FixedUpdate()
    {
        if (body.isKinematic || body.position.y >= respawnBelowY)
            return;

        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = startPosition;
        body.rotation = startRotation;
    }
}
