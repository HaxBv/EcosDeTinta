using System.Collections;
using System.ComponentModel;
using TMPro;
using UnityEngine;

public class BossSurvivalVictory : MonoBehaviour
{
    [Header("Mensaje inicial")]
    [SerializeField] private TMP_Text introMessage;
    [SerializeField, Min(0f)] private float introMessageDuration = 3f;

    [Header("Supervivencia")]
    [SerializeField, Min(1f)] private float survivalDuration = 60f;
    [SerializeField] private EnemySpawner timedSpawner;
    [SerializeField] private InkQueenMonster queenSpawner;

    [SerializeField]private float timer;
    private bool victoryTriggered;

    private void Start()
    {
        StartCoroutine(BeginBossWave());
    }

    private IEnumerator BeginBossWave()
    {
        if (introMessage != null)
        {
            introMessage.text =
                "Oleada de Jefe\nsobrevive 1 minuto para ganar";

            introMessage.gameObject.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(introMessageDuration);

        if (introMessage != null)
            introMessage.gameObject.SetActive(false);

        timedSpawner?.StartSpawning();
        queenSpawner?.StartSpawning();

        timer = survivalDuration;

        while (timer > 0f && !victoryTriggered)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        if (!victoryTriggered)
            CompleteSurvival();
    }

    private void CompleteSurvival()
    {
        victoryTriggered = true;

        timedSpawner?.StopSpawning();
        queenSpawner?.StopSpawning();

        if (ScenesManager.instance != null)
            ScenesManager.instance.CompleteLevel();
        else
            Debug.LogError("No se encontró ScenesManager.", this);
    }
}