using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathPlayer : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField] private PlayerController movimientoJugador;

    [Header("Interfaz")]
    [SerializeField] private GameObject panelDerrota;

    private bool estaMuerto = false;

    public void Morir()
    {
        if (estaMuerto) return;

        estaMuerto = true;

        Debug.Log("Inkman ha muerto. DERROTA.");

        if (movimientoJugador != null)
        {
            movimientoJugador.enabled = false;
        }

        if (panelDerrota != null)
        {
            panelDerrota.SetActive(true);
        }
    }

    public void Reintentar()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}
