using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AudioManager : MonoBehaviour
{
    private const string SoundsResourcePath = "Sounds";

    private static readonly Dictionary<string, string> ClipAliases = new Dictionary<string, string>
    {
        { "sfx_ui_confirm", "sfx_ui_click" },
        { "sfx_item_letter", "sfx_item_fishcake" },
        { "sfx_mural_enter", "sfx_mural_exit" }
    };

    private static AudioManager instance;

    [Header("Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.7f;

    private readonly Dictionary<string, AudioClip> sfxClips = new Dictionary<string, AudioClip>();
    private readonly HashSet<string> missingClipWarnings = new HashSet<string>();

    public static AudioManager Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void PlaySfx(string clipName, float volumeScale = 1f)
    {
        Instance.PlaySfxInternal(clipName, volumeScale);
    }

    public static void PlayBgm(string clipName, bool loop = true)
    {
        Instance.PlayBgmInternal(clipName, loop);
    }

    public static void StopBgm()
    {
        if (instance == null || instance.bgmSource == null)
            return;

        instance.bgmSource.Stop();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        AudioManager existing = FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            instance.Initialize();
            return;
        }

        GameObject audioManagerObject = new GameObject("AudioManager");
        instance = audioManagerObject.AddComponent<AudioManager>();
        instance.Initialize();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Initialize();
    }

    private void Initialize()
    {
        DontDestroyOnLoad(gameObject);
        EnsureAudioSources();
        LoadClips();
    }

    private void EnsureAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }

        sfxSource.volume = sfxVolume;
        bgmSource.volume = bgmVolume;
    }

    private void LoadClips()
    {
        if (sfxClips.Count > 0)
            return;

        AudioClip[] clips = Resources.LoadAll<AudioClip>(SoundsResourcePath);
        foreach (AudioClip clip in clips)
        {
            if (clip == null || string.IsNullOrWhiteSpace(clip.name))
                continue;

            string key = NormalizeClipName(clip.name);
            if (!sfxClips.ContainsKey(key))
                sfxClips.Add(key, clip);
        }
    }

    private void PlaySfxInternal(string clipName, float volumeScale)
    {
        AudioClip clip = GetClip(clipName);
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale) * sfxVolume);
    }

    private void PlayBgmInternal(string clipName, bool loop)
    {
        AudioClip clip = GetClip(clipName);
        if (clip == null || bgmSource == null)
            return;

        if (bgmSource.clip == clip && bgmSource.isPlaying)
            return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    private AudioClip GetClip(string clipName)
    {
        string key = NormalizeClipName(clipName);
        if (string.IsNullOrEmpty(key))
            return null;

        if (sfxClips.TryGetValue(key, out AudioClip clip))
            return clip;

        if (ClipAliases.TryGetValue(key, out string aliasName))
        {
            string aliasKey = NormalizeClipName(aliasName);
            if (sfxClips.TryGetValue(aliasKey, out clip))
                return clip;
        }

        WarnMissingClipOnce(key);
        return null;
    }

    private void WarnMissingClipOnce(string clipName)
    {
        if (!missingClipWarnings.Add(clipName))
            return;

        Debug.LogWarning($"AudioManager could not find sound clip '{clipName}' in Resources/{SoundsResourcePath}.", this);
    }

    private static string NormalizeClipName(string clipName)
    {
        return string.IsNullOrWhiteSpace(clipName)
            ? string.Empty
            : clipName.Trim().ToLowerInvariant();
    }
}