using UnityEngine;
using System.Collections;

public class PickleballAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float movementSpeed = 3.5f;
    public float serveDelay = 1.0f;
    public float arcHeight = 1.8f;

    [Header("Serve Targets")]
    public Transform leftCourtTarget;
    public Transform rightCourtTarget;

    private GameObject currentBall;
    private Rigidbody rb;
    private bool isServingRoutineRunning = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Ensure AI paddle doesn't fall over
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void FixedUpdate()
    {
        if (currentBall == null) return;

        Vector3 ballPos = currentBall.transform.position;

        // Infinity Check (prevents crash)
        if (float.IsNaN(ballPos.x) || float.IsInfinity(ballPos.x)) return;

        if (IsMyServe())
        {
            if (!isServingRoutineRunning) StartCoroutine(PerformServe());

            // Hover safely behind ball
            Vector3 hoverPos = ballPos;
            hoverPos.z += 0.5f;
            MovePaddle(hoverPos);
        }
        else
        {
            MovePaddle(ballPos);
        }
    }

    private void MovePaddle(Vector3 targetPos)
    {
        Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, movementSpeed * Time.fixedDeltaTime);

        // Clamp to court boundaries
        newPos.y = Mathf.Clamp(newPos.y, 0.5f, 2.5f);
        newPos.z = Mathf.Clamp(newPos.z, 0.5f, 15.0f);
        newPos.x = Mathf.Clamp(newPos.x, -10f, 10f);

        if (rb != null) rb.MovePosition(newPos);
        else transform.position = newPos;
    }

    public void AssignBall(GameObject ball)
    {
        currentBall = ball;
        isServingRoutineRunning = false;
    }

    private bool IsMyServe()
    {
        if (currentBall == null) return false;

        // --- FIX 2: Check Gravity, NOT Kinematic ---
        // If ball is floating (Gravity OFF) and on my side (Z > 0), it's my serve.
        return currentBall.transform.position.z > 0 && !currentBall.GetComponent<Rigidbody>().useGravity;
    }

    private IEnumerator PerformServe()
    {
        isServingRoutineRunning = true;
        yield return new WaitForSeconds(serveDelay);

        // Double check we still have the ball and it's floating
        if (currentBall != null && !currentBall.GetComponent<Rigidbody>().useGravity)
        {
            Transform target = (Random.value > 0.5f) ? leftCourtTarget : rightCourtTarget;
            Vector3 targetPos = (target != null) ? target.position : new Vector3(0, 0, -5);

            // Calculate Shot
            Vector3 finalVelocity = CalculateParabola(currentBall.transform.position, targetPos, arcHeight);

            // Fallback if Arc fails
            if (float.IsNaN(finalVelocity.x) || finalVelocity == Vector3.zero)
            {
                finalVelocity = (targetPos - currentBall.transform.position).normalized * 10f; // Line Drive
                finalVelocity += Vector3.up * 2f; // Slight lift
            }

            // Execute
            BallController bCtrl = currentBall.GetComponent<BallController>();
            if (bCtrl != null)
            {
                // Force launch because we are bypassing collision
                bCtrl.LaunchBall(Vector3.zero);
                currentBall.GetComponent<Rigidbody>().linearVelocity = finalVelocity;
            }
        }
        isServingRoutineRunning = false;
    }

    private Vector3 CalculateParabola(Vector3 start, Vector3 end, float height)
    {
        float gravity = Physics.gravity.y;
        float displacementY = end.y - start.y;
        Vector3 displacementXZ = new Vector3(end.x - start.x, 0, end.z - start.z);

        if (gravity >= 0) return Vector3.zero;

        if (displacementY > height) height = displacementY + 0.5f;

        float timeUp = Mathf.Sqrt(-2 * height / gravity);
        float timeDown = Mathf.Sqrt(2 * (displacementY - height) / gravity);
        float totalTime = timeUp + timeDown;

        if (float.IsNaN(totalTime) || totalTime < 0.1f) return Vector3.zero;

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * height);
        Vector3 velocityXZ = displacementXZ / totalTime;

        return velocityXZ + velocityY;
    }
}