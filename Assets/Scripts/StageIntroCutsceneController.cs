using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class StageIntroCutsceneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayController player;
    [SerializeField] private CanvasGroup introGroup;

    [Header("Timing")]
    [SerializeField] private float introDuration = 2f;
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    [Header("Input")]
    [SerializeField] private KeyCode skipKey = KeyCode.Space;

    [Header("Events")]
    [SerializeField] private UnityEvent onIntroStart;
    [SerializeField] private UnityEvent onIntroEnd;

    private Sequence introSequence;
    private bool playing;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayController>();

        if (introGroup != null)
        {
            introGroup.alpha = 0f;
            introGroup.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        PlayIntro();
    }

    private void Update()
    {
        if (playing && Input.GetKeyDown(skipKey))
            EndIntro();
    }

    public void PlayIntro()
    {
        if (playing)
            return;

        playing = true;
        player?.SetControlEnabled(false);
        onIntroStart?.Invoke();

        introSequence?.Kill();
        introSequence = DOTween.Sequence();

        if (introGroup != null)
        {
            introGroup.gameObject.SetActive(true);
            introGroup.alpha = 0f;
            introSequence.Append(introGroup.DOFade(1f, fadeInDuration));
            introSequence.AppendInterval(introDuration);
            introSequence.Append(introGroup.DOFade(0f, fadeOutDuration));
        }
        else
        {
            introSequence.AppendInterval(introDuration);
        }

        introSequence.OnComplete(EndIntro);
    }

    public void EndIntro()
    {
        if (!playing)
            return;

        playing = false;
        introSequence?.Kill();

        if (introGroup != null)
        {
            introGroup.alpha = 0f;
            introGroup.gameObject.SetActive(false);
        }

        player?.SetControlEnabled(true);
        onIntroEnd?.Invoke();
    }

    private void OnDestroy()
    {
        introSequence?.Kill();
    }
}
