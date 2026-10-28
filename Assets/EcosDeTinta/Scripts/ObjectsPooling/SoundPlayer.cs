using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SoundPlayer : MonoBehaviour
{


    private AudioSource audioSource;
    private SoundType audiotype;


    private Coroutine fadeCoroutine;
    private float targetVolume;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    void Start()
    {

    }
    public void PlayAudio(AudioClip clip, float volume, SoundType type)
    {

        CancelInvoke();

        audiotype = type;
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();

        Invoke(nameof(ReturnToPool), clip.length);
    }
    public void ReturnToPool()
    {
        //->comunicarme con mi music pool
        //-> regresar a casa :c 
        CancelInvoke();

        audioSource.Stop();
        audioSource.clip = null;

        SoundManager.OnFinishAudio?.Invoke(this);
    }

    public void FadeIn(AudioClip clip, float targetVolume, float duration, SoundType type)
    {
        CancelInvoke();

        audiotype = type;
        audioSource.clip = clip;
        audioSource.volume = 0f;
        audioSource.Play();

        Invoke(nameof(ReturnToPool), clip.length);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeInRoutine(targetVolume, duration));
    }
    public void FadeOut(float duration)
    {
        if (!gameObject.activeInHierarchy)
            return;
        CancelInvoke();

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOutRoutine(duration));
    }
    private IEnumerator FadeInRoutine(float targetVolume, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, time / duration);

            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVolume = audioSource.volume;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);

            yield return null;
        }

        ReturnToPool();
    }


    public SoundType AudioType => audiotype;
}