using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-90)]
public class GameCursorManager : MonoBehaviour
{
    private const string CursorResourcePath = "Cursor";
    private const string SampleSceneName = "SampleScene";
    private const string TitleSceneName = "TitleScene";
    private const string StoryCutSceneName = "StoryCutScene";
    private const string ClearSceneName = "ClearScene";
    private const string Stage1SceneName = "Stage1";
    private const string Stage2SceneName = "Stage2";
    private const string Stage3SceneName = "Stage3";

    private static GameCursorManager instance;
    private static bool stagePortalCursorActive;

    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    [SerializeField, Min(1f)] private float cursorScale = 2f;
    [SerializeField] private CursorMode cursorMode = CursorMode.ForceSoftware;

    private Texture2D cursorTexture;
    private Texture2D scaledCursorTexture;
    private float scaledCursorTextureScale;
    private bool warnedMissingCursor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void SetStagePortalCursorActive(bool active)
    {
        stagePortalCursorActive = active;
        EnsureInstance();
        instance.ApplyCursorForCurrentScene();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameCursorManager existing = FindFirstObjectByType<GameCursorManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            instance.Initialize();
            return;
        }

        GameObject cursorManagerObject = new GameObject("GameCursorManager");
        instance = cursorManagerObject.AddComponent<GameCursorManager>();
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

        if (scaledCursorTexture != null)
        {
            Destroy(scaledCursorTexture);
            scaledCursorTexture = null;
        }
    }

    private void Initialize()
    {
        DontDestroyOnLoad(gameObject);
        LoadCursorTexture();

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        ApplyCursorForCurrentScene();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        stagePortalCursorActive = false;
        ApplyCursorForScene(scene.name);
    }

    private void ApplyCursorForCurrentScene()
    {
        ApplyCursorForScene(SceneManager.GetActiveScene().name);
    }

    private void ApplyCursorForScene(string sceneName)
    {
        if (!ShouldUseCustomCursor(sceneName))
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        Texture2D activeCursorTexture = GetActiveCursorTexture();
        if (activeCursorTexture == null)
        {
            WarnMissingCursorOnce();
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.SetCursor(activeCursorTexture, cursorHotspot * GetCursorScale(), cursorMode);
    }

    private void LoadCursorTexture()
    {
        if (cursorTexture != null)
            return;

        cursorTexture = Resources.Load<Texture2D>(CursorResourcePath);
    }

    private Texture2D GetActiveCursorTexture()
    {
        LoadCursorTexture();
        if (cursorTexture == null)
            return null;

        float scale = GetCursorScale();
        if (Mathf.Approximately(scale, 1f))
            return cursorTexture;

        if (scaledCursorTexture != null && Mathf.Approximately(scaledCursorTextureScale, scale))
            return scaledCursorTexture;

        if (scaledCursorTexture != null)
        {
            Destroy(scaledCursorTexture);
            scaledCursorTexture = null;
        }

        int width = Mathf.Max(1, Mathf.RoundToInt(cursorTexture.width * scale));
        int height = Mathf.Max(1, Mathf.RoundToInt(cursorTexture.height * scale));
        scaledCursorTexture = CreateScaledTexture(cursorTexture, width, height);
        scaledCursorTextureScale = scale;

        return scaledCursorTexture != null ? scaledCursorTexture : cursorTexture;
    }

    private float GetCursorScale()
    {
        return Mathf.Max(1f, cursorScale);
    }

    private static Texture2D CreateScaledTexture(Texture2D source, int width, int height)
    {
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        temporary.filterMode = FilterMode.Point;

        FilterMode previousFilterMode = source.filterMode;
        source.filterMode = FilterMode.Point;
        Graphics.Blit(source, temporary);
        source.filterMode = previousFilterMode;

        RenderTexture.active = temporary;

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply(false, false);

        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(temporary);

        return result;
    }

    private static bool ShouldUseCustomCursor(string sceneName)
    {
        return sceneName == SampleSceneName
            || sceneName == TitleSceneName
            || sceneName == StoryCutSceneName
            || sceneName == ClearSceneName
            || stagePortalCursorActive && IsStageScene(sceneName);
    }

    private static bool IsStageScene(string sceneName)
    {
        return sceneName == Stage1SceneName
            || sceneName == Stage2SceneName
            || sceneName == Stage3SceneName;
    }

    private void WarnMissingCursorOnce()
    {
        if (warnedMissingCursor)
            return;

        warnedMissingCursor = true;
        Debug.LogWarning($"GameCursorManager could not find Resources/{CursorResourcePath}.png.", this);
    }
}
