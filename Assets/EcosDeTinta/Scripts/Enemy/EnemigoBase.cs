using UnityEngine;

public class EnemigoBase : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] protected Transform jugador;

    [Header("Estadisticas")]
    [SerializeField] protected float vida = 100f;
    [SerializeField] protected float velocidad = 3f;

    [Header("Distancias")]
    [SerializeField] protected float distanciaDeteccion = 8f;
    [SerializeField] protected float distanciaAtaque = 1.5f;

    protected virtual void Start()
    {
        if (jugador == null)
        {
            GameObject objeto =
                GameObject.FindGameObjectWithTag("Player");

            if (objeto != null)
            {
                jugador = objeto.transform;
            }
        }
    }

    protected virtual void Update()
    {
        if (jugador == null)
        {
            return;
        }

        float distancia = Vector3.Distance(
            transform.position,
            jugador.position
        );

        if (distancia <= distanciaDeteccion &&
            distancia > distanciaAtaque)
        {
            SeguirJugador();
        }
    }

    protected virtual void SeguirJugador()
    {
        Vector3 direccion =
            jugador.position - transform.position;

        direccion.y = 0f;

        transform.position +=
            direccion.normalized *
            velocidad *
            Time.deltaTime;

        if (direccion.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.LookRotation(direccion);
        }
    }
}
