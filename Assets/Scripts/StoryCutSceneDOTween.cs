using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryCutSceneDOTween : MonoBehaviour
{
    private const float HiddenScale = 0.95f;
    private const float NextButtonFadeDuration = 0.3f;

    [Header("Cut Images")]
    [SerializeField] private CanvasGroup[] cutGroups;

    [Header("Next Button")]
    [SerializeField] private CanvasGroup nextButtonGroup;

    [Header("Timing")]
    [SerializeField] private float firstDelay = 0.5f;
    [SerializeField] private float intervalBetweenCuts = 1.2f;
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float scaleDuration = 0.4f;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "Stage1";
    [SerializeField] private KeyCode skipKey = KeyCode.Space;

    private Sequence sequence;
    private bool isFinished;

    private void Awake()
    {
        HideAllCuts();
        SetNextButtonVisible(false, 0f);
    }

    private void Start()
    {
        PlayStoryCutScene();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(skipKey))
        {
            return;
        }

        if (isFinished)
        {
            GoToNextScene();
            return;
        }

        ShowAllImmediately();
    }

    private void PlayStoryCutScene()
    {
        KillSequence();
        sequence = DOTween.Sequence();
        sequence.AppendInterval(firstDelay);

        if (cutGroups != null)
        {
            foreach (CanvasGroup cutGroup in cutGroups)
            {
                AppendCutTween(cutGroup);
            }
        }

        sequence.AppendCallback(FinishCutScene);
    }

    private void AppendCutTween(CanvasGroup cutGroup)
    {
        if (cutGroup == null)
        {
            return;
        }

        RectTransform rect = cutGroup.GetComponent<RectTransform>();

        sequence.AppendCallback(() => PrepareCut(cutGroup, rect));
        sequence.Append(cutGroup.DOFade(1f, fadeDuration));

        if (rect != null)
        {
            sequence.Join(rect.DOScale(Vector3.one, scaleDuration).SetEase(Ease.OutBack));
        }

        sequence.AppendInterval(intervalBetweenCuts);
    }

    private void PrepareCut(CanvasGroup cutGroup, RectTransform rect)
    {
        cutGroup.gameObject.SetActive(true);
        cutGroup.alpha = 0f;

        if (rect != null)
        {
            rect.localScale = Vector3.one * HiddenScale;
        }
    }

    private void HideAllCuts()
    {
        if (cutGroups == null)
        {
            return;
        }

        foreach (CanvasGroup cutGroup in cutGroups)
        {
            SetCutVisible(cutGroup, false, 0f, HiddenScale);
        }
    }

    private void ShowAllImmediately()
    {
        KillSequence();

        if (cutGroups != null)
        {
            foreach (CanvasGroup cutGroup in cutGroups)
            {
                SetCutVisible(cutGroup, true, 1f, 1f);
            }
        }

        FinishCutScene();
    }

    private void SetCutVisible(CanvasGroup cutGroup, bool isVisible, float alpha, float scale)
    {
        if (cutGroup == null)
        {
            return;
        }

        cutGroup.gameObject.SetActive(isVisible);
        cutGroup.alpha = alpha;

        RectTransform rect = cutGroup.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one * scale;
        }
    }

    private void FinishCutScene()
    {
        isFinished = true;
        SetNextButtonVisible(true, 0f);
        nextButtonGroup?.DOFade(1f, NextButtonFadeDuration);
    }

    private void SetNextButtonVisible(bool isVisible, float alpha)
    {
        if (nextButtonGroup == null)
        {
            return;
        }

        nextButtonGroup.gameObject.SetActive(isVisible);
        nextButtonGroup.alpha = alpha;
        nextButtonGroup.interactable = isVisible;
        nextButtonGroup.blocksRaycasts = isVisible;
    }

    public void GoToNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("Next scene name is empty.");
            return;
        }

        KillSequence();
        SceneManager.LoadScene(nextSceneName);
    }

    private void KillSequence()
    {
        if (sequence == null)
        {
            return;
        }

        sequence.Kill();
        sequence = null;
    }

    private void OnDestroy()
    {
        KillSequence();
    }
}
