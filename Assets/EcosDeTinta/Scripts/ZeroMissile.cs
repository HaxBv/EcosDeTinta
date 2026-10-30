using System;
using UnityEngine;

public class ZeroMissile : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private float visualRotationOffsetZ;
    private Vector3 visualBaseEuler;
    [SerializeField] private float impactRadius = 0.35f;
    [SerializeField] private float turnRate = 8f;
    [SerializeField] private float lifeTime = 6f;

    private EnemyController target;
    private Transform owner;
    private Action<Collider, float> onHit;

    private float damage;
    private float speed;
    private float turnDuration;
    private float turnDistance;
    private float arcHeight;
    private float sideSpread;
    private float side;

    private float elapsed;
    private float lifeElapsed;
    private bool homing;

    private Vector3 start;
    private Vector3 controlPoint;
    private Vector3 turnPoint;
    private Vector3 currentDirection;
    private void Awake()
    {
        if (visual == null)
            visual = transform;

        // Conserva la orientación inicial del sprite, incluidos X e Y.
        visualBaseEuler = visual.localEulerAngles;
    }
    public void Launch(
        EnemyController target,
        Transform owner,
        float damage,
        float speed,
        float turnDuration,
        float turnDistance,
        float arcHeight,
        float sideSpread,
        float side,
        Action<Collider, float> onHit)
    {
        this.target = target;
        this.owner = owner;
        this.damage = damage;
        this.speed = speed;
        this.turnDuration = Mathf.Max(0.01f, turnDuration);
        this.turnDistance = turnDistance;
        this.arcHeight = arcHeight;
        this.sideSpread = sideSpread;
        this.side = side;
        this.onHit = onHit;

        start = transform.position;

        Vector3 towardTarget = (target.transform.position - start).normalized;
        Vector3 sideways = Vector3.Cross(Vector3.up, towardTarget).normalized;

        if (sideways.sqrMagnitude < 0.001f)
            sideways = Vector3.forward;

        // El primer tramo se aleja del enemigo y luego curva para regresar hacia él.
        controlPoint =
            start
            - towardTarget * turnDistance
            + Vector3.up * arcHeight
            + sideways * side * sideSpread;

        turnPoint =
            start
            - towardTarget * turnDistance * 0.4f
            + Vector3.up * arcHeight * 0.65f
            + sideways * side * sideSpread * 1.5f;

        currentDirection = (controlPoint - start).normalized;
        UpdateVisualRotation(currentDirection);
    }

    private void Update()
    {
        lifeElapsed += Time.deltaTime;

        if (lifeElapsed >= lifeTime ||
            target == null ||
            !target.gameObject.activeInHierarchy ||
            target.CurrentHealth <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (!homing)
            FlyThroughTurn();
        else
            HomeTowardTarget();
    }

    private void FlyThroughTurn()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / turnDuration);

        float inverseT = 1f - t;

        transform.position =
            inverseT * inverseT * start
            + 2f * inverseT * t * controlPoint
            + t * t * turnPoint;

        Vector3 tangent =
            2f * inverseT * (controlPoint - start)
            + 2f * t * (turnPoint - controlPoint);

        if (tangent.sqrMagnitude > 0.001f)
        {
            currentDirection = tangent.normalized;
            UpdateVisualRotation(currentDirection);
        }

        if (t >= 1f)
            homing = true;
    }

    private void HomeTowardTarget()
    {
        Vector3 toTarget = target.transform.position - transform.position;

        if (toTarget.sqrMagnitude <= impactRadius * impactRadius)
        {
            HitTarget();
            return;
        }

        Vector3 desiredDirection = toTarget.normalized;

        currentDirection = Vector3.RotateTowards(
            currentDirection,
            desiredDirection,
            turnRate * Time.deltaTime,
            0f
        ).normalized;

        transform.position += currentDirection * speed * Time.deltaTime;
        UpdateVisualRotation(currentDirection);
    }

    private void HitTarget()
    {
        Collider targetCollider = target.GetComponentInChildren<Collider>();

        target.TakeDamage(
            damage,
            owner != null ? owner.position : transform.position
        );

        if (targetCollider != null)
            onHit?.Invoke(targetCollider, damage);

        Destroy(gameObject);
    }

    private void UpdateVisualRotation(Vector3 direction)
    {
        Vector2 direction2D = new Vector2(direction.x, direction.y);

        if (visual == null || direction2D.sqrMagnitude < 0.001f)
            return;

        // El dibujo apunta hacia arriba (+Y), por eso restamos 90°.
        float angleZ = Mathf.Atan2(direction2D.y, direction2D.x) * Mathf.Rad2Deg - 90f;

        visual.localRotation = Quaternion.Euler(
            visualBaseEuler.x,
            visualBaseEuler.y,
            angleZ + visualRotationOffsetZ
        );
    }
}