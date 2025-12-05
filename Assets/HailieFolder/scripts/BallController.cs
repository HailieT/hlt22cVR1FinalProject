using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    private PickleballGameManager gameManager;
    private Rigidbody rb;

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
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    public void LaunchBall(Vector3 forceVector)
    {
        rb.useGravity = true;
        rb.isKinematic = false;
        if (forceVector != Vector3.zero) rb.AddForce(forceVector, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameManager == null) return;

        GameObject hitObj = collision.gameObject;

        // --- DEBUGGER ---
        // Look at your Console! It will tell you exactly what you hit.
        Debug.Log("Ball Hit: " + hitObj.name);

        // 1. Check Paddles
        if (hitObj == gameManager.player1Paddle)
        {
            gameManager.BallHitPaddle(gameManager.player1Paddle);
            return;
        }
        if (hitObj == gameManager.player2Paddle)
        {
            gameManager.BallHitPaddle(gameManager.player2Paddle);
            return;
        }

        // 2. Check Court Zones
        // IMPORTANT: We check if the hit object IS the zone, OR is a CHILD of the zone
        if (IsZone(hitObj, gameManager.player1RightCourt)) gameManager.BallHitGround(gameManager.player1RightCourt);
        else if (IsZone(hitObj, gameManager.player1LeftCourt)) gameManager.BallHitGround(gameManager.player1LeftCourt);
        else if (IsZone(hitObj, gameManager.player1Kitchen)) gameManager.BallHitGround(gameManager.player1Kitchen);
        else if (IsZone(hitObj, gameManager.player2RightCourt)) gameManager.BallHitGround(gameManager.player2RightCourt);
        else if (IsZone(hitObj, gameManager.player2LeftCourt)) gameManager.BallHitGround(gameManager.player2LeftCourt);
        else if (IsZone(hitObj, gameManager.player2Kitchen)) gameManager.BallHitGround(gameManager.player2Kitchen);
        else if (IsZone(hitObj, gameManager.outOfBoundsZone)) gameManager.BallHitGround(gameManager.outOfBoundsZone);
    }

    // Helper to check if the object we hit is the zone OR part of the zone
    private bool IsZone(GameObject hitObj, Collider zoneCollider)
    {
        if (zoneCollider == null) return false;
        // Check if we hit the collider directly
        if (hitObj == zoneCollider.gameObject) return true;
        // Check if the hit object is a child of the zone transform
        if (hitObj.transform.IsChildOf(zoneCollider.transform)) return true;
        return false;
    }
}