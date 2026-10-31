using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class ObjectPooling : MonoBehaviour
{
    [Serializable]
    private class PoolEntry
    {
        public GameObject prefab;
        [Min(0)] public int initialAmount = 5;
    }

    [Header("Pools")]
    [SerializeField] private PoolEntry[] pools;
    [SerializeField] private Transform poolPosition;
    [Min(1)]
    [SerializeField] private int expansionAmount = 5;

    private readonly Dictionary<GameObject, Stack<GameObject>> availableByPrefab = new();
    private readonly Dictionary<GameObject, GameObject> prefabByInstance = new();
    private readonly HashSet<GameObject> availableObjects = new();

    private Transform PoolParent => poolPosition != null ? poolPosition : transform;

    private void Awake()
    {
        if (pools == null)
            return;

        foreach (PoolEntry entry in pools)
        {
            if (entry == null || entry.prefab == null)
            {
                Debug.LogError("Hay un pool sin prefab asignado.", this);
                continue;
            }

            if (availableByPrefab.ContainsKey(entry.prefab))
            {
                Debug.LogWarning(
                    $"El prefab {entry.prefab.name} está repetido en Pools.",
                    this
                );
                continue;
            }

            availableByPrefab.Add(entry.prefab, new Stack<GameObject>());
            AddObjects(entry.prefab, entry.initialAmount);
        }
    }

    public GameObject GetObject(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        bool activate = true)
    {
        if (prefab == null)
        {
            Debug.LogError("Se intentó solicitar un objeto con prefab nulo.", this);
            return null;
        }

        if (!availableByPrefab.TryGetValue(prefab, out Stack<GameObject> pool))
        {
            Debug.LogError(
                $"El prefab {prefab.name} no está registrado en Pools.",
                prefab
            );
            return null;
        }

        if (pool.Count == 0)
            AddObjects(prefab, expansionAmount);

        if (pool.Count == 0)
            return null;

        GameObject obj = pool.Pop();
        availableObjects.Remove(obj);

        obj.transform.SetParent(null, false);
        obj.transform.localScale = prefab.transform.localScale;
        obj.transform.SetPositionAndRotation(position, rotation);

        if (activate)
            obj.SetActive(true);

        return obj;
    }

    [Button]
    public void ReturnObject(GameObject obj)
    {
        if (obj == null || availableObjects.Contains(obj))
            return;

        if (!prefabByInstance.TryGetValue(obj, out GameObject prefab) ||
            !availableByPrefab.TryGetValue(prefab, out Stack<GameObject> pool))
        {
            Debug.LogWarning(
                $"{obj.name} no pertenece a este ObjectPooling.",
                obj
            );
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(PoolParent, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = prefab.transform.localScale;

        availableObjects.Add(obj);
        pool.Push(obj);
    }

    private void AddObjects(GameObject prefab, int amount)
    {
        if (amount <= 0)
            return;

        if (!availableByPrefab.TryGetValue(prefab, out Stack<GameObject> pool))
            return;

        for (int i = 0; i < amount; i++)
        {
            GameObject obj = Instantiate(prefab, PoolParent);
            obj.name = prefab.name;
            obj.SetActive(false);

            prefabByInstance[obj] = prefab;
            availableObjects.Add(obj);
            pool.Push(obj);
        }
    }
}