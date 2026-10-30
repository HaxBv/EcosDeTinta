using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class AttackController : MonoBehaviour
{
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    [Header("Attack")]
    [SerializeField] private float activeTime = 0.15f;
    [SerializeField] private LayerMask targetLayers;

    private PlayerStats stats;
    private PlayerInputs playerInputs;
    private MovementController movement;
    private Animator characterAnimator;
    private BoxCollider hitbox;

    private Coroutine hitboxRoutine;
    private float nextAttackTime;
    private FormData activeAttackForm;

    private readonly HashSet<Collider> hitTargets = new HashSet<Collider>();

    public event Action<Collider, float> TargetHit;

    private void Awake()
    {
        stats = GetComponentInParent<PlayerStats>();
        playerInputs = GetComponentInParent<PlayerInputs>();
        movement = GetComponentInParent<MovementController>();
        hitbox = GetComponent<BoxCollider>();

        // Encuentra el Animator del objeto Player, aunque esté en un hijo como Sprite.
        characterAnimator = transform.root.GetComponentInChildren<Animator>(true);

        hitbox.isTrigger = true;
        hitbox.enabled = false;

        if (stats == null || playerInputs == null || movement == null)
        {
            Debug.LogError(
                "AttackHitbox debe ser hijo de Player con PlayerStats, PlayerInputs y MovementController.",
                this
            );
        }

        if (characterAnimator == null)
            Debug.LogError("No se encontró el Animator del jugador.", this);
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

        StopHitboxRoutine();
        hitbox.enabled = false;
    }

    private void TryAttack()
    {
        Debug.Log("[AttackController] Input de ataque recibido.", this);

        if (stats == null || stats.CurrentForm == null ||
            movement == null || characterAnimator == null)
        {
            return;
        }

        if (Time.time < nextAttackTime || movement.IsMovementLocked)
            return;

        FormData form = stats.CurrentForm;
        nextAttackTime = Time.time + form.SpeedAttack;

        movement.LockMovement();
        characterAnimator.SetTrigger(AttackHash);

        Debug.Log($"[AttackController] Ataque iniciado: {form.FormName}", this);
    }

    // Llama a este método desde el Animation Event del fotograma de impacto.
    public void ActivateHitboxFromAnimation()
    {
        if (stats == null || stats.CurrentForm == null || movement == null)
            return;

        // Apaga y reinicia el collider si el evento vuelve a llamarse rápidamente.
        StopHitboxRoutine();
        hitbox.enabled = false;
        hitTargets.Clear();

        activeAttackForm = stats.CurrentForm;

        hitbox.size = activeAttackForm.AttackBoxSize;

        Vector3 offset = activeAttackForm.AttackOffset;
        offset.x *= movement.FacingDirection;
        transform.localPosition = offset;

        hitboxRoutine = StartCoroutine(HitboxRoutine());

        Debug.Log("[AttackController] Animation Event recibido; collider reiniciado.", this);

    }

    private IEnumerator HitboxRoutine()
    {
        // Permite que Unity registre el apagado antes de reactivarlo.
        yield return new WaitForFixedUpdate();

        hitbox.enabled = true;
        Debug.Log($"[AttackController] Collider activado: {hitbox.size}", this);

        yield return new WaitForSeconds(activeTime);

        hitbox.enabled = false;
        hitboxRoutine = null;
    }

    // Opcional: llama a esto con otro Animation Event al terminar la animación.
   
    public void EndAttackAnimation()
    {
        StopHitboxRoutine();

        if (hitbox != null)
            hitbox.enabled = false;

        movement?.UnlockMovement();
    }

    private void StopHitboxRoutine()
    {
        if (hitboxRoutine == null)
            return;

        StopCoroutine(hitboxRoutine);
        hitboxRoutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hitbox.enabled || activeAttackForm == null)
            return;

        bool isTargetLayer =
            (targetLayers.value & (1 << other.gameObject.layer)) != 0;

        if (!isTargetLayer || !hitTargets.Add(other))
            return;

        Debug.Log(
            $"Ataque de {activeAttackForm.FormName} detectó: {other.name}",
            other
        );

        TargetHit?.Invoke(other, activeAttackForm.AttackDamage);
    }
    public void CancelAttack()
    {
        StopHitboxRoutine();

        if (hitbox != null)
            hitbox.enabled = false;

        activeAttackForm = null;

        if (characterAnimator != null)
            characterAnimator.ResetTrigger(AttackHash);

        movement?.UnlockMovement();
    }

}