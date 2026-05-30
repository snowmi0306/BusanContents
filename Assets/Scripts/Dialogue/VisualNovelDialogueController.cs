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

    [Header("Layout Recovery")]
    [SerializeField] private bool applyLayoutOnAwake = true;
    [SerializeField] private RectTransform dialogueBox;
    [SerializeField] private RectTransform dimBackground;

    [Header("Input")]
    [SerializeField] private KeyCode nextKey = KeyCode.Space;

    [Header("Typing")]
    [SerializeField] private float charactersPerSecond = 35f;

    [Header("Cursor")]
    [SerializeField] private bool showCursorDuringDialogue = true;
    [SerializeField] private bool restoreCursorAfterDialogue = false;

    public bool IsPlaying { get; private set; }

    private DialogueLine[] currentLines;
    private int currentLineIndex;
    private Action finishedCallback;
    private Coroutine typingCoroutine;
    private bool isTyping;
    private string currentFullText = string.Empty;
    private GameObject currentPlayerObject;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockState;

    private void Awake()
    {
        ResolveMissingReferences();

        if (applyLayoutOnAwake)
        {
            ApplyVisualNovelLayout();
        }

        ValidateRequiredReferences();

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

#if UNITY_EDITOR
    [ContextMenu("Recover/Apply Visual Novel Layout")]
    private void ApplyVisualNovelLayoutFromContextMenu()
    {
        ResolveMissingReferences();
        ApplyVisualNovelLayout();
        ValidateRequiredReferences();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    public void SetPortraitSprites(Sprite dandiSprite, Sprite npcSprite)
    {
        SetPortraitSprite(dandiPortrait, dandiSprite);
        SetPortraitSprite(npcPortrait, npcSprite);
    }

    public void StartDialogue(DialogueLine[] dialogueLines, GameObject playerObject, Action onDialogueFinished)
    {
        if (IsPlaying)
            return;

        ResolveMissingReferences();
        ValidateRequiredReferences();

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning("VisualNovelDialogueController received no dialogue lines. Finishing immediately.", this);
            onDialogueFinished?.Invoke();
            return;
        }

        StopTypingCoroutine();

        currentLines = dialogueLines;
        currentLineIndex = 0;
        finishedCallback = onDialogueFinished;
        currentPlayerObject = GetPlayerObject(playerObject);
        IsPlaying = true;
        isTyping = false;
        currentFullText = string.Empty;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        DisablePlayerControl(currentPlayerObject);
        ShowCursorForDialogue();
        RegisterNextButtonListener();
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

        EnablePlayerControl(currentPlayerObject);
        RestoreCursorIfNeeded();
        currentPlayerObject = null;

        Action callback = finishedCallback;
        finishedCallback = null;
        callback?.Invoke();
    }

    private void ResolveMissingReferences()
    {
        if (dialoguePanel == null)
        {
            dialoguePanel = gameObject;
        }

        Transform root = dialoguePanel != null ? dialoguePanel.transform : transform;

        if (dandiPortrait == null)
        {
            dandiPortrait = FindImageByName(root, "DandiPortrait");
        }

        if (npcPortrait == null)
        {
            npcPortrait = FindImageByName(root, "NPCPortrait");
        }

        if (speakerNameText == null)
        {
            speakerNameText = FindTextByName(root, "SpeakerNameText");
        }

        if (dialogueText == null)
        {
            dialogueText = FindTextByName(root, "DialogueText");
        }

        if (nextButton == null)
        {
            nextButton = root.GetComponentInChildren<Button>(true);
        }

        if (dialogueBox == null)
        {
            Transform foundDialogueBox = FindChildByName(root, "DialogueBox");
            if (foundDialogueBox != null)
            {
                dialogueBox = foundDialogueBox as RectTransform;
            }
        }

        if (dimBackground == null)
        {
            Transform foundDimBackground = FindChildByName(root, "DimBackground");
            if (foundDimBackground != null)
            {
                dimBackground = foundDimBackground as RectTransform;
            }
        }
    }

    private void ValidateRequiredReferences()
    {
        if (dialoguePanel == null)
        {
            Debug.LogWarning("VisualNovelDialogueController is missing Dialogue Panel.", this);
        }

        if (dandiPortrait == null)
        {
            Debug.LogWarning("VisualNovelDialogueController is missing DandiPortrait Image. Create or assign a child named DandiPortrait.", this);
        }

        if (npcPortrait == null)
        {
            Debug.LogWarning("VisualNovelDialogueController is missing NPCPortrait Image. Create or assign a child named NPCPortrait.", this);
        }

        if (speakerNameText == null)
        {
            Debug.LogWarning("VisualNovelDialogueController is missing SpeakerNameText TMP_Text. Create or assign a child named SpeakerNameText.", this);
        }

        if (dialogueText == null)
        {
            Debug.LogWarning("VisualNovelDialogueController is missing DialogueText TMP_Text. Create or assign a child named DialogueText.", this);
        }

        if (nextButton == null)
        {
            Debug.LogWarning("VisualNovelDialogueController is missing Next Button.", this);
        }
    }

    private void ApplyVisualNovelLayout()
    {
        ConfigurePortrait(dandiPortrait);
        ConfigurePortrait(npcPortrait);
        ConfigureSpeakerNameText();
        ConfigureDialogueText();
    }

    private void ConfigurePortrait(Image portrait)
    {
        if (portrait == null)
            return;

        portrait.preserveAspect = true;
    }

    private void ConfigureSpeakerNameText()
    {
        if (speakerNameText == null)
            return;

        speakerNameText.enableAutoSizing = false;
        speakerNameText.fontSize = 32f;
        speakerNameText.alignment = TextAlignmentOptions.Left;
    }

    private void ConfigureDialogueText()
    {
        if (dialogueText == null)
            return;

        dialogueText.enableAutoSizing = false;
        dialogueText.fontSize = 30f;
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        dialogueText.enableWordWrapping = true;
    }

    private GameObject GetPlayerObject(GameObject playerObject)
    {
        if (playerObject == null)
            return null;

        PlayController player = playerObject.GetComponentInParent<PlayController>();
        if (player != null)
        {
            return player.gameObject;
        }

        return playerObject;
    }

    private void DisablePlayerControl(GameObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayController player = playerObject.GetComponentInParent<PlayController>();
        if (player != null)
        {
            player.enabled = false;
        }

        Rigidbody2D rb = playerObject.GetComponentInParent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void EnablePlayerControl(GameObject playerObject)
    {
        if (playerObject == null)
            return;

        PlayController player = playerObject.GetComponentInParent<PlayController>();
        if (player != null)
        {
            player.enabled = true;
        }
    }

    private void ShowCursorForDialogue()
    {
        previousCursorVisible = Cursor.visible;
        previousCursorLockState = Cursor.lockState;

        if (!showCursorDuringDialogue)
            return;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void RestoreCursorIfNeeded()
    {
        if (!restoreCursorAfterDialogue)
            return;

        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLockState;
    }

    private void SetPortraitSprite(Image portrait, Sprite sprite)
    {
        if (portrait == null)
            return;

        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
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

    private static Image FindImageByName(Transform root, string targetName)
    {
        Transform child = FindChildByName(root, targetName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static TMP_Text FindTextByName(Transform root, string targetName)
    {
        Transform child = FindChildByName(root, targetName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrEmpty(targetName))
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == targetName)
            {
                return child;
            }
        }

        return null;
    }
}
