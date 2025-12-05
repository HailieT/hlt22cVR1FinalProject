using UnityEngine;

// --- BallController.cs ---
// Attach this script to your Pickleball PREFAB.
// Requires Rigidbody and SphereCollider.
// Handles floating states, VR physics optimization, and collision reporting.

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class BallController : MonoBehaviour
{
    private PickleballGameManager gameManager;
    private Rigidbody rb;

    [Header("Physics Settings")]
    [Tooltip("Standard Pickleball mass is approx 0.026 kg")]
    public float ballMass = 0.026f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // --- VR PHYSICS SETUP ---
        // 1. Interpolate makes the ball look smooth at high VR refresh rates (90/120Hz).
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 2. ContinuousDynamic is CRITICAL for VR. 
        // It prevents the ball from phasing through the paddle when swung fast.
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 3. Set standard mass
        rb.mass = ballMass;
    }

    private void Start()
    {
        // Find the GameManager automatically
        gameManager = PickleballGameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("BallController: Could not find PickleballGameManager!");
        }

        // Start in "Floating" mode (wait for start button/AI serve)
        SetFloating(true);
    }

    // ------------------------------------------------------------------------
    // PUBLIC METHODS (Call these from GameManager or AI Opponent)
    // ------------------------------------------------------------------------

    /// <summary>
    /// Resets the ball to a specific position (e.g., AI's hand) and makes it float.
    /// Call this when the "Start Button" is pressed.
    /// </summary>
    public void ResetToPosition(Vector3 startPosition)
    {
        transform.position = startPosition;

        // Kill all momentum
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Make it float again
        SetFloating(true);
    }

    /// <summary>
    /// Call this when the AI "hits" the ball to serve.
    /// This enables physics and applies the strike force.
    /// </summary>
    /// <param name="forceVector">Direction and power of the hit</param>
    public void LaunchBall(Vector3 forceVector)
    {
        // Turn on physics
        SetFloating(false);

        // Apply the hit
        rb.AddForce(forceVector, ForceMode.Impulse);
    }

    // ------------------------------------------------------------------------
    // INTERNAL LOGIC
    // ------------------------------------------------------------------------

    private void SetFloating(bool isFloating)
    {
        if (isFloating)
        {
            rb.useGravity = false;
            rb.isKinematic = true; // Locks ball in place, immune to physics
        }
        else
        {
            rb.useGravity = true;
            rb.isKinematic = false; // Unlocks ball, gravity and forces apply
        }
    }

    // ------------------------------------------------------------------------
    // COLLISION LOGIC (Original)
    // ------------------------------------------------------------------------

    private void OnCollisionEnter(Collision collision)
    {
        if (gameManager == null) return;

        // If the ball is floating (kinematic) and gets hit by a moving physics object, 
        // we might want to activate it. 
        // NOTE: Usually, paddles in VR are kinematic too. If using physics paddles:
        if (rb.isKinematic)
        {
            // Optional: Activate ball if player touches it while it's floating
            // SetFloating(false); 
        }

        GameObject hitObject = collision.gameObject;

        if (hitObject == gameManager.player1Paddle)
        {
            gameManager.BallHitPaddle(gameManager.player1Paddle);
        }
        else if (hitObject == gameManager.player2Paddle)
        {
            gameManager.BallHitPaddle(gameManager.player2Paddle);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (gameManager == null) return;

        // Check court zones
        if (other == gameManager.player1RightCourt ||
            other == gameManager.player1LeftCourt ||
            other == gameManager.player1Kitchen ||
            other == gameManager.player2RightCourt ||
            other == gameManager.player2LeftCourt ||
            other == gameManager.player2Kitchen ||
            other == gameManager.outOfBoundsZone)
        {
            gameManager.BallHitGround(other);
        }
    }
}
