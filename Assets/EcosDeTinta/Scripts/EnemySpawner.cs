using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySpawner : MonoBehaviour
{
    [Serializable]
    private class EnemyGroup
    {
        public ObjectPooling pool;
        [Min(0)] public int amount = 1;
    }

    [Serializable]
    private class Phase
    {
        public string phaseName;
        public Transform[] spawnPoints;
        public EnemyGroup[] enemyGroups;
    }

    [Header("Jugador")]
    [SerializeField] private Transform player;

    [Header("Oleadas")]
    [SerializeField] private Phase[] phases = new Phase[3];
    [SerializeField] private bool startOnPlay = true;
    [SerializeField] private float delayBetweenPhases = 1.5f;

    [Header("Al terminar todas las fases")]
    [SerializeField] private UnityEvent onAllPhasesCleared;

    private readonly HashSet<EnemyController> activeEnemies =
        new HashSet<EnemyController>();

    private int currentPhaseIndex = -1;
    private int spawnPointIndex;
    private bool transitionRunning;

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player == null)
        {
            Debug.LogError("Asigna el jugador o usa el tag Player.", this);
            return;
        }

        if (startOnPlay)
            StartPhase(0);
    }

    public void StartPhase(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= phases.Length)
        {
            Debug.LogWarning($"Fase inválida: {phaseIndex}", this);
            return;
        }

        currentPhaseIndex = phaseIndex;
        spawnPointIndex = 0;

        Phase phase = phases[phaseIndex];
        Debug.Log($"Comienza la fase: {phase.phaseName}", this);

        foreach (EnemyGroup group in phase.enemyGroups)
        {
            if (group == null || group.pool == null)
            {
                Debug.LogWarning(
                    $"Hay un grupo sin pool asignado en {phase.phaseName}.",
                    this
                );
                continue;
            }

            for (int i = 0; i < group.amount; i++)
                SpawnEnemy(group.pool, phase.spawnPoints);
        }

        if (activeEnemies.Count == 0)
            StartCoroutine(AdvancePhaseAfterDelay());
    }

    private void SpawnEnemy(ObjectPooling pool, Transform[] spawnPoints)
    {
        Transform point = transform;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            point = spawnPoints[spawnPointIndex % spawnPoints.Length];
            spawnPointIndex++;
        }

        GameObject enemyObject = pool.GetObject(
            point.position,
            point.rotation,
            false
        );

        if (enemyObject == null)
            return;

        EnemyController enemy = enemyObject.GetComponent<EnemyController>();

        if (enemy == null)
        {
            Debug.LogError(
                $"El prefab {enemyObject.name} no tiene EnemyController.",
                enemyObject
            );

            pool.ReturnObject(enemyObject);
            return;
        }

        enemy.ConfigureForSpawn(player, pool);
        enemy.ReturnedToPool += OnEnemyReturnedToPool;
        activeEnemies.Add(enemy);

        enemyObject.SetActive(true);
    }

    private void OnEnemyReturnedToPool(EnemyController enemy)
    {
        enemy.ReturnedToPool -= OnEnemyReturnedToPool;
        activeEnemies.Remove(enemy);

        if (activeEnemies.Count == 0 && !transitionRunning)
            StartCoroutine(AdvancePhaseAfterDelay());
    }

    private IEnumerator AdvancePhaseAfterDelay()
    {
        transitionRunning = true;
        yield return new WaitForSeconds(delayBetweenPhases);

        int nextPhase = currentPhaseIndex + 1;

        if (nextPhase < phases.Length)
        {
            transitionRunning = false;
            StartPhase(nextPhase);
        }
        else
        {
            Debug.Log("Se completaron todas las fases.", this);
            onAllPhasesCleared?.Invoke();
        }
    }
}