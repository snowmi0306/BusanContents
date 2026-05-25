using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayController player;
    [SerializeField] private Image[] heartImages;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Text staminaText;
    [SerializeField] private bool consumeFromRight = true;

    [Header("Letter HUD")]
    [SerializeField] private PlayerLetterInventory playerInventory;
    [SerializeField] private Image[] letterSlotImages;
    [SerializeField] private Sprite letterIconSprite;
    [SerializeField] private TMP_Text letterCountText;
    [SerializeField] private Color collectedLetterColor = Color.white;
    [SerializeField] private Color uncollectedLetterColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    [Header("Heart Fallback")]
    [SerializeField] private Color fullHeartColor = Color.white;
    [SerializeField] private Color emptyHeartColor = new Color(1f, 1f, 1f, 0.2f);

    private int cachedHealth = int.MinValue;
    private int cachedStamina = int.MinValue;
    private int cachedLetterCurrent = int.MinValue;

    private void Awake()
    {
        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = player != null ? player.GetMaxStamina() : 100f;
        }

        ApplyLetterIconSprite();
        ForceRefresh();
    }

    private void OnEnable()
    {
        ResolveLetterInventoryIfNeeded();

        if (playerInventory != null)
        {
            playerInventory.OnLetterCountChanged += HandleLetterChanged;
            RefreshLetterUI(playerInventory.GetCurrentLetterCount(), true);
        }
        else
        {
            RefreshLetterUI(0, true);
        }
    }

    private void OnDisable()
    {
        if (playerInventory != null)
        {
            playerInventory.OnLetterCountChanged -= HandleLetterChanged;
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        int currentHealth = player.GetCurrentHealth();
        int currentStamina = Mathf.RoundToInt(player.GetCurrentStamina());

        if (currentHealth != cachedHealth)
        {
            cachedHealth = currentHealth;
            UpdateHealthUI(currentHealth);
        }

        if (currentStamina != cachedStamina)
        {
            cachedStamina = currentStamina;
            UpdateStaminaUI(currentStamina);
        }
    }

    private void ForceRefresh()
    {
        cachedHealth = int.MinValue;
        cachedStamina = int.MinValue;
        cachedLetterCurrent = int.MinValue;
    }

    private void ResolveLetterInventoryIfNeeded()
    {
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerLetterInventory>();
        }
    }

    private void ApplyLetterIconSprite()
    {
        if (letterIconSprite == null || letterSlotImages == null)
        {
            return;
        }

        for (int i = 0; i < letterSlotImages.Length; i++)
        {
            if (letterSlotImages[i] != null)
            {
                letterSlotImages[i].sprite = letterIconSprite;
            }
        }
    }

    private void HandleLetterChanged(int current, int required)
    {
        RefreshLetterUI(current, false);
    }

    private void RefreshLetterUI(int current, bool force)
    {
        int safeCurrent = Mathf.Max(0, current);

        if (!force && safeCurrent == cachedLetterCurrent)
        {
            return;
        }

        cachedLetterCurrent = safeCurrent;

        if (letterCountText != null)
        {
            letterCountText.text = $"x{safeCurrent}";
        }

        UpdateLetterSlotVisuals(safeCurrent);
    }

    private void UpdateLetterSlotVisuals(int collectedCount)
    {
        if (letterSlotImages == null || letterSlotImages.Length == 0)
        {
            return;
        }

        for (int i = 0; i < letterSlotImages.Length; i++)
        {
            Image slotImage = letterSlotImages[i];
            if (slotImage == null)
            {
                continue;
            }

            bool isCollected = i < collectedCount;
            slotImage.color = isCollected ? collectedLetterColor : uncollectedLetterColor;
        }
    }

    private void UpdateHealthUI(int currentHealth)
    {
        if (heartImages == null)
        {
            return;
        }

        for (int i = 0; i < heartImages.Length; i++)
        {
            Image heart = heartImages[i];
            if (heart == null)
            {
                continue;
            }

            int heartThreshold = consumeFromRight ? (heartImages.Length - i) : (i + 1);
            bool isFilled = currentHealth >= heartThreshold;

            if (fullHeartSprite != null && emptyHeartSprite != null)
            {
                heart.sprite = isFilled ? fullHeartSprite : emptyHeartSprite;
            }
            else
            {
                heart.color = isFilled ? fullHeartColor : emptyHeartColor;
            }
        }
    }

    private void UpdateStaminaUI(int currentStamina)
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }

        if (staminaText != null)
        {
            int maxStamina = player != null ? Mathf.RoundToInt(player.GetMaxStamina()) : 100;
            staminaText.text = $"{currentStamina} / {maxStamina}";
        }
    }
}