using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public class ObjectPooling : MonoBehaviour
{
    [Header("Object Pooling")]
    public GameObject Reference;
    private Stack<GameObject> Pool = new Stack<GameObject>();

    [SerializeField] private Transform PoolPosition;



    [Button]
    public GameObject GetObject()
    {
        if (Pool.Count > 0)
        {
            GameObject obj = Pool.Pop();
            obj.SetActive(true);
            return obj;
        }
        else
        {
            GameObject obj = Instantiate(Reference, PoolPosition.position, Quaternion.identity);
            return obj;
        }
    }
    [Button]
    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);


        Pool.Push(obj);
    }

}
