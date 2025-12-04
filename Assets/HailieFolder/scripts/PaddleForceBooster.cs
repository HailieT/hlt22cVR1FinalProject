using UnityEngine;

// Attach this to your VR Paddle. 
// It adds a little extra "pop" to the ball when you hit it.

public class PaddleForceBooster : MonoBehaviour
{
    [Tooltip("Multiplies the force of your hit. 1.2 = 20% harder.")]
    public float forceMultiplier = 1.3f;

    [Tooltip("The maximum speed the ball is allowed to travel. Prevents it from glitching through walls.")]
    public float maxBallSpeed = 15f; // 15-20 is usually a good "Fast" speed in Unity meters

    private void OnCollisionEnter(Collision collision)
    {
        // Check if we hit the ball
        // Ensure your Ball prefab is tagged "Ball" in the Inspector!
        if (collision.gameObject.CompareTag("Ball"))
        {
            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                // 1. Apply the multiplier to the ball's current velocity
                ballRb.linearVelocity *= forceMultiplier;

                // 2. NEW: Clamp the magnitude (speed) to prevent physics glitches
                // This ensures the ball never moves faster than 'maxBallSpeed'
                ballRb.linearVelocity = Vector3.ClampMagnitude(ballRb.linearVelocity, maxBallSpeed);
            }
        }
    }
}
