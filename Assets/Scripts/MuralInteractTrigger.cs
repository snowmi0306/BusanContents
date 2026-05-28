using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MuralInteractTrigger : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private GameObject interactHint;
    [SerializeField] private GameObject notEnoughLetterHint;
    [SerializeField] private float notEnoughHintDuration = 1.2f;
    [SerializeField] private bool consumeLettersOnUse = false;
    [SerializeField, Min(0)] private int lettersToConsume = 3;
    [SerializeField] private UnityEvent onLettersRequirementMet;

    [Header("Background Toggle")]
    [SerializeField] private GameObject defaultBackground;
    [SerializeField] private GameObject muralBackground;

    [Header("Interact Hint Transparency")]
    [SerializeField, Range(0f, 1f)] private float defaultInteractHintAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float notEnoughInteractHintAlpha = 0.5f;

    private PlayerLetterInventory currentPlayerInventory;
    private bool isPlayerInside;
    private Coroutine notEnoughHintRoutine;

    private void Awake()
    {
        SetHintActive(interactHint, false);
        SetHintActive(notEnoughLetterHint, false);
        SetHintTransparency(interactHint, defaultInteractHintAlpha);
        SetInitialBackgroundState();
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
            Debug.Log("벽화 조건 불충족");
            ShowNotEnoughLetterHint();
            return;
        }

        if (consumeLettersOnUse)
        {
            int consumeAmount = Mathf.Max(0, lettersToConsume);
            bool consumed = currentPlayerInventory.ConsumeLetters(consumeAmount);

            if (!consumed)
            {
                Debug.Log("벽화 조건 불충족");
                ShowNotEnoughLetterHint();
                return;
            }
        }

        SetHintActive(interactHint, false);
        SetHintTransparency(interactHint, defaultInteractHintAlpha);

        ActivateMuralBackground();
        onLettersRequirementMet?.Invoke();
        Debug.Log("벽화 조건 충족: 벽화 상호작용 완료");
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
        SetHintTransparency(interactHint, defaultInteractHintAlpha);
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
        SetHintActive(notEnoughLetterHint, false);
        SetHintTransparency(interactHint, defaultInteractHintAlpha);

        if (notEnoughHintRoutine != null)
        {
            StopCoroutine(notEnoughHintRoutine);
            notEnoughHintRoutine = null;
        }
    }

    private void ShowNotEnoughLetterHint()
    {
        if (notEnoughHintRoutine != null)
        {
            StopCoroutine(notEnoughHintRoutine);
        }

        notEnoughHintRoutine = StartCoroutine(ShowNotEnoughLetterHintRoutine());
    }

    private IEnumerator ShowNotEnoughLetterHintRoutine()
    {
        SetHintActive(notEnoughLetterHint, true);

        // 조건 불충족 시 Interact Hint를 반투명하게 처리
        SetHintTransparency(interactHint, notEnoughInteractHintAlpha);

        float duration = Mathf.Max(0f, notEnoughHintDuration);

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        SetHintActive(notEnoughLetterHint, false);

        // 일정 시간 후 다시 기본 투명도로 복원
        SetHintTransparency(interactHint, defaultInteractHintAlpha);

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

    private static void SetHintTransparency(GameObject target, float alpha)
    {
        if (target == null)
        {
            return;
        }

        alpha = Mathf.Clamp01(alpha);

        // UI Image, Text 등에 적용
        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }

        // SpriteRenderer가 있는 오브젝트에도 적용
        SpriteRenderer[] spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }

    private void SetInitialBackgroundState()
    {
        if (defaultBackground != null)
        {
            defaultBackground.SetActive(true);
        }

        if (muralBackground != null)
        {
            muralBackground.SetActive(false);
        }
    }

    private void ActivateMuralBackground()
    {
        if (defaultBackground != null)
        {
            defaultBackground.SetActive(false);
        }

        if (muralBackground != null)
        {
            muralBackground.SetActive(true);
        }
    }
}
