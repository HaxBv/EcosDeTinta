using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class AttackController : MonoBehaviour
{
    private static readonly int AttackHash =
        Animator.StringToHash("Attack");

    [Header("Ataque cuerpo a cuerpo")]
    [SerializeField] private float activeTime = 0.15f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Ataque a distancia")]
    [SerializeField] private Transform projectileLaunchPoint;

    private PlayerStats stats;
    private PlayerInputs playerInputs;
    private MovementController movement;
    private Animator characterAnimator;
    private SpriteRenderer playerSpriteRenderer;
    private BoxCollider hitbox;

    private Coroutine hitboxRoutine;
    private float nextAttackTime;
    private FormData activeAttackForm;

    private readonly HashSet<EnemyController> hitEnemies = new();

    public event Action<Collider, float> TargetHit;

    [SerializeField] private float projectileForwardOffset = 0.7f;
    [SerializeField] private float maximwolfDamageMultiplier = 1.5f;
    [SerializeField] private float maximwolfCooldownMultiplier = 0.65f;
    [SerializeField] private float zeroBulletSeparation = 0.12f;

    private OutOfControlManager outOfControlManager;
    private bool maximwolfOutOfControl;
    private bool zeroOutOfControl;

    private void Awake()
    {
        outOfControlManager = GetComponentInParent<OutOfControlManager>();
        stats = GetComponentInParent<PlayerStats>();
        playerInputs = GetComponentInParent<PlayerInputs>();
        movement = GetComponentInParent<MovementController>();
        hitbox = GetComponent<BoxCollider>();

        Transform playerRoot = transform.root;

        characterAnimator = playerRoot.GetComponentInChildren<Animator>(true);
        playerSpriteRenderer =
            playerRoot.GetComponentInChildren<SpriteRenderer>(true);

        hitbox.isTrigger = true;
        hitbox.enabled = false;

        if (stats == null || playerInputs == null || movement == null)
        {
            Debug.LogError(
                "AttackController necesita PlayerStats, PlayerInputs y MovementController en Player.",
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
        if (outOfControlManager != null)
            outOfControlManager.FormEffectChanged += OnOutOfControlFormChanged;
    }

    private void OnDisable()
    {
        if (playerInputs != null)
            playerInputs.AttackPressed -= TryAttack;
        if (outOfControlManager != null)
            outOfControlManager.FormEffectChanged -= OnOutOfControlFormChanged;

        StopHitboxRoutine();

        if (hitbox != null)
            hitbox.enabled = false;

        activeAttackForm = null;
        movement?.UnlockMovement();
    }
    private void OnOutOfControlFormChanged(FormData form, bool isActive)
    {
        if (form == null)
            return;

        if (form.FormName == "Maximwolf")
            maximwolfOutOfControl = isActive;

        if (form.FormName == "Zero")
            zeroOutOfControl = isActive;
    }
    private void TryAttack()
    {
        if (stats == null ||
            stats.CurrentForm == null ||
            movement == null ||
            characterAnimator == null)
        {
            return;
        }

        if (Time.time < nextAttackTime || movement.IsMovementLocked)
            return;

        FormData form = stats.CurrentForm;

        float cooldownMultiplier = maximwolfOutOfControl ? maximwolfCooldownMultiplier: 1f;

        nextAttackTime = Time.time + form.SpeedAttack * cooldownMultiplier;
        movement.LockMovement();
        characterAnimator.SetTrigger(AttackHash);

        Debug.Log($"Ataque iniciado: {form.FormName} ({form.TypeOfAttack})", this);
    }

    // Animation Event en el fotograma de impacto/disparo.
    public void ActivateHitboxFromAnimation()
    {
        if (stats == null || stats.CurrentForm == null || movement == null)
            return;

        StopHitboxRoutine();

        if (hitbox != null)
            hitbox.enabled = false;

        activeAttackForm = stats.CurrentForm;
        hitEnemies.Clear();

        if (activeAttackForm.TypeOfAttack == AttackType.Ranged)
        {
            LaunchProjectile(activeAttackForm);
            return;
        }

        ActivateMeleeHitbox(activeAttackForm);
    }

    private void ActivateMeleeHitbox(FormData form)
    {
        if (hitbox == null)
            return;

        hitbox.size = form.AttackBoxSize;

        Vector3 offset = form.AttackOffset;
        offset.x = Mathf.Abs(offset.x) * movement.FacingDirection;
        transform.localPosition = offset;

        hitboxRoutine = StartCoroutine(MeleeHitboxRoutine());

        Debug.Log($"Collider melee activado: {hitbox.size}", this);



    }

    private IEnumerator MeleeHitboxRoutine()
    {
        // Da tiempo a Unity para registrar que el collider se apagó
        // antes de volver a activarlo.
        yield return new WaitForFixedUpdate();

        hitbox.enabled = true;

        yield return new WaitForSeconds(activeTime);

        hitbox.enabled = false;
        hitboxRoutine = null;
    }
    private void LaunchProjectile(FormData form)
    {
        if (!zeroOutOfControl)
        {
            LaunchSingleProjectile(form, 0f);
            return;
        }

        LaunchSingleProjectile(form, -zeroBulletSeparation);
        LaunchSingleProjectile(form, zeroBulletSeparation);
    }
    private void LaunchSingleProjectile(FormData form, float verticalOffset)
    {
        if (form.ProjectileSprite == null)
        {
            Debug.LogError(
                $"No hay Projectile Sprite asignado en el FormData de {form.FormName}.",
                this
            );
            return;
        }

        if (projectileLaunchPoint == null)
        {
            Debug.LogError("Asigna Projectile Launch Point en AttackController.", this);
            return;
        }

        Vector3 direction = Vector3.right * movement.FacingDirection;

        GameObject projectileObject =
            new GameObject($"{form.FormName}_Projectile");

        Vector3 spawnPosition = projectileLaunchPoint.position;

        // El punto de salida cambia de lado según hacia dónde mira el personaje.
        spawnPosition.x = transform.root.position.x
            + projectileForwardOffset * movement.FacingDirection;

        // Separa verticalmente las balas de Zero.
        spawnPosition.y += verticalOffset;

        projectileObject.transform.position = spawnPosition;

        BoxCollider projectileCollider =
            projectileObject.AddComponent<BoxCollider>();

        projectileCollider.isTrigger = true;
        projectileCollider.size = form.ProjectileColliderSize;

        Rigidbody projectileBody =
            projectileObject.AddComponent<Rigidbody>();

        projectileBody.isKinematic = true;
        projectileBody.useGravity = false;
        projectileBody.constraints = RigidbodyConstraints.FreezeRotation;
        projectileBody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        GameObject visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(projectileObject.transform, false);

        if (playerSpriteRenderer != null)
            visualObject.transform.rotation = playerSpriteRenderer.transform.rotation;

        SpriteRenderer projectileRenderer =
            visualObject.AddComponent<SpriteRenderer>();

        projectileRenderer.sprite = form.ProjectileSprite;
        projectileRenderer.flipX = movement.FacingDirection < 0;

        if (playerSpriteRenderer != null)
        {
            projectileRenderer.sortingLayerID =
                playerSpriteRenderer.sortingLayerID;

            projectileRenderer.sortingOrder =
                playerSpriteRenderer.sortingOrder + 1;
        }

        if (form.AnimatorProyectile != null)
        {
            Animator projectileAnimator =
                visualObject.AddComponent<Animator>();

            projectileAnimator.runtimeAnimatorController =
                form.AnimatorProyectile;

            projectileAnimator.applyRootMotion = false;
        }

        float damage = form.AttackDamage *
            (maximwolfOutOfControl ? maximwolfDamageMultiplier : 1f);

        PlayerProjectile projectile =
            projectileObject.AddComponent<PlayerProjectile>();

        projectile.Initialize(
            direction,
            form.ProjectileSpeed,
            form.ProjectileLifetime,
            damage,
            targetLayers,
            transform.root,
            (collider, hitDamage) => TargetHit?.Invoke(collider, hitDamage)
        );

        Debug.Log($"Proyectil de {form.FormName} lanzado.", projectileObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hitbox.enabled ||
            activeAttackForm == null ||
            activeAttackForm.TypeOfAttack != AttackType.Melee)
        {
            return;
        }

        if ((targetLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        EnemyController enemy = other.GetComponentInParent<EnemyController>();

        if (enemy == null ||
            enemy.CurrentHealth <= 0f ||
            !hitEnemies.Add(enemy))
        {
            return;
        }
        float damage = activeAttackForm.AttackDamage *
    (maximwolfOutOfControl ? maximwolfDamageMultiplier : 1f);

        enemy.TakeDamage(damage, transform.root.position);
        TargetHit?.Invoke(other, damage);

        Debug.Log(
            $"Ataque de {activeAttackForm.FormName} golpeó a {enemy.name}: {damage} de daño.",
            enemy
        );
    }

    public void EndAttackAnimation()
    {
        StopHitboxRoutine();

        if (hitbox != null)
            hitbox.enabled = false;

        activeAttackForm = null;
        movement?.UnlockMovement();
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

    private void StopHitboxRoutine()
    {
        if (hitboxRoutine == null)
            return;

        StopCoroutine(hitboxRoutine);
        hitboxRoutine = null;
    }
}