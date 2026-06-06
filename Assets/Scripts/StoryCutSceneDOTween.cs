using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryCutSceneDOTween : MonoBehaviour
{
    [Header("Cut Images")]
    [SerializeField] private CanvasGroup[] cutGroups;

    [Header("Next Button")]
    [SerializeField] private Button nextButton;
    [SerializeField] private CanvasGroup nextButtonGroup;

    [Header("Responsive Layout")]
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private bool configureCanvasScaler = true;
    [SerializeField] private Vector2 referenceResolution = new Vector2(800f, 600f);
    [SerializeField, Range(0f, 1f)] private float matchWidthOrHeight = 0.5f;
    [SerializeField] private bool preserveCutImageAspect = true;

    [Header("Timing")]
    [SerializeField] private float firstDelay = 0.5f;
    [SerializeField] private float intervalBetweenCuts = 1.2f;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float scaleDuration = 0.4f;
    [SerializeField] private float sceneLoadFadeDuration = 0.25f;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "Stage1";
    [SerializeField] private KeyCode advanceKey = KeyCode.Space;
    [SerializeField] private bool allowEnterKey = true;
    [SerializeField] private bool allowMouseClick = true;
    [SerializeField] private bool useUnscaledTime = true;

    private Sequence sequence;
    private bool isFinished;
    private bool isLoadingNextScene;

    private void Awake()
    {
        ConfigureResponsiveLayout();
        ResolveNextButtonReferences();
        RegisterNextButton();
        HideAllCuts();
        HideNextButton();
    }

    private void Start()
    {
        PlayStoryCutScene();
    }

    private void Update()
    {
        if (!IsAdvanceInputDown())
            return;

        Advance();
    }

    private void PlayStoryCutScene()
    {
        isFinished = false;
        isLoadingNextScene = false;

        sequence = DOTween.Sequence();
        sequence.SetUpdate(useUnscaledTime);
        sequence.AppendInterval(firstDelay);

        foreach (CanvasGroup cutGroup in cutGroups)
        {
            if (cutGroup == null)
                continue;

            RectTransform rect = cutGroup.GetComponent<RectTransform>();

            sequence.AppendCallback(() =>
            {
                cutGroup.gameObject.SetActive(true);
                cutGroup.alpha = 0f;
                cutGroup.interactable = false;
                cutGroup.blocksRaycasts = false;

                if (rect != null)
                    rect.localScale = Vector3.one * 0.95f;
            });

            sequence.Append(cutGroup.DOFade(1f, fadeDuration));

            if (rect != null)
            {
                sequence.Join(
                    rect.DOScale(Vector3.one, scaleDuration)
                        .SetEase(Ease.OutBack)
                );
            }

            sequence.AppendInterval(intervalBetweenCuts);
        }

        sequence.AppendCallback(() =>
        {
            isFinished = true;
            ShowNextButton();
        });
    }

    private void ConfigureResponsiveLayout()
    {
        if (configureCanvasScaler)
        {
            if (canvasScaler == null)
                canvasScaler = GetComponentInParent<CanvasScaler>();

            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = referenceResolution;
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = matchWidthOrHeight;
            }
        }

        if (!preserveCutImageAspect || cutGroups == null)
            return;

        foreach (CanvasGroup cutGroup in cutGroups)
        {
            if (cutGroup == null)
                continue;

            Image image = cutGroup.GetComponent<Image>();
            if (image != null)
                image.preserveAspect = true;
        }
    }

    private void ResolveNextButtonReferences()
    {
        if (nextButton == null && nextButtonGroup != null)
        {
            nextButton = nextButtonGroup.GetComponentInChildren<Button>(true);

            if (nextButton == null)
                nextButton = nextButtonGroup.GetComponentInParent<Button>(true);
        }

        if (nextButton == null)
            nextButton = FindFirstObjectByType<Button>(FindObjectsInactive.Include);

        if (nextButton == null)
            return;

        CanvasGroup buttonGroup = nextButton.GetComponent<CanvasGroup>();
        if (buttonGroup == null)
            buttonGroup = nextButton.gameObject.AddComponent<CanvasGroup>();

        nextButtonGroup = buttonGroup;
    }

    private void RegisterNextButton()
    {
        if (nextButton == null)
            return;

        nextButton.onClick.RemoveListener(GoToNextScene);
        nextButton.onClick.RemoveListener(Advance);
        nextButton.onClick.AddListener(Advance);
    }

    private bool IsAdvanceInputDown()
    {
        if (isLoadingNextScene)
            return false;

        if (Input.GetKeyDown(advanceKey))
            return true;

        if (allowEnterKey && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            return true;

        return allowMouseClick && Input.GetMouseButtonDown(0);
    }

    public void Advance()
    {
        if (isLoadingNextScene)
            return;

        if (!isFinished)
        {
            ShowAllImmediately();
            return;
        }

        GoToNextScene();
    }

    private void HideAllCuts()
    {
        foreach (CanvasGroup cutGroup in cutGroups)
        {
            if (cutGroup == null)
                continue;

            cutGroup.alpha = 0f;
            cutGroup.interactable = false;
            cutGroup.blocksRaycasts = false;
            cutGroup.gameObject.SetActive(false);

            RectTransform rect = cutGroup.GetComponent<RectTransform>();
            if (rect != null)
                rect.localScale = Vector3.one * 0.95f;
        }
    }

    private void HideNextButton()
    {
        if (nextButtonGroup == null)
            return;

        nextButtonGroup.alpha = 0f;
        nextButtonGroup.gameObject.SetActive(false);
        nextButtonGroup.interactable = false;
        nextButtonGroup.blocksRaycasts = false;

        if (nextButton != null)
            nextButton.interactable = false;
    }

    private void ShowNextButton()
    {
        if (nextButtonGroup == null)
            return;

        nextButtonGroup.gameObject.SetActive(true);
        nextButtonGroup.alpha = 0f;
        nextButtonGroup.interactable = true;
        nextButtonGroup.blocksRaycasts = true;

        if (nextButton != null)
            nextButton.interactable = true;

        nextButtonGroup.DOFade(1f, 0.3f).SetUpdate(useUnscaledTime);
    }

    private void ShowAllImmediately()
    {
        sequence?.Kill();

        foreach (CanvasGroup cutGroup in cutGroups)
        {
            if (cutGroup == null)
                continue;

            cutGroup.gameObject.SetActive(true);
            cutGroup.alpha = 1f;
            cutGroup.interactable = false;
            cutGroup.blocksRaycasts = false;

            RectTransform rect = cutGroup.GetComponent<RectTransform>();
            if (rect != null)
                rect.localScale = Vector3.one;
        }

        isFinished = true;
        ShowNextButton();
    }

    public void GoToNextScene()
    {
        if (isLoadingNextScene)
            return;

        StartCoroutine(LoadNextSceneRoutine());
    }

    private IEnumerator LoadNextSceneRoutine()
    {
        isLoadingNextScene = true;
        sequence?.Kill();

        if (nextButton != null)
            nextButton.interactable = false;

        float duration = Mathf.Max(0f, sceneLoadFadeDuration);

        if (duration > 0f)
        {
            Sequence fadeOutSequence = DOTween.Sequence();
            fadeOutSequence.SetUpdate(useUnscaledTime);

            foreach (CanvasGroup cutGroup in cutGroups)
            {
                if (cutGroup == null || !cutGroup.gameObject.activeInHierarchy)
                    continue;

                fadeOutSequence.Join(cutGroup.DOFade(0f, duration));
            }

            if (nextButtonGroup != null && nextButtonGroup.gameObject.activeInHierarchy)
                fadeOutSequence.Join(nextButtonGroup.DOFade(0f, duration));

            yield return fadeOutSequence.WaitForCompletion();
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("StoryCutSceneDOTween nextSceneName is empty. Cannot load next scene.", this);
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void OnDestroy()
    {
        sequence?.Kill();

        if (nextButton != null)
            nextButton.onClick.RemoveListener(Advance);
    }
}
