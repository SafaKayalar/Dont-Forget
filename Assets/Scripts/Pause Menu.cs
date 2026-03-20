using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject menu;   // Canvas içindeki menü paneli
    private bool isActive = false;

    private void Start()
    {
        menu.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        isActive = !isActive;
        menu.SetActive(isActive);

        // Menü açýkken oyunu durdur
        if (isActive)
            Time.timeScale = 0f;
        else
            Time.timeScale = 1f;
    }

    // === Butonlar için ===
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // zamaný normale döndür
        SceneManager.LoadScene("Main Menu"); // kendi main menu sahne adýný yaz
    }

    public void ExitGame()
    {
        Time.timeScale = 1f; // zamaný normale döndür
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
