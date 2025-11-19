using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Uses standard Unity UI to prevent errors

public class PickleballGameManager : MonoBehaviour
{
    public static PickleballGameManager Instance { get; private set; }

    [Header("Game Settings")]
    // Public so the Menu can access it
    public float ballDrag = 0.5f;

    [Header("Player & Paddle Setup")]
    public GameObject player1Paddle;
    public GameObject player2Paddle;

    [Header("AI Setup")]
    public PickleballAI aiOpponent;

    [Header("Ball & Spawn Setup")]
    public GameObject ballPrefab;
    public Transform player1RightServePos;
    public Transform player1LeftServePos;
    public Transform player2RightServePos;
    public Transform player2LeftServePos;

    [Header("Court Zone Colliders")]
    public Collider player1RightCourt;
    public Collider player1LeftCourt;
    public Collider player1Kitchen;
    public Collider player2RightCourt;
    public Collider player2LeftCourt;
    public Collider player2Kitchen;
    public Collider outOfBoundsZone;

    [Header("Scoring UI")]
    // Using standard Text objects
    public Text player1ScoreText;
    public Text player2ScoreText;

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
        // We do NOT start the game automatically anymore.
        // We wait for the Menu to call StartNewGame.
        Debug.Log("Game Manager Ready. Waiting for Menu...");
    }

    // Public function called by the Menu button
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

    private IEnumerator SetupServe(bool player1Serves)
    {
        yield return new WaitForSeconds(2.0f);

        if (currentBall != null) Destroy(currentBall);

        Transform servePos;
        if (player1Serves)
        {
            servePos = isPlayer1ServingRightSide ? player1RightServePos : player1LeftServePos;
        }
        else
        {
            servePos = isPlayer2ServingRightSide ? player2RightServePos : player2LeftServePos;
        }

        currentBall = Instantiate(ballPrefab, servePos.position, servePos.rotation);

        // --- APPLY DRAG SETTING ---
        Rigidbody ballRb = currentBall.GetComponent<Rigidbody>();
        if (ballRb != null)
        {
            // Using .drag is safe for Unity 2020, 2021, 2022, and 6
            ballRb.linearDamping = ballDrag;
        }

        // Tell AI about the ball
        if (aiOpponent != null)
        {
            aiOpponent.AssignBall(currentBall);
        }

        pointInProgress = true;
        isServing = true;
        bounceCount = 0;
        lastPaddleHit = null;
    }

    public void BallHitPaddle(GameObject paddle)
    {
        if (!pointInProgress) return;
        lastPaddleHit = paddle;
        isServing = false;
        bounceCount = 0;
    }

    public void BallHitGround(Collider groundZone)
    {
        if (!pointInProgress) return;

        if (groundZone == outOfBoundsZone)
        {
            Debug.Log("FAULT: Out of Bounds!");
            AwardPointToOpponent(lastPaddleHit);
            return;
        }

        if (isServing)
        {
            if (groundZone == player1Kitchen || groundZone == player2Kitchen)
            {
                Debug.Log("FAULT: Serve landed in the Kitchen!");
                AwardPointToOpponent(null);
                return;
            }

            bool validServe = false;
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
            isServing = false;
        }

        bounceCount++;

        bool isP1Side = (groundZone == player1RightCourt || groundZone == player1LeftCourt || groundZone == player1Kitchen);
        bool isP2Side = (groundZone == player2RightCourt || groundZone == player2LeftCourt || groundZone == player2Kitchen);

        if (bounceCount >= 2)
        {
            Debug.Log("POINT: Double Bounce!");
            AwardPointToHitter(lastPaddleHit);
            return;
        }

        if (bounceCount == 1)
        {
            if (lastPaddleHit == player1Paddle && isP1Side) AwardPointToOpponent(player1Paddle);
            else if (lastPaddleHit == player2Paddle && isP2Side) AwardPointToOpponent(player2Paddle);
        }
    }

    private void AwardPointToHitter(GameObject hitter)
    {
        pointInProgress = false;

        if (hitter == player1Paddle)
        {
            player1Score++;
            isPlayer1Serving = true;
            isPlayer1ServingRightSide = !isPlayer1ServingRightSide;
        }
        else
        {
            player2Score++;
            isPlayer1Serving = false;
            isPlayer2ServingRightSide = !isPlayer2ServingRightSide;
        }

        UpdateScoreUI();
        StartCoroutine(SetupServe(isPlayer1Serving));
    }

    private void AwardPointToOpponent(GameObject hitter)
    {
        pointInProgress = false;

        if (isPlayer1Serving)
        {
            player2Score++;
            isPlayer1Serving = false;
            isPlayer2ServingRightSide = !isPlayer2ServingRightSide;
        }
        else
        {
            player1Score++;
            isPlayer1Serving = true;
            isPlayer1ServingRightSide = !isPlayer1ServingRightSide;
        }

        UpdateScoreUI();
        StartCoroutine(SetupServe(isPlayer1Serving));
    }

    private void UpdateScoreUI()
    {
        if (player1ScoreText != null) player1ScoreText.text = $"P1: {player1Score}";
        if (player2ScoreText != null) player2ScoreText.text = $"P2: {player2Score}";
    }
}