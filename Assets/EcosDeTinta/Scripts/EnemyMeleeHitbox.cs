using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EnemyMeleeHitbox : MonoBehaviour
{
    [SerializeField] private EnemyController enemy;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponentInParent<EnemyController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        enemy?.RegisterMeleeHit(other);
    }
}