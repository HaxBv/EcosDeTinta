using UnityEngine;
using UnityEngine.Events;

public class Vida : MonoBehaviour
{

    [Header("Vida")]
    [SerializeField] private float vidaMaxima = 100f;

    [Header("Eventos")]
    [SerializeField] private UnityEvent alMorir;

    private float vidaActual;
    private bool muerto;

    void OnEnable()
    {
        vidaActual = vidaMaxima;
        muerto = false;
    }

    public void RecibirDanio(float cantidad)
    {
        if (muerto || cantidad <= 0f)
        {
            return;
        }

        vidaActual = Mathf.Max(
            0f,
            vidaActual - cantidad
        );

        Debug.Log(
            gameObject.name +
            " recibió " +
            cantidad +
            " de daño. Vida: " +
            vidaActual
        );

        if (vidaActual <= 0f)
        {
            Morir();
        }
    }

    public void Curar(float cantidad)
    {
        if (muerto || cantidad <= 0f)
        {
            return;
        }

        vidaActual = Mathf.Min(
            vidaMaxima,
            vidaActual + cantidad
        );
    }

    private void Morir()
    {
        if (muerto)
        {
            return;
        }

        muerto = true;

        Debug.Log(gameObject.name + " murió.");

        alMorir?.Invoke();
    }

    public float ObtenerVidaActual()
    {
        return vidaActual;
    }

    public float ObtenerVidaMaxima()
    {
        return vidaMaxima;
    }
}
