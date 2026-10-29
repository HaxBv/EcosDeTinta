using UnityEngine;

public class EnemigoVolador : EnemigoBase
{
    protected override void Start()
    {
        base.Start();

        vida = 40f;
        velocidad = 4f;
        distanciaDeteccion = 10f;
        distanciaAtaque = 1.5f;
    }

    protected override void SeguirJugador()
    {
        Vector3 direccion =
            jugador.position - transform.position;

        transform.position +=
            direccion.normalized *
            velocidad *
            Time.deltaTime;
    }
}
