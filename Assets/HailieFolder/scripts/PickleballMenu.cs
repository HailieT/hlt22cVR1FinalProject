using UnityEngine;
using TMPro;

public class PickleballMenu : MonoBehaviour
{
    [Header("UI Feedback")]
    public TextMeshPro statusText;

    private void Start()
    {
        if (statusText != null) statusText.text = "Select Mode";
    }

    public void SetEasyMode()
    {
        // Activates Slow Motion / Moon Gravity
        PickleballGameManager.Instance.SetDifficulty(true);

        Debug.Log("Mode: EASY (Slow Motion)");
        if (statusText != null) statusText.text = "Mode: SLOW MOTION";
    }

    public void SetNormalMode()
    {
        // Activates Earth Gravity
        PickleballGameManager.Instance.SetDifficulty(false);

        Debug.Log("Mode: REALISTIC");
        if (statusText != null) statusText.text = "Mode: REALISTIC";
    }

    public void StartMatch()
    {
        Debug.Log("Starting Match...");
        if (statusText != null) statusText.text = "Match Started!";

        // Start game, AI serves first
        PickleballGameManager.Instance.StartNewGame(true);

        // Optional: Hide menu here
        // gameObject.SetActive(false);
    }
}