using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InkQueenMonster : MonoBehaviour
{
    private static readonly int RoarHash = Animator.StringToHash("Roar");

    [Serializable]
    private class EnemyGroup
    {
        public GameObject prefab;
        [Min(0)] public int amount = 1;
    }

    [Serializable]
    private class Wave
    {
        public string waveName;
        public EnemyGroup[] enemyGroups;
    }
    [SerializeField] private bool waitForSummonedEnemiesToDie = true;
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private ObjectPooling enemyPool;
    [SerializeField] private Animator bossAnimator;

    [Header("Oleadas configurables")]
    [SerializeField] private Wave[] waves = new Wave[3];

    [Header("Puntos de aparición")]
    [SerializeField] private Transform[] leftSpawnPoints = new Transform[2];
    [SerializeField] private Transform[] rightSpawnPoints = new Transform[2];

    [Header("Rugido")]
    [SerializeField] private bool startOnPlay = true;
    [Min(0f)][SerializeField] private float minRoarInterval = 3f;
    [Min(0f)][SerializeField] private float maxRoarInterval = 5f;

    [Header("Al terminar todas las oleadas")]
    [SerializeField] private UnityEvent onAllWavesCleared;

    private readonly HashSet<EnemyController> activeEnemies = new();
    private readonly List<int> remainingByGroup = new();

    private int currentWaveIndex = -1;
    private bool isSpawning;
    private bool isRoaring;
    private bool waitingForWaveClear;
    private Coroutine roarRoutine;
    [Header("Enemigos voladores")]
    [SerializeField, Min(0f)] private float flyingSpawnHeight = 2f;
    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (bossAnimator == null)
            bossAnimator = GetComponentInChildren<Animator>(true);

        if (player == null)
        {
            Debug.LogError("Asigna el jugador o usa el tag Player.", this);
            return;
        }

        if (enemyPool == null)
        {
            Debug.LogError("Asigna el ObjectPooling de enemigos.", this);
            return;
        }

        if (bossAnimator == null)
        {
            Debug.LogError("InkQueenMonster no encontró el Animator del jefe.", this);
            return;
        }

        if (startOnPlay)
            StartSpawning();
    }

    public void StartSpawning()
    {
        if (isSpawning)
            return;

        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("Configura al menos una oleada.", this);
            return;
        }

        isSpawning = true;
        isRoaring = false;
        waitingForWaveClear = false;
        currentWaveIndex = -1;

        AdvanceToNextWave();
    }

    public void StopSpawning()
    {
        isSpawning = false;
        isRoaring = false;

        if (roarRoutine != null)
        {
            StopCoroutine(roarRoutine);
            roarRoutine = null;
        }
    }
    private void AdvanceToNextWave()
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("Configura al menos una oleada.", this);
            isSpawning = false;
            return;
        }

        // Busca la siguiente oleada válida, dando la vuelta al llegar al final.
        for (int attempt = 0; attempt < waves.Length; attempt++)
        {
            currentWaveIndex = (currentWaveIndex + 1) % waves.Length;

            Wave wave = waves[currentWaveIndex];

            if (wave == null || wave.enemyGroups == null)
                continue;

            remainingByGroup.Clear();

            foreach (EnemyGroup group in wave.enemyGroups)
            {
                if (group == null || group.prefab == null)
                {
                    remainingByGroup.Add(0);
                    continue;
                }

                remainingByGroup.Add(Mathf.Max(0, group.amount));
            }

            if (!HasEnemiesRemainingToSpawn())
                continue;

            waitingForWaveClear = false;

            Debug.Log(
                $"Comienza la oleada {currentWaveIndex + 1}: {wave.waveName}",
                this
            );

            ScheduleNextRoar();
            return;
        }

        Debug.LogWarning("No hay oleadas configuradas con enemigos para invocar.", this);
        isSpawning = false;
    }

    private void ScheduleNextRoar()
    {
        if (!isSpawning ||
            isRoaring ||
            waitingForWaveClear ||
            roarRoutine != null ||
            !HasEnemiesRemainingToSpawn())
        {
            return;
        }

        float min = Mathf.Min(minRoarInterval, maxRoarInterval);
        float max = Mathf.Max(minRoarInterval, maxRoarInterval);
        float interval = UnityEngine.Random.Range(min, max);

        roarRoutine = StartCoroutine(RoarAfterInterval(interval));
    }

    private IEnumerator RoarAfterInterval(float interval)
    {
        yield return new WaitForSeconds(interval);

        roarRoutine = null;
        TryStartRoar();
    }

    private void TryStartRoar()
    {
        if (!isSpawning ||
            isRoaring ||
            waitingForWaveClear ||
            !HasEnemiesRemainingToSpawn())
        {
            return;
        }

        isRoaring = true;
        bossAnimator.ResetTrigger(RoarHash);
        bossAnimator.SetTrigger(RoarHash);
    }

    // Animation Event en el momento del rugido.
    // Invoca todos los enemigos restantes de la oleada.
    public void SpawnEnemiesFromRoar()
    {
        if (!isSpawning || !isRoaring)
            return;

        SoundManager.Instance.PlaySFX(SoundType.SFX, 1);

        while (HasEnemiesRemainingToSpawn())
        {
            if (!SpawnOneEnemy())
                break;
        }

        if (!HasEnemiesRemainingToSpawn())
           waitingForWaveClear = waitForSummonedEnemiesToDie && !HasEnemiesRemainingToSpawn();
    }

    // Animation Event al final del clip Roar.
    public void FinishRoarAnimation()
    {
        isRoaring = false;

        if (!isSpawning)
            return;

        if (!HasEnemiesRemainingToSpawn())
        {
            if (waitForSummonedEnemiesToDie)
            {
                waitingForWaveClear = true;
                TryAdvanceAfterQueenWave();
            }
            else
            {
                waitingForWaveClear = false;
                AdvanceToNextWave();
            }

            return;
        }

        waitingForWaveClear = false;
        ScheduleNextRoar();
    }
    private bool SpawnOneEnemy()
    {
        Wave wave = waves[currentWaveIndex];
        int groupIndex = ChooseRandomRemainingGroup();

        if (groupIndex < 0)
            return false;

        EnemyGroup group = wave.enemyGroups[groupIndex];
        Transform spawnPoint = GetRandomSpawnPoint();

        GameObject enemyObject = enemyPool.GetObject(
            group.prefab,
            spawnPoint.position,
            Quaternion.identity,
            false
        );

        EnemyController enemy = enemyObject.GetComponent<EnemyController>();

        if (enemy == null)
        {
            Debug.LogError(
                $"{enemyObject.name} no tiene EnemyController.",
                enemyObject
            );

            enemyPool.ReturnObject(enemyObject);
            remainingByGroup[groupIndex] = 0;
            return true;
        }

        Vector3 spawnPosition = spawnPoint.position;

        if (enemy.IsFlying)
            spawnPosition += Vector3.up * flyingSpawnHeight;

        enemyObject.transform.SetPositionAndRotation(
            spawnPosition,
            spawnPoint.rotation
        );

        enemy.ConfigureForSpawn(player, enemyPool);
        enemy.ReturnedToPool += OnEnemyReturnedToPool;
        activeEnemies.Add(enemy);

        remainingByGroup[groupIndex]--;
        enemyObject.SetActive(true);

        Debug.Log(
            $"Oleada {currentWaveIndex + 1}: apareció {enemyObject.name}.",
            enemyObject
        );

        return true;
    }

    private int ChooseRandomRemainingGroup()
    {
        int totalRemaining = 0;

        foreach (int remaining in remainingByGroup)
            totalRemaining += remaining;

        if (totalRemaining <= 0)
            return -1;

        int choice = UnityEngine.Random.Range(0, totalRemaining);

        for (int i = 0; i < remainingByGroup.Count; i++)
        {
            choice -= remainingByGroup[i];

            if (choice < 0)
                return i;
        }

        return -1;
    }

    private bool HasEnemiesRemainingToSpawn()
    {
        foreach (int remaining in remainingByGroup)
        {
            if (remaining > 0)
                return true;
        }

        return false;
    }

    private Transform GetRandomSpawnPoint()
    {
        bool chooseLeft = UnityEngine.Random.value < 0.5f;
        Transform[] chosenSide = chooseLeft ? leftSpawnPoints : rightSpawnPoints;
        Transform[] otherSide = chooseLeft ? rightSpawnPoints : leftSpawnPoints;

        List<Transform> validPoints = GetValidPoints(chosenSide);

        if (validPoints.Count == 0)
            validPoints = GetValidPoints(otherSide);

        return validPoints.Count > 0
            ? validPoints[UnityEngine.Random.Range(0, validPoints.Count)]
            : transform;
    }

    private List<Transform> GetValidPoints(Transform[] points)
    {
        List<Transform> validPoints = new();

        if (points == null)
            return validPoints;

        foreach (Transform point in points)
        {
            if (point != null)
                validPoints.Add(point);
        }

        return validPoints;
    }
    private void TryAdvanceAfterQueenWave()
    {
        if (!isSpawning ||
            !waitingForWaveClear ||
            activeEnemies.Count > 0)
        {
            return;
        }

        waitingForWaveClear = false;
        AdvanceToNextWave();
    }
    public void StopSpawningAndClearEnemies()
    {
        StopSpawning();

        foreach (EnemyController enemy in activeEnemies)
        {
            if (enemy == null)
                continue;

            enemy.ReturnedToPool -= OnEnemyReturnedToPool;

            if (enemyPool != null)
                enemyPool.ReturnObject(enemy.gameObject);
            else
                enemy.gameObject.SetActive(false);
        }

        activeEnemies.Clear();
    }
    private void OnEnemyReturnedToPool(EnemyController enemy)
    {
        if (enemy == null)
            return;

        enemy.ReturnedToPool -= OnEnemyReturnedToPool;
        activeEnemies.Remove(enemy);

        TryAdvanceAfterQueenWave();
    }

    private void OnDestroy()
    {
        foreach (EnemyController enemy in activeEnemies)
        {
            if (enemy != null)
                enemy.ReturnedToPool -= OnEnemyReturnedToPool;
        }
    }
}