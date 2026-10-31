using Sirenix.OdinInspector;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "FormData", menuName = "EcosDeTinta/FormData")]
public class FormData : ScriptableObject
{
    [Header("Identidad y apariencia")]
    [SerializeField] private string formName;
    [SerializeField] private Sprite formSprite;
    [SerializeField] private RuntimeAnimatorController animator;

    [Header("Movimiento")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float jumpHeight;
    [SerializeField] private float maxStamina;
    [SerializeField] private float sprintStaminaCostPerSecond;
    [SerializeField] private float staminaRecoveryPerSecond;

    [Header("Tipo de ataque")]
    [SerializeField] private AttackType attackType = AttackType.Melee;

    [Header("Combate")]
    [SerializeField] private float attackDamage;

    [Tooltip("Segundos entre ataques. Un valor menor permite atacar más rápido.")]
    [SerializeField] private float speedAttack;

    [Header("Ataque cuerpo a cuerpo")]
    [ShowIf(nameof(IsMelee))]
    [SerializeField] private Vector3 attackBoxSize = Vector3.one;

    [ShowIf(nameof(IsMelee))]
    [SerializeField] private Vector3 attackOffset = new Vector3(0.8f, 0f, 0f);

    [Header("Ataque a distancia")]
    [ShowIf(nameof(IsRanged))]
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private RuntimeAnimatorController animatorProyectile;
    [ShowIf(nameof(IsRanged))]
    [SerializeField] private float projectileSpeed = 10f;

    [ShowIf(nameof(IsRanged))]
    [SerializeField] private float projectileLifetime = 2f;

    [ShowIf(nameof(IsRanged))]
    [SerializeField]
    private Vector3 projectileColliderSize =
        new Vector3(0.5f, 0.5f, 0.5f);

    [Header("Defensa")]
    [SerializeField] private float health;
    [SerializeField] private float defense;






    private bool IsMelee => attackType == AttackType.Melee;
    private bool IsRanged => attackType == AttackType.Ranged;

    [Header("Pérdida de control")]
    [SerializeField, Min(0f)] private float controlLossMultiplier = 1f;





    #region Getters
    public string FormName => formName;
    public Sprite FormSprite => formSprite;
    public RuntimeAnimatorController Animator => animator;

    public float WalkSpeed => walkSpeed;
    public float SprintSpeed => sprintSpeed;
    public float Acceleration => acceleration;
    public float JumpHeight => jumpHeight;

    public AttackType TypeOfAttack => attackType;
    public float AttackDamage => attackDamage;
    public float SpeedAttack => speedAttack;

    public Vector3 AttackBoxSize => attackBoxSize;
    public Vector3 AttackOffset => attackOffset;

    public Sprite ProjectileSprite => projectileSprite;

    public RuntimeAnimatorController AnimatorProyectile => animatorProyectile;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileLifetime => projectileLifetime;
    public Vector3 ProjectileColliderSize => projectileColliderSize;

    public float Health => health;
    public float Defense => defense;

    public float MaxStamina => maxStamina;
    public float SprintStaminaCostPerSecond => sprintStaminaCostPerSecond;
    public float StaminaRecoveryPerSecond => staminaRecoveryPerSecond;

    public float ControlLossMultiplier => controlLossMultiplier;
    #endregion
}