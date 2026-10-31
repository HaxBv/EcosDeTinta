using System;
using System.Collections.Generic;
using UnityEngine;

public class ShadowDaggerProjectile : MonoBehaviour
{
    [Header("Colisión")]
    [SerializeField] private float hitRadius = 0.2f;

    [Header("Sprite")]
    [SerializeField] private Transform visual;
    [SerializeField] private float visualRotationOffsetZ;

    private Transform owner;
    private LayerMask targetLayers;
    private ShadowDaggerProjectile splitPrefab;
    private Action<Collider, float> onHit;

    private EnemyController excludedEnemy;
    private Vector2 direction;

    private float damage;
    private float speed;
    private float maxDistance;
    private float splitAngleStep;
    private float splitDamageMultiplier;
    private float splitSpawnOffset;
    private int splitCount;

    private float distanceTravelled;
    private bool splitsOnImpact;
    private bool initialized;
    private Vector3 visualBaseEuler;

    private readonly HashSet<EnemyController> hitEnemies = new();

    private void Awake()
    {
        if (visual == null)
            visual = transform;

        visualBaseEuler = visual.localEulerAngles;
    }

    public void Launch(
        Vector2 launchDirection,
        Transform projectileOwner,
        LayerMask layers,
        float projectileDamage,
        float projectileSpeed,
        float projectileMaxDistance,
        bool shouldSplit,
        EnemyController enemyToIgnore,
        ShadowDaggerProjectile projectileToSpawn,
        float childSplitAngleStep,
        float childDamageMultiplier,
        float childSpawnOffset,
        int childDaggerCount,
        Action<Collider, float> hitCallback)
    {
        direction = launchDirection.normalized;
        owner = projectileOwner;
        targetLayers = layers;
        damage = projectileDamage;
        speed = projectileSpeed;
        maxDistance = projectileMaxDistance;
        splitsOnImpact = shouldSplit;
        excludedEnemy = enemyToIgnore;
        splitPrefab = projectileToSpawn;
        splitAngleStep = childSplitAngleStep;
        splitDamageMultiplier = childDamageMultiplier;
        splitSpawnOffset = childSpawnOffset;
        splitCount = Mathf.Max(0, childDaggerCount);
        onHit = hitCallback;

        initialized = true;
        UpdateVisualRotation();
    }

    private void Update()
    {
        if (!initialized)
            return;

        float step = speed * Time.deltaTime;

        // direction.y representa profundidad Z, no altura.
        transform.position += new Vector3(
            direction.x,
            0f,
            direction.y
        ) * step;

        distanceTravelled += step;

        UpdateVisualRotation();
        CheckForEnemyHit();

        if (distanceTravelled >= maxDistance)
            Destroy(gameObject);
    }

    private void CheckForEnemyHit()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            hitRadius,
            targetLayers,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {
            EnemyController enemy =
                hit.GetComponentInParent<EnemyController>();

            if (enemy == null ||
                enemy == excludedEnemy ||
                hitEnemies.Contains(enemy))
            {
                continue;
            }

            if (owner != null && enemy.transform.root == owner.root)
                continue;

            hitEnemies.Add(enemy);

            enemy.TakeDamage(
                damage,
                owner != null ? owner.position : transform.position
            );

            onHit?.Invoke(hit, damage);

            if (splitsOnImpact)
                Split(enemy);

            Destroy(gameObject);
            return;
        }
    }

    private void Split(EnemyController enemyHit)
    {
        if (splitPrefab == null || splitCount <= 0)
        {
            Debug.LogWarning(
                "La daga necesita un prefab y una cantidad de divisiones mayor que cero.",
                this
            );
            return;
        }

        // Ejemplo con 5: -2, -1, 0, +1 y +2 pasos de ángulo.
        float centerIndex = (splitCount - 1) * 0.5f;

        for (int i = 0; i < splitCount; i++)
        {
            float angle =
                (i - centerIndex) * splitAngleStep;

            Vector2 childDirection = Rotate(direction, angle);
            SpawnChild(childDirection, enemyHit);
        }
    }

    private void SpawnChild(
        Vector2 childDirection,
        EnemyController enemyToIgnore)
    {
        // El abanico se abre en XZ y conserva la altura Y.
        Vector3 spawnPosition = transform.position
            + new Vector3(childDirection.x, 0f, childDirection.y)
            * splitSpawnOffset;

        ShadowDaggerProjectile child = Instantiate(
            splitPrefab,
            spawnPosition,
            Quaternion.identity
        );

        child.Launch(
            childDirection,
            owner,
            targetLayers,
            damage * splitDamageMultiplier,
            speed,
            maxDistance,
            false,
            enemyToIgnore,
            null,
            splitAngleStep,
            splitDamageMultiplier,
            splitSpawnOffset,
            0,
            onHit
        );
    }

    private static Vector2 Rotate(Vector2 vector, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        ).normalized;
    }

    private void UpdateVisualRotation()
    {
        if (visual == null || direction.sqrMagnitude < 0.001f)
            return;

        float angleZ =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        visual.localRotation = Quaternion.Euler(
            visualBaseEuler.x,
            visualBaseEuler.y,
            visualBaseEuler.z + angleZ + visualRotationOffsetZ
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}