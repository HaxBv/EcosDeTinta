using UnityEngine;
using Sirenix.OdinInspector;

public class PlayerStats : MonoBehaviour
{
    [ShowInInspector, ReadOnly]
    public FormData CurrentForm { get; private set; }

    [ShowInInspector, ReadOnly]
    public float WalkSpeed => CurrentForm != null ? CurrentForm.WalkSpeed : 0f;

    [ShowInInspector, ReadOnly]
    public float SprintSpeed => CurrentForm != null ? CurrentForm.SprintSpeed : 0f;

    [ShowInInspector, ReadOnly]
    public float Acceleration => CurrentForm != null ? CurrentForm.Acceleration : 0f;

    [ShowInInspector, ReadOnly]
    public float JumpHeight => CurrentForm != null ? CurrentForm.JumpHeight : 0f;

    [ShowInInspector, ReadOnly]
    public float AttackDamage => CurrentForm != null ? CurrentForm.AttackDamage : 0f;

    [ShowInInspector, ReadOnly]
    public float Health => CurrentForm != null ? CurrentForm.Health : 0f;

    [ShowInInspector, ReadOnly]
    public float Defense => CurrentForm != null ? CurrentForm.Defense : 0f;

    public void SetForm(FormData form)
    {
        if (form == null)
        {
            Debug.LogError("No se asignó un FormData.", this);
            return;
        }

        CurrentForm = form;
        Debug.Log($"PlayerStats actualizado: {form.FormName}", this);
    }
}