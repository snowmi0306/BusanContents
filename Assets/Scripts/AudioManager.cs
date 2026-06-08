using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class AudioManager : MonoBehaviour
{
    private const string SoundsResourcePath = "Sounds";
    private const string ButtonClickSfx = "sfx_ui_click";

    private static readonly Dictionary<string, string> SceneBgms = new Dictionary<string, string>
    {
        { "TitleScene", "bgm_title_loop" },
        { "StoryCutScene", "bgm_story_cutscene" },
        { "Stage1", "bgm_stage1_normal_loop" },
        { "Stage2", "bgm_stage2_normal_loop" },
        { "Stage3", "bgm_stage3_normal_loop" },
        { "ClearScene", "bgm_clear_loop" }
    };

    private static AudioManager instance;

    [Header("Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.7f;
    [SerializeField] private float defaultBgmFadeDuration = 0.5f;

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly HashSet<string> missingClipWarnings = new HashSet<string>();
    private readonly List<Button> hookedButtons = new List<Button>();

    private AudioClip currentBgm;
    private Coroutine bgmFadeRoutine;
    private bool initialized;

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

    public static void PlayBgmWithFade(string clipName, float fadeDuration = -1f, bool loop = true)
    {
        Instance.PlayBgmWithFadeInternal(clipName, fadeDuration, loop);
    }

    public static void PlayCurrentSceneBgm(bool muralWorld = false)
    {
        Instance.PlaySceneBgm(SceneManager.GetActiveScene().name, muralWorld);
    }

    public static void StopBgm()
    {
        if (instance == null)
            return;

        instance.StopBgmInternal();
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

    private void OnDestroy()
    {
        if (instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSources();
        LoadClips();

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        PlaySceneBgm(SceneManager.GetActiveScene().name, false);
        HookSceneButtons();
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
        if (clips.Count > 0)
            return;

        AudioClip[] loadedClips = Resources.LoadAll<AudioClip>(SoundsResourcePath);
        foreach (AudioClip clip in loadedClips)
        {
            if (clip == null || string.IsNullOrWhiteSpace(clip.name))
                continue;

            string key = NormalizeClipName(clip.name);
            if (!clips.ContainsKey(key))
                clips.Add(key, clip);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneBgm(scene.name, false);
        HookSceneButtons();
        StartCoroutine(HookSceneButtonsAfterFrame());
    }

    private IEnumerator HookSceneButtonsAfterFrame()
    {
        yield return null;
        HookSceneButtons();
    }

    private void HookSceneButtons()
    {
        hookedButtons.RemoveAll(button => button == null);

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            button.onClick.RemoveListener(PlayButtonClickSfx);

            if (hookedButtons.Contains(button))
                continue;

            EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
            if (eventTrigger == null)
                eventTrigger = button.gameObject.AddComponent<EventTrigger>();

            if (eventTrigger.triggers == null)
                eventTrigger.triggers = new List<EventTrigger.Entry>();

            EventTrigger.Entry pressEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            pressEntry.callback.AddListener(_ => PlayButtonPressSfx(button));
            eventTrigger.triggers.Add(pressEntry);

            hookedButtons.Add(button);
        }
    }

    private void PlayButtonClickSfx()
    {
        PlaySfxInternal(ButtonClickSfx, 1f);
    }

    private void PlayButtonPressSfx(Button button)
    {
        if (button != null && !button.IsInteractable())
            return;

        PlaySfxInternal(ButtonClickSfx, 1f);
    }

    private void PlaySceneBgm(string sceneName, bool muralWorld)
    {
        string bgmName = GetSceneBgmName(sceneName, muralWorld);
        if (string.IsNullOrEmpty(bgmName))
            return;

        PlayBgmWithFadeInternal(bgmName, defaultBgmFadeDuration, true);
    }

    private static string GetSceneBgmName(string sceneName, bool muralWorld)
    {
        switch (sceneName)
        {
            case "Stage1":
                return muralWorld ? "bgm_stage1_mural_loop" : "bgm_stage1_normal_loop";
            case "Stage2":
                return muralWorld ? "bgm_stage2_mural_loop" : "bgm_stage2_normal_loop";
            case "Stage3":
                return muralWorld ? "bgm_stage3_mural_loop" : "bgm_stage3_normal_loop";
            default:
                return SceneBgms.TryGetValue(sceneName, out string bgmName) ? bgmName : string.Empty;
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

        if (currentBgm == clip && bgmSource.clip == clip && bgmSource.isPlaying)
        {
            bgmSource.loop = loop;
            return;
        }

        StopBgmFadeRoutine();
        currentBgm = clip;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    private void PlayBgmWithFadeInternal(string clipName, float fadeDuration, bool loop)
    {
        AudioClip clip = GetClip(clipName);
        if (clip == null || bgmSource == null)
            return;

        if (currentBgm == clip && bgmSource.clip == clip && bgmSource.isPlaying)
        {
            bgmSource.loop = loop;
            return;
        }

        float duration = fadeDuration < 0f ? defaultBgmFadeDuration : fadeDuration;
        if (duration <= 0f)
        {
            PlayBgmInternal(clipName, loop);
            return;
        }

        StopBgmFadeRoutine();
        bgmFadeRoutine = StartCoroutine(FadeToBgmRoutine(clip, duration, loop));
    }

    private IEnumerator FadeToBgmRoutine(AudioClip clip, float fadeDuration, bool loop)
    {
        float halfDuration = Mathf.Max(0.01f, fadeDuration * 0.5f);

        if (bgmSource.isPlaying && bgmSource.clip != null)
            yield return FadeBgmVolumeRoutine(bgmSource.volume, 0f, halfDuration);

        currentBgm = clip;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = 0f;
        bgmSource.Play();

        yield return FadeBgmVolumeRoutine(0f, bgmVolume, halfDuration);
        bgmSource.volume = bgmVolume;
        bgmFadeRoutine = null;
    }

    private IEnumerator FadeBgmVolumeRoutine(float from, float to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            bgmSource.volume = Mathf.Lerp(from, to, t);
            yield return null;
        }

        bgmSource.volume = to;
    }

    private void StopBgmInternal()
    {
        StopBgmFadeRoutine();

        if (bgmSource == null)
            return;

        bgmSource.Stop();
        bgmSource.clip = null;
        currentBgm = null;
    }

    private void StopBgmFadeRoutine()
    {
        if (bgmFadeRoutine == null)
            return;

        StopCoroutine(bgmFadeRoutine);
        bgmFadeRoutine = null;
    }

    private AudioClip GetClip(string clipName)
    {
        string key = NormalizeClipName(clipName);
        if (string.IsNullOrEmpty(key))
            return null;

        if (clips.TryGetValue(key, out AudioClip clip))
            return clip;

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
