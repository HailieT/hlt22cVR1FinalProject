using UnityEngine;

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

    // Called by GameManager when a new ball is spawned
    public void AssignBall(GameObject newBall)
    {
        currentBall = newBall;
        if (currentBall != null)
        {
            currentBallRb = currentBall.GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        if (currentBall == null)
        {
            ReturnToIdle();
            return;
        }

        // Calculate distance to ball
        float distanceToBall = Vector3.Distance(transform.position, currentBall.transform.position);
        bool ballIsComing = currentBallRb.linearVelocity.z > 0; // Assuming AI is on Positive Z side facing Negative Z

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
        // We allow slight Z movement to step forward into the shot
        Vector3 targetPos = new Vector3(currentBall.transform.position.x, transform.position.y, transform.position.z);

        // Clamp X so AI doesn't run off court
        targetPos.x = Mathf.Clamp(targetPos.x, -xBoundary, xBoundary);

        // Move smoothly
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // Rotate paddle slightly to face the ball (optional aesthetic)
        transform.LookAt(currentBall.transform);
    }

    private void ReturnToIdle()
    {
        if (defaultPosition != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, defaultPosition.position, moveSpeed * Time.deltaTime);

            // Reset rotation smoothly
            transform.rotation = Quaternion.Slerp(transform.rotation, defaultPosition.rotation, Time.deltaTime * 2f);
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
        // We perturb the target X slightly so the AI doesn't hit the exact same spot every time
        float randomOffset = Random.Range(-2f, 2f) * errorRate;
        targetDir.x += randomOffset;

        // 3. Add Upward Arc (to clear the net)
        targetDir.y += upwardArc;

        // 4. Apply Velocity
        // We reset velocity first to cancel out current momentum, then apply the hit
        currentBallRb.linearVelocity = Vector3.zero;
        currentBallRb.linearVelocity = targetDir.normalized * hitForce;

        // Audio feedback could go here
        Debug.Log("AI Returned the ball!");
    }
}
