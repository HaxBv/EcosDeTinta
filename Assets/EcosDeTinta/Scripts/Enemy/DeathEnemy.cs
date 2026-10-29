using UnityEngine;

public class DeathEnemy : MonoBehaviour
{
    [Header("Object Pooling")]
    [SerializeField] private ObjectPooling pool;

    void Start()
    {
        if (pool == null)
        {
            pool = FindFirstObjectByType<ObjectPooling>();
        }
    }

    public void Morir()
    {
        if (pool != null)
        {
            pool.ReturnObject(gameObject);
        }
        else
        {
            Debug.LogWarning(
                "No se encontró ObjectPooling."
            );

            gameObject.SetActive(false);
        }
    }
}
