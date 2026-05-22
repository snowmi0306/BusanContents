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

    [Header("Heart Fallback")]
    [SerializeField] private Color fullHeartColor = Color.white;
    [SerializeField] private Color emptyHeartColor = new Color(1f, 1f, 1f, 0.2f);

    private int cachedHealth = int.MinValue;
    private int cachedStamina = int.MinValue;

    private void Awake()
    {
        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = player != null ? player.GetMaxStamina() : 100f;
        }

        ForceRefresh();
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