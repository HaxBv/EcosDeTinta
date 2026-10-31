using System.Collections;
using TMPro;
using UnityEngine;

public class BossSurvivalVictory : MonoBehaviour
{
    [Header("Mensaje inicial")]
    [SerializeField] private TMP_Text introMessage;

    [SerializeField] private float introMessageDuration = 3f;

    [Header("Supervivencia")]
    [SerializeField] private float survivalDuration = 60f;
    [SerializeField] private EnemySpawner timedSpawner;
    [SerializeField] private InkQueenMonster queenSpawner;
    [SerializeField] private UIManager uiManager;

    [SerializeField] private float timer;
    private bool victoryTriggered;

    private void Start()
    {
        StartCoroutine(BeginBossWave());
    }

    private IEnumerator BeginBossWave()
    {
        if (introMessage != null)
        {
            introMessage.text = "<size=100%>OLEADA DE JEFE</size>\n<size=50%>SOBREVIVE DURANTE 90 SEGUNDOS</size>";
            introMessage.gameObject.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(introMessageDuration);

        if (introMessage != null)
            introMessage.gameObject.SetActive(false);

        timer = survivalDuration;
        uiManager?.UpdateBossCountdown(timer);

        timedSpawner?.StartSpawning();
        queenSpawner?.StartSpawning();

        while (timer > 0f && !victoryTriggered)
        {
            timer -= Time.deltaTime;
            uiManager?.UpdateBossCountdown(timer);
            yield return null;
        }

        if (!victoryTriggered)
            CompleteSurvival();
    }

    private void CompleteSurvival()
    {
        if (victoryTriggered)
            return;

        victoryTriggered = true;
        uiManager?.HideBossCountdown();

        timedSpawner?.StopSpawningAndClearEnemies();
        queenSpawner?.StopSpawningAndClearEnemies();

        if (ScenesManager.instance != null)
            ScenesManager.instance.CompleteLevel();
        else
            Debug.LogError("No se encontró ScenesManager.", this);
    }
}