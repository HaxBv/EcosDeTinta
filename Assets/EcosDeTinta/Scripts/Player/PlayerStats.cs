using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Daño recibido")]
    [SerializeField] private SpriteRenderer damageSpriteRenderer;
    [SerializeField, Min(0f)] private float damageImmunityDuration = 1.5f;
    [SerializeField, Min(0f)] private float redFlashDuration = 0.2f;

    public FormData CurrentForm { get; private set; }

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float WalkSpeed => CurrentForm != null ? CurrentForm.WalkSpeed : 0f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float SprintSpeed => CurrentForm != null ? CurrentForm.SprintSpeed : 0f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float Acceleration => CurrentForm != null ? CurrentForm.Acceleration : 0f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float JumpHeight => CurrentForm != null ? CurrentForm.JumpHeight : 0f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float AttackDamage => CurrentForm != null ? CurrentForm.AttackDamage : 0f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float SpeedAttack => CurrentForm != null ? CurrentForm.SpeedAttack : 0f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float MaxHealth => CurrentForm != null ? CurrentForm.Health : 0f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float Defense => CurrentForm != null ? CurrentForm.Defense : 0f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float CurrentHealth { get; private set; }

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float HealthNormalized =>
        MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float MaxStamina =>
        CurrentForm != null ? CurrentForm.MaxStamina : 0f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float SprintStaminaCostPerSecond =>
        CurrentForm != null ? CurrentForm.SprintStaminaCostPerSecond : 0f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float StaminaRecoveryPerSecond =>
        CurrentForm != null ? CurrentForm.StaminaRecoveryPerSecond : 0f;

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float CurrentStamina { get; private set; }

    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
    public float StaminaNormalized =>
        MaxStamina > 0f ? CurrentStamina / MaxStamina : 0f;

    public bool IsDead => CurrentForm != null && CurrentHealth <= 0f;
    public bool IsDamageImmune => Time.time < nextDamageAllowedTime;

    public event Action<float, float> HealthChanged;
    public event Action<float, Vector3> DamageTaken;
    public event Action Died;

    private Color originalSpriteColor = Color.white;
    private float nextDamageAllowedTime;
    private Coroutine damageFlashRoutine;

    private void Awake()
    {
        if (damageSpriteRenderer == null)
            damageSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (damageSpriteRenderer != null)
            originalSpriteColor = damageSpriteRenderer.color;
        else
            Debug.LogWarning(
                "PlayerStats no encontró un SpriteRenderer para el destello de daño.",
                this
            );
    }

    private void OnDisable()
    {
        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
            damageFlashRoutine = null;
        }

        if (damageSpriteRenderer != null)
            damageSpriteRenderer.color = originalSpriteColor;
    }

    public void SetForm(FormData form)
    {
        if (form == null)
        {
            Debug.LogError("No se asignó un FormData.", this);
            return;
        }

        float healthPercent = CurrentForm == null
            ? 1f
            : (MaxHealth > 0f ? HealthNormalized : 0f);

        float staminaPercent = CurrentForm == null
            ? 1f
            : (MaxStamina > 0f ? StaminaNormalized : 0f);

        CurrentForm = form;

        CurrentHealth = MaxHealth * healthPercent;
        CurrentStamina = MaxStamina * staminaPercent;

        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void TakeDamage(float damage, Vector3 attackerPosition)
    {
        if (CurrentForm == null ||
            IsDead ||
            damage <= 0f ||
            IsDamageImmune)
        {
            return;
        }

        // Defense se interpreta como porcentaje: 20 reduce el daño un 20%.
        float defensePercent = Mathf.Clamp(Defense, 0f, 100f);
        float finalDamage = damage * (1f - defensePercent / 100f);

        if (finalDamage <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - finalDamage);
        nextDamageAllowedTime = Time.time + damageImmunityDuration;

        StartDamageFlash();

        DamageTaken?.Invoke(finalDamage, attackerPosition);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);

        SoundManager.Instance.PlaySFX(SoundType.SFX, 3);

        if (CurrentHealth <= 0f)
            Died?.Invoke();
    }

    private void StartDamageFlash()
    {
        if (damageSpriteRenderer == null)
            return;

        if (damageFlashRoutine != null)
            StopCoroutine(damageFlashRoutine);

        damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        float duration = Mathf.Max(0.01f, redFlashDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            damageSpriteRenderer.color = Color.Lerp(
                originalSpriteColor,
                Color.red,
                elapsed / duration
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        damageSpriteRenderer.color = originalSpriteColor;
        damageFlashRoutine = null;
    }

    public void Heal(float amount)
    {
        if (CurrentForm == null || IsDead || amount <= 0f)
            return;

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void ConsumeStamina(float amount)
    {
        CurrentStamina = Mathf.Max(0f, CurrentStamina - Mathf.Max(0f, amount));
    }

    public void RecoverStamina(float amount)
    {
        CurrentStamina = Mathf.Min(
            MaxStamina,
            CurrentStamina + Mathf.Max(0f, amount)
        );
    }
}