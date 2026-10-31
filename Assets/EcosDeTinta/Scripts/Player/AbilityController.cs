using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class AbilityController : MonoBehaviour
{
    private static readonly int AbilityHash =
        Animator.StringToHash("Ability");

    [Header("Habilidad")]
    [SerializeField] private float abilityCooldown = 4f;
    [SerializeField] private LayerMask targetLayers;

    private PlayerStats stats;
    private PlayerInputs playerInputs;
    private MovementController movement;
    private Animator characterAnimator;

    private float nextAbilityTime;
    private bool abilityAnimationActive;

    public event Action<Collider, float> TargetHit;
    public event Action<Collider, float> TargetStunned;

    [Header("Habilidad de Inkman")]
    [SerializeField] private Collider inkmanHammerHitbox;
    [SerializeField] private float inkmanHammerActiveTime = 0.25f;
    [SerializeField] private float inkmanHammerDamageMultiplier = 2f;
    [SerializeField] private CinemachineImpulseSource inkmanImpulseSource;
    [SerializeField] private float inkmanShakeForce = 1f;
    [SerializeField] private float inkmanShakeDuration = 0.2f;
    [SerializeField] private float inkmanShakeMagnitude = 0.15f;

    private bool inkmanHitboxActive;
    private Coroutine inkmanHitboxRoutine;
    private Vector3 inkmanHammerBaseLocalPosition;

    private readonly HashSet<EnemyController> inkmanHitEnemies = new();

    [Header("Habilidad de Shadow")]
    [SerializeField] private ShadowDaggerProjectile shadowDaggerPrefab;
    [SerializeField] private Transform shadowDaggerLaunchPoint;
    [SerializeField] private float shadowDaggerSpeed = 12f;
    [SerializeField] private float shadowDaggerRange = 8f;
    [SerializeField] private float shadowDaggerDamageMultiplier = 1f;
    [SerializeField] private float shadowSplitAngle = 25f;
    [SerializeField] private float shadowSplitDamageMultiplier = 0.6f;
    [SerializeField] private float shadowSplitSpawnOffset = 0.2f;



    [SerializeField] private int normalShadowSplitCount = 3;
    [SerializeField] private int outOfControlShadowSplitCount = 5;
    [SerializeField] private float outOfControlShadowSplitAngle = 65f;

    private OutOfControlManager outOfControlManager;
    [Header("Habilidad de Maximwolf")]
    [SerializeField] private Collider maximwolfAbilityHitbox;
    [SerializeField] private float maximwolfDamageMultiplier = 1f;
    [SerializeField] private float maximwolfHealPerEnemy = 8f;
    [SerializeField] private float maximwolfMaxHealPerUse = 24f;
    [SerializeField] private float maximwolfHitboxActiveTime = 0.25f;
    [SerializeField] private float maximwolfStunDuration = 1.5f;

    private bool maximwolfHitboxActive;
    private float maximwolfHitboxTimeLeft;
    private float maximwolfHealThisUse;
    private Vector3 maximwolfHitboxBaseLocalPosition;

    private readonly HashSet<EnemyController> maximwolfHitEnemies = new();

    [Header("Habilidad de Zero")]
    [SerializeField] private ZeroMissile zeroMissilePrefab;
    [SerializeField] private Transform zeroMissileLaunchPoint;
    [SerializeField] private float zeroMissileRange = 8f;
    [SerializeField] private int zeroMissileCount = 3;
    [SerializeField] private float zeroMissileSpeed = 10f;
    [SerializeField] private float zeroMissileTurnDuration = 0.45f;
    [SerializeField] private float zeroMissileTurnDistance = 1.5f;
    [SerializeField] private float zeroMissileArcHeight = 1.2f;
    [SerializeField] private float zeroMissileSideSpread = 0.8f;
    [SerializeField] private float zeroMissileDamageMultiplier = 1.2f;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        playerInputs = GetComponent<PlayerInputs>();
        movement = GetComponent<MovementController>();
        characterAnimator = GetComponentInChildren<Animator>(true);
        outOfControlManager = GetComponent<OutOfControlManager>();
        // Estos colliders deben ser hijos directos de Player,
        // fuera del objeto Sprite que se voltea.
        if (inkmanHammerHitbox != null)
        {
            inkmanHammerHitbox.isTrigger = true;
            inkmanHammerHitbox.enabled = false;
            inkmanHammerBaseLocalPosition =
                inkmanHammerHitbox.transform.localPosition;
        }

        if (maximwolfAbilityHitbox != null)
        {
            maximwolfAbilityHitbox.isTrigger = true;
            maximwolfAbilityHitbox.enabled = false;
            maximwolfHitboxBaseLocalPosition =
                maximwolfAbilityHitbox.transform.localPosition;
        }

        if (stats == null || playerInputs == null || movement == null)
        {
            Debug.LogError(
                "AbilityController necesita PlayerStats, PlayerInputs y MovementController.",
                this
            );
        }

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

        DeactivateInkmanHammerHitbox();
        DeactivateMaximwolfHitbox();

        if (abilityAnimationActive)
        {
            abilityAnimationActive = false;
            movement?.UnlockMovement();
        }
    }

    private void Update()
    {
        if (!maximwolfHitboxActive)
            return;

        maximwolfHitboxTimeLeft -= Time.deltaTime;

        if (maximwolfHitboxTimeLeft <= 0f)
            DeactivateMaximwolfHitbox();
    }

    private void TryUseAbility()
    {
        if (stats == null ||
            stats.CurrentForm == null ||
            characterAnimator == null ||
            movement == null)
        {
            return;
        }

        if (abilityAnimationActive || Time.time < nextAbilityTime)
            return;

        string formName = stats.CurrentForm.FormName;

        if (formName != "Inkman" &&
            formName != "Shadow" &&
            formName != "Maximwolf" &&
            formName != "Zero")
        {
            Debug.LogWarning($"No hay una habilidad definida para {formName}.", this);
            return;
        }

        nextAbilityTime = Time.time + abilityCooldown;
        abilityAnimationActive = true;

        movement.LockMovement();
        characterAnimator.SetTrigger(AbilityHash);
    }

    private Vector3 GetHitboxPositionForFacing(Vector3 basePosition)
    {
        basePosition.x =
            Mathf.Abs(basePosition.x) * movement.FacingDirection;

        return basePosition;
    }

    // INKMAN

    public void ActivateInkmanHammerFromAnimation()
    {
        if (stats == null ||
            stats.CurrentForm == null ||
            stats.CurrentForm.FormName != "Inkman" ||
            inkmanHammerHitbox == null)
        {
            return;
        }

        inkmanHitEnemies.Clear();
        inkmanHitboxActive = true;

        inkmanHammerHitbox.transform.localPosition =
            GetHitboxPositionForFacing(inkmanHammerBaseLocalPosition);

        inkmanHammerHitbox.enabled = true;
        if (inkmanImpulseSource != null)
        {
            var impulse = inkmanImpulseSource.ImpulseDefinition;
            impulse.ImpulseDuration = inkmanShakeDuration;
            impulse.AmplitudeGain = inkmanShakeMagnitude;

            inkmanImpulseSource.GenerateImpulseWithForce(inkmanShakeForce);
        }
        else
        {
            Debug.LogError(
                "Asigna Cinemachine Impulse Source en AbilityController.",
                this
            );
        }
        if (inkmanHitboxRoutine != null)
            StopCoroutine(inkmanHitboxRoutine);

        inkmanHitboxRoutine =
            StartCoroutine(DisableInkmanHammerAfterTime());
    }

    private IEnumerator DisableInkmanHammerAfterTime()
    {
        yield return new WaitForSeconds(inkmanHammerActiveTime);

        inkmanHitboxRoutine = null;
        inkmanHitboxActive = false;

        if (inkmanHammerHitbox != null)
            inkmanHammerHitbox.enabled = false;
    }

    private void DeactivateInkmanHammerHitbox()
    {
        if (inkmanHitboxRoutine != null)
        {
            StopCoroutine(inkmanHitboxRoutine);
            inkmanHitboxRoutine = null;
        }

        inkmanHitboxActive = false;

        if (inkmanHammerHitbox != null)
            inkmanHammerHitbox.enabled = false;
    }

    public void RegisterInkmanHammerHit(Collider other)
    {
        if (!inkmanHitboxActive ||
            other == null ||
            stats == null ||
            stats.CurrentForm == null)
        {
            return;
        }

        EnemyController enemy = other.GetComponentInParent<EnemyController>();

        if (enemy == null ||
            enemy.CurrentHealth <= 0f ||
            !inkmanHitEnemies.Add(enemy))
        {
            return;
        }

        float damage =
            stats.CurrentForm.AttackDamage * inkmanHammerDamageMultiplier;

        enemy.TakeDamage(damage, transform.position);
        TargetHit?.Invoke(other, damage);

        Debug.Log(
            $"El mazo de Inkman golpeó a {enemy.name} e hizo {damage} de daño.",
            enemy
        );
    }

    // MAXIMWOLF

    public void ActivateMaximwolfHitboxFromAnimation()
    {
        if (stats == null ||
            stats.CurrentForm == null ||
            stats.CurrentForm.FormName != "Maximwolf" ||
            maximwolfAbilityHitbox == null)
        {
            return;
        }

        if (!maximwolfHitboxActive)
        {
            maximwolfHitEnemies.Clear();
            maximwolfHealThisUse = 0f;
        }

        maximwolfAbilityHitbox.transform.localPosition =
            GetHitboxPositionForFacing(maximwolfHitboxBaseLocalPosition);

        SoundManager.Instance.PlaySFX(SoundType.SFX, 5);

        maximwolfHitboxActive = true;
        maximwolfHitboxTimeLeft = maximwolfHitboxActiveTime;
        maximwolfAbilityHitbox.enabled = true;
    }

    private void DeactivateMaximwolfHitbox()
    {
        maximwolfHitboxActive = false;
        maximwolfHitboxTimeLeft = 0f;

        if (maximwolfAbilityHitbox != null)
            maximwolfAbilityHitbox.enabled = false;
    }

    public void RegisterMaximwolfHit(Collider other)
    {
        if (!maximwolfHitboxActive ||
            other == null ||
            stats == null ||
            stats.CurrentForm == null)
        {
            return;
        }

        EnemyController enemy = other.GetComponentInParent<EnemyController>();

        if (enemy == null ||
            enemy.CurrentHealth <= 0f ||
            !maximwolfHitEnemies.Add(enemy))
        {
            return;
        }

        float damage = stats.CurrentForm.AttackDamage * maximwolfDamageMultiplier;
        enemy.TakeDamage(damage, transform.position);
        TargetHit?.Invoke(other, damage);
        TargetStunned?.Invoke(other, maximwolfStunDuration);

        float remainingHeal =
            maximwolfMaxHealPerUse - maximwolfHealThisUse;

        float healAmount = Mathf.Min(
            maximwolfHealPerEnemy,
            Mathf.Max(0f, remainingHeal)
        );

        if (healAmount > 0f)
        {
            stats.Heal(healAmount);
            maximwolfHealThisUse += healAmount;
        }

        Debug.Log(
            $"Maximwolf golpeó a {enemy.name}, hizo {damage} de daño y recuperó {healAmount} de vida.",
            enemy
        );
    }

    // SHADOW
    public void LaunchShadowDaggerFromAnimation()
    {
        if (stats == null ||
            stats.CurrentForm == null ||
            stats.CurrentForm.FormName != "Shadow" ||
            movement == null)
        {
            return;
        }

        if (shadowDaggerPrefab == null || shadowDaggerLaunchPoint == null)
        {
            Debug.LogError(
                "Asigna el prefab de la daga y su punto de lanzamiento.",
                this
            );
            return;
        }

        bool shadowOutOfControl =
            outOfControlManager != null &&
            outOfControlManager.IsOutOfControl;

        int splitCount = shadowOutOfControl
            ? outOfControlShadowSplitCount
            : normalShadowSplitCount;

        float splitAngle = shadowOutOfControl
            ? outOfControlShadowSplitAngle
            : shadowSplitAngle;

        Vector2 direction = new Vector2(movement.FacingDirection, 0f);

        ShadowDaggerProjectile dagger = Instantiate(
            shadowDaggerPrefab,
            shadowDaggerLaunchPoint.position,
            Quaternion.identity
        );

        dagger.Launch(
            direction,
            transform,
            targetLayers,
            stats.CurrentForm.AttackDamage * shadowDaggerDamageMultiplier,
            shadowDaggerSpeed,
            shadowDaggerRange,
            true,
            null,
            shadowDaggerPrefab,
            splitAngle,
            shadowSplitDamageMultiplier,
            shadowSplitSpawnOffset,
            splitCount,
            (collider, damage) => TargetHit?.Invoke(collider, damage)
        );
    }

    // ZERO

    public void LaunchZeroMissilesFromAnimation()
    {
        if (stats == null ||
            stats.CurrentForm == null ||
            stats.CurrentForm.FormName != "Zero")
        {
            return;
        }

        if (zeroMissilePrefab == null || zeroMissileLaunchPoint == null)
        {
            Debug.LogError(
                "Asigna el prefab del misil y su punto de lanzamiento.",
                this
            );
            return;
        }

        List<EnemyController> targets = GetNearestZeroTargets();

        if (targets.Count == 0)
        {
            Debug.Log(
                "[AbilityController] No hay enemigos dentro del rango de Zero.",
                this
            );
            return;
        }

        float damage =
            stats.CurrentForm.AttackDamage * zeroMissileDamageMultiplier;

        for (int i = 0; i < targets.Count; i++)
        {
            EnemyController target = targets[i];

            float side = targets.Count == 1
                ? 0f
                : Mathf.Lerp(-1f, 1f, i / (float)(targets.Count - 1));

            Vector3 spawnPosition =
                zeroMissileLaunchPoint.position +
                Vector3.up * (i - (targets.Count - 1) * 0.5f) * 0.15f;

            ZeroMissile missile = Instantiate(
                zeroMissilePrefab,
                spawnPosition,
                zeroMissileLaunchPoint.rotation
            );

            missile.Launch(
                target,
                transform,
                damage,
                zeroMissileSpeed,
                zeroMissileTurnDuration,
                zeroMissileTurnDistance,
                zeroMissileArcHeight,
                zeroMissileSideSpread,
                side,
                TargetHit
            );
        }
    }

    private List<EnemyController> GetNearestZeroTargets()
    {
        Vector3 origin = transform.position + Vector3.up;

        Collider[] colliders = Physics.OverlapSphere(
            origin,
            zeroMissileRange,
            targetLayers,
            QueryTriggerInteraction.Collide
        );

        List<EnemyController> targets = new();
        HashSet<EnemyController> addedEnemies = new();

        foreach (Collider hit in colliders)
        {
            EnemyController enemy = hit.GetComponentInParent<EnemyController>();

            if (enemy == null ||
                !enemy.gameObject.activeInHierarchy ||
                enemy.CurrentHealth <= 0f ||
                !addedEnemies.Add(enemy))
            {
                continue;
            }

            targets.Add(enemy);
        }

        targets.Sort((a, b) =>
        {
            float distanceA = (a.transform.position - origin).sqrMagnitude;
            float distanceB = (b.transform.position - origin).sqrMagnitude;

            return distanceA.CompareTo(distanceB);
        });

        if (targets.Count > zeroMissileCount)
        {
            targets.RemoveRange(
                zeroMissileCount,
                targets.Count - zeroMissileCount
            );
        }

        return targets;
    }

    // Animation Event al final de cada animación de habilidad.
    public void FinishAbilityAnimation()
    {
        DeactivateInkmanHammerHitbox();
        DeactivateMaximwolfHitbox();

        if (!abilityAnimationActive)
            return;

        abilityAnimationActive = false;

        movement?.UnlockMovement();

    }
    public void CancelAbilityForFormChange()
    {
        // Ejecuta la limpieza que normalmente hace el evento final de la animación.
        FinishAbilityAnimation();

        // Garantiza que cambiar de forma no deje al jugador bloqueado.
        abilityAnimationActive = false;
        movement?.UnlockMovement();

        if (characterAnimator != null &&
            characterAnimator.runtimeAnimatorController != null)
        {
            characterAnimator.ResetTrigger(AbilityHash);
        }
    }
}