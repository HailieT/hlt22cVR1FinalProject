using UnityEngine;
using System.Collections;
using TMPro;

public class PickleballGameManager : MonoBehaviour
{
    public static PickleballGameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float ballDrag = 0.5f;

    [Header("Player & Paddle Setup")]
    public GameObject player1Paddle;
    public GameObject player2Paddle;

    [Header("AI Setup")]
    public PickleballAI aiOpponent;

    [Header("Ball & Spawn Setup")]
    public GameObject ballPrefab;
    // These transforms should be placed where the ball "floats" before serving
    public Transform player1RightServePos;
    public Transform player1LeftServePos;
    public Transform player2RightServePos; // AI Right Hand Position
    public Transform player2LeftServePos;  // AI Left Hand Position

    [Header("Court Zone Colliders")]
    public Collider player1RightCourt;
    public Collider player1LeftCourt;
    public Collider player1Kitchen;
    public Collider player2RightCourt;
    public Collider player2LeftCourt;
    public Collider player2Kitchen;
    public Collider outOfBoundsZone;

    [Header("Scoring UI")]
    public TextMeshProUGUI player1ScoreText;
    public TextMeshProUGUI player2ScoreText;

    // Private Variables
    private int player1Score;
    private int player2Score;
    private GameObject currentBall;
    private GameObject lastPaddleHit;
    private int bounceCount;
    private bool pointInProgress;
    private bool isPlayer1Serving;
    private bool isServing;
    private bool isPlayer1ServingRightSide;
    private bool isPlayer2ServingRightSide;

    private void Awake()
    {
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
        Debug.Log("Game Manager Ready. Waiting for Menu to start game...");
    }

    // --- GAME FLOW START ---

    public void StartNewGame(bool aiStartsServing)
    {
        player1Score = 0;
        player2Score = 0;

        if (aiStartsServing)
        {
            isPlayer1Serving = false;
            Debug.Log("Game Started: AI Serving");
        }
        else
        {
            isPlayer1Serving = true;
            Debug.Log("Game Started: Player 1 Serving");
        }

        isPlayer1ServingRightSide = true;
        isPlayer2ServingRightSide = true;

        UpdateScoreUI();
        StartCoroutine(SetupServe(isPlayer1Serving));
    }

    // --- CORE LOGIC: SERVE SETUP ---

    private IEnumerator SetupServe(bool player1Serves)
    {
        // 1. Wait a moment so the previous point can settle visually
        yield return new WaitForSeconds(2.0f);

        // 2. Determine where the ball should float
        Transform serveTransform;
        if (player1Serves)
        {
            serveTransform = isPlayer1ServingRightSide ? player1RightServePos : player1LeftServePos;
        }
        else
        {
            serveTransform = isPlayer2ServingRightSide ? player2RightServePos : player2LeftServePos;
        }

        // 3. Ensure the ball exists
        if (currentBall == null)
        {
            currentBall = Instantiate(ballPrefab, serveTransform.position, serveTransform.rotation);

            // Assign ball to AI so it knows what to hit
            if (aiOpponent != null)
            {
                aiOpponent.AssignBall(currentBall);
            }
        }

        // 4. Update Physics Settings (Drag/Damping)
        Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            // Unity 6 uses linearDamping. Older versions use drag.
            ballRb.linearDamping = ballDrag;
        }

        // 5. RESET THE BALL TO FLOATING STATE
        // This connects to your new BallController script!
        BallController bCtrl = currentBall.GetComponent<BallController>();
        if (bCtrl != null)
        {
            bCtrl.ResetToPosition(serveTransform.position);
        }
        else
        {
            // Fallback if script is missing: just move it
            currentBall.transform.position = serveTransform.position;
            ballRb.linearVelocity = Vector3.zero;
        }

        // 6. Reset Turn Logic
        pointInProgress = true;
        isServing = true;
        bounceCount = 0;
        lastPaddleHit = null;

        Debug.Log($"Serve Setup Complete. Server: {(player1Serves ? "Player 1" : "AI")}");
    }

    // --- GAMEPLAY EVENTS ---

    public void BallHitPaddle(GameObject paddle)
    {
        if (!pointInProgress) return;

        lastPaddleHit = paddle;
        isServing = false; // The moment a paddle hits, the serve "shot" is over
        bounceCount = 0;   // Reset bounce count on every hit
    }

    public void BallHitGround(Collider groundZone)
    {
        if (!pointInProgress) return;

        // 1. Check Out of Bounds
        if (groundZone == outOfBoundsZone)
        {
            Debug.Log("FAULT: Out of Bounds!");
            AwardPointToOpponent(lastPaddleHit);
            return;
        }

        // 2. Check Serve Faults (Wrong Box / Kitchen)
        if (isServing)
        {
            if (groundZone == player1Kitchen || groundZone == player2Kitchen)
            {
                Debug.Log("FAULT: Serve landed in the Kitchen!");
                AwardPointToOpponent(null);
                return;
            }

            bool validServe = false;
            // Validate Diagonal Serves
            if (isPlayer1Serving)
            {
                if (isPlayer1ServingRightSide && groundZone == player2RightCourt) validServe = true;
                if (!isPlayer1ServingRightSide && groundZone == player2LeftCourt) validServe = true;
            }
            else
            {
                if (isPlayer2ServingRightSide && groundZone == player1RightCourt) validServe = true;
                if (!isPlayer2ServingRightSide && groundZone == player1LeftCourt) validServe = true;
            }

            if (!validServe)
            {
                Debug.Log("FAULT: Serve landed in wrong box!");
                AwardPointToOpponent(null);
                return;
            }

            // If we land safely in the correct box, the serve phase ends
            isServing = false;
        }

        // 3. Handle Bounces
        bounceCount++;

        bool isP1Side = (groundZone == player1RightCourt || groundZone == player1LeftCourt || groundZone == player1Kitchen);
        bool isP2Side = (groundZone == player2RightCourt || groundZone == player2LeftCourt || groundZone == player2Kitchen);

        // Double Bounce Rule
        if (bounceCount >= 2)
        {
            Debug.Log("POINT: Double Bounce!");
            // The person who HIT the ball last wins the point
            AwardPointToHitter(lastPaddleHit);
            return;
        }

        // 4. Check if ball landed on hitter's own side (e.g. hit net and fell back)
        if (bounceCount == 1)
        {
            if (lastPaddleHit == player1Paddle && isP1Side)
            {
                AwardPointToOpponent(player1Paddle);
            }
            else if (lastPaddleHit == player2Paddle && isP2Side)
            {
                AwardPointToOpponent(player2Paddle);
            }
        }
    }

    // --- SCORING LOGIC ---

    private void AwardPointToHitter(GameObject hitter)
    {
        pointInProgress = false;

        if (hitter == player1Paddle)
        {
            HandleScore(true);
        }
        else
        {
            HandleScore(false);
        }
    }

    private void AwardPointToOpponent(GameObject hitter)
    {
        pointInProgress = false;

        // If P1 messed up, P2 wins point (or side out logic)
        // Note: Simplified logic (Rally Scoring vs Side Out). 
        // This assumes basic scoring where winning a rally gives a point or changes server.

        if (isPlayer1Serving)
        {
            // P1 was serving but lost. P2 scores/serves.
            HandleScore(false);
        }
        else
        {
            // P2 was serving but lost. P1 scores/serves.
            HandleScore(true);
        }
    }

    private void HandleScore(bool player1WonPoint)
    {
        if (player1WonPoint)
        {
            player1Score++;
            isPlayer1Serving = true;
            isPlayer1ServingRightSide = !isPlayer1ServingRightSide; // Switch sides
        }
        else
        {
            player2Score++;
            isPlayer1Serving = false;
            isPlayer2ServingRightSide = !isPlayer2ServingRightSide; // Switch sides
        }

        UpdateScoreUI();

        // RESTART THE LOOP
        StartCoroutine(SetupServe(isPlayer1Serving));
    }

    private void UpdateScoreUI()
    {
        if (player1ScoreText != null) player1ScoreText.text = $"P1: {player1Score}";
        if (player2ScoreText != null) player2ScoreText.text = $"P2: {player2Score}";
    }
}