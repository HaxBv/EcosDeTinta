using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform jugador;

    [Header("Movimiento")]
    [SerializeField] private float velocidad = 3f;

    [Header("Distancias")]
    [SerializeField] private float distanciaDeteccion = 8f;
    [SerializeField] private float distanciaAtaque = 1.5f;

    void Update()
    {
        if (jugador == null)
        {
            return;
        }

        Vector3 direccion = jugador.position - transform.position;

        direccion.y = 0f;

        float distancia = direccion.magnitude;

        if (distancia > distanciaDeteccion)
        {
            return;
        }

        if (distancia <= distanciaAtaque)
        {
            return;
        }

        transform.position +=
            direccion.normalized *
            velocidad *
            Time.deltaTime;

        if (direccion.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direccion);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            distanciaDeteccion
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            distanciaAtaque
        );
    }
}
