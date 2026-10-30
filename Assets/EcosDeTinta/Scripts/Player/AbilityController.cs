using System;
using System.Collections.Generic;
using UnityEngine;

public class AbilityController : MonoBehaviour
{
    private static readonly int AbilityHash = Animator.StringToHash("Ability");

    [Header("Habilidad")]
    [SerializeField] private float abilityCooldown = 4f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Alcances")]
    [SerializeField] private float inkmanRadius = 2.5f;
    [SerializeField] private Vector3 shadowBoxSize = new Vector3(2.5f, 2f, 1.5f);
    [SerializeField] private float shadowForwardDistance = 1.5f;
    [SerializeField] private float maximwolfRadius = 3f;
    [SerializeField] private float maximwolfStunDuration = 1.5f;
    [SerializeField] private float zeroLaserRange = 8f;

    private PlayerStats stats;
    private PlayerInputs playerInputs;
    private MovementController movement;
    private Animator characterAnimator;

    private float nextAbilityTime;
    private readonly HashSet<Collider> hitTargets = new HashSet<Collider>();

    public event Action<Collider, float> TargetHit;
    public event Action<Collider, float> TargetStunned;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        playerInputs = GetComponent<PlayerInputs>();
        movement = GetComponent<MovementController>();
        characterAnimator = GetComponentInChildren<Animator>(true);

        if (stats == null || playerInputs == null || movement == null)
            Debug.LogError("AbilityController necesita PlayerStats, PlayerInputs y MovementController.", this);

        if (characterAnimator == null)
            Debug.LogError("No se encontró el Animator del jugador.", this);
    }

    private void OnEnable()
    {
        if (playerInputs != null)
            playerInputs.AbilityPressed += TryUseAbility;
    }

    private void OnDisable()
    {
        if (playerInputs != null)
            playerInputs.AbilityPressed -= TryUseAbility;
    }

    private void TryUseAbility()
    {
        if (stats == null || stats.CurrentForm == null || characterAnimator == null)
            return;

        if (Time.time < nextAbilityTime)
            return;

        nextAbilityTime = Time.time + abilityCooldown;
        characterAnimator.SetTrigger(AbilityHash);

        hitTargets.Clear();

        switch (stats.CurrentForm.FormName)
        {
            case "Inkman":
                UseInkmanAbility();
                break;

            case "Shadow":
                UseShadowAbility();
                break;

            case "Maximwolf":
                UseMaximwolfAbility();
                break;

            case "Zero":
                UseZeroAbility();
                break;

            default:
                Debug.LogWarning(
                    $"No hay una habilidad definida para {stats.CurrentForm.FormName}.",
                    this
                );
                break;
        }
    }

    private Vector3 GetAbilityOrigin()
    {
        return transform.position + Vector3.up;
    }

    private void UseInkmanAbility()
    {
        Collider[] targets = Physics.OverlapSphere(
            GetAbilityOrigin(),
            inkmanRadius,
            targetLayers
        );

        foreach (Collider target in targets)
            ApplyDamage(target, 1f);
    }

    private void UseShadowAbility()
    {
        Vector3 center = GetAbilityOrigin()
            + Vector3.right * movement.FacingDirection * shadowForwardDistance;

        Collider[] targets = Physics.OverlapBox(
            center,
            shadowBoxSize * 0.5f,
            Quaternion.identity,
            targetLayers
        );

        foreach (Collider target in targets)
            ApplyDamage(target, 1.25f);
    }

    private void UseMaximwolfAbility()
    {
        Collider[] targets = Physics.OverlapSphere(
            GetAbilityOrigin(),
            maximwolfRadius,
            targetLayers
        );

        foreach (Collider target in targets)
        {
            ApplyDamage(target, 1f);
            TargetStunned?.Invoke(target, maximwolfStunDuration);
        }
    }

    private void UseZeroAbility()
    {
        Vector3 direction = Vector3.right * movement.FacingDirection;

        if (Physics.Raycast(
            GetAbilityOrigin(),
            direction,
            out RaycastHit hit,
            zeroLaserRange,
            targetLayers,
            QueryTriggerInteraction.Collide))
        {
            ApplyDamage(hit.collider, 1.2f);
        }
    }

    private void ApplyDamage(Collider target, float damageMultiplier)
    {
        if (target == null || !hitTargets.Add(target))
            return;

        float damage = stats.CurrentForm.AttackDamage * damageMultiplier;

        Debug.Log(
            $"Habilidad de {stats.CurrentForm.FormName} alcanzó a {target.name}. Daño: {damage}",
            target
        );

        TargetHit?.Invoke(target, damage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 origin = transform.position + Vector3.up;

        Gizmos.DrawWireSphere(origin, inkmanRadius);
        Gizmos.DrawWireSphere(origin, maximwolfRadius);

        int facing = movement != null ? movement.FacingDirection : 1;
        Vector3 shadowCenter =
            origin + Vector3.right * facing * shadowForwardDistance;

        Gizmos.DrawWireCube(shadowCenter, shadowBoxSize);
    }
}