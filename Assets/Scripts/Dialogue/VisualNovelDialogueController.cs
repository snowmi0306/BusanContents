using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VisualNovelDialogueController : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image dandiPortrait;
    [SerializeField] private Image npcPortrait;
    [SerializeField] private float activeAlpha = 1f;
    [SerializeField] private float inactiveAlpha = 0.5f;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button nextButton;

    [Header("Input")]
    [SerializeField] private KeyCode nextKey = KeyCode.Space;

    [Header("Typing")]
    [SerializeField] private float charactersPerSecond = 35f;
    [SerializeField] private bool showCursorDuringDialogue = true;

    public bool IsPlaying { get; private set; }

    private DialogueLine[] currentLines;
    private int currentLineIndex;
    private Action onFinished;
    private Coroutine typingCoroutine;
    private bool isTyping;
    private string currentFullText = string.Empty;

    private void Awake()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        RegisterNextButtonListener();
    }

    private void Update()
    {
        if (!IsPlaying)
            return;

        if (Input.GetKeyDown(nextKey))
        {
            AdvanceDialogue();
        }
    }

    private void OnDestroy()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(AdvanceDialogue);
        }
    }

    public void SetPortraitSprites(Sprite dandiSprite, Sprite npcSprite)
    {
        if (dandiPortrait != null)
        {
            dandiPortrait.sprite = dandiSprite;
            dandiPortrait.enabled = dandiSprite != null;
        }

        if (npcPortrait != null)
        {
            npcPortrait.sprite = npcSprite;
            npcPortrait.enabled = npcSprite != null;
        }
    }

    public void StartDialogue(DialogueLine[] lines, GameObject playerObject, Action onDialogueFinished)
    {
        StopTypingCoroutine();

        currentLines = lines;
        currentLineIndex = 0;
        onFinished = onDialogueFinished;
        IsPlaying = true;
        isTyping = false;
        currentFullText = string.Empty;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        DisablePlayerControl(playerObject);
        ShowCursorForDialogue();
        RegisterNextButtonListener();

        if (currentLines == null || currentLines.Length == 0)
        {
            FinishDialogue();
            return;
        }

        ShowLine(currentLineIndex);
    }

    public void AdvanceDialogue()
    {
        if (!IsPlaying)
            return;

        if (isTyping)
        {
            StopTypingCoroutine();
            SetDialogueText(currentFullText);
            isTyping = false;
            return;
        }

        currentLineIndex++;

        if (currentLines == null || currentLineIndex >= currentLines.Length)
        {
            FinishDialogue();
            return;
        }

        ShowLine(currentLineIndex);
    }

    private void ShowLine(int lineIndex)
    {
        if (currentLines == null || lineIndex < 0 || lineIndex >= currentLines.Length)
        {
            FinishDialogue();
            return;
        }

        DialogueLine line = currentLines[lineIndex];
        if (line == null)
        {
            currentFullText = string.Empty;
            SetSpeakerNameText(string.Empty);
            SetDialogueText(string.Empty);
            SetPortraitAlpha(DialogueSpeaker.NPC);
            isTyping = false;
            return;
        }

        SetSpeakerNameText(line.speakerName);
        SetPortraitAlpha(line.speaker);

        currentFullText = line.text ?? string.Empty;
        SetDialogueText(string.Empty);

        StopTypingCoroutine();

        if (dialogueText == null || string.IsNullOrEmpty(currentFullText))
        {
            SetDialogueText(currentFullText);
            isTyping = false;
            return;
        }

        typingCoroutine = StartCoroutine(TypeLine(currentFullText));
    }

    private IEnumerator TypeLine(string fullText)
    {
        isTyping = true;

        float secondsPerCharacter = charactersPerSecond > 0f ? 1f / charactersPerSecond : 0f;

        for (int i = 1; i <= fullText.Length; i++)
        {
            SetDialogueText(fullText.Substring(0, i));

            if (secondsPerCharacter > 0f)
            {
                yield return new WaitForSeconds(secondsPerCharacter);
            }
            else
            {
                yield return null;
            }
        }

        isTyping = false;
        typingCoroutine = null;
    }

    private void FinishDialogue()
    {
        StopTypingCoroutine();

        IsPlaying = false;
        isTyping = false;
        currentLines = null;
        currentFullText = string.Empty;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        Action finishedCallback = onFinished;
        onFinished = null;
        finishedCallback?.Invoke();
    }

    private void DisablePlayerControl(GameObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayController player = playerObject.GetComponent<PlayController>();
        if (player != null)
        {
            player.enabled = false;
        }

        Rigidbody2D rb = playerObject.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void ShowCursorForDialogue()
    {
        if (!showCursorDuringDialogue)
            return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void SetPortraitAlpha(DialogueSpeaker activeSpeaker)
    {
        SetImageAlpha(dandiPortrait, activeSpeaker == DialogueSpeaker.Dandi ? activeAlpha : inactiveAlpha);
        SetImageAlpha(npcPortrait, activeSpeaker == DialogueSpeaker.NPC ? activeAlpha : inactiveAlpha);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    private void SetSpeakerNameText(string value)
    {
        if (speakerNameText != null)
        {
            speakerNameText.text = value ?? string.Empty;
        }
    }

    private void SetDialogueText(string value)
    {
        if (dialogueText != null)
        {
            dialogueText.text = value ?? string.Empty;
        }
    }

    private void RegisterNextButtonListener()
    {
        if (nextButton == null)
            return;

        nextButton.onClick.RemoveListener(AdvanceDialogue);
        nextButton.onClick.AddListener(AdvanceDialogue);
    }

    private void StopTypingCoroutine()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }
}
