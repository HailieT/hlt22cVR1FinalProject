using UnityEngine;
using System.Collections.Generic;

// Enum for court zones
public enum CourtZone
{
    LeftServiceBox,
    RightServiceBox,
    LeftKitchen,
    RightKitchen,
    LeftBackcourt,
    RightBackcourt,
    OutOfBounds
}

// Enum for game states
public enum GameState
{
    WaitingForServe,
    ServingPlayer1,
    ServingPlayer2,
    InPlay,
    PointScored
}

// Class to store ball hit data
public class BallHitData
{
    public Vector3 hitPosition;
    public Vector3 hitVelocity;
    public float hitTime;
    public bool wasPlayerHit;
    public int playerNumber;

    public BallHitData(Vector3 pos, Vector3 vel, float time, bool playerHit, int player)
    {
        hitPosition = pos;
        hitVelocity = vel;
        hitTime = time;
        wasPlayerHit = playerHit;
        playerNumber = player;
    }
}

public class PickleballGameManager : MonoBehaviour
{
    [Header("Court Dimensions (Standard Pickleball Court)")]
    public Vector3 courtCenter = Vector3.zero;
    public float courtLength = 13.41f; // 44 feet in meters
    public float courtWidth = 6.1f;    // 20 feet in meters
    public float kitchenDepth = 2.13f; // 7 feet in meters

    [Header("Game Objects")]
    public Transform ball;
    public Transform player1Paddle;
    public Transform player2Paddle;

    [Header("Game State")]
    public GameState currentState = GameState.WaitingForServe;
    public int player1Score = 0;
    public int player2Score = 0;
    public int servingPlayer = 1;
    public bool isServerOnRightSide = true;

    [Header("Tracking")]
    public List<BallHitData> hitHistory = new List<BallHitData>();
    public int bounceCount = 0;
    public CourtZone lastBounceZone = CourtZone.OutOfBounds;
    public bool hasBallBouncedOnServe = false;

    private Vector3 lastBallPosition;
    private Rigidbody ballRb;

    void Start()
    {
        if (ball != null)
        {
            ballRb = ball.GetComponent<Rigidbody>();
            lastBallPosition = ball.position;
        }
    }

    void Update()
    {
        if (ball != null)
        {
            CheckBallBounce();
        }
    }

    // Detect when ball hits the ground
    void CheckBallBounce()
    {
        // Simple ground detection - adjust based on your ground height
        if (ball.position.y < 0.1f && lastBallPosition.y >= 0.1f)
        {
            OnBallBounce(ball.position);
        }
        lastBallPosition = ball.position;
    }

    // Called when ball bounces on court
    public void OnBallBounce(Vector3 bouncePosition)
    {
        bounceCount++;
        CourtZone zone = GetCourtZone(bouncePosition);
        lastBounceZone = zone;

        Debug.Log($"Ball bounced in zone: {zone}, Bounce count: {bounceCount}");

        ProcessBounce(zone);
    }

    // Called when paddle hits ball
    public void OnBallHit(Transform paddle, Vector3 contactPoint, Vector3 velocity)
    {
        int playerNum = (paddle == player1Paddle) ? 1 : 2;
        BallHitData hitData = new BallHitData(contactPoint, velocity, Time.time, true, playerNum);
        hitHistory.Add(hitData);

        Debug.Log($"Player {playerNum} hit the ball at {contactPoint}");
    }

    // Determine which zone the ball landed in
    public CourtZone GetCourtZone(Vector3 position)
    {
        Vector3 localPos = position - courtCenter;

        // Check if out of bounds
        if (Mathf.Abs(localPos.x) > courtWidth / 2 || Mathf.Abs(localPos.z) > courtLength / 2)
        {
            return CourtZone.OutOfBounds;
        }

        bool isLeftSide = localPos.x < 0;
        float absZ = Mathf.Abs(localPos.z);

        // Kitchen zone (no-volley zone)
        if (absZ < kitchenDepth)
        {
            return isLeftSide ? CourtZone.LeftKitchen : CourtZone.RightKitchen;
        }
        // Service box zone
        else if (absZ < courtLength / 4)
        {
            return isLeftSide ? CourtZone.LeftServiceBox : CourtZone.RightServiceBox;
        }
        // Backcourt zone
        else
        {
            return isLeftSide ? CourtZone.LeftBackcourt : CourtZone.RightBackcourt;
        }
    }

    // Process the bounce based on game state
    void ProcessBounce(CourtZone zone)
    {
        switch (currentState)
        {
            case GameState.ServingPlayer1:
            case GameState.ServingPlayer2:
                ProcessServeBounce(zone);
                break;

            case GameState.InPlay:
                ProcessInPlayBounce(zone);
                break;
        }
    }

    // Handle serve bounce rules
    void ProcessServeBounce(CourtZone zone)
    {
        if (!hasBallBouncedOnServe)
        {
            // First bounce must be in diagonal service box
            bool isValidServe = false;

            if (servingPlayer == 1 && isServerOnRightSide)
            {
                isValidServe = (zone == CourtZone.LeftServiceBox);
            }
            else if (servingPlayer == 1 && !isServerOnRightSide)
            {
                isValidServe = (zone == CourtZone.RightServiceBox);
            }
            else if (servingPlayer == 2 && isServerOnRightSide)
            {
                isValidServe = (zone == CourtZone.LeftServiceBox);
            }
            else if (servingPlayer == 2 && !isServerOnRightSide)
            {
                isValidServe = (zone == CourtZone.RightServiceBox);
            }

            if (isValidServe)
            {
                hasBallBouncedOnServe = true;
                Debug.Log("Valid serve!");
            }
            else
            {
                Debug.Log("Serve fault!");
                HandleFault();
            }
        }
        else
        {
            // Second bounce (receiving player's side)
            currentState = GameState.InPlay;
        }
    }

    // Handle in-play bounce rules
    void ProcessInPlayBounce(CourtZone zone)
    {
        if (zone == CourtZone.OutOfBounds)
        {
            Debug.Log("Ball out of bounds!");
            AwardPoint();
        }
        else if (bounceCount > 1)
        {
            // Ball bounced twice on one side
            Debug.Log("Double bounce!");
            AwardPoint();
        }
    }

    // Handle serve faults
    void HandleFault()
    {
        // Switch server or award point based on your rules
        currentState = GameState.WaitingForServe;
        ResetRally();
    }

    // Award point to appropriate player
    void AwardPoint()
    {
        // Determine who gets the point based on last successful hit
        if (hitHistory.Count > 0)
        {
            BallHitData lastHit = hitHistory[hitHistory.Count - 1];
            if (lastHit.playerNumber == 1)
            {
                player1Score++;
                Debug.Log($"Point to Player 1! Score: {player1Score}-{player2Score}");
            }
            else
            {
                player2Score++;
                Debug.Log($"Point to Player 2! Score: {player1Score}-{player2Score}");
            }
        }

        currentState = GameState.PointScored;
        Invoke("PrepareNextServe", 2f);
    }

    // Prepare for next serve
    void PrepareNextServe()
    {
        ResetRally();
        currentState = GameState.WaitingForServe;
    }

    // Reset rally counters
    void ResetRally()
    {
        bounceCount = 0;
        hasBallBouncedOnServe = false;
        hitHistory.Clear();
    }

    // Start a serve
    public void StartServe(int player)
    {
        servingPlayer = player;
        currentState = (player == 1) ? GameState.ServingPlayer1 : GameState.ServingPlayer2;
        ResetRally();
        Debug.Log($"Player {player} serving from {(isServerOnRightSide ? "right" : "left")} side");
    }

    // Visualize court zones in editor
    void OnDrawGizmos()
    {
        // Draw court boundaries
        Gizmos.color = Color.white;
        DrawCourtRectangle(courtCenter, courtWidth, courtLength);

        // Draw kitchen (no-volley zone)
        Gizmos.color = Color.yellow;
        DrawCourtRectangle(courtCenter + Vector3.forward * (courtLength / 4 - kitchenDepth / 2),
                          courtWidth, kitchenDepth);
        DrawCourtRectangle(courtCenter + Vector3.back * (courtLength / 4 - kitchenDepth / 2),
                          courtWidth, kitchenDepth);

        // Draw service boxes
        Gizmos.color = Color.green;
        float serviceBoxDepth = courtLength / 4 - kitchenDepth;
        DrawCourtRectangle(courtCenter + Vector3.forward * (kitchenDepth + serviceBoxDepth / 2),
                          courtWidth, serviceBoxDepth);
        DrawCourtRectangle(courtCenter + Vector3.back * (kitchenDepth + serviceBoxDepth / 2),
                          courtWidth, serviceBoxDepth);

        // Draw center line
        Gizmos.color = Color.white;
        Gizmos.DrawLine(courtCenter + Vector3.forward * courtLength / 2,
                       courtCenter + Vector3.back * courtLength / 2);
    }

    void DrawCourtRectangle(Vector3 center, float width, float length)
    {
        Vector3 topLeft = center + new Vector3(-width / 2, 0, length / 2);
        Vector3 topRight = center + new Vector3(width / 2, 0, length / 2);
        Vector3 bottomLeft = center + new Vector3(-width / 2, 0, -length / 2);
        Vector3 bottomRight = center + new Vector3(width / 2, 0, -length / 2);

        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}