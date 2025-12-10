using UnityEngine;

public class PaddleForceBooster : MonoBehaviour
{
    [Header("Power Settings")]
    [Tooltip("Minimum speed the ball will fly off the paddle, even on weak hits.")]
    public float minLaunchSpeed = 7.0f; // Increased for better "tap" feel

    [Tooltip("Added upward lift to help clear the net.")]
    public float upwardLift = 2.5f;

    [Tooltip("Maximum speed cap.")]
    public float maxBallSpeed = 15.0f;

    private float lastHitTime = 0f;
    private const float MIN_HIT_INTERVAL = 0.2f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            // Debounce check
            if (Time.time - lastHitTime < MIN_HIT_INTERVAL) return;

            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                lastHitTime = Time.time;

                // 1. Determine direction
                // Use the collision normal (bounce direction)
                Vector3 bounceDir = collision.contacts[0].normal;

                // Fix weird downward bounces: always ensure some forward/up direction
                if (bounceDir.y < 0) bounceDir.y = 0.2f;
                bounceDir = (bounceDir + transform.forward).normalized;

                // 2. Calculate Speed
                // Take current speed, but ensure it's at least minLaunchSpeed
                float currentSpeed = ballRb.linearVelocity.magnitude;
                float finalSpeed = Mathf.Max(currentSpeed, minLaunchSpeed);

                // 3. Apply Velocity
                // Direction * Speed + Lift
                Vector3 newVelocity = (bounceDir * finalSpeed) + (Vector3.up * upwardLift);

                // 4. Clamp Max Speed
                if (newVelocity.magnitude > maxBallSpeed)
                {
                    newVelocity = Vector3.ClampMagnitude(newVelocity, maxBallSpeed);
                }

                // Apply
                ballRb.linearVelocity = newVelocity;

                // Ensure ball physics is ON (in case it was frozen)
                BallController bCtrl = collision.gameObject.GetComponent<BallController>();
                if (bCtrl != null) bCtrl.LaunchBall(Vector3.zero);
            }
        }
    }
}