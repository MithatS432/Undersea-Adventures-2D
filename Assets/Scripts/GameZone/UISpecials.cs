using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UISpecials : MonoBehaviour
{
    public static UISpecials Instance;

    [Header("Hammer")]
    public Button hammerButton;
    public TextMeshProUGUI hammerCountText;
    private int hammerCount = 3;
    private bool hammerActive = false;

    [Header("Lightning")]
    public Button lightningButton;
    public TextMeshProUGUI lightningCountText;
    private int lightningCount = 3;

    [Header("Magic Star")]
    public Button starButton;
    public TextMeshProUGUI starCountText;
    private int starCount = 3;

    public Button hammerShopButton;
    public Button lightningShopButton;
    public Button magicStarShopButton;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        UpdateUI();

        hammerButton.onClick.AddListener(OnHammerClick);
        lightningButton.onClick.AddListener(OnLightningClick);
        starButton.onClick.AddListener(OnStarClick);

        hammerShopButton.onClick.AddListener(() => ShopPanel.Instance.OpenShop("hammer"));
        lightningShopButton.onClick.AddListener(() => ShopPanel.Instance.OpenShop("lightning"));
        magicStarShopButton.onClick.AddListener(() => ShopPanel.Instance.OpenShop("magicstar"));
    }

    void UpdateUI()
    {
        hammerCountText.text = hammerCount.ToString();
        lightningCountText.text = lightningCount.ToString();
        starCountText.text = starCount.ToString();

        hammerButton.interactable = hammerCount > 0;
        lightningButton.interactable = lightningCount > 0;
        starButton.interactable = starCount > 0;
    }

    // ── Çekiç ──────────────────────────────────────────────
    void OnHammerClick()
    {
        if (hammerCount <= 0) return;
        if (PuzzleManager.Instance.IsProcessing) return;

        hammerActive = true;
        PuzzleManager.Instance.SetHammerMode(true, OnHammerTileSelected);
    }

    void OnHammerTileSelected()
    {
        hammerActive = false;
        hammerCount--;
        UpdateUI();
    }

    // ── Yıldırım ──────────────────────────────────────────────
    void OnLightningClick()
    {
        if (lightningCount <= 0) return;
        if (PuzzleManager.Instance.IsProcessing) return;

        lightningCount--;
        UpdateUI();
        StartCoroutine(PuzzleManager.Instance.ActivateLightning());
    }

    // ── Sihirli Yıldız ──────────────────────────────────────────────
    void OnStarClick()
    {
        if (starCount <= 0) return;
        if (PuzzleManager.Instance.IsProcessing) return;

        starCount--;
        UpdateUI();
        StartCoroutine(PuzzleManager.Instance.ActivateMagicStar());
    }

    public void AddSpecial(string itemType)
    {
        switch (itemType)
        {
            case "hammer":
                hammerCount++;
                break;
            case "lightning":
                lightningCount++;
                break;
            case "magicstar":
                starCount++;
                break;
        }
        UpdateUI();
    }
}