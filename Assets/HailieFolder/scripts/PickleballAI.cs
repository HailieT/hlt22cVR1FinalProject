using UnityEngine;

[RequireComponent(typeof(Rigidbody))] // Ensures the AI always has a Rigidbody
public class PickleballAI : MonoBehaviour
{
    [Header("References")]
    public Transform defaultPosition; // Where the AI returns to when not hitting
    public Transform opponentCourtTarget; // An empty GameObject in the center of the PLAYER'S court

    [Header("Movement Settings")]
    public float moveSpeed = 3.5f; // How fast the AI moves
    public float xBoundary = 2.5f; // How far left/right the AI can go
    public float reactionDistance = 8.0f; // How close ball must be before AI moves

    [Header("Hitting Settings")]
    public float hitForce = 8f; // Power of the return
    public float upwardArc = 0.3f; // How much arc to add to the hit (0.0 to 1.0)
    [Range(0, 1)] public float errorRate = 0.1f; // 0 = Perfect, 1 = Very clumsy

    private GameObject currentBall;
    private Rigidbody currentBallRb;
    private Rigidbody rb; // Reference to our own Rigidbody

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Ensure the paddle doesn't fall over or get pushed by the ball
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // Called by GameManager when a new ball is spawned
    public void AssignBall(GameObject newBall)
    {
        currentBall = newBall;
        if (currentBall != null)
        {
            currentBallRb = currentBall.GetComponent<Rigidbody>();
        }
    }

    // CHANGED: Update -> FixedUpdate for smooth physics interactions
    private void FixedUpdate()
    {
        if (currentBall == null)
        {
            ReturnToIdle(); // Fixed the typo here
            return;
        }

        // Calculate distance to ball
        float distanceToBall = Vector3.Distance(rb.position, currentBall.transform.position);

        // Check if ball is moving towards Positive Z (assuming AI is at +Z end of court)
        bool ballIsComing = currentBallRb.linearVelocity.z > 0;

        // If ball is close enough and coming towards us, move to intercept
        if (distanceToBall < reactionDistance && ballIsComing)
        {
            MoveTowardsBall();
        }
        else
        {
            ReturnToIdle();
        }
    }

    private void MoveTowardsBall()
    {
        // We only want to match the Ball's X position, but keep our own Z (depth) position roughly
        Vector3 targetPos = new Vector3(currentBall.transform.position.x, transform.position.y, transform.position.z);

        // Clamp X so AI doesn't run off court
        targetPos.x = Mathf.Clamp(targetPos.x, -xBoundary, xBoundary);

        // CHANGED: Use MovePosition with fixedDeltaTime
        // This gives the paddle 'velocity' so it hits the ball solidly instead of ghosting through it
        Vector3 newPosition = Vector3.MoveTowards(rb.position, targetPos, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);

        // Face the ball
        transform.LookAt(currentBall.transform);
    }

    // --- THIS WAS MISSING IN YOUR PREVIOUS COPY ---
    private void ReturnToIdle()
    {
        if (defaultPosition != null)
        {
            // Move smoothly back to start
            Vector3 newPosition = Vector3.MoveTowards(rb.position, defaultPosition.position, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(newPosition);

            // Reset rotation smoothly
            Quaternion newRotation = Quaternion.Slerp(transform.rotation, defaultPosition.rotation, Time.fixedDeltaTime * 2f);
            rb.MoveRotation(newRotation);
        }
    }

    // Triggers when the ball physically touches the AI paddle
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == currentBall)
        {
            HitBallBack();
        }
    }

    private void HitBallBack()
    {
        if (currentBallRb == null || opponentCourtTarget == null) return;

        // 1. Calculate direction towards the player's court center
        Vector3 targetDir = (opponentCourtTarget.position - transform.position).normalized;

        // 2. Add Randomness (Error Rate)
        float randomOffset = Random.Range(-2f, 2f) * errorRate;
        targetDir.x += randomOffset;

        // 3. Add Upward Arc (to clear the net)
        targetDir.y += upwardArc;

        // 4. Apply Velocity
        currentBallRb.linearVelocity = Vector3.zero;
        currentBallRb.linearVelocity = targetDir.normalized * hitForce;

        Debug.Log("AI Returned the ball!");
    }
}
