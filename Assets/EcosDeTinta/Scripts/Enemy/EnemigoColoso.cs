using UnityEngine;

public class EnemigoColoso : EnemigoBase
{
    protected override void Start()
    {
        base.Start();

        vida = 250f;
        velocidad = 1.8f;
        distanciaDeteccion = 12f;
        distanciaAtaque = 2.5f;
    }

    protected override void SeguirJugador()
    {
        base.SeguirJugador();
    }
}
