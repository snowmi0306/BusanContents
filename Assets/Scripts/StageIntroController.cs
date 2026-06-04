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

    [Header("Confirm Button")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private CanvasGroup confirmButtonCanvasGroup;
    [SerializeField] private float confirmButtonFadeInDuration = 0.25f;

    [Header("Animation")]
    [SerializeField] private Vector2 mailStartPosition = new Vector2(0f, 400f);
    [SerializeField] private Vector2 mailEndPosition = new Vector2(0f, -200f);
    [SerializeField] private float moveDuration = 2.2f;
    [SerializeField] private float panelFadeOutDuration = 0.5f;

    [Header("Alpha")]
    [SerializeField] private float mailStartAlpha = 0.5f;
    [SerializeField] private float mailEndAlpha = 1.0f;

    [Header("Input")]
    [SerializeField] private KeyCode confirmKey = KeyCode.Space;
    [SerializeField] private bool allowSpaceToConfirm = true;

    [Header("Player Control")]
    [SerializeField] private MonoBehaviour playerController;

    private Rigidbody2D playerRigidbody;

    private bool isPlaying;
    private bool canConfirm;
    private bool confirmRequested;

    private void Awake()
    {
        if (introCanvasGroup == null && introPanel != null)
        {
            introCanvasGroup = introPanel.GetComponent<CanvasGroup>();
        }

        if (confirmButtonCanvasGroup == null && confirmButton != null)
        {
            confirmButtonCanvasGroup = confirmButton.GetComponent<CanvasGroup>();
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(RequestConfirm);
        }

        HideConfirmButtonInstant();
    }

    private void Start()
    {
        FindPlayerIfNeeded();
        StartCoroutine(PlayIntro());
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(RequestConfirm);
        }
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        if (!canConfirm)
            return;

        if (!allowSpaceToConfirm)
            return;

        if (Input.GetKeyDown(confirmKey))
        {
            RequestConfirm();
        }
    }

    private IEnumerator PlayIntro()
    {
        isPlaying = true;
        canConfirm = false;
        confirmRequested = false;

        SetPlayerControl(false);

        if (introPanel != null)
            introPanel.SetActive(true);

        if (introCanvasGroup != null)
        {
            introCanvasGroup.alpha = 1f;
            introCanvasGroup.interactable = true;
            introCanvasGroup.blocksRaycasts = true;
        }

        HideConfirmButtonInstant();

        if (mailRect != null)
            mailRect.anchoredPosition = mailStartPosition;

        SetMailAlpha(mailStartAlpha);

        yield return MoveMailRoutine();

        if (mailRect != null)
            mailRect.anchoredPosition = mailEndPosition;

        SetMailAlpha(mailEndAlpha);

        yield return ShowConfirmButtonRoutine();

        canConfirm = true;

        while (!confirmRequested)
        {
            yield return null;
        }

        canConfirm = false;

        yield return FadePanelRoutine(1f, 0f, panelFadeOutDuration);

        EndIntro();
    }

    private IEnumerator MoveMailRoutine()
    {
        float time = 0f;

        while (time < moveDuration)
        {
            time += Time.unscaledDeltaTime;

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
    }

    private IEnumerator ShowConfirmButtonRoutine()
    {
        if (confirmButton == null)
            yield break;

        confirmButton.gameObject.SetActive(true);
        confirmButton.interactable = false;

        if (confirmButtonCanvasGroup == null)
        {
            confirmButton.interactable = true;
            yield break;
        }

        confirmButtonCanvasGroup.alpha = 0f;
        confirmButtonCanvasGroup.interactable = false;
        confirmButtonCanvasGroup.blocksRaycasts = false;

        float time = 0f;
        float duration = Mathf.Max(0.01f, confirmButtonFadeInDuration);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            confirmButtonCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        confirmButtonCanvasGroup.alpha = 1f;
        confirmButtonCanvasGroup.interactable = true;
        confirmButtonCanvasGroup.blocksRaycasts = true;
        confirmButton.interactable = true;
    }

    private IEnumerator FadePanelRoutine(float from, float to, float duration)
    {
        if (introCanvasGroup == null)
            yield break;

        float time = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            introCanvasGroup.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        introCanvasGroup.alpha = to;
    }

    private void RequestConfirm()
    {
        if (!canConfirm)
            return;

        confirmRequested = true;

        if (confirmButton != null)
            confirmButton.interactable = false;

        if (confirmButtonCanvasGroup != null)
        {
            confirmButtonCanvasGroup.interactable = false;
            confirmButtonCanvasGroup.blocksRaycasts = false;
        }
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

        SetPlayerControl(true);
    }

    private void SetMailAlpha(float alpha)
    {
        if (mailImage == null)
            return;

        Color color = mailImage.color;
        color.a = Mathf.Clamp01(alpha);
        mailImage.color = color;
    }

    private void HideConfirmButtonInstant()
    {
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
            confirmButton.gameObject.SetActive(false);
        }

        if (confirmButtonCanvasGroup != null)
        {
            confirmButtonCanvasGroup.alpha = 0f;
            confirmButtonCanvasGroup.interactable = false;
            confirmButtonCanvasGroup.blocksRaycasts = false;
        }
    }

    private void FindPlayerIfNeeded()
    {
        if (playerController == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                playerController = playerObject.GetComponentInParent<PlayController>();
            }
        }

        if (playerController != null)
        {
            playerRigidbody = playerController.GetComponentInParent<Rigidbody2D>();
        }
    }

    private void SetPlayerControl(bool enabled)
    {
        FindPlayerIfNeeded();

        if (playerController != null)
            playerController.enabled = enabled;

        if (!enabled && playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
        }
    }
}