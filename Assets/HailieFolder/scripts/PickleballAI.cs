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

        // --- FORCE PHYSICS SETTINGS (Fixes Disappearing) ---
        if (rb != null)
        {
            rb.isKinematic = true; // MUST BE TRUE
            rb.useGravity = false; // MUST BE FALSE

            // Freezing rotation prevents it from getting knocked over if physics glitch happens
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    private void FixedUpdate()
    {
        // 1. Safety Check: If ball is missing, do nothing
        if (currentBall == null) return;

        // 2. Safety Check: Check for NaN (Not a Number) to prevent vanishing
        if (float.IsNaN(currentBall.transform.position.x)) return;

        if (IsMyServe())
        {
            if (!isServingRoutineRunning) StartCoroutine(PerformServe());

            // Stand 0.5m behind the ball
            Vector3 hoverPos = currentBall.transform.position;
            hoverPos.z += 0.5f;
            MovePaddle(hoverPos);
        }
        else
        {
            MovePaddle(currentBall.transform.position);
        }
    }

    private void MovePaddle(Vector3 targetPos)
    {
        // --- MOVEMENT CLAMP (Fixes Flying Off) ---
        // We calculate the next position, but we clamp it strictly to the court area.
        Vector3 newPos = Vector3.MoveTowards(transform.position, targetPos, movementSpeed * Time.fixedDeltaTime);

        newPos.y = Mathf.Clamp(newPos.y, 0.5f, 2.5f);  // Never go below floor or too high
        newPos.z = Mathf.Clamp(newPos.z, 0.5f, 15.0f); // Never go behind net or too far back
        newPos.x = Mathf.Clamp(newPos.x, -10f, 10f);   // Stay within side boundaries

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
        // Check if ball is floating (Kinematic) and on AI side (Z > 0)
        return currentBall.transform.position.z > 0 && currentBall.GetComponent<Rigidbody>().isKinematic;
    }

    private IEnumerator PerformServe()
    {
        isServingRoutineRunning = true;
        yield return new WaitForSeconds(serveDelay);

        if (currentBall != null)
        {
            Transform target = (Random.value > 0.5f) ? leftCourtTarget : rightCourtTarget;
            // Fallback if target is not assigned
            Vector3 targetPos = (target != null) ? target.position : new Vector3(0, 0, -5);

            Vector3 calculatedVelocity = CalculateParabola(currentBall.transform.position, targetPos, arcHeight);

            // Double check for Math errors (NaN) before applying
            if (!float.IsNaN(calculatedVelocity.x))
            {
                BallController bCtrl = currentBall.GetComponent<BallController>();
                if (bCtrl != null)
                {
                    bCtrl.LaunchBall(Vector3.zero); // Unlock physics
                    currentBall.GetComponent<Rigidbody>().linearVelocity = calculatedVelocity; // Apply precise arc
                }
            }
        }
        isServingRoutineRunning = false;
    }

    private Vector3 CalculateParabola(Vector3 start, Vector3 end, float height)
    {
        float gravity = Physics.gravity.y;
        float displacementY = end.y - start.y;
        Vector3 displacementXZ = new Vector3(end.x - start.x, 0, end.z - start.z);

        if (gravity == 0) return Vector3.zero; // Safety check

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * height);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * height / gravity) + Mathf.Sqrt(2 * (displacementY - height) / gravity));

        return velocityXZ + velocityY;
    }
}