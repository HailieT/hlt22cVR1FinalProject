using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    private PickleballGameManager gameManager;
    private Rigidbody rb;
    private bool hasServed = false;

    // NEW: Prevents instant collisions when spawning inside a paddle
    private float spawnTime;
    private const float SPAWN_PROTECTION_TIME = 1.0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void Start()
    {
        gameManager = PickleballGameManager.Instance;
    }

    public void ResetToPosition(Vector3 startPosition)
    {
        transform.position = startPosition;
        spawnTime = Time.time; // Mark the time we spawned

        // Stop movement
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // FLOAT MODE:
        // isKinematic = FALSE (so it detects hits)
        // useGravity = FALSE (so it floats)
        rb.isKinematic = false;
        rb.useGravity = false;

        hasServed = false;
    }

    public void LaunchBall(Vector3 forceVector)
    {
        if (hasServed) return;
        hasServed = true;

        // Enable real physics
        rb.useGravity = true;
        rb.isKinematic = false;

        if (forceVector != Vector3.zero)
        {
            rb.AddForce(forceVector, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameManager == null) return;

        // --- FIX 1: IGNORE COLLISIONS FOR 1 SECOND ---
        // This prevents the ball from dropping instantly if it spawns touching a paddle
        if (Time.time < spawnTime + SPAWN_PROTECTION_TIME) return;

        GameObject hitObj = collision.gameObject;

        // If we are floating and get hit by a Paddle, Launch!
        if (!rb.useGravity && (hitObj.CompareTag("Paddle") || hitObj.GetComponent<PaddleForceBooster>() != null))
        {
            LaunchBall(Vector3.zero);
        }

        // Standard Logic
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
        if (hitObj.transform.parent == zoneCollider.transform) return true;
        return false;
    }
}