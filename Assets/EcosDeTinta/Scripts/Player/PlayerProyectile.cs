using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerProjectile : MonoBehaviour
{
    private Vector3 direction;
    private float speed;
    private float lifetime;
    private float damage;
    private float elapsedTime;
    private LayerMask targetLayers;
    private Transform owner;
    private Action<Collider, float> onHit;

    private bool initialized;
    private bool hasHit;

    public void Initialize(
        Vector3 travelDirection,
        float projectileSpeed,
        float projectileLifetime,
        float projectileDamage,
        LayerMask layers,
        Transform projectileOwner,
        Action<Collider, float> hitCallback)
    {
        direction = travelDirection.normalized;
        speed = projectileSpeed;
        lifetime = projectileLifetime;
        damage = projectileDamage;
        targetLayers = layers;
        owner = projectileOwner;
        onHit = hitCallback;
        initialized = true;
    }

    private void FixedUpdate()
    {
        if (!initialized || hasHit)
            return;

        transform.position += direction * speed * Time.fixedDeltaTime;
    }

    private void Update()
    {
        if (!initialized || hasHit)
            return;

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= lifetime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || hasHit || other == null)
            return;

        if ((targetLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        EnemyController enemy = other.GetComponentInParent<EnemyController>();

        if (enemy == null || enemy.CurrentHealth <= 0f)
            return;

        hasHit = true;

        enemy.TakeDamage(
            damage,
            owner != null ? owner.position : transform.position
        );

        onHit?.Invoke(other, damage);

        Debug.Log($"El proyectil golpeó a {enemy.name}: {damage} de daño.", enemy);

        Destroy(gameObject);
    }
}