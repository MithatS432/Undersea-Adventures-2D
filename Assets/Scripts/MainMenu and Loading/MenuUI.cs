using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MenuUI : MonoBehaviour
{
    public TextMeshProUGUI gameNameText;
    public Button startButton;
    public Button exitButton;
    public SceneFader fader;
    public ScreenShake screenShake;

    private bool isTransitioning = false;

    [Header("Button Animation Settings")]
    public float buttonHoverScale = 1.1f;
    public float buttonClickScale = 0.9f;
    public float buttonAnimationDuration = 0.15f;

    private Vector3 startButtonOriginalScale;
    private Vector3 exitButtonOriginalScale;

    [Header("Button Sound")]
    public AudioSource audioSource;
    public AudioClip buttonClickSound;

    void Awake()
    {
        if (startButton != null)
            startButtonOriginalScale = startButton.transform.localScale;
        if (exitButton != null)
            exitButtonOriginalScale = exitButton.transform.localScale;

        startButton.onClick.AddListener(OnStartButton);
        exitButton.onClick.AddListener(OnExitButton);

        AddButtonAnimations();
    }

    void Start()
    {
        if (gameNameText != null)
        {
            GameNameAnimator animator = gameNameText.gameObject.AddComponent<GameNameAnimator>();
            animator.screenShake = screenShake;
        }
    }

    void AddButtonAnimations()
    {
        AddButtonEvents(startButton, startButtonOriginalScale);

        AddButtonEvents(exitButton, exitButtonOriginalScale);
    }

    void AddButtonEvents(Button button, Vector3 originalScale)
    {
        if (button == null) return;

        UnityEngine.EventSystems.EventTrigger trigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        // Pointer Enter (üzerine gelince)
        UnityEngine.EventSystems.EventTrigger.Entry entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        entryEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { OnButtonHover(button, true); });
        trigger.triggers.Add(entryEnter);

        // Pointer Exit (üzerinden ayrılınca)
        UnityEngine.EventSystems.EventTrigger.Entry entryExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        entryExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { OnButtonHover(button, false); });
        trigger.triggers.Add(entryExit);

        // Pointer Down (basılı tutunca)
        UnityEngine.EventSystems.EventTrigger.Entry entryDown = new UnityEngine.EventSystems.EventTrigger.Entry();
        entryDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => { OnButtonPress(button, true); });
        trigger.triggers.Add(entryDown);

        // Pointer Up (bırakınca)
        UnityEngine.EventSystems.EventTrigger.Entry entryUp = new UnityEngine.EventSystems.EventTrigger.Entry();
        entryUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => { OnButtonPress(button, false); });
        trigger.triggers.Add(entryUp);
    }

    void OnButtonHover(Button button, bool isHovering)
    {
        if (!button.interactable || isTransitioning) return;

        StopAllCoroutines();
        Vector3 targetScale = isHovering ?
            button.transform.localScale * buttonHoverScale :
            (button == startButton ? startButtonOriginalScale : exitButtonOriginalScale);

        StartCoroutine(AnimateButtonScale(button, targetScale));
    }

    void OnButtonPress(Button button, bool isPressed)
    {
        if (!button.interactable || isTransitioning) return;

        StopAllCoroutines();
        Vector3 targetScale;

        if (isPressed)
        {
            targetScale = (button == startButton ? startButtonOriginalScale : exitButtonOriginalScale) * buttonClickScale;
        }
        else
        {
            targetScale = button == startButton ? startButtonOriginalScale : exitButtonOriginalScale;
        }

        StartCoroutine(AnimateButtonScale(button, targetScale));
    }

    IEnumerator AnimateButtonScale(Button button, Vector3 targetScale)
    {
        Vector3 startScale = button.transform.localScale;
        float elapsedTime = 0;

        while (elapsedTime < buttonAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / buttonAnimationDuration;

            t = Mathf.Sin(t * Mathf.PI * 0.5f); // Smooth out

            button.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        button.transform.localScale = targetScale;
    }

    void OnStartButton()
    {
        if (isTransitioning) return;
        PlayButtonClickSound();
        StartCoroutine(StartGameRoutine());
    }

    IEnumerator StartGameRoutine()
    {
        isTransitioning = true;
        startButton.interactable = false;
        exitButton.interactable = false;

        yield return StartCoroutine(AnimateButtonScale(startButton, startButtonOriginalScale * buttonClickScale));
        yield return new WaitForSeconds(0.05f);
        yield return StartCoroutine(AnimateButtonScale(startButton, startButtonOriginalScale));

        if (fader != null)
            yield return fader.FadeOut();

        SceneManager.LoadScene("Loading Screen");
    }

    void OnExitButton()
    {
        if (isTransitioning) return;
        PlayButtonClickSound();
        StartCoroutine(ExitGameRoutine());
    }

    IEnumerator ExitGameRoutine()
    {
        isTransitioning = true;
        startButton.interactable = false;
        exitButton.interactable = false;

        yield return StartCoroutine(AnimateButtonScale(exitButton, exitButtonOriginalScale * buttonClickScale));
        yield return new WaitForSeconds(0.05f);
        yield return StartCoroutine(AnimateButtonScale(exitButton, exitButtonOriginalScale));

        if (fader != null)
            yield return fader.FadeOut();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    void PlayButtonClickSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
}