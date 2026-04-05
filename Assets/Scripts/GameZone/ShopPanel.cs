using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPanel : MonoBehaviour
{
    public static ShopPanel Instance;

    public GameObject panel;
    public Button yesButton;
    public Button noButton;

    private int pendingCost;
    private string pendingItem;

    private const int HAMMER_COST = 2000;
    private const int LIGHTNING_COST = 4000;
    private const int MAGIC_STAR_COST = 8000;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        panel.SetActive(false);
        yesButton.onClick.AddListener(OnYes);
        noButton.onClick.AddListener(OnNo);
    }

    public void OpenShop(string itemType)
    {
        pendingItem = itemType;

        switch (itemType)
        {
            case "hammer": pendingCost = HAMMER_COST; break;
            case "lightning": pendingCost = LIGHTNING_COST; break;
            case "magicstar": pendingCost = MAGIC_STAR_COST; break;
        }

        yesButton.interactable = GoldManager.Instance.HasEnoughGold(pendingCost);

        panel.SetActive(true);
    }

    void OnYes()
    {
        if (!GoldManager.Instance.SpendGold(pendingCost)) return;
        UISpecials.Instance.AddSpecial(pendingItem);
        panel.SetActive(false);
    }

    void OnNo()
    {
        panel.SetActive(false);
    }
}