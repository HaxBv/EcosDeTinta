using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Configuración del proyectil")]
    [SerializeField, Min(0f)] private float damage = 10f;
    [SerializeField, Min(0f)] private float speed = 8f;
    [SerializeField, Min(0.1f)] private float lifetime = 5f;
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.8f, 0f);

    private Rigidbody body;
    private Vector3 direction;
    private bool launched;
    private bool hasHit;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;
    }

    public void Launch(Transform target)
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = target.position + targetOffset;
        direction = (targetPosition - transform.position).normalized;
        launched = true;

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (!launched || hasHit)
            return;

        body.MovePosition(
            body.position + direction * speed * Time.fixedDeltaTime
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launched || hasHit || other == null)
            return;

        PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();

        if (playerStats == null)
            return;

        hasHit = true;
        playerStats.TakeDamage(damage, transform.position);
        Destroy(gameObject);
    }
}