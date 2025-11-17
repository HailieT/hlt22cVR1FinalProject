using UnityEngine;
using System.Collections;
using TMPro; // Use this if you're using TextMeshPro for UI

// --- PickleballGameManager.cs ---
// Attach this script to an empty GameObject in your scene, e.g., "_GameManager".
// This script will act as the "brain" for your game, handling scoring, state, and rules.

public class PickleballGameManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // This makes it easy for other scripts (like the ball) to find the one and only GameManager.
    public static PickleballGameManager Instance { get; private set; }

    [Header("Player & Paddle Setup")]
    public GameObject player1Paddle;
    public GameObject player2Paddle;

    [Header("Ball & Spawn Setup")]
    public GameObject ballPrefab; // Your pickleball prefab
    [Tooltip("Position for P1 serving from the RIGHT (Deuce) side")]
    public Transform player1RightServePos;
    [Tooltip("Position for P1 serving from the LEFT (Ad) side")]
    public Transform player1LeftServePos;
    [Tooltip("Position for P2 serving from the RIGHT (Deuce) side")]
    public Transform player2RightServePos;
    [Tooltip("Position for P2 serving from the LEFT (Ad) side")]
    public Transform player2LeftServePos;

    [Header("Court Zone Colliders")]
    [Tooltip("Assign the BoxCollider (set to IsTrigger) for P1's RIGHT (Deuce) court area")]
    public Collider player1RightCourt;
    [Tooltip("Assign the BoxCollider (set to IsTrigger) for P1's LEFT (Ad) court area")]
    public Collider player1LeftCourt;
    [Tooltip("Assign the BoxCollider (set to IsTrigger) for P1's kitchen (NVZ)")]
    public Collider player1Kitchen;
    [Tooltip("Assign the BoxCollider (set to IsTrigger) for P2's RIGHT (Deuce) court area")]
    public Collider player2RightCourt;
    [Tooltip("Assign the BoxCollider (set to IsTrigger) for P2's LEFT (Ad) court area")]
    public Collider player2LeftCourt;
    [Tooltip("Assign the BoxCollider (set to IsTrigger) for P2's kitchen (NVZ)")]
    public Collider player2Kitchen;
    [Tooltip("Assign a large BoxCollider (IsTrigger) that surrounds the court for out-of-bounds")]
    public Collider outOfBoundsZone;

    [Header("Scoring UI (Optional)")]
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;

    // --- Private Game State Variables ---
    private int player1Score;
    private int player2Score;

    private GameObject currentBall;
    private GameObject lastPaddleHit; // Tracks who hit the ball last
    private int bounceCount;
    private bool pointInProgress;
    private bool isPlayer1Serving; // Tracks whose turn it is to serve
    private bool isServing; // Is this the first hit of the point?
    private bool isPlayer1ServingRightSide; // Tracks which side P1 is serving from (Deuce/Ad)
    private bool isPlayer2ServingRightSide; // Tracks which side P2 is serving from (Deuce/Ad)

    private void Awake()
    {
        // Set up the Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Start a new game when the scene loads
        StartNewGame();
    }

    /// <summary>
    /// Initializes a new game, resetting scores and starting the first serve.
    /// </summary>
    public void StartNewGame()
    {
        player1Score = 0;
        player2Score = 0;
        isPlayer1Serving = true; // Player 1 starts serving
        isPlayer1ServingRightSide = true; // Always start on the right side
        isPlayer2ServingRightSide = true;
        UpdateScoreUI();
        StartCoroutine(SetupServe(true));
    }

    /// <summary>
    /// Prepares the court for a new serve.
    /// </summary>
    private IEnumerator SetupServe(bool player1Serves)
    {
        // Wait 2 seconds before starting the next point
        yield return new WaitForSeconds(2.0f);

        // Clean up the old ball if it exists
        if (currentBall != null)
        {
            Destroy(currentBall);
        }

        // Determine serve position
        Transform servePos;
        if (player1Serves)
        {
            servePos = isPlayer1ServingRightSide ? player1RightServePos : player1LeftServePos;
        }
        else
        {
            servePos = isPlayer2ServingRightSide ? player2RightServePos : player2LeftServePos;
        }

        // Spawn a new ball
        currentBall = Instantiate(ballPrefab, servePos.position, servePos.rotation);

        // Reset point state
        pointInProgress = true;
        isServing = true; // This is now a serve
        bounceCount = 0;
        lastPaddleHit = null; // No one has hit the ball yet this point
    }

    /// <summary>
    /// Called by the BallController when it hits a paddle.
    /// </summary>
    public void BallHitPaddle(GameObject paddle)
    {
        if (!pointInProgress) return; // Don't register hits if point is over

        lastPaddleHit = paddle;
        isServing = false; // The ball has been hit (or returned), it's no longer a serve.
        bounceCount = 0; // Reset bounce count on every paddle hit
    }

    /// <summary>
    /// Called by the BallController when it hits a ground zone (trigger).
    /// </summary>
    public void BallHitGround(Collider groundZone)
    {
        if (!pointInProgress) return; // Point is already over, ignore further bounces

        // --- FAULT: Ball landed OUT of bounds ---
        if (groundZone == outOfBoundsZone)
        {
            Debug.Log("FAULT: Out of Bounds!");
            AwardPointToOpponent(lastPaddleHit);
            return;
        }

        // --- SERVE FAULT LOGIC ---
        if (isServing)
        {
            // A serve *must* land in the correct diagonal box.
            // It cannot land in the kitchen.
            if (groundZone == player1Kitchen || groundZone == player2Kitchen)
            {
                Debug.Log("FAULT: Serve landed in the Kitchen!");
                AwardPointToOpponent(null); // 'null' hitter means serve fault
                return;
            }

            // Check for correct service box
            bool validServe = false;
            if (isPlayer1Serving) // P1 is serving
            {
                // Serving from Right (P1) -> Must land in Right (P2)
                if (isPlayer1ServingRightSide && groundZone == player2RightCourt) validServe = true;
                // Serving from Left (P1) -> Must land in Left (P2)
                if (!isPlayer1ServingRightSide && groundZone == player2LeftCourt) validServe = true;
            }
            else // P2 is serving
            {
                // Serving from Right (P2) -> Must land in Right (P1)
                if (isPlayer2ServingRightSide && groundZone == player1RightCourt) validServe = true;
                // Serving from Left (P2) -> Must land in Left (P1)
                if (!isPlayer2ServingRightSide && groundZone == player1LeftCourt) validServe = true;
            }

            if (!validServe)
            {
                Debug.Log("FAULT: Serve landed in wrong box!");
                AwardPointToOpponent(null); // 'null' hitter means serve fault
                return;
            }

            // If we get here, the serve was valid.
            isServing = false; // The next hit is a return, not a serve.
        }

        // --- RALLY LOGIC (after serve) ---
        bounceCount++;

        bool isP1Side = (groundZone == player1RightCourt || groundZone == player1LeftCourt || groundZone == player1Kitchen);
        bool isP2Side = (groundZone == player2RightCourt || groundZone == player2LeftCourt || groundZone == player2Kitchen);

        // --- POINT: Double Bounce ---
        // If the ball bounces twice on the opponent's side, the hitter scores.
        if (bounceCount >= 2)
        {
            Debug.Log("POINT: Double Bounce!");
            AwardPointToHitter(lastPaddleHit);
            return;
        }

        // --- FIRST BOUNCE LOGIC ---
        if (bounceCount == 1)
        {
            // --- FAULT: Hitter hit the ball on their *own* side ---
            if (lastPaddleHit == player1Paddle && isP1Side)
            {
                Debug.Log("FAULT: P1 hit their own side.");
                AwardPointToOpponent(player1Paddle);
            }
            else if (lastPaddleHit == player2Paddle && isP2Side)
            {
                Debug.Log("FAULT: P2 hit their own side.");
                AwardPointToOpponent(player2Paddle);
            }
            // --- ADD SERVE/KITCHEN RULES HERE ---
            // This is where you would check for service faults (e.g., landing in kitchen)
            // or kitchen volley faults (which requires knowing the *player's* position).
        }
    }

    /// <summary>
    /// Awards a point to the player who *hit* the ball.
    /// </summary>
    private void AwardPointToHitter(GameObject hitter)
    {
        pointInProgress = false; // Point is over

        if (hitter == player1Paddle)
        {
            player1Score++;
            isPlayer1Serving = true; // Hitter keeps the serve
            isPlayer1ServingRightSide = !isPlayer1ServingRightSide; // Switch serve side
            Debug.Log("Point for Player 1!");
        }
        else // Must be player 2 (or null, but AI will be P2)
        {
            player2Score++;
            isPlayer1Serving = false; // Hitter keeps the serve
            isPlayer2ServingRightSide = !isPlayer2ServingRightSide; // Switch serve side
            Debug.Log("Point for Player 2!");
        }

        UpdateScoreUI();
        StartCoroutine(SetupServe(isPlayer1Serving)); // Start next serve
    }

    /// <summary>
    /// Awards a point to the *opponent* of the hitter (who committed the fault).
    /// </summary>
    private void AwardPointToOpponent(GameObject hitter)
    {
        pointInProgress = false; // Point is over

        if (isPlayer1Serving) // P1 was serving (or P1 was last to hit)
        {
            player2Score++;
            isPlayer1Serving = false; // Serve goes to P2
            isPlayer2ServingRightSide = !isPlayer2ServingRightSide; // P2 switches side for their first serve
            Debug.Log("Point for Player 2!");
        }
        else // P2 was serving (or P2 was last to hit)
        {
            player1Score++;
            isPlayer1Serving = true; // Serve goes to P1
            isPlayer1ServingRightSide = !isPlayer1ServingRightSide; // P1 switches side for their first serve
            Debug.Log("Point for Player 1!");
        }

        UpdateScoreUI();
        StartCoroutine(SetupServe(isPlayer1Serving)); // Start next serve
    }

    /// <summary>
    /// Updates the score text on the UI.
    /// </summary>
    private void UpdateScoreUI()
    {
        if (player1ScoreText != null)
        {
            player1ScoreText.text = $"P1: {player1Score}";
        }
        if (player2ScoreText != null)
        {
            player2ScoreText.text = $"P2: {player2Score}";
        }
    }
}