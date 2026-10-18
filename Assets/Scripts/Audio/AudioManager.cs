using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global audio playback singleton. Assigns named clip categories at edit time;
/// mute preference persists in PlayerPrefs.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // Named clips wired in the prefab / builder.
    [Header("SFX")]
    public AudioClip click;
    public AudioClip cut;
    public AudioClip fall;
    public AudioClip win;
    public AudioClip lose;
    public AudioClip pop;

    [Header("Music")]
    public AudioClip music;

    [Header("Settings")]
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    private const string MutePref = "Audio_Muted";

    private AudioSource sfxSource;
    private AudioSource musicSource;
    private bool muted;

    private readonly Dictionary<string, AudioClip> namedClips = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        Rebuild();
        muted = PlayerPrefs.GetInt(MutePref, 0) == 1;
        AudioListener.pause = muted;
        PlayMusic();
    }

    /// <summary>Fills the named lookup used by Play().</summary>
    public void Rebuild()
    {
        namedClips.Clear();
        if (click) namedClips["Click"] = click;
        if (cut) namedClips["Cut"] = cut;
        if (fall) namedClips["Fall"] = fall;
        if (win) namedClips["Win"] = win;
        if (lose) namedClips["Lose"] = lose;
        if (pop) namedClips["Pop"] = pop;
    }

    public void Play(string name, float volumeScale = 1f)
    {
        if (muted) return;
        if (!namedClips.TryGetValue(name, out var clip) || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    public void PlayMusic()
    {
        if (music == null || muted) return;
        if (musicSource.clip != music) musicSource.clip = music;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();

    public bool Muted => muted;

    public void ToggleMute()
    {
        muted = !muted;
        AudioListener.pause = muted;
        if (muted) musicSource.Stop();
        else PlayMusic();
        PlayerPrefs.SetInt(MutePref, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetMuted(bool value)
    {
        if (muted == value) return;
        ToggleMute();
    }
}
