using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    //public GameObject mainPanel;
    public GameObject settingsPanel;

    public bool settingsActive;

    void Start()
    {
        //mainPanel.SetActive(true);
        settingsPanel.SetActive(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void OpenSettings()
    {
        //mainPanel.SetActive(false);
        settingsActive = !settingsActive;
        settingsPanel.SetActive(settingsActive);
    }

    //public void CloseSettings()
    //{
    //    settingsPanel.SetActive(false);
    //    //mainPanel.SetActive(true);
    //}

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit"); // only visible in editor since Quit doesnt work in editor
    }
}