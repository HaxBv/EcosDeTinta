using UnityEngine;
using UnityEngine.Events;

public class Vida : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float vidaMaxima = 100f;

    [SerializeField] private float vidaActual;

    [Header("Eventos")]
    [SerializeField] private UnityEvent alMorir;

    private bool muerto = false;

    void Awake()
    {
        vidaActual = vidaMaxima;
    }

    public void RecibirDanio(float cantidad)
    {
        if (muerto || cantidad <= 0f)
        {
            return;
        }

        vidaActual -= cantidad;

        vidaActual = Mathf.Clamp(
            vidaActual,
            0f,
            vidaMaxima
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
            vidaActual + cantidad,
            vidaMaxima
        );
    }

    void Morir()
    {
        if (muerto)
        {
            return;
        }

        muerto = true;

        Debug.Log(
            gameObject.name + " ha muerto."
        );

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
