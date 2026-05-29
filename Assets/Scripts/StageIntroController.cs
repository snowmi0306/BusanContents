using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StageIntroController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private CanvasGroup introCanvasGroup;

    [Header("Images")]
    [SerializeField] private RectTransform mailRect;
    [SerializeField] private Image mailImage;

    [Header("Animation")]
    [SerializeField] private Vector2 mailStartPosition = new Vector2(0f, 400f);
    [SerializeField] private Vector2 mailEndPosition = new Vector2(0f, -200f);
    [SerializeField] private float moveDuration = 0.7f;
    [SerializeField] private float holdDuration = 1.0f;

    [Header("Alpha")]
    [SerializeField] private float mailStartAlpha = 0.5f;
    [SerializeField] private float mailEndAlpha = 1.0f;

    [Header("Input")]
    [SerializeField] private KeyCode skipKey = KeyCode.Space;

    [Header("Player Control")]
    [SerializeField] private MonoBehaviour playerController;

    private bool isPlaying;
    private bool skipRequested;
    private Coroutine introRoutine;

    private void Start()
    {
        introRoutine = StartCoroutine(PlayIntro());
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        if (Input.GetKeyDown(skipKey))
        {
            skipRequested = true;
        }
    }

    private IEnumerator PlayIntro()
    {
        isPlaying = true;
        skipRequested = false;

        if (playerController != null)
            playerController.enabled = false;

        if (introPanel != null)
            introPanel.SetActive(true);

        if (introCanvasGroup != null)
        {
            introCanvasGroup.alpha = 1f;
            introCanvasGroup.interactable = true;
            introCanvasGroup.blocksRaycasts = true;
        }

        if (mailRect != null)
            mailRect.anchoredPosition = mailStartPosition;

        SetMailAlpha(mailStartAlpha);

        float time = 0f;

        while (time < moveDuration && !skipRequested)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / moveDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (mailRect != null)
            {
                mailRect.anchoredPosition = Vector2.Lerp(
                    mailStartPosition,
                    mailEndPosition,
                    smoothT
                );
            }

            float alpha = Mathf.Lerp(mailStartAlpha, mailEndAlpha, smoothT);
            SetMailAlpha(alpha);

            yield return null;
        }

        if (mailRect != null)
            mailRect.anchoredPosition = mailEndPosition;

        SetMailAlpha(mailEndAlpha);

        float holdTime = 0f;

        while (holdTime < holdDuration && !skipRequested)
        {
            holdTime += Time.deltaTime;
            yield return null;
        }

        EndIntro();
    }

    private void SetMailAlpha(float alpha)
    {
        if (mailImage == null)
            return;

        Color color = mailImage.color;
        color.a = alpha;
        mailImage.color = color;
    }

    private void EndIntro()
    {
        isPlaying = false;

        if (introCanvasGroup != null)
        {
            introCanvasGroup.alpha = 0f;
            introCanvasGroup.interactable = false;
            introCanvasGroup.blocksRaycasts = false;
        }

        if (introPanel != null)
            introPanel.SetActive(false);

        if (playerController != null)
            playerController.enabled = true;
    }
}