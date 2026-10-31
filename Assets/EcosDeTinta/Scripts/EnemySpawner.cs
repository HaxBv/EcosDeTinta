using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemySpawner : MonoBehaviour
{
    private enum EnemyType
    {
        Fast,
        Flyer,
        Tank
    }

    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private ObjectPooling enemyPool;

    [Header("Prefabs")]
    [SerializeField] private GameObject fastPrefab;
    [SerializeField] private GameObject flyerPrefab;
    [SerializeField] private GameObject tankPrefab;

    [Header("Cantidad en esta escena")]
    [Min(0)][SerializeField] private int fastCount = 4;
    [Min(0)][SerializeField] private int flyerCount = 3;
    [Min(0)][SerializeField] private int tankCount = 2;

    [Header("Desbloqueo desde que empieza esta escena")]
    [Min(0f)][SerializeField] private float flyerUnlockTime = 10f;
    [Min(0f)][SerializeField] private float tankUnlockTime = 20f;

    [Header("Puntos de aparición")]
    [SerializeField] private Transform[] leftSpawnPoints = new Transform[2];
    [SerializeField] private Transform[] rightSpawnPoints = new Transform[2];

    [Header("Intervalo entre apariciones")]
    [Min(0.1f)][SerializeField] private float minSpawnInterval = 2f;
    [Min(0.1f)][SerializeField] private float maxSpawnInterval = 4f;

    [Header("Al derrotar a todos los enemigos de esta escena")]
    [SerializeField] private UnityEvent onAllEnemiesDefeated;

    private readonly HashSet<EnemyController> activeEnemies = new();
    private readonly Queue<EnemyType> guaranteedSpawns = new();
    private readonly int[] remainingByType = new int[3];

    private float sceneElapsed;
    private float spawnTimer;

    private bool flyersUnlocked;
    private bool tanksUnlocked;
    private bool completionInvoked;
    private bool isSpawning;

    [Header("Límite en pantalla")]
    [Min(1)][SerializeField] private int maxEnemiesOnScreen = 5;
    [Min(0.1f)][SerializeField] private float retryDelayOnFailure = 0.5f;



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

        if (enemyPool == null)
        {
            Debug.LogError("Asigna el ObjectPooling de esta escena.", this);
            return;
        }

        StartSpawning();
    }


    private void Update()
    {
        if (!isSpawning || completionInvoked)
            return;

        sceneElapsed += Time.deltaTime;
        UnlockEnemiesByTime();

        // Al alcanzar el máximo, el contador queda pausado.
        if (activeEnemies.Count >= maxEnemiesOnScreen)
            return;

        spawnTimer -= Time.deltaTime;

        if (spawnTimer > 0f)
            return;

        if (TrySpawnNextEnemy())
            spawnTimer = GetRandomSpawnInterval();
        else
            spawnTimer = retryDelayOnFailure;

        CheckCompletion();
    }

    // Puedes llamarlo desde un evento si desactivas el inicio automático.
    public void StartSpawning()
    {
        if (player == null || enemyPool == null)
        {
            Debug.LogError("El spawner necesita Player y ObjectPooling.", this);
            return;
        }

        remainingByType[(int)EnemyType.Fast] = fastCount;
        remainingByType[(int)EnemyType.Flyer] = flyerCount;
        remainingByType[(int)EnemyType.Tank] = tankCount;

        guaranteedSpawns.Clear();

        sceneElapsed = 0f;
        spawnTimer = GetRandomSpawnInterval();
        flyersUnlocked = false;
        tanksUnlocked = false;
        completionInvoked = false;
        isSpawning = true;

        // El primer enemigo será veloz, si hay alguno configurado.
        if (fastCount > 0)
            guaranteedSpawns.Enqueue(EnemyType.Fast);

        Debug.Log("Comienza el spawner de esta escena.", this);
        CheckCompletion();
    }

    private void UnlockEnemiesByTime()
    {
        if (!flyersUnlocked && sceneElapsed >= flyerUnlockTime)
        {
            flyersUnlocked = true;

            if (remainingByType[(int)EnemyType.Flyer] > 0)
                guaranteedSpawns.Enqueue(EnemyType.Flyer);

            Debug.Log("Se desbloquearon los enemigos voladores.", this);
        }

        if (!tanksUnlocked && sceneElapsed >= tankUnlockTime)
        {
            tanksUnlocked = true;

            if (remainingByType[(int)EnemyType.Tank] > 0)
                guaranteedSpawns.Enqueue(EnemyType.Tank);

            Debug.Log("Se desbloquearon los enemigos tanque.", this);
        }
    }
    private bool TrySpawnNextEnemy()
    {
        if (!TryChooseEnemyType(out EnemyType type, out bool isGuaranteed))
            return false;

        if (!TryChooseSpawnPoint(out Transform spawnPoint))
            return false;

        GameObject prefab = GetPrefab(type);

        if (prefab == null)
        {
            Debug.LogError($"No hay prefab asignado para {type}.", this);
            return false;
        }

        GameObject enemyObject = enemyPool.GetObject(
            prefab,
            spawnPoint.position,
            Quaternion.identity,
            false
        );

        if (enemyObject == null)
            return false;

        EnemyController enemy = enemyObject.GetComponent<EnemyController>();

        if (enemy == null)
        {
            Debug.LogError(
                $"{enemyObject.name} no tiene EnemyController.",
                enemyObject
            );

            enemyPool.ReturnObject(enemyObject);
            return false;
        }

        enemy.ConfigureForSpawn(player, enemyPool);
        enemy.ReturnedToPool += OnEnemyReturnedToPool;
        activeEnemies.Add(enemy);

        remainingByType[(int)type]--;

        if (isGuaranteed && guaranteedSpawns.Count > 0)
            guaranteedSpawns.Dequeue();

        enemyObject.SetActive(true);

        Debug.Log(
            $"Apareció {type}. Restantes de este tipo: " +
            $"{remainingByType[(int)type]}",
            enemyObject
        );

        return true;
    }

    private bool TryChooseEnemyType(
        out EnemyType selectedType,
        out bool isGuaranteed)
    {
        while (guaranteedSpawns.Count > 0)
        {
            EnemyType guaranteedType = guaranteedSpawns.Peek();

            if (remainingByType[(int)guaranteedType] > 0 &&
                IsTypeUnlocked(guaranteedType))
            {
                selectedType = guaranteedType;
                isGuaranteed = true;
                return true;
            }

            guaranteedSpawns.Dequeue();
        }

        int totalAvailable = 0;

        for (int i = 0; i < remainingByType.Length; i++)
        {
            EnemyType type = (EnemyType)i;

            if (IsTypeUnlocked(type))
                totalAvailable += remainingByType[i];
        }

        if (totalAvailable == 0)
        {
            selectedType = default;
            isGuaranteed = false;
            return false;
        }

        int randomValue = Random.Range(0, totalAvailable);

        for (int i = 0; i < remainingByType.Length; i++)
        {
            EnemyType type = (EnemyType)i;

            if (!IsTypeUnlocked(type))
                continue;

            randomValue -= remainingByType[i];

            if (randomValue < 0)
            {
                selectedType = type;
                isGuaranteed = false;
                return true;
            }
        }

        selectedType = default;
        isGuaranteed = false;
        return false;
    }

    private bool IsTypeUnlocked(EnemyType type)
    {
        return type switch
        {
            EnemyType.Fast => true,
            EnemyType.Flyer => flyersUnlocked,
            EnemyType.Tank => tanksUnlocked,
            _ => false
        };
    }

    private GameObject GetPrefab(EnemyType type)
    {
        return type switch
        {
            EnemyType.Fast => fastPrefab,
            EnemyType.Flyer => flyerPrefab,
            EnemyType.Tank => tankPrefab,
            _ => null
        };
    }

    private bool TryChooseSpawnPoint(out Transform spawnPoint)
    {
        // Primero se elige izquierda o derecha; luego un punto de ese lado.
        bool chooseLeft = Random.value < 0.5f;
        Transform[] sidePoints = chooseLeft
            ? leftSpawnPoints
            : rightSpawnPoints;

        List<Transform> validPoints = new();

        if (sidePoints != null)
        {
            foreach (Transform point in sidePoints)
            {
                if (point != null)
                    validPoints.Add(point);
            }
        }

        if (validPoints.Count == 0)
        {
            Debug.LogWarning(
                $"No hay puntos asignados en el lado " +
                $"{(chooseLeft ? "izquierdo" : "derecho")}.",
                this
            );

            spawnPoint = null;
            return false;
        }

        spawnPoint = validPoints[Random.Range(0, validPoints.Count)];
        return true;
    }

    private void OnEnemyReturnedToPool(EnemyController enemy)
    {
        if (enemy == null)
            return;

        enemy.ReturnedToPool -= OnEnemyReturnedToPool;
        activeEnemies.Remove(enemy);

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (completionInvoked || !isSpawning)
            return;

        foreach (int remaining in remainingByType)
        {
            if (remaining > 0)
                return;
        }

        if (activeEnemies.Count > 0)
            return;

        completionInvoked = true;
        isSpawning = false;

        Debug.Log("Todos los enemigos de esta escena fueron derrotados.", this);
        onAllEnemiesDefeated?.Invoke();
    }

    private float GetRandomSpawnInterval()
    {
        float min = Mathf.Min(minSpawnInterval, maxSpawnInterval);
        float max = Mathf.Max(minSpawnInterval, maxSpawnInterval);

        return Random.Range(min, max);
    }
}