using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    private PickleballGameManager gameManager;
    private Rigidbody rb;
    private bool isPlayable = false; // Is ball currently active?

    // Prevents instant collisions right after spawning
    private float spawnTime;
    private const float SPAWN_PROTECTION_TIME = 0.5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // IMPORTANT: We turn off Unity gravity to use our Custom Slow-Motion Gravity
        rb.useGravity = false;
    }

    private void Start()
    {
        gameManager = PickleballGameManager.Instance;
    }

    private void FixedUpdate()
    {
        if (gameManager == null) return;

        // Apply Custom Gravity only if the ball is in play (not frozen for serve)
        if (isPlayable && !rb.isKinematic)
        {
            // Apply downward force based on GameManager settings
            Vector3 gravityForce = Vector3.down * gameManager.currentGravity;
            rb.AddForce(gravityForce, ForceMode.Acceleration);

            // Clamp Speed (Prevent it from moving too fast)
            float limit = gameManager.maxBallSpeed;
            if (rb.linearVelocity.magnitude > limit)
            {
                rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, limit);
            }
        }
    }

    public void ResetToPosition(Vector3 startPosition)
    {
        transform.position = startPosition;
        spawnTime = Time.time;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // FREEZE PHYSICS so it doesn't drop
        rb.isKinematic = true;
        isPlayable = false;
    }

    public void LaunchBall(Vector3 forceVector)
    {
        isPlayable = true;
        rb.isKinematic = false; // Enable movement

        if (forceVector != Vector3.zero)
        {
            rb.AddForce(forceVector, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameManager == null) return;
        if (Time.time < spawnTime + SPAWN_PROTECTION_TIME) return;

        GameObject hitObj = collision.gameObject;

        // If ball was frozen (waiting for serve) and gets hit by paddle, Launch it!
        if (!isPlayable && (hitObj.CompareTag("Paddle") || hitObj.GetComponent<PaddleForceBooster>() != null))
        {
            LaunchBall(Vector3.zero);
        }

        // Report collisions to Manager
        if (hitObj == gameManager.player1Paddle) gameManager.BallHitPaddle(hitObj);
        else if (hitObj == gameManager.player2Paddle) gameManager.BallHitPaddle(hitObj);
        else if (IsZone(hitObj, gameManager.player1RightCourt)) gameManager.BallHitGround(gameManager.player1RightCourt);
        else if (IsZone(hitObj, gameManager.player1LeftCourt)) gameManager.BallHitGround(gameManager.player1LeftCourt);
        else if (IsZone(hitObj, gameManager.player1Kitchen)) gameManager.BallHitGround(gameManager.player1Kitchen);
        else if (IsZone(hitObj, gameManager.player2RightCourt)) gameManager.BallHitGround(gameManager.player2RightCourt);
        else if (IsZone(hitObj, gameManager.player2LeftCourt)) gameManager.BallHitGround(gameManager.player2LeftCourt);
        else if (IsZone(hitObj, gameManager.player2Kitchen)) gameManager.BallHitGround(gameManager.player2Kitchen);
        else if (IsZone(hitObj, gameManager.outOfBoundsZone)) gameManager.BallHitGround(gameManager.outOfBoundsZone);
    }

    private bool IsZone(GameObject hitObj, Collider zoneCollider)
    {
        if (zoneCollider == null) return false;
        if (hitObj == zoneCollider.gameObject) return true;
        // Check parent incase collider is a child object
        if (hitObj.transform.parent == zoneCollider.transform) return true;
        return false;
    }
}