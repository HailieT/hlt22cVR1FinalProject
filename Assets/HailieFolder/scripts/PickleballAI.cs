using UnityEngine;
using System.Collections;

public class PickleballAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float movementSpeed = 3.5f;
    public float serveDelay = 1.0f;
    public float arcHeight = 2.0f;

    [Header("Targets")]
    public Transform leftCourtTarget;
    public Transform rightCourtTarget;
    public Transform restingPosition; // Create an empty GameObject behind the court for this

    private GameObject currentBall;
    private Rigidbody rb;
    private bool isServingRoutineRunning = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // AI Paddle must not fall
            rb.useGravity = false;
        }
    }

    private void FixedUpdate()
    {
        if (currentBall == null) return;

        Vector3 ballPos = currentBall.transform.position;

        // SAFETY CHECK: If ball position is invalid (NaN), do nothing.
        if (HasNaNs(ballPos)) return;

        if (IsMyServe())
        {
            if (!isServingRoutineRunning) StartCoroutine(PerformServe());

            // Hover safely behind ball while waiting
            Vector3 hoverPos = ballPos + new Vector3(0, 0, 0.5f);
            SmoothMove(hoverPos);
        }
        else
        {
            // Simple Logic: If ball coming to my side (Z > 0), move to it.
            // Adjust '0' based on where your net is.
            if (ballPos.z > 0)
            {
                SmoothMove(ballPos);
            }
            else if (restingPosition != null)
            {
                SmoothMove(restingPosition.position);
            }
        }
    }

    private void SmoothMove(Vector3 targetPos)
    {
        // SAFETY CHECK: Don't move to infinity
        if (HasNaNs(targetPos)) return;

        // Constraint movement to court area (Adjust values for your specific court size)
        targetPos.x = Mathf.Clamp(targetPos.x, -5f, 5f);
        targetPos.y = Mathf.Clamp(targetPos.y, 0.5f, 2.5f);
        targetPos.z = Mathf.Clamp(targetPos.z, 0.5f, 12f); // Don't go past net

        // Lerp for smooth non-jittery movement
        Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPos, movementSpeed * Time.fixedDeltaTime);

        if (rb != null) rb.MovePosition(smoothedPos);
        else transform.position = smoothedPos;
    }

    private bool HasNaNs(Vector3 v)
    {
        return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
               float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);
    }

    public void AssignBall(GameObject ball)
    {
        currentBall = ball;
        isServingRoutineRunning = false;
    }

    private bool IsMyServe()
    {
        if (currentBall == null) return false;
        // If ball is kinematic (frozen), it's a serve situation
        return currentBall.GetComponent<Rigidbody>().isKinematic;
    }

    private IEnumerator PerformServe()
    {
        isServingRoutineRunning = true;
        yield return new WaitForSeconds(serveDelay);

        if (currentBall != null && currentBall.GetComponent<Rigidbody>().isKinematic)
        {
            Transform target = (Random.value > 0.5f) ? leftCourtTarget : rightCourtTarget;
            Vector3 targetPos = (target != null) ? target.position : new Vector3(0, 0, -5);

            // Calculate trajectory
            Vector3 finalVelocity = CalculateParabola(currentBall.transform.position, targetPos, arcHeight);

            // If Math failed, use a simple fallback hit
            if (HasNaNs(finalVelocity) || finalVelocity == Vector3.zero)
            {
                finalVelocity = (targetPos - currentBall.transform.position).normalized * 5f + Vector3.up * 2f;
            }

            BallController bCtrl = currentBall.GetComponent<BallController>();
            if (bCtrl != null)
            {
                bCtrl.LaunchBall(Vector3.zero); // Activate Physics
                currentBall.GetComponent<Rigidbody>().linearVelocity = finalVelocity; // Apply Shot
            }
        }
        isServingRoutineRunning = false;
    }

    private Vector3 CalculateParabola(Vector3 start, Vector3 end, float height)
    {
        // VITAL: Get gravity from Manager so AI understands Slow Motion
        float gravity = -PickleballGameManager.Instance.currentGravity;

        // Safety for divide by zero
        if (Mathf.Abs(gravity) < 0.01f) gravity = -1.0f;

        float displacementY = end.y - start.y;
        Vector3 displacementXZ = new Vector3(end.x - start.x, 0, end.z - start.z);

        if (displacementY > height) height = displacementY + 0.5f;

        float term1 = -2 * height / gravity;
        float term2 = 2 * (displacementY - height) / gravity;

        // Negative Sqrt check
        if (term1 < 0 || term2 < 0) return Vector3.zero;

        float timeUp = Mathf.Sqrt(term1);
        float timeDown = Mathf.Sqrt(term2);
        float totalTime = timeUp + timeDown;

        if (totalTime < 0.1f) return Vector3.zero;

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * height);
        Vector3 velocityXZ = displacementXZ / totalTime;

        return velocityXZ + velocityY;
    }
}