using UnityEngine;

public class EnemigoTerrestre : EnemigoBase
{
    protected override void Start()
    {
        base.Start();

        vida = 80f;
        velocidad = 3.5f;
        distanciaDeteccion = 8f;
        distanciaAtaque = 1.3f;
    }

    protected override void SeguirJugador()
    {
        base.SeguirJugador();
    }
}
