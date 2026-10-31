using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerStats))]
public class PlayerDefeatHandler : MonoBehaviour
{
    private PlayerStats playerStats;
    private bool transitionStarted;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
    }

    private void OnEnable()
    {
        playerStats.Died += HandlePlayerDied;
    }

    private void OnDisable()
    {
        playerStats.Died -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        if (transitionStarted)
            return;

        transitionStarted = true;

        if (ScenesManager.instance != null)
            ScenesManager.instance.GameOver();
        else
            SceneManager.LoadScene("Lose");
    }
}