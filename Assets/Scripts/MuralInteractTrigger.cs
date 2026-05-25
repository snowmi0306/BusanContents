using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class MuralInteractTrigger : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject interactHint;
    [SerializeField] private GameObject notEnoughLetterHint;
    [SerializeField] private float notEnoughHintDuration = 1.2f;
    [SerializeField] private bool consumeLettersOnUse = false;
    [SerializeField, Min(0)] private int lettersToConsume = 3;
    [SerializeField] private UnityEvent onLettersRequirementMet;

    private PlayerLetterInventory currentPlayerInventory;
    private bool isPlayerInside;
    private Coroutine notEnoughHintRoutine;

    private void Awake()
    {
        SetHintActive(interactHint, false);
        SetHintActive(notEnoughLetterHint, false);
    }

    private void Update()
    {
        if (!isPlayerInside || currentPlayerInventory == null)
        {
            return;
        }

        if (!Input.GetKeyDown(interactKey))
        {
            return;
        }

        if (!currentPlayerInventory.HasRequiredLetters())
        {
            Debug.Log("편지 아이콘 부족");
            ShowNotEnoughLetterHint();
            return;
        }

        if (consumeLettersOnUse)
        {
            int consumeAmount = Mathf.Max(0, lettersToConsume);
            bool consumed = currentPlayerInventory.ConsumeLetters(consumeAmount);
            if (!consumed)
            {
                Debug.Log("편지 아이콘 부족");
                ShowNotEnoughLetterHint();
                return;
            }
        }

        SetHintActive(interactHint, false);
        onLettersRequirementMet?.Invoke();
        Debug.Log("편지 조건 충족: 벽화 상호작용 가능");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || !other.CompareTag("Player"))
        {
            return;
        }

        PlayerLetterInventory inventory = other.GetComponentInParent<PlayerLetterInventory>();
        if (inventory == null)
        {
            return;
        }

        currentPlayerInventory = inventory;
        isPlayerInside = true;
        SetHintActive(interactHint, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null || !other.CompareTag("Player"))
        {
            return;
        }

        if (currentPlayerInventory != null)
        {
            PlayerLetterInventory exitingInventory = other.GetComponentInParent<PlayerLetterInventory>();
            if (exitingInventory != currentPlayerInventory)
            {
                return;
            }
        }

        isPlayerInside = false;
        currentPlayerInventory = null;
        SetHintActive(interactHint, false);
    }

    private void ShowNotEnoughLetterHint()
    {
        if (notEnoughLetterHint == null)
        {
            return;
        }

        if (notEnoughHintRoutine != null)
        {
            StopCoroutine(notEnoughHintRoutine);
        }

        notEnoughHintRoutine = StartCoroutine(ShowNotEnoughLetterHintRoutine());
    }

    private IEnumerator ShowNotEnoughLetterHintRoutine()
    {
        SetHintActive(notEnoughLetterHint, true);
        float duration = Mathf.Max(0f, notEnoughHintDuration);
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        SetHintActive(notEnoughLetterHint, false);
        notEnoughHintRoutine = null;
    }

    private static void SetHintActive(GameObject target, bool active)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(active);
    }
}
