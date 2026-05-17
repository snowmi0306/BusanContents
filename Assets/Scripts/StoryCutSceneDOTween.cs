using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryCutSceneDOTween : MonoBehaviour
{
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
        HideNextButton();
    }

    private void Start()
    {
        PlayStoryCutScene();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(skipKey))
            return;

        if (!isFinished)
        {
            ShowAllImmediately();
            return;
        }

        GoToNextScene();
    }

    private void PlayStoryCutScene()
    {
        sequence = DOTween.Sequence();

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

    private void HideAllCuts()
    {
        foreach (CanvasGroup cutGroup in cutGroups)
        {
            if (cutGroup == null)
                continue;

            cutGroup.alpha = 0f;
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
    }

    private void ShowNextButton()
    {
        if (nextButtonGroup == null)
            return;

        nextButtonGroup.gameObject.SetActive(true);
        nextButtonGroup.alpha = 0f;
        nextButtonGroup.interactable = true;
        nextButtonGroup.blocksRaycasts = true;

        nextButtonGroup.DOFade(1f, 0.3f);
    }

    private void ShowAllImmediately()
    {
        if (sequence != null)
            sequence.Kill();

        foreach (CanvasGroup cutGroup in cutGroups)
        {
            if (cutGroup == null)
                continue;

            cutGroup.gameObject.SetActive(true);
            cutGroup.alpha = 1f;

            RectTransform rect = cutGroup.GetComponent<RectTransform>();
            if (rect != null)
                rect.localScale = Vector3.one;
        }

        isFinished = true;
        ShowNextButton();
    }

    public void GoToNextScene()
    {
        if (sequence != null)
            sequence.Kill();

        SceneManager.LoadScene(nextSceneName);
    }

    private void OnDestroy()
    {
        if (sequence != null)
            sequence.Kill();
    }
}