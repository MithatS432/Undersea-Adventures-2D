using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance;

    public int currentGold = 0;
    public TextMeshProUGUI goldText;
    public AudioClip goldSound;
    public AudioSource audioSource;

    private int displayedGold = 0;

    public GameObject goldCoinPrefab;
    public RectTransform goldTextRect;
    public Canvas mainCanvas;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        displayedGold = currentGold;
        UpdateGoldText();
    }

    public void AddGold(int amount)
    {
        currentGold += amount;
        StartCoroutine(AnimateGold(displayedGold, currentGold));

        if (goldSound != null && audioSource != null)
            audioSource.PlayOneShot(goldSound);
    }

    IEnumerator AnimateGold(int from, int to)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smooth = t * t * (3f - 2f * t);
            displayedGold = Mathf.RoundToInt(Mathf.Lerp(from, to, smooth));
            UpdateGoldText();
            yield return null;
        }

        displayedGold = to;
        UpdateGoldText();
    }

    void UpdateGoldText()
    {
        if (goldText != null)
            goldText.text = displayedGold.ToString();
    }

    public bool HasEnoughGold(int amount)
    {
        return currentGold >= amount;
    }

    public bool SpendGold(int amount)
    {
        if (!HasEnoughGold(amount)) return false;
        currentGold -= amount;
        StartCoroutine(AnimateGold(displayedGold, currentGold));
        return true;
    }

    public void SpawnGoldFromPosition(Vector3 worldPos, int amount)
    {
        AddGold(amount);
        StartCoroutine(GoldFlyAnimation(worldPos, amount));
    }

    IEnumerator GoldFlyAnimation(Vector3 worldPos, int amount)
    {
        if (goldCoinPrefab == null || goldTextRect == null) yield break;

        int coinCount = Mathf.Clamp(amount / 10, 1, 3);

        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        Canvas canvas = mainCanvas != null ? mainCanvas : goldTextRect.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            uiCamera,
            out Vector2 startLocalPos
        );

        Vector2 targetLocalPos = GetCanvasLocalPosition(goldTextRect, canvasRect);

        for (int i = 0; i < coinCount; i++)
            StartCoroutine(FlyOneCoin(startLocalPos, targetLocalPos, canvasRect, i * 0.08f));

        yield return new WaitForSeconds(0.7f + coinCount * 0.08f);
    }

    Vector2 GetCanvasLocalPosition(RectTransform target, RectTransform canvasRect)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) / 2f;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, center);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            null,
            out Vector2 localPoint
        );

        return localPoint;
    }
    IEnumerator FlyOneCoin(Vector2 from, Vector2 to, RectTransform canvasRect, float delay)
    {
        yield return new WaitForSeconds(delay);

        GameObject coin = Instantiate(goldCoinPrefab, canvasRect);

        RectTransform coinRect = coin.GetComponent<RectTransform>();
        if (coinRect == null) coinRect = coin.AddComponent<RectTransform>();

        coinRect.sizeDelta = new Vector2(100f, 100f);
        coinRect.anchoredPosition = from + Random.insideUnitCircle * 20f;
        coinRect.localScale = Vector3.one * 1.5f;

        Image img = coin.GetComponent<Image>();
        if (img != null) img.color = new Color(1f, 0.85f, 0f, 1f);

        float duration = 0.7f;
        float elapsed = 0f;
        Vector2 startPos = coinRect.anchoredPosition;

        while (elapsed < duration)
        {
            if (coin == null) yield break;

            float t = elapsed / duration;
            float smooth = t * t * (3f - 2f * t);

            Vector2 mid = Vector2.Lerp(startPos, to, 0.5f) + Vector2.up * 100f;
            Vector2 pos = Vector2.Lerp(
                Vector2.Lerp(startPos, mid, smooth),
                Vector2.Lerp(mid, to, smooth),
                smooth
            );

            coinRect.anchoredPosition = pos;
            coinRect.localScale = Vector3.one * Mathf.Lerp(1.5f, 0.1f, smooth);

            if (img != null)
                img.color = new Color(1f, 0.85f, 0f, Mathf.Lerp(1f, 0f, smooth * smooth));

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(coin);
    }
}