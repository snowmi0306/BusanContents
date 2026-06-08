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
    private static bool stageIntroCursorActive;
    private static bool stagePortalCursorActive;

    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    [SerializeField] private CursorMode cursorMode = CursorMode.ForceSoftware;

    private Texture2D cursorTexture;
    private bool warnedMissingCursor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static void SetStagePortalCursorActive(bool active)
    {
        stagePortalCursorActive = active;
        ApplyIfAvailableOrNeeded(active);
    }

    public static void SetStageIntroCursorActive(bool active)
    {
        stageIntroCursorActive = active;
        ApplyIfAvailableOrNeeded(active);
    }

    private static void ApplyIfAvailableOrNeeded(bool active)
    {
        if (!active && instance == null)
            return;

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
        stageIntroCursorActive = false;
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

        LoadCursorTexture();
        if (cursorTexture == null)
        {
            WarnMissingCursorOnce();
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.SetCursor(cursorTexture, cursorHotspot, cursorMode);
    }

    private void LoadCursorTexture()
    {
        if (cursorTexture != null)
            return;

        cursorTexture = Resources.Load<Texture2D>(CursorResourcePath);
    }

    private static bool ShouldUseCustomCursor(string sceneName)
    {
        return sceneName == SampleSceneName
            || sceneName == TitleSceneName
            || sceneName == StoryCutSceneName
            || sceneName == ClearSceneName
            || stageIntroCursorActive && IsStageScene(sceneName)
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
