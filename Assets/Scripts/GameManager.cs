using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    bool gameOver = false;
    bool isPaused = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !gameOver)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0;
        var pm = FindObjectOfType<PauseMenu>();
        if (pm != null) pm.Show();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1;
        var pm = FindObjectOfType<PauseMenu>();
        if (pm != null) pm.Hide();
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Game");
    }

    public void OnPlayerDied()
    {
        if (gameOver) return;
        gameOver = true;

        // сохраняем рекорд
        var sm = FindObjectOfType<ScoreManager>();
        if (sm != null)
            SaveManager.TrySaveHighScore((int)sm.GetScore());

        Invoke("RestartGame", 1.5f);
    }

    public void GoToMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu");
    }

    public bool IsGameOver() { return gameOver; }
    public bool IsPaused() { return isPaused; }
}
