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

    private Stack<GameObject> Pool = new Stack<GameObject>();

    private void Awake()
    {
        AddObjects(InitialAmount);
    }

    [Button]
    public GameObject GetObject()
    {
        // Si no quedan objetos disponibles, expandimos el pool.
        if (Pool.Count == 0)
        {
            AddObjects(ExpansionAmount);
        }

        GameObject obj = Pool.Pop();
        obj.SetActive(true);
        return obj;
    }

    [Button]
    public void ReturnObject(GameObject obj)
    {
        if (obj == null)
            return;

        obj.SetActive(false);
        Pool.Push(obj);
    }

    private void AddObjects(int amount)
    {
        if (Reference == null)
        {
            Debug.LogError("Asigna un prefab en Reference.", this);
            return;
        }

        Transform parent = PoolPosition != null ? PoolPosition : transform;
        Vector3 position = parent.position;

        for (int i = 0; i < amount; i++)
        {
            GameObject obj = Instantiate(Reference, position, Quaternion.identity, parent);
            obj.SetActive(false);
            Pool.Push(obj);
        }
    }
}