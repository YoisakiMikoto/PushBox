using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject victoryPanel;
    public GameObject pausePanel;

    private void OnEnable()
    {
        GridManager.OnLevelComplete += ShowVictoryScreen;
        GridManager.OnLevelRestored += HideVictoryScreen;
    }

    private void OnDisable()
    {
        GridManager.OnLevelComplete -= ShowVictoryScreen;
        GridManager.OnLevelRestored -= HideVictoryScreen;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void ShowVictoryScreen()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
        }
    }

    private void HideVictoryScreen()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(false);
        }
    }

    public void GoToNextLevel()
    {
        Time.timeScale = 1f;
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName.StartsWith("Level") && int.TryParse(currentSceneName.Substring(5), out int levelNumber))
        {
            if (levelNumber >= 1 && levelNumber < 5)
            {
                SceneManager.LoadScene($"Level{levelNumber + 1}");
                return;
            }
        }

        GoToMainMenu();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void PauseGame()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
