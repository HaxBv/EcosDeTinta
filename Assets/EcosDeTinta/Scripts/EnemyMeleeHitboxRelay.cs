using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyMeleeHitboxRelay : MonoBehaviour
{
    [SerializeField] private EnemyController enemy;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponentInParent<EnemyController>();

        Collider hitbox = GetComponent<Collider>();
        hitbox.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        enemy?.RegisterMeleeHit(other);
    }
}