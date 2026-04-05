using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundAndEffectManager : MonoBehaviour
{
    public static SoundAndEffectManager Instance;

    public AudioSource audioSource;

    public AudioClip matchSound;
    public AudioClip horizontalPowerSound;
    public AudioClip verticalPowerSound;
    public AudioClip smallBombSound;
    public AudioClip largeBombSound;
    public AudioClip colorBombSound;
    public AudioClip comboSound;
    public int comboThreshold = 3;

    public AudioClip hammerSound;
    public AudioClip lightningSound;
    public AudioClip magicStarSound;


    [Range(1f, 3f)] public float pitchMultiplier = 1.05f;

    public GameObject[] matchEffectPrefabs;
    public GameObject horizontalEffectPrefab;
    public GameObject verticalEffectPrefab;
    public GameObject smallBombEffectPrefab;
    public GameObject largeBombEffectPrefab;
    public GameObject colorBombEffectPrefab;


    public float delayBetweenEffects = 0.15f;

    private Queue<Vector3> effectQueue = new Queue<Vector3>();
    private bool isPlayingSequence = false;
    private int comboCount = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    public void AddToMatchQueue(Vector3 position)
    {
        effectQueue.Enqueue(position);

        if (!isPlayingSequence)
        {
            StartCoroutine(PlayEffectSequence());
        }
    }

    IEnumerator PlayEffectSequence()
    {
        isPlayingSequence = true;
        comboCount = 0;

        comboCount++;

        if (comboCount >= comboThreshold && comboSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(comboSound);
        }

        while (effectQueue.Count > 0)
        {
            Vector3 targetPos = effectQueue.Dequeue();

            if (matchEffectPrefabs != null && matchEffectPrefabs.Length > 0)
            {
                int effectIndex = Mathf.Min(comboCount, matchEffectPrefabs.Length - 1);

                if (matchEffectPrefabs[effectIndex] != null)
                {
                    GameObject effect = Instantiate(matchEffectPrefabs[effectIndex], targetPos, Quaternion.identity);
                    Destroy(effect, 1f);
                }
            }

            if (matchSound != null)
            {
                audioSource.pitch = 1f + (comboCount * (pitchMultiplier - 1f));
                audioSource.PlayOneShot(matchSound);
            }

            comboCount++;

            yield return new WaitForSeconds(delayBetweenEffects);
        }

        audioSource.pitch = 1f;
        isPlayingSequence = false;
    }

    public void PlayHorizontalPower(Vector3 position)
    {
        if (horizontalPowerSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(horizontalPowerSound);
        }

        if (horizontalEffectPrefab != null)
        {
            GameObject effect = Instantiate(horizontalEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    public void PlayVerticalPower(Vector3 position)
    {
        if (verticalPowerSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(verticalPowerSound);
        }

        if (verticalEffectPrefab != null)
        {
            GameObject effect = Instantiate(verticalEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }
    public void PlaySmallBomb(Vector3 position)
    {
        if (smallBombSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(smallBombSound);
        }

        if (smallBombEffectPrefab != null)
        {
            GameObject effect = Instantiate(smallBombEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }
    public void PlayLargeBomb(Vector3 position)
    {
        if (largeBombSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(largeBombSound);
        }

        if (largeBombEffectPrefab != null)
        {
            GameObject effect = Instantiate(largeBombEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    public void PlayColorBomb(Vector3 position)
    {
        if (colorBombSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(colorBombSound);
        }

        if (colorBombEffectPrefab != null)
        {
            GameObject effect = Instantiate(colorBombEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f);
        }
    }

    public void PlayHammer(Vector3 position)
    {
        if (hammerSound != null) audioSource.PlayOneShot(hammerSound);
        AddToMatchQueue(position);
    }

    public void PlayLightning(Vector3 position)
    {
        if (lightningSound != null) audioSource.PlayOneShot(lightningSound);
        AddToMatchQueue(position);
    }

    public void PlayMagicStar()
    {
        if (magicStarSound != null) audioSource.PlayOneShot(magicStarSound);
    }
}