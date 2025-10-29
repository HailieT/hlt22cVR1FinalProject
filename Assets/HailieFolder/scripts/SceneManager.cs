using UnityEngine;
using UnityEngine.SceneManagement; // For scene management

public class GameManager : MonoBehaviour
{
    // Define possible game states
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Paused,
        GameOver
    }

    public static GameManager Instance { get; private set; } // Singleton pattern

    public GameState CurrentGameState { get; private set; }

    [SerializeField] private GameObject vrCameraRig; // Reference to the VR camera rig

    private void Awake()
    {
        // Implement Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetGameState(GameState.MainMenu); // Start in the main menu
    }

    public void SetGameState(GameState newState)
    {
        CurrentGameState = newState;
        Debug.Log($"Game State Changed to: {newState}");

        // Handle state-specific actions
        switch (newState)
        {
            case GameState.MainMenu:
                // Load main menu scene, enable menu UI, etc.
                SceneManager.LoadScene("MainMenuScene");
                break;
            case GameState.Playing:
                // Load game scene, disable menu UI, enable gameplay elements
                SceneManager.LoadScene("GameScene");
                break;
            case GameState.Paused:
                // Pause game logic, show pause menu
                Time.timeScale = 0f; // Pause time
                // Enable pause menu UI
                break;
            case GameState.GameOver:
                // Show game over screen, reset game state
                // Load game over scene or display UI
                break;
                // Add other states as needed
        }
    }

    public void LoadGameScene(string sceneName)
    {
        SetGameState(GameState.Loading);
        SceneManager.LoadScene(sceneName);
    }

    public void PauseGame()
    {
        SetGameState(GameState.Paused);
    }

    public void ResumeGame()
    {
        SetGameState(GameState.Playing);
        Time.timeScale = 1f; // Resume time
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}