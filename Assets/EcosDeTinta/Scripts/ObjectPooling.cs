using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class ObjectPooling : MonoBehaviour
{
    [Header("Object Pooling")]
    [SerializeField] private GameObject Reference;
    [SerializeField] private Transform PoolPosition;
    [SerializeField] private int InitialAmount = 10;
    [SerializeField] private int ExpansionAmount = 10;

    private readonly Stack<GameObject> pool = new Stack<GameObject>();
    private readonly HashSet<GameObject> availableObjects = new HashSet<GameObject>();

    private Transform PoolParent =>
        PoolPosition != null ? PoolPosition : transform;

    private void Awake()
    {
        AddObjects(InitialAmount);
    }

    [Button]
    public GameObject GetObject()
    {
        return GetObject(PoolParent.position, Quaternion.identity);
    }

    // El spawner puede dejarlo inactivo mientras lo configura.
    public GameObject GetObject(
        Vector3 position,
        Quaternion rotation,
        bool activate = true)
    {
        if (pool.Count == 0)
            AddObjects(ExpansionAmount);

        if (pool.Count == 0)
            return null;

        GameObject obj = pool.Pop();
        availableObjects.Remove(obj);

        obj.transform.SetParent(null, true);
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

        obj.SetActive(false);
        obj.transform.SetParent(PoolParent, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        availableObjects.Add(obj);
        pool.Push(obj);
    }

    private void AddObjects(int amount)
    {
        if (Reference == null)
        {
            Debug.LogError("Asigna un prefab en Reference.", this);
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            GameObject obj = Instantiate(
                Reference,
                PoolParent.position,
                Quaternion.identity,
                PoolParent
            );

            obj.SetActive(false);
            availableObjects.Add(obj);
            pool.Push(obj);
        }
    }
}