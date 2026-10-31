using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManager : MonoBehaviour
{
    public static ScenesManager instance;

    public Animator FadeAnim;

    [SerializeField] private float fadeDuration = 1f;




    public Action OnChangeScene;


    [Header("Victoria del nivel")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField, Min(0f)] private float victoryPanelDuration = 3f;
    [SerializeField] private string nextSceneAfterVictory;

    private bool levelVictoryStarted;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);


        FadeAnim = GetComponentInChildren<Animator>();

        if (victoryPanel != null)
            victoryPanel.SetActive(false);


    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

    }
    public void Play()
    {
        Debug.Log("Play");
        StartCoroutine(SceneLoad("2D"));
    }
    public void YouWon()
    {
        Debug.Log("Ganaste");
        StartCoroutine(SceneLoad("Win"));
    }
    public void Quit()
    {
        Debug.Log("Saliendo");
        Application.Quit();
    }

    public void GameOver()
    {
        SceneManager.LoadScene("GameOver");
    }
 
    
    public IEnumerator SceneLoad(string scene)
    {
        FadeAnim.SetTrigger("Fade");

        yield return new WaitForSeconds(fadeDuration);
        SceneManager.LoadScene(scene);



    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        OnChangeScene?.Invoke();
    }

    public void CompleteLevel()
    {
        if (levelVictoryStarted)
            return;

        levelVictoryStarted = true;

        if (victoryPanel != null)
            victoryPanel.SetActive(true);
        else
            Debug.LogWarning("No asignaste el panel de victoria.", this);

        StartCoroutine(ContinueAfterVictory());
    }

    private IEnumerator ContinueAfterVictory()
    {
        yield return new WaitForSecondsRealtime(victoryPanelDuration);

        if (string.IsNullOrWhiteSpace(nextSceneAfterVictory))
        {
            Debug.LogWarning("No asignaste la siguiente escena de victoria.", this);
            yield break;
        }

        yield return SceneLoad(nextSceneAfterVictory);
    }

}