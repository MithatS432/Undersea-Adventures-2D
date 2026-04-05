using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("States")]
    public bool isMusicOn = true;
    public bool isSFXOn = true;

    [Header("UI")]
    public Image musicButtonImage;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;

    public Image sfxButtonImage;
    public Sprite sfxOnSprite;
    public Sprite sfxOffSprite;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource[] sfxSources;

    [Header("Vibration")]
    public bool isVibrationOn = true;

    public Image vibrationButtonImage;
    public Sprite vibrationOnSprite;
    public Sprite vibrationOffSprite;

    void Start()
    {
        UpdateMusicUI();
        UpdateSFXUI();
        UpdateVibrationUI();

        if (musicSource != null)
            musicSource.mute = !isMusicOn;

        if (sfxSources != null)
        {
            foreach (var sfx in sfxSources)
            {
                if (sfx != null)
                    sfx.mute = !isSFXOn;
            }
        }
    }

    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn;

        if (musicSource != null)
            musicSource.mute = !isMusicOn;

        UpdateMusicUI();
    }

    public void ToggleSFX()
    {
        isSFXOn = !isSFXOn;

        if (sfxSources != null)
        {
            foreach (var sfx in sfxSources)
            {
                if (sfx != null)
                    sfx.mute = !isSFXOn;
            }
        }

        UpdateSFXUI();
    }
    public void ToggleVibration()
    {
        isVibrationOn = !isVibrationOn;
        UpdateVibrationUI();
    }
    public void Vibrate()
    {
        if (!isVibrationOn) return;

#if UNITY_ANDROID || UNITY_IOS
    Handheld.Vibrate();
#endif
    }

    void UpdateMusicUI()
    {
        if (musicButtonImage != null)
            musicButtonImage.sprite = isMusicOn ? musicOnSprite : musicOffSprite;
    }

    void UpdateSFXUI()
    {
        if (sfxButtonImage != null)
            sfxButtonImage.sprite = isSFXOn ? sfxOnSprite : sfxOffSprite;
    }
    void UpdateVibrationUI()
    {
        if (vibrationButtonImage != null)
            vibrationButtonImage.sprite = isVibrationOn ? vibrationOnSprite : vibrationOffSprite;
    }
}