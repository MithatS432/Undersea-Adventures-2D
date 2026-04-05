using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class TileGoal
{
    public int tileType;
    public int requiredCount;
    public int currentCount;
    public Image tileImage;
    public TextMeshProUGUI countText;
}

[System.Serializable]
public class LevelData
{
    public int moveCount;
    public List<TileGoal> goals;
}

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance;
    public MermaidEnter mermaidEnter;

    [Header("Level Settings")]
    public List<LevelData> levels;
    public int currentLevel = 0;

    [Header("Move Settings")]
    public int currentMoves;
    public TextMeshProUGUI moveCountText;

    [Header("Life Settings")]
    public int maxLives = 5;
    public int currentLives;
    public TextMeshProUGUI livesText;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip warningSound;
    public AudioClip winSound;
    public AudioClip loseSound;

    [Header("Effect Settings")]
    public GameObject winEffect;
    public GameObject loseEffect;

    private bool isGameOver = false;
    private bool isMissionComplete = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
        currentLives = PlayerPrefs.GetInt("CurrentLives", maxLives);
    }

    void Start()
    {
        LoadLevel(currentLevel);
    }

    void LoadLevel(int levelIndex)
    {
        if (levelIndex >= levels.Count) return;

        isGameOver = false;
        isMissionComplete = false;

        LevelData level = levels[levelIndex];
        currentMoves = level.moveCount;

        // Goal'ları sıfırla
        foreach (var goal in level.goals)
        {
            goal.currentCount = 0;
            UpdateGoalUI(goal);
        }

        UpdateMoveUI();
        UpdateLivesUI();

    }

    public void OnTileDestroyed(int tileType)
    {
        if (isGameOver || isMissionComplete) return;

        LevelData level = levels[currentLevel];
        foreach (var goal in level.goals)
        {
            if (goal.tileType == tileType && goal.currentCount < goal.requiredCount)
            {
                goal.currentCount++;
                UpdateGoalUI(goal);
            }
        }

        CheckMissionComplete();
    }

    void UpdateGoalUI(TileGoal goal)
    {
        if (goal.countText != null)
            goal.countText.text = $"{goal.currentCount}/{goal.requiredCount}";

        if (goal.tileImage != null && PuzzleManager.Instance != null)
        {
            if (goal.tileType >= 0 && goal.tileType < PuzzleManager.Instance.tilePrefabs.Length)
            {
                SpriteRenderer sr = PuzzleManager.Instance.tilePrefabs[goal.tileType].GetComponent<SpriteRenderer>();
                if (sr != null)
                    goal.tileImage.sprite = sr.sprite;
            }
        }

        if (goal.countText != null)
            goal.countText.color = goal.currentCount >= goal.requiredCount ? Color.green : Color.white;
    }

    void CheckMissionComplete()
    {
        LevelData level = levels[currentLevel];
        foreach (var goal in level.goals)
        {
            if (goal.currentCount < goal.requiredCount)
                return;
        }

        isMissionComplete = true;
        StartCoroutine(WinRoutine());
    }

    public void UseMove()
    {
        if (isGameOver || isMissionComplete) return;

        currentMoves--;
        UpdateMoveUI();

        if (currentMoves == 5)
        {
            if (warningSound != null && audioSource != null)
                audioSource.PlayOneShot(warningSound);
            mermaidEnter.LowMovesTrigger();
        }

        if (currentMoves <= 0)
        {
            currentMoves = 0;
            UpdateMoveUI();

            if (!isMissionComplete)
                StartCoroutine(LoseRoutine());
        }
    }

    void UpdateMoveUI()
    {
        if (moveCountText != null)
            moveCountText.text = currentMoves.ToString();

        if (moveCountText != null)
            moveCountText.color = currentMoves <= 5 ? Color.red : Color.white;
    }

    void UpdateLivesUI()
    {
        if (livesText != null)
            livesText.text = currentLives.ToString();
    }

    IEnumerator WinRoutine()
    {
        PuzzleManager.Instance.IsProcessing = true;

        yield return new WaitForSeconds(0.5f);

        mermaidEnter.WinTrigger();

        if (winSound != null && audioSource != null)
            audioSource.PlayOneShot(winSound);

        if (winEffect != null)
        {
            Vector3 center = Camera.main.transform.position;
            center.z = 0f;
            GameObject effect = Instantiate(winEffect, center, Quaternion.identity);
            Destroy(effect, 3f);
        }

        yield return new WaitForSeconds(3f);

        currentLevel++;
        if (currentLevel >= levels.Count)
            currentLevel = 0;

        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("CurrentLives", currentLives);
        PlayerPrefs.Save();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    IEnumerator LoseRoutine()
    {
        isGameOver = true;
        PuzzleManager.Instance.IsProcessing = true;

        yield return new WaitForSeconds(0.5f);

        currentLives--;
        if (currentLives <= 0)
            currentLives = maxLives;

        UpdateLivesUI();

        mermaidEnter.LoseTrigger();

        if (loseSound != null && audioSource != null)
            audioSource.PlayOneShot(loseSound);

        if (loseEffect != null)
        {
            Vector3 center = Camera.main.transform.position;
            center.z = 0f;
            GameObject effect = Instantiate(loseEffect, center, Quaternion.identity);
            Destroy(effect, 3f);
        }

        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("CurrentLives", currentLives);
        PlayerPrefs.Save();

        yield return new WaitForSeconds(3f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}