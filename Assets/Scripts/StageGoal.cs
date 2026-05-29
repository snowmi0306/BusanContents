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

    [Header("Visual Novel Dialogue")]
    [SerializeField] private bool useDialogueBeforeClear = true;
    [SerializeField] private VisualNovelDialogueController dialogueController;
    [SerializeField] private DialogueLine[] dialogueLines;
    [SerializeField] private Sprite dandiPortraitSprite;
    [SerializeField] private Sprite npcPortraitSprite;

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

        if (!other.CompareTag("Player"))
            return;

        currentPlayerObject = GetPlayerObject(other);
        playerInRange = true;

        if (requireInteractKey)
        {
            SetInteractHintActive(true);
            return;
        }

        StartClearFlow();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (isCleared)
        {
            SetInteractHintActive(false);
            return;
        }

        GameObject exitingPlayerObject = GetPlayerObject(other);
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
        SetInteractHintActive(false);

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
            dialogueController = FindFirstObjectByType<VisualNovelDialogueController>();
        }

        if (dialogueController == null)
        {
            Debug.LogWarning("StageGoal could not find a VisualNovelDialogueController. Loading the next scene without dialogue.", this);
            DisablePlayerControl(currentPlayerObject);
            StartCoroutine(LoadNextSceneAfterDelay(loadDelayAfterDialogue));
            return;
        }

        dialogueController.SetPortraitSprites(dandiPortraitSprite, npcPortraitSprite);
        dialogueController.StartDialogue(dialogueLines, currentPlayerObject, HandleDialogueFinished);
    }

    private void HandleDialogueFinished()
    {
        DisablePlayerControl(currentPlayerObject);
        StartCoroutine(LoadNextSceneAfterDelay(loadDelayAfterDialogue));
    }

    private void StartImmediateSceneTransition()
    {
        if (disablePlayerControl)
        {
            DisablePlayerControl(currentPlayerObject);
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


    private GameObject GetPlayerObject(Collider2D playerCollider)
    {
        if (playerCollider == null)
            return null;

        PlayController player = playerCollider.GetComponentInParent<PlayController>();
        if (player != null)
        {
            return player.gameObject;
        }

        return playerCollider.gameObject;
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
