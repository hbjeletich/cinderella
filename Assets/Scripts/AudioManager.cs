using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    [Header("Background Music")]
    public AudioClip backgroundMusic;
    public float backgroundMusicVolume = 0.5f;
    public Slider backgroundMusicVolumeSlider;
    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = gameObject.AddComponent<AudioSource>();

        if (backgroundMusicVolumeSlider != null)
        {
            backgroundMusicVolumeSlider.value = backgroundMusicVolume;
            backgroundMusicVolumeSlider.onValueChanged.AddListener(AdjustBackgroundMusicVolume);
        }
    }

    private void Start()
    {
        PlayBackgroundMusic();
    }

    private void PlayBackgroundMusic()
    {
        if (backgroundMusic != null)
        {
            audioSource.clip = backgroundMusic;
            audioSource.loop = true;
            audioSource.volume = backgroundMusicVolume;
            audioSource.Play();
        }
    }

    private void AdjustBackgroundMusicVolume(float volume)
    {
        backgroundMusicVolume = volume;
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
}
