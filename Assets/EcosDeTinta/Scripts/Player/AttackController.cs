using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class AttackController : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float activeTime = 0.15f;
    [SerializeField] private LayerMask targetLayers;

    private PlayerStats stats;
    private PlayerInputs playerInputs;
    private MovementController movement;
    private BoxCollider hitbox;
    private Coroutine attackRoutine;
    private float nextAttackTime;

    private readonly HashSet<Collider> hitTargets = new HashSet<Collider>();

    public event Action<Collider, float> TargetHit;

    private void Awake()
    {
        stats = GetComponentInParent<PlayerStats>();
        playerInputs = GetComponentInParent<PlayerInputs>();
        movement = GetComponentInParent<MovementController>();
        hitbox = GetComponent<BoxCollider>();

        hitbox.isTrigger = true;
        hitbox.enabled = false;

        if (stats == null || playerInputs == null || movement == null)
            Debug.LogError("AttackHitbox debe ser hijo de Player con los componentes necesarios.", this);
    }

    private void OnEnable()
    {
        if (playerInputs != null)
            playerInputs.AttackPressed += TryAttack;
    }

    private void OnDisable()
    {
        if (playerInputs != null)
            playerInputs.AttackPressed -= TryAttack;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (hitbox != null)
            hitbox.enabled = false;
    }

    private void TryAttack()
    {
        Debug.Log("[AttackController] Input de ataque recibido.");

        if (stats == null || stats.CurrentForm == null || movement == null)
            return;

        if (Time.time < nextAttackTime || attackRoutine != null)
            return;

        FormData form = stats.CurrentForm;
        nextAttackTime = Time.time + form.SpeedAttack;
        attackRoutine = StartCoroutine(AttackRoutine(form));
    }

    private IEnumerator AttackRoutine(FormData form)
    {
        hitTargets.Clear();

        hitbox.size = form.AttackBoxSize;

        // Se espera que AttackOffset.x sea positivo para atacar hacia la derecha.
        Vector3 attackOffset = form.AttackOffset;
        attackOffset.x *= movement.FacingDirection;
        transform.localPosition = attackOffset;

        hitbox.enabled = true;

        Debug.Log(
            $"[AttackController] Collider activado: {hitbox.enabled}, tamaño: {hitbox.size}"
        );

        yield return new WaitForSeconds(activeTime);

        hitbox.enabled = false;
        attackRoutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        bool isTargetLayer =
            (targetLayers.value & (1 << other.gameObject.layer)) != 0;

        if (!hitbox.enabled || !isTargetLayer || !hitTargets.Add(other))
            return;

        Debug.Log($"Ataque de {stats.CurrentForm.FormName} detectó: {other.name}", other);
        TargetHit?.Invoke(other, stats.CurrentForm.AttackDamage);
    }
}