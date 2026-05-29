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

    [Header("Visual Novel Dialogue")]
    [SerializeField] private bool useDialogueBeforeClear = true;
    [SerializeField] private VisualNovelDialogueController dialogueController;
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private Sprite dandiPortraitSprite;
    [SerializeField] private Sprite npcPortraitSprite;
    [SerializeField] private float loadDelayAfterDialogue = 0.5f;

    [Header("Interaction")]
    [SerializeField] private bool requireInteractKey = false;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject interactHint;

    private bool isCleared;
    private GameObject currentPlayerObject;

    private void Awake()
    {
        if (transitionPanel != null)
        {
            transitionPanel.SetActive(false);
        }

        SetInteractHintActive(false);
    }

    private void Update()
    {
        if (isCleared || !requireInteractKey || currentPlayerObject == null)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            StartClearSequence(currentPlayerObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCleared)
            return;

        if (!other.CompareTag("Player"))
            return;

        currentPlayerObject = other.gameObject;

        if (requireInteractKey)
        {
            SetInteractHintActive(true);
            return;
        }

        StartClearSequence(currentPlayerObject);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (currentPlayerObject == other.gameObject)
        {
            currentPlayerObject = null;
        }

        SetInteractHintActive(false);
    }

    private void StartClearSequence(GameObject playerObject)
    {
        if (isCleared)
            return;

        isCleared = true;
        SetInteractHintActive(false);

        if (useDialogueBeforeClear)
        {
            StartDialogueBeforeClear(playerObject);
            return;
        }

        StartImmediateSceneTransition(playerObject);
    }

    private void StartDialogueBeforeClear(GameObject playerObject)
    {
        VisualNovelDialogueController controller = GetDialogueController();

        if (controller == null)
        {
            Debug.LogWarning("StageGoal could not find a VisualNovelDialogueController. Loading the next scene without dialogue.", this);
            StartCoroutine(LoadNextSceneAfterDelay(loadDelayAfterDialogue));
            return;
        }

        controller.SetPortraitSprites(dandiPortraitSprite, npcPortraitSprite);
        controller.StartDialogue(dialogueLines, playerObject, HandleDialogueFinished);
    }

    private VisualNovelDialogueController GetDialogueController()
    {
        if (dialogueController == null)
        {
            dialogueController = FindFirstObjectByType<VisualNovelDialogueController>();
        }

        return dialogueController;
    }

    private void HandleDialogueFinished()
    {
        StartCoroutine(LoadNextSceneAfterDelay(loadDelayAfterDialogue));
    }

    private void StartImmediateSceneTransition(GameObject playerObject)
    {
        if (disablePlayerControl)
        {
            DisablePlayerControl(playerObject);
        }

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

    private void SetInteractHintActive(bool isActive)
    {
        if (interactHint != null)
        {
            interactHint.SetActive(isActive);
        }
    }
}
