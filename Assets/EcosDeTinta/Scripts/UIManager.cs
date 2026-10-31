using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Rellenos de las barras")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Image staminaFill;
    [SerializeField] private TMP_Text bossCountdownText;

    public void UpdateBossCountdown(float secondsRemaining)
    {
        if (bossCountdownText == null)
            return;

        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, secondsRemaining));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        bossCountdownText.text = $"{minutes:00}:{seconds:00}";
        bossCountdownText.gameObject.SetActive(true);
    }

    public void HideBossCountdown()
    {
        if (bossCountdownText != null)
            bossCountdownText.gameObject.SetActive(false);
    }
    private void Awake()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        ConfigureFillImage(healthFill);
        ConfigureFillImage(staminaFill);

        if (playerStats == null)
            Debug.LogError("UIManager no encontró PlayerStats.", this);
    }

    private void Update()
    {
        if (playerStats == null || playerStats.CurrentForm == null)
            return;

        float maxHealth = playerStats.CurrentForm.Health;
        UpdateBar(healthFill, playerStats.CurrentHealth, maxHealth);
        UpdateBar(staminaFill, playerStats.CurrentStamina, playerStats.MaxStamina);
    }

    private void ConfigureFillImage(Image fill)
    {
        if (fill == null)
            return;

        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private void UpdateBar(Image fill, float currentValue, float maxValue)
    {
        if (fill == null)
            return;

        fill.fillAmount = maxValue > 0f
            ? Mathf.Clamp01(currentValue / maxValue)
            : 0f;
    }
}