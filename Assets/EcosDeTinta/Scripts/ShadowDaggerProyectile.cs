using System;
using System.Collections.Generic;
using UnityEngine;

public class ShadowDaggerProjectile : MonoBehaviour
{
    [Header("Colisión")]
    [SerializeField] private float hitRadius = 0.2f;

    [Header("Sprite")]
    [SerializeField] private Transform visual;

    // Si el dibujo de la daga apunta hacia arriba, prueba -90.
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
    private float splitAngle;
    private float splitDamageMultiplier;
    private float splitSpawnOffset;

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
        float childSplitAngle,
        float childDamageMultiplier,
        float childSpawnOffset,
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
        splitAngle = childSplitAngle;
        splitDamageMultiplier = childDamageMultiplier;
        splitSpawnOffset = childSpawnOffset;
        onHit = hitCallback;

        initialized = true;
        UpdateVisualRotation();
    }

    private void Update()
    {
        if (!initialized)
            return;

        float step = speed * Time.deltaTime;

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
            EnemyController enemy = hit.GetComponentInParent<EnemyController>();

            if (enemy == null || enemy == excludedEnemy || hitEnemies.Contains(enemy))
                continue;

            if (owner != null && enemy.transform.root == owner.root)
                continue;

            hitEnemies.Add(enemy);
            enemy.TakeDamage(damage, owner != null ? owner.position : transform.position);
            onHit?.Invoke(hit, damage);

            if (splitsOnImpact)
                Split(enemy);

            Destroy(gameObject);
            return;
        }
    }

    private void Split(EnemyController enemyHit)
    {
        if (splitPrefab == null)
        {
            Debug.LogWarning("Asigna el prefab de daga para la división.", this);
            return;
        }

        SpawnChild(direction, enemyHit);
        SpawnChild(Rotate(direction, splitAngle), enemyHit);
        SpawnChild(Rotate(direction, -splitAngle), enemyHit);
    }
    private void SpawnChild(
    Vector2 childDirection,
    EnemyController enemyToIgnore)
    {
        Vector3 spawnPosition = transform.position
            + new Vector3(childDirection.x, childDirection.y, 0f)
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
            enemyToIgnore, // Ignora al enemigo que recibió el impacto inicial.
            null,
            splitAngle,
            splitDamageMultiplier,
            splitSpawnOffset,
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

        // Esta fórmula asume que la punta de la daga apunta hacia +X.
        float angleZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

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