using UnityEngine;

// --- BallController.cs ---
// Attach this script to your Pickleball PREFAB.
// It requires a Rigidbody and a SphereCollider on the object.
// Its only job is to detect collisions/triggers and report them to the GameManager.

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class BallController : MonoBehaviour
{
    private PickleballGameManager gameManager;

    private void Start()
    {
        // Find the GameManager in the scene automatically when the ball spawns
        gameManager = PickleballGameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("BallController could not find the PickleballGameManager! Make sure the Manager is in the scene.");
        }
    }

    /// <summary>
    // This detects collisions with solid objects (specifically the paddles).
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (gameManager == null) return;

        GameObject hitObject = collision.gameObject;

        // Check if the ball hit one of the registered paddles
        if (hitObject == gameManager.player1Paddle)
        {
            gameManager.BallHitPaddle(gameManager.player1Paddle);
        }
        else if (hitObject == gameManager.player2Paddle)
        {
            gameManager.BallHitPaddle(gameManager.player2Paddle);
        }
    }

    /// <summary>
    // This detects when the ball enters a TRIGGER zone (your court floor areas).
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (gameManager == null) return;

        // Check if the trigger hit is one of the specific court zones registered in the Manager
        if (other == gameManager.player1RightCourt ||
            other == gameManager.player1LeftCourt ||
            other == gameManager.player1Kitchen ||
            other == gameManager.player2RightCourt ||
            other == gameManager.player2LeftCourt ||
            other == gameManager.player2Kitchen ||
            other == gameManager.outOfBoundsZone)
        {
            // Tell the manager *which* specific zone was hit
            gameManager.BallHitGround(other);
        }
    }
}
