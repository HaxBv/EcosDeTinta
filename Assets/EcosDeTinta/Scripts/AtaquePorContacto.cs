using UnityEngine;

public class AtaquePorContacto : MonoBehaviour
{
    [Header("Daño")]
    [SerializeField] private float danio = 10f;

    [SerializeField] private float tiempoEntreAtaques = 1f;

    private float siguienteAtaque = 0f;

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (Time.time < siguienteAtaque)
        {
            return;
        }

        Vida vidaJugador =
            other.GetComponentInParent<Vida>();

        if (vidaJugador != null)
        {
            vidaJugador.RecibirDanio(danio);

            siguienteAtaque =
                Time.time + tiempoEntreAtaques;
        }
    }
}
