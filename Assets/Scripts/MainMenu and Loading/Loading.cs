using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class Loading : MonoBehaviour
{
    public Image loadingBar;
    public TextMeshProUGUI loadingText;

    [Header("Loading Settings")]
    public float duration = 5f;
    public string nextSceneName;

    [Header("Fade Settings")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    private float timer = 0f;
    private bool loaded = false;

    void Start()
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    void Update()
    {
        if (loaded) return;

        if (timer < duration)
        {
            timer += Time.deltaTime;
        }

        float progress = Mathf.Clamp01(timer / duration);
        loadingBar.fillAmount = progress;

        int dotCount = Mathf.FloorToInt(Time.time * 2f) % 4;
        loadingText.text = "Loading" + new string('.', dotCount);

        if (progress >= 1f)
        {
            loaded = true;
            StartCoroutine(FadeAndLoadScene());
        }
    }

    IEnumerator FadeAndLoadScene()
    {
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / fadeDuration);

            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = alpha;
                fadeImage.color = c;
            }

            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}