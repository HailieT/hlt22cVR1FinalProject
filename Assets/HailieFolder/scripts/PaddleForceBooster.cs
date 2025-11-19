using UnityEngine;

// Attach this to your VR Paddle. 
// It adds a little extra "pop" to the ball when you hit it.

public class PaddleForceBooster : MonoBehaviour
{
    [Tooltip("Multiplies the force of your hit. 1.2 = 20% harder.")]
    public float forceMultiplier = 1.3f;

    private void OnCollisionEnter(Collision collision)
    {
        // Check if we hit the ball (ensure ball has "Ball" tag or check name)
        if (collision.gameObject.CompareTag("Ball") || collision.gameObject.name.Contains("Ball"))
        {
            Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                // Apply the multiplier to the ball's current velocity
                ballRb.linearVelocity *= forceMultiplier;
            }
        }
    }
}
