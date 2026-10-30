using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum AttackType
    {
        Melee,
        Ranged
    }

    private static readonly int AttackHash = Animator.StringToHash("Attack");

    [Header("Tipo y detección")]
    [SerializeField] private AttackType attackType;
    [SerializeField] private Transform player;
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Vida y ataque")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Hitbox de melee")]
    [SerializeField] private BoxCollider meleeHitbox;
    [SerializeField] private Vector3 hitboxSize = new Vector3(1.2f, 1f, 1f);
    [SerializeField] private Vector3 hitboxOffset = new Vector3(0.8f, 0.5f, 0f);

    [Header("Daño recibido")]
    [SerializeField] private float knockbackDistance = 1.2f;
    [SerializeField] private float knockbackHeight = 0.5f;
    [SerializeField] private float knockbackDuration = 0.25f;
    [SerializeField] private float redFlashDuration = 0.2f;

    private float currentHealth;
    private float nextAttackTime;
    private int facingDirection = 1;
    private bool isAttacking;
    private bool isDead;

    private Color originalColor;
    private Coroutine knockbackRoutine;
    private Coroutine flashRoutine;

    private readonly HashSet<Collider> hitTargets = new HashSet<Collider>();

    // El sistema de vida del jugador podrá escuchar este evento.
    public event Action<Collider, float> TargetHit;

    public float CurrentHealth => currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (meleeHitbox != null)
        {
            meleeHitbox.isTrigger = true;
            meleeHitbox.enabled = false;
        }
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
            else
                Debug.LogWarning("No se encontró un objeto con el tag Player.", this);
        }
    }

    private void Update()
    {
        if (isDead || isAttacking || player == null || animator == null)
            return;

        float distance = Mathf.Abs(player.position.x - transform.position.x);

        if (distance > detectionRange || distance > attackRange)
            return;

        facingDirection = player.position.x >= transform.position.x ? 1 : -1;

        if (Time.time < nextAttackTime)
            return;

        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;
        hitTargets.Clear();

        animator.SetTrigger(AttackHash);
    }

    // Animation Event: fotograma de impacto melee.
    public void ActivateMeleeHitboxFromAnimation()
    {
        if (isDead || attackType != AttackType.Melee || meleeHitbox == null)
            return;

        hitTargets.Clear();
        meleeHitbox.size = hitboxSize;

        Vector3 offset = hitboxOffset;
        offset.x = Mathf.Abs(offset.x) * facingDirection;
        meleeHitbox.transform.localPosition = offset;

        meleeHitbox.enabled = true;
    }

    // Animation Event: después del fotograma activo del golpe.
    public void DeactivateMeleeHitboxFromAnimation()
    {
        if (meleeHitbox != null)
            meleeHitbox.enabled = false;
    }

    // Animation Event: fotograma de impacto a distancia.
    // Por ahora detecta al jugador en rango; luego aquí podemos lanzar un proyectil.
    public void RangedAttackFromAnimation()
    {
        if (isDead || attackType != AttackType.Ranged || player == null)
            return;

        float distance = Mathf.Abs(player.position.x - transform.position.x);

        if (distance <= attackRange)
        {
            Collider playerCollider = player.GetComponentInChildren<Collider>();

            if (playerCollider != null)
                TargetHit?.Invoke(playerCollider, attackDamage);
        }
    }

    // Animation Event: final de la animación de ataque.
    public void FinishAttackAnimation()
    {
        DeactivateMeleeHitboxFromAnimation();
        isAttacking = false;
    }

    public void RegisterMeleeHit(Collider target)
    {
        if (isDead || !isAttacking || target == null)
            return;

        // Evita que el hitbox golpee otros objetos que no sean el jugador.
        if (target.GetComponentInParent<PlayerStats>() == null)
            return;

        if (!hitTargets.Add(target))
            return;

        TargetHit?.Invoke(target, attackDamage);
    }

    // Lo llama el AttackController del jugador cuando golpea el collider del cuerpo.
    public void TakeDamage(float damage, Vector3 attackerPosition)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (spriteRenderer != null)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(RedFlashRoutine());
        }

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        int direction = transform.position.x >= attackerPosition.x ? 1 : -1;
        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction));

        if (currentHealth <= 0f)
        {
            isDead = true;

            if (meleeHitbox != null)
                meleeHitbox.enabled = false;

            Debug.Log($"{name} fue derrotado.", this);
        }
    }

    private IEnumerator RedFlashRoutine()
    {
        float elapsed = 0f;

        while (elapsed < redFlashDuration)
        {
            spriteRenderer.color = Color.Lerp(
                originalColor,
                Color.red,
                elapsed / redFlashDuration
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = originalColor;
        flashRoutine = null;
    }

    private IEnumerator KnockbackRoutine(int direction)
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            float t = elapsed / knockbackDuration;

            Vector3 sideOffset =
                Vector3.right * direction * knockbackDistance * t;

            float jumpArc = Mathf.Sin(t * Mathf.PI) * knockbackHeight;

            transform.position =
                startPosition + sideOffset + Vector3.up * jumpArc;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position =
            startPosition + Vector3.right * direction * knockbackDistance;

        knockbackRoutine = null;
    }
}