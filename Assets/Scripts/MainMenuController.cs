using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public GameObject InfoPanel;

    void Start()
    {
        if (InfoPanel != null) InfoPanel.SetActive(false);
    }
    
    public void GoToLevel1()
    {
        SceneManager.LoadScene("Level1");
    }

    public void GoToLevelEditor()
    {
        SceneManager.LoadScene("LevelEditor");
    }

    public void ShowInfoPanel()
    {
        if (InfoPanel != null)
        {
            InfoPanel.SetActive(true);
        }
    }

    public void HideInfoPanel()
    {
        if (InfoPanel != null)
        {
            InfoPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
