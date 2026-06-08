using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageGoal : MonoBehaviour
{
    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "Stage2";

    [Header("Transition")]
    [SerializeField] private float transitionDelay = 1.5f;
    [SerializeField] private GameObject transitionPanel;
    [SerializeField] private bool disablePlayerControl = true;
    [SerializeField] private float loadDelayAfterDialogue = 0.5f;

    [Header("Letter Handoff")]
    [SerializeField] private bool playLetterHandoff = true;
    [SerializeField] private LetterHandoffWorldAnimator letterHandoffAnimator;
    [SerializeField] private Transform npcHandoffTarget;

    [Header("Visual Novel Dialogue")]
    [SerializeField] private bool useDialogueBeforeClear = true;
    [SerializeField] private VisualNovelDialogueController dialogueController;
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private Sprite dandiPortraitSprite;
    [SerializeField] private Sprite npcPortraitSprite;
    [SerializeField] private bool autoAdvanceDialogue = true;
    [SerializeField] private float autoAdvanceDialogueDelay = 9f;

    [Header("Interaction")]
    [SerializeField] private bool requireInteractKey = false;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject interactHint;

    private bool isCleared;
    private bool playerInRange;
    private GameObject currentPlayerObject;

    private void Awake()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        if (interactHint != null)
        {
            interactHint.SetActive(false);
        }

        if (transitionPanel != null)
        {
            transitionPanel.SetActive(false);
        }

        if (npcHandoffTarget == null)
        {
            npcHandoffTarget = transform;
        }
    }

    private void Update()
    {
        if (isCleared || !playerInRange || !requireInteractKey)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            StartClearFlow();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCleared)
            return;

        TryStartClearFlow(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (isCleared || playerInRange)
            return;

        TryStartClearFlow(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GameObject exitingPlayerObject = GetPlayerObject(other);
        if (exitingPlayerObject == null || !exitingPlayerObject.CompareTag("Player"))
            return;

        if (isCleared)
        {
            SetInteractHintActive(false);
            return;
        }

        if (currentPlayerObject == exitingPlayerObject)
        {
            currentPlayerObject = null;
            playerInRange = false;
        }

        SetInteractHintActive(false);
    }

    private void StartClearFlow()
    {
        if (isCleared)
            return;

        isCleared = true;
        GameCursorManager.SetStagePortalCursorActive(true);
        AudioManager.PlaySfx("sfx_stage_clear");
        SetInteractHintActive(false);

        if (disablePlayerControl)
        {
            DisablePlayerControl(currentPlayerObject);
        }

        StartLetterHandoffFlow();
    }

    private void StartLetterHandoffFlow()
    {
        if (!playLetterHandoff)
        {
            StartPostClearFlow();
            return;
        }

        if (letterHandoffAnimator == null)
        {
            letterHandoffAnimator = FindFirstObjectByType<LetterHandoffWorldAnimator>(FindObjectsInactive.Include);
        }

        if (letterHandoffAnimator == null)
        {
            Debug.LogWarning("StageGoal could not find a LetterHandoffWorldAnimator. Continuing without the letter handoff animation.", this);
            StartPostClearFlow();
            return;
        }

        Transform playerTransform = currentPlayerObject != null ? currentPlayerObject.transform : null;
        Transform targetTransform = npcHandoffTarget != null ? npcHandoffTarget : transform;
        letterHandoffAnimator.Play(playerTransform, targetTransform, StartPostClearFlow);
    }

    private void StartPostClearFlow()
    {
        if (useDialogueBeforeClear)
        {
            StartDialogueFlow();
            return;
        }

        StartImmediateSceneTransition();
    }

    private void StartDialogueFlow()
    {
        if (dialogueController == null)
        {
            dialogueController = FindFirstObjectByType<VisualNovelDialogueController>(FindObjectsInactive.Include);
        }

        if (dialogueController == null)
        {
            Debug.LogWarning("StageGoal could not find a VisualNovelDialogueController. Loading the next scene without dialogue. Add a VisualNovelDialogueController under the Canvas or assign it here.", this);
            StartCoroutine(LoadNextSceneAfterDelay(loadDelayAfterDialogue));
            return;
        }

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning("StageGoal has no dialogue lines. Loading the next scene without dialogue.", this);
            StartCoroutine(LoadNextSceneAfterDelay(loadDelayAfterDialogue));
            return;
        }

        dialogueController.SetPortraitSprites(dandiPortraitSprite, npcPortraitSprite);
        dialogueController.StartDialogue(dialogueLines, currentPlayerObject, HandleDialogueFinished, autoAdvanceDialogue, autoAdvanceDialogueDelay);
    }

    private void HandleDialogueFinished()
    {
        if (disablePlayerControl)
        {
            DisablePlayerControl(currentPlayerObject);
        }

        StartCoroutine(LoadNextSceneAfterDelay(loadDelayAfterDialogue));
    }

    private void StartImmediateSceneTransition()
    {
        StartCoroutine(LoadNextSceneAfterDelay(transitionDelay));
    }

    private IEnumerator LoadNextSceneAfterDelay(float delay)
    {
        if (transitionPanel != null)
        {
            transitionPanel.SetActive(true);
        }

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("StageGoal nextSceneName is empty. Cannot load the next scene.", this);
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void TryStartClearFlow(Collider2D playerCollider)
    {
        if (!TrySetCurrentPlayer(playerCollider))
            return;

        playerInRange = true;

        if (requireInteractKey)
        {
            SetInteractHintActive(true);
            return;
        }

        StartClearFlow();
    }

    private bool TrySetCurrentPlayer(Collider2D playerCollider)
    {
        currentPlayerObject = GetPlayerObject(playerCollider);
        return currentPlayerObject != null && currentPlayerObject.CompareTag("Player");
    }

    private static GameObject GetPlayerObject(Collider2D playerCollider)
    {
        if (playerCollider == null)
            return null;

        PlayController player = playerCollider.GetComponentInParent<PlayController>();
        if (player != null)
        {
            return player.gameObject;
        }

        return playerCollider.CompareTag("Player") ? playerCollider.gameObject : null;
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

    private void SetInteractHintActive(bool isActive)
    {
        if (interactHint != null)
        {
            interactHint.SetActive(isActive);
        }
    }
}
