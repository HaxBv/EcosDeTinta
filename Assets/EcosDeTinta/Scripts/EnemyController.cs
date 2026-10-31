using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    public enum AttackType { Melee, Ranged }

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    [Header("Tipo de enemigo")]
    [SerializeField] private bool isFlying;
    [SerializeField] private bool isRangedAttacker;

    [Header("Proyectil a distancia")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;

    public bool IsFlying => isFlying;

    [Header("Ataque")]
    [SerializeField] private AttackType attackType;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private Animator animator;
    [SerializeField] private BoxCollider meleeHitbox;
    [SerializeField] private Vector3 meleeHitboxOffset = new Vector3(0.8f, 0.5f, 0f);

    [Header("Vida")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float currentHealth;

    [Header("Aturdimiento")]
    [SerializeField] private float stunDuration = 1f;

    [Header("Seguimiento")]
    [SerializeField] private float followSpeed = 3f;

    [Header("Daño recibido")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float knockbackDistance = 1.2f;
    [SerializeField] private float knockbackHeight = 0.5f;
    [SerializeField] private float knockbackDuration = 0.25f;
    [SerializeField] private float redFlashDuration = 0.2f;
    [SerializeField] private float knockbackHorizontalForce = 4f;
    [SerializeField] private float knockbackUpwardForce = 2.5f;
    private Rigidbody body;
    private Transform player;

    private Quaternion initialRotation;
    private Quaternion targetRotation;
    private int facingDirection = 1;

    private bool isAttacking;
    private bool isDead;
    private bool isStunned;
    private bool playerInAttackRange;

    private float originalAnimatorSpeed;
    private Color originalColor;

    private Coroutine stunRoutine;
    private Coroutine knockbackRoutine;
    private Coroutine flashRoutine;

    private readonly HashSet<PlayerStats> hitPlayers = new HashSet<PlayerStats>();

    public event Action<Collider, float> TargetHit;

    public float CurrentHealth => currentHealth;
    [Header("Muerte")]
    [SerializeField] private int deathFlashCount = 4;
    [SerializeField] private float deathFlashInterval = 0.08f;

    private ObjectPooling objectPool;
    private Coroutine deathRoutine;

    public event Action<EnemyController> ReturnedToPool;

    public void ConfigureForSpawn(Transform playerTarget, ObjectPooling pool)
    {
        player = playerTarget;
        objectPool = pool;
    }

    private void OnEnable()
    {
        body.useGravity = !isFlying;
        currentHealth = maxHealth;

        isDead = false;
        isAttacking = false;
        isStunned = false;
        playerInAttackRange = false;

        hitPlayers.Clear();

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        if (animator != null)
        {
            animator.speed = originalAnimatorSpeed;
            animator.SetBool(AttackHash, false);
            animator.SetBool(WalkHash, false);
        }

        if (meleeHitbox != null)
            meleeHitbox.enabled = false;

        stunRoutine = null;
        knockbackRoutine = null;
        flashRoutine = null;
        deathRoutine = null;
        if (body != null)
            body.linearVelocity = Vector3.zero;
    }
    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        if (body == null)
        {
            Debug.LogError(
                $"{name} necesita un Rigidbody en el mismo objeto que EnemyController.",
                this
            );
            enabled = false;
            return;
        }

        body.isKinematic = false;
        body.useGravity = !isFlying;
        body.constraints = RigidbodyConstraints.FreezeRotationX
                         | RigidbodyConstraints.FreezeRotationZ;

        initialRotation = transform.rotation;
        targetRotation = initialRotation;

        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (animator != null)
            originalAnimatorSpeed = animator.speed;

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
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            player = playerObject.transform;
        else
            Debug.LogError("No se encontró al jugador. Asígnale el tag Player.", this);
    }

    private void Update()
    {
        if (player == null || isDead)
            return;

        UpdateFacingDirection(player.position.x - body.position.x);
    }
    private void FixedUpdate()
    {
        if (body == null)
            return;

        body.MoveRotation(targetRotation);

        if (isDead || player == null || isAttacking)
        {
            StopHorizontalMovement();
            animator?.SetBool(WalkHash, false);
            return;
        }

        // Durante el stun no detenemos el empuje físico.
        if (isStunned)
        {
            animator?.SetBool(WalkHash, false);
            return;
        }

        Vector3 direction = player.position - body.position;
        direction.y = 0f;

        bool isWalking = direction.sqrMagnitude > 0.0001f;
        animator?.SetBool(WalkHash, isWalking);

        Vector3 velocity = body.linearVelocity;

        if (isWalking)
        {
            direction.Normalize();
            velocity.x = direction.x * followSpeed;
            velocity.z = direction.z * followSpeed;
        }
        else
        {
            velocity.x = 0f;
            velocity.z = 0f;
        }

        // Conserva la velocidad vertical para que actúe la gravedad.
        body.linearVelocity = velocity;
    }

    private void StopHorizontalMovement()
    {
        Vector3 velocity = body.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        body.linearVelocity = velocity;
    }

    private void UpdateFacingDirection(float distanceX)
    {
        if (Mathf.Abs(distanceX) < 0.01f)
            return;

        facingDirection = distanceX > 0f ? 1 : -1;

        float yaw = facingDirection < 0 ? 180f : 0f;
        targetRotation = initialRotation * Quaternion.Euler(0f, yaw, 0f);
    }

    // Lo llama EnemyAttackRange al entrar o salir el jugador.
    public void SetPlayerInAttackRange(Transform target, bool isInside)
    {
        if (isDead || target == null)
            return;

        player = target;
        playerInAttackRange = isInside;

        if (isInside)
            UpdateFacingDirection(player.position.x - body.position.x);

        if (isStunned)
            return;

        isAttacking = playerInAttackRange;

        animator?.SetBool(AttackHash, isAttacking);
        animator?.SetBool(WalkHash, !isAttacking);

        if (!isInside)
            DeactivateMeleeHitboxFromAnimation();
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;
        isAttacking = false;

        DeactivateMeleeHitboxFromAnimation();

        if (animator != null)
        {
            animator.SetBool(AttackHash, false);
            animator.SetBool(WalkHash, false);
            animator.speed = 0f;
        }

        yield return new WaitForSeconds(stunDuration);

        if (animator != null)
            animator.speed = originalAnimatorSpeed;

        isStunned = false;

        if (!isDead)
        {
            isAttacking = playerInAttackRange;
            animator?.SetBool(AttackHash, isAttacking);
            animator?.SetBool(WalkHash, !isAttacking && player != null);
        }

        stunRoutine = null;
    }

    // Animation Event melee: en el fotograma de impacto.
    public void ActivateMeleeHitboxFromAnimation()
    {
        if (isDead || isStunned || !isAttacking ||
            attackType != AttackType.Melee || meleeHitbox == null)
            return;

        hitPlayers.Clear();

        // El tamaño y posición del collider se configuran en el Inspector.
        meleeHitbox.transform.localPosition = meleeHitboxOffset;
        meleeHitbox.enabled = true;
    }

    // Animation Event melee: al terminar la ventana de impacto.
    public void DeactivateMeleeHitboxFromAnimation()
    {
        if (meleeHitbox != null)
            meleeHitbox.enabled = false;
    }

    // Animation Event ranged: en el fotograma del impacto.
    public void RangedAttackFromAnimation()
    {
        if (isDead || isStunned || !isAttacking ||
            !isRangedAttacker || player == null)
        {
            return;
        }

        if (projectilePrefab == null || projectileSpawnPoint == null)
        {
            Debug.LogError(
                $"Asigna el prefab y el punto de lanzamiento del proyectil en {name}.",
                this
            );
            return;
        }

        EnemyProjectile projectile = Instantiate(
            projectilePrefab,
            projectileSpawnPoint.position,
            projectileSpawnPoint.rotation
        );

        projectile.Launch(player);
    }

    // Animation Event al terminar la animación.
    public void FinishAttackAnimation()
    {
        DeactivateMeleeHitboxFromAnimation();
    }

    public void RegisterMeleeHit(Collider target)
    {
        if (isDead || isStunned || !isAttacking || target == null)
            return;

        PlayerStats playerStats = target.GetComponentInParent<PlayerStats>();

        if (playerStats == null || !hitPlayers.Add(playerStats))
            return;

        playerStats.TakeDamage(attackDamage, transform.position);
        TargetHit?.Invoke(target, attackDamage);
    }
    public void TakeDamage(float damage, Vector3 attackerPosition)
    {
        if (isDead || damage <= 0f)
            return;

        bool applyHitReaction = !isStunned;
        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (spriteRenderer != null)
        {
            if (flashRoutine != null)
                StopCoroutine(flashRoutine);

            flashRoutine = StartCoroutine(RedFlashRoutine());
        }

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        SoundManager.Instance.PlaySFX(SoundType.SFX, 4);

        // Los golpes repetidos durante el stun no reinician la reacción.
        if (!applyHitReaction)
            return;

        stunRoutine = StartCoroutine(StunRoutine());

        int direction = transform.position.x >= attackerPosition.x ? 1 : -1;

        // Quitamos el movimiento horizontal anterior antes de aplicar el nuevo empuje.
        Vector3 velocity = body.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        body.linearVelocity = velocity;

        body.AddForce(
            Vector3.right * direction * knockbackHorizontalForce
            + Vector3.up * knockbackUpwardForce,
            ForceMode.Impulse
        );
    }
    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        isAttacking = false;
        isStunned = false;
        playerInAttackRange = false;

        if (stunRoutine != null)
            StopCoroutine(stunRoutine);

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        DeactivateMeleeHitboxFromAnimation();

        if (animator != null)
        {
            animator.speed = originalAnimatorSpeed;
            animator.SetBool(AttackHash, false);
            animator.SetBool(WalkHash, false);
        }

        deathRoutine = StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        for (int i = 0; i < deathFlashCount; i++)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;

            yield return new WaitForSeconds(deathFlashInterval);

            if (spriteRenderer != null)
                spriteRenderer.color = originalColor;

            yield return new WaitForSeconds(deathFlashInterval);
        }

        if (objectPool != null)
            objectPool.ReturnObject(gameObject);
        else
            gameObject.SetActive(false);

        ReturnedToPool?.Invoke(this);
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
        Vector3 startPosition = body.position;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            float t = elapsed / knockbackDuration;
            float jumpArc = Mathf.Sin(t * Mathf.PI) * knockbackHeight;

            Vector3 position =
                startPosition
                + Vector3.right * direction * knockbackDistance * t
                + Vector3.up * jumpArc;

            body.MovePosition(position);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        body.MovePosition(
            startPosition + Vector3.right * direction * knockbackDistance
        );

        knockbackRoutine = null;
    }
}