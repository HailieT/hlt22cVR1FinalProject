using UnityEngine;

public class PaddleForceBooster : MonoBehaviour
{
    [Tooltip("Multiplies the force of your hit. 1.2 = 20% harder.")]
    public float forceMultiplier = 1.2f; // Reduced slightly for safety

    [Tooltip("The maximum speed the ball is allowed to travel.")]
    public float maxBallSpeed = 15f;

    // NEW: Debounce timer to prevent double-hits
    private float lastHitTime = 0f;
    private const float MIN_HIT_INTERVAL = 0.25f; // Must wait 0.25s between boosts

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            // 1. COOLDOWN CHECK
            // If we just hit the ball 0.01 seconds ago, ignore this collision.
            if (Time.time - lastHitTime < MIN_HIT_INTERVAL) return;

            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                // Update the hit time
                lastHitTime = Time.time;

                // 2. Apply the boost
                ballRb.linearVelocity *= forceMultiplier;

                // 3. CLAMP SPEED (Stop the skyrocketing)
                if (ballRb.linearVelocity.magnitude > maxBallSpeed)
                {
                    ballRb.linearVelocity = Vector3.ClampMagnitude(ballRb.linearVelocity, maxBallSpeed);
                }

                Debug.Log($"Boosted Ball! Speed: {ballRb.linearVelocity.magnitude}");
            }
        }
    }
}