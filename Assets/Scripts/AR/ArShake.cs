using UnityEngine;

// Makes the dragon roar, bounces the logo and vibrates the phone when the can is shaken in front of the camera.
public class ArShake : MonoBehaviour
{
    [SerializeField] ArDragon dragon;
    [SerializeField] ParticleSystem bubbles;
    [SerializeField] Transform logo;

    [SerializeField] float stroke = 0.02f;
    [SerializeField] float turnsNeeded = 3f;
    [SerializeField] float turnsWithin = 0.55f;
    [SerializeField] float pause = 1.7f;
    [SerializeField] float phoneUnrestLimit = 0.35f;
    [SerializeField] float topHeight = 0.0585f;

    Vector3 lastPoint, lastMove;
    float lastTurn, turns;
    float lastSeen = -10f, lastRoar = -10f, bounceLeft;
    int roars;

    public int Roars => roars;

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        // Relative to the camera, so that only movement in view counts.
        Vector3 point = cam.transform.InverseTransformPoint(transform.TransformPoint(Vector3.up * topHeight));
        bool justFound = Time.time - lastSeen > 0.6f;
        lastSeen = Time.time;

        if (justFound || ArGravity.Unrest > phoneUnrestLimit)
        {
            lastPoint = point;
            lastMove = Vector3.zero;
            turns = 0f;
        }
        else
        {
            Track(point);
        }

        Bounce();
    }

    // Counts changes of direction that follow each other quickly, each after at least one stroke.
    void Track(Vector3 point)
    {
        Vector3 move = point - lastPoint;
        if (move.magnitude < stroke * transform.lossyScale.x)
            return;

        if (Vector3.Dot(move, lastMove) < 0f)
        {
            turns = Time.time - lastTurn <= turnsWithin ? turns + 1f : 1f;
            lastTurn = Time.time;
            if (turns >= turnsNeeded)
                Rouse();
        }
        lastMove = move;
        lastPoint = point;
    }

    public void Rouse()
    {
        if (Time.time - lastRoar < pause)
            return;

        lastRoar = Time.time;
        bounceLeft = 1f;
        roars++;
        dragon.Roar();
        bubbles.Emit(30);

        if (HapticManager.Instance != null)
            HapticManager.Instance.PlayCollision();
    }

    void Bounce()
    {
        if (bounceLeft <= 0f)
            return;

        bounceLeft = Mathf.Max(0f, bounceLeft - Time.deltaTime / 0.7f);
        float wave = Mathf.Sin((1f - bounceLeft) * 6f * Mathf.PI);
        logo.localScale = Vector3.one * (1f + 0.3f * bounceLeft * wave);
    }
}
