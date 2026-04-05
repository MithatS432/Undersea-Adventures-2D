using UnityEngine;
using UnityEngine.UI;

public class MenuUIManager : MonoBehaviour
{
    public Button pauseButton;
    public Button resumeButton;
    public Button exitButton;
    public GameObject pausePanel;

    public AudioSource audioSource;
    public AudioClip UIClickSound;

    void Start()
    {
        pauseButton.onClick.AddListener(PauseGame);
        resumeButton.onClick.AddListener(PauseGame);
        exitButton.onClick.AddListener(ExitGame);

        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }
    void OnDisable()
    {
        Time.timeScale = 1f;
    }
    void PauseGame()
    {
        PlayClickSound();
        bool isActive = !pausePanel.activeSelf;

        pausePanel.SetActive(isActive);
        Time.timeScale = isActive ? 0f : 1f;
    }

    void ExitGame()
    {
        PlayClickSound();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    void PlayClickSound()
    {
        audioSource.PlayOneShot(UIClickSound);
    }
}