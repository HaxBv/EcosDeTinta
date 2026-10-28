using UnityEngine;

public class GameManager : MonoBehaviour
{
    //-> Singelton
    public static GameManager Instance;

    [Header("Player References")]
    public PlayerController PlayerController;
    public BeatemupCamera FirstPersonCameraController;
    private void Awake()
    {
        if(Instance != null && Instance !=this)
        {
            Destroy(gameObject);
            return;        
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        
    }

    
    void Update()
    {
        
    }
}
