using UnityEngine;
using System.Collections;
using TMPro;

public class PickleballGameManager : MonoBehaviour
{
    public static PickleballGameManager Instance { get; private set; }

    [Header("Difficulty / Physics Settings")]
    // 9.81 is Earth gravity. 3.5 is Moon gravity (Slow Motion).
    public float currentGravity = 9.81f;
    public float maxBallSpeed = 15.0f;

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
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // NEW: Called by the Menu to change difficulty
    public void SetDifficulty(bool isEasy)
    {
        if (isEasy)
        {
            currentGravity = 3.5f; // Slow motion (Moon gravity)
            maxBallSpeed = 7.0f;   // Slower max speed
        }
        else
        {
            currentGravity = 9.81f; // Real gravity
            maxBallSpeed = 15.0f;   // Fast speed
        }
    }

    public void StartNewGame(bool aiStartsServing)
    {
        player1Score = 0;
        player2Score = 0;

        if (aiStartsServing) isPlayer1Serving = false;
        else isPlayer1Serving = true;

        isPlayer1ServingRightSide = true;
        isPlayer2ServingRightSide = true;

        UpdateScoreUI();
        StartCoroutine(SetupServe(isPlayer1Serving));
    }

    private IEnumerator SetupServe(bool player1Serves)
    {
        yield return new WaitForSeconds(1.0f);

        Transform serveTransform;
        if (player1Serves)
            serveTransform = isPlayer1ServingRightSide ? player1RightServePos : player1LeftServePos;
        else
            serveTransform = isPlayer2ServingRightSide ? player2RightServePos : player2LeftServePos;

        if (currentBall == null)
        {
            currentBall = Instantiate(ballPrefab, serveTransform.position, serveTransform.rotation);
            if (aiOpponent != null) aiOpponent.AssignBall(currentBall);
        }

        // Reset Ball using the Controller
        BallController bCtrl = currentBall.GetComponent<BallController>();
        if (bCtrl != null)
        {
            bCtrl.ResetToPosition(serveTransform.position);
        }
        else
        {
            // Fallback if script missing
            currentBall.transform.position = serveTransform.position;
            currentBall.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
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
        isServing = false; // Serve is complete once hit
        bounceCount = 0;
    }

    public void BallHitGround(Collider groundZone)
    {
        if (!pointInProgress) return;

        // 1. Out of Bounds
        if (groundZone == outOfBoundsZone)
        {
            AwardPointToOpponent(lastPaddleHit);
            return;
        }

        // 2. Kitchen Fault on Serve
        if (isServing && (groundZone == player1Kitchen || groundZone == player2Kitchen))
        {
            AwardPointToOpponent(null);
            return;
        }

        // 3. Wrong Box on Serve
        if (isServing)
        {
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
                AwardPointToOpponent(null);
                return;
            }
            isServing = false;
        }

        bounceCount++;

        // 4. Double Bounce
        if (bounceCount >= 2)
        {
            AwardPointToHitter(lastPaddleHit);
            return;
        }
    }

    private void AwardPointToHitter(GameObject hitter)
    {
        pointInProgress = false;
        HandleScore(hitter == player1Paddle);
    }

    private void AwardPointToOpponent(GameObject hitter)
    {
        pointInProgress = false;
        if (isPlayer1Serving) HandleScore(false);
        else HandleScore(true);
    }

    private void HandleScore(bool player1WonPoint)
    {
        if (player1WonPoint)
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

    private void UpdateScoreUI()
    {
        if (player1ScoreText != null) player1ScoreText.text = $"P1: {player1Score}";
        if (player2ScoreText != null) player2ScoreText.text = $"P2: {player2Score}";
    }
}