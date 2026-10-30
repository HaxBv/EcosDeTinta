using Sirenix.OdinInspector;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [ShowInInspector, ReadOnly, FoldoutGroup("Valores activos")]
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
    public float MaxStamina => CurrentForm != null ? CurrentForm.MaxStamina : 0f;

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

    public void SetForm(FormData form)
    {
        if (form == null)
        {
            Debug.LogError("No se asignó un FormData.", this);
            return;
        }

        float previousStaminaPercent = CurrentForm == null
            ? 1f
            : StaminaNormalized;

        CurrentForm = form;
        CurrentStamina = MaxStamina * previousStaminaPercent;
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