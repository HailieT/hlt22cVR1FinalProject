
using UnityEngine;
using System.Collections;

public class PickleballAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float movementSpeed = 3.5f;
    public float serveDelay = 1.0f;

    // We no longer need "Serve Power" because Math calculates the exact power needed.
    [Header("Trajectory Settings")]
    public float arcHeight = 1.8f; // Peak height of the ball (Higher than net which is ~0.9m)

    [Header("Serve Targets")]
    public Transform leftCourtTarget;
    public Transform rightCourtTarget;

    private GameObject currentBall;
    private PickleballGameManager gameManager;
    private Rigidbody rb;
    private bool isServingRoutineRunning = false;

    private void Start()
    {
        gameManager = PickleballGameManager.Instance;
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void FixedUpdate()
    {
        if (currentBall == null || gameManager == null) return;

        bool isMyServe = IsMyServe();

        if (isMyServe)
        {
            if (!isServingRoutineRunning)
            {
                StartCoroutine(PerformServe());
            }

            // Hover behind ball
            Vector3 hoverPos = currentBall.transform.position;
            hoverPos.z += 0.5f;
            MovePaddle(hoverPos);
        }
        else
        {
            MovePaddle(currentBall.transform.position);
        }
    }

    public void AssignBall(GameObject ball)
    {
        currentBall = ball;
        isServingRoutineRunning = false;
    }

    private bool IsMyServe()
    {
        if (currentBall == null) return false;
        return currentBall.transform.position.z > 0 && currentBall.GetComponent<Rigidbody>().isKinematic;
    }

    private IEnumerator PerformServe()
    {
        isServingRoutineRunning = true;
        yield return new WaitForSeconds(serveDelay);

        if (currentBall != null)
        {
            // 1. Pick a Target
            Transform targetTransform = (Random.value > 0.5f) ? leftCourtTarget : rightCourtTarget;
            Vector3 targetPosition = targetTransform.position;

            // 2. CALCULATE PHYSICS ARC
            // We use the ball's current position and the target position.
            // "arcHeight" ensures it clears the net.
            Vector3 calculatedVelocity = CalculateParabola(currentBall.transform.position, targetPosition, arcHeight);

            // 3. Launch
            BallController bCtrl = currentBall.GetComponent<BallController>();
            if (bCtrl != null)
            {
                // Note: We use VelocityChange to set exact speed, ignoring mass
                bCtrl.LaunchBall(Vector3.zero); // Unfreeze first

                // We apply the velocity directly to the Rigidbody for precision
                currentBall.GetComponent<Rigidbody>().linearVelocity = calculatedVelocity;
            }
        }

        isServingRoutineRunning = false;
    }

    private void MovePaddle(Vector3 targetPos)
    {
        Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, movementSpeed * Time.fixedDeltaTime);
        newPos.y = Mathf.Clamp(newPos.y, 0.5f, 2.0f);
        newPos.z = Mathf.Clamp(newPos.z, 0.5f, 15f);
        rb.MovePosition(newPos);
    }

    // --- THE MATH MAGIC ---
    // This calculates the exact velocity needed to throw Object A to Position B with a specific height curve.
    private Vector3 CalculateParabola(Vector3 start, Vector3 end, float height)
    {
        float gravity = Physics.gravity.y;
        float displacementY = end.y - start.y;
        Vector3 displacementXZ = new Vector3(end.x - start.x, 0, end.z - start.z);

        // Math formula for projectile motion
        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * height);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * height / gravity) + Mathf.Sqrt(2 * (displacementY - height) / gravity));

        return velocityXZ + velocityY;
    }
}