using UnityEngine;

public class EnemyAttackRange : MonoBehaviour
{
    [SerializeField] private EnemyController enemy;

    private CharacterController playerController;
    private Transform playerTransform;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponentInParent<EnemyController>();

        Collider rangeCollider = GetComponent<Collider>();

        if (rangeCollider != null)
        {
            rangeCollider.isTrigger = true;
            rangeCollider.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();

        if (stats == null)
            return;

        CharacterController controller =
            stats.GetComponent<CharacterController>();

        // Ignora los colliders hijos, como el AttackHitbox del jugador.
        if (controller == null || other != controller)
            return;

        playerController = controller;
        playerTransform = stats.transform;

        enemy?.SetPlayerInAttackRange(playerTransform, true);
        Debug.Log("Jugador entró al rango del enemigo.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (playerController == null || other != playerController)
            return;

        enemy?.SetPlayerInAttackRange(playerTransform, false);
        Debug.Log("Jugador salió del rango del enemigo.");

        playerController = null;
        playerTransform = null;
    }

    private void OnDisable()
    {
        if (playerController != null)
            enemy?.SetPlayerInAttackRange(playerTransform, false);

        playerController = null;
        playerTransform = null;
    }
}