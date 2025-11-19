using UnityEngine;
using TMPro;

public class PickleballMenu : MonoBehaviour
{
    [Header("UI Feedback (Optional)")]
    public TextMeshPro statusText; // Assign a 3D text object here to show "Normal Mode Selected" etc.

    private void Start()
    {
        if (statusText != null) statusText.text = "Select Difficulty";
    }

    public void SetEasyMode()
    {
        // Easy Mode = Drag 1.0 (Ball slows down fast)
        PickleballGameManager.Instance.ballDrag = 1.0f;

        Debug.Log("Difficulty Set: EASY (Drag 1.0)");
        if (statusText != null) statusText.text = "Mode: EASY";
    }

    public void SetNormalMode()
    {
        // Normal Mode = Drag 0.5 (Ball flies normally)
        PickleballGameManager.Instance.ballDrag = 0.5f;

        Debug.Log("Difficulty Set: NORMAL (Drag 0.5)");
        if (statusText != null) statusText.text = "Mode: NORMAL";
    }

    public void StartMatch()
    {
        // Start the game, passing 'true' to make the AI serve first
        Debug.Log("Starting Match...");
        if (statusText != null) statusText.text = "Match Started!";

        // Hide the menu buttons (Optional: attach the parent of the buttons to this script to disable them)
        // gameObject.SetActive(false); 

        PickleballGameManager.Instance.StartNewGame(true);
    }
}