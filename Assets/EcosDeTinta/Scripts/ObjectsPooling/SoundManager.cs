
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using EcosDeTinta.Collections;

public class SoundManager : MonoBehaviour
{
    [FoldoutGroup("References")]
    public MusicDatabase musicDatabase;
    [FoldoutGroup("References")]
    public SoundPlayer SoundPlayerPrefab;



    [FoldoutGroup("References")]
    public List<SoundPlayer> SoundPlayerList;

    public MyQueue<SoundPlayer> MusicPool = new();
    public MyQueue<SoundPlayer> SFXPool = new();
    public MyQueue<SoundPlayer> UIPool = new();
    public MyQueue<SoundPlayer> AmbientPool = new();
    public MyQueue<SoundPlayer> VoicePool = new();

    public int size = 20;

    [FoldoutGroup("Music Background")]
    [SerializeField] private float fadeOutDuration = 2;

    [FoldoutGroup("Music Background")]
    [SerializeField] private float fadeInDuration = 2;
    [FoldoutGroup("Music Background")]
    [SerializeField] private SoundPlayer currentMusic;
    [FoldoutGroup("Music Background")]
    [SerializeField] private SoundType SoundType;
    [FoldoutGroup("Music Background")]
    [SerializeField] private int ID;



    public static Action<SoundPlayer> OnFinishAudio;

    void Start()
    {
        ExpandSoundPlayerMusic(size);
        ExpandSoundPlayerSFX(size);
        ExpandSoundPlayerUI(size);
        ExpandSoundPlayerAmbient(size);
        ExpandSoundPlayerVoice(size);

        Invoke(nameof(PlayMusicBackground), 1.5f);

    }
    [Button]
    public void PlayMusicBackground()
    {

        PlayMusic(SoundType, ID);
    }

    private void OnEnable()
    {
        OnFinishAudio += EnqueueAudio;
    }
    private void OnDisable()
    {
        OnFinishAudio -= EnqueueAudio;
    }

    public void CheckTypeAudio(SoundType type, int id)
    {
        switch (type)
        {
            case SoundType.None:
                Debug.LogWarning("El SoundPlayer tiene un tipo None, no se puede encolar.");

                break;

            case SoundType.Music:
                PlayMusic(type, id);
                break;

            case SoundType.UI:
                PlayUI(type, id);
                break;

            case SoundType.Ambient:
                PlayAmbient(type, id);
                break;

            case SoundType.Voice:
                PlayVoice(type, id);
                break;

            case SoundType.SFX:
                PlaySFX(type, id);
                break;

            default:
                Debug.LogWarning("El SoundPlayer no tiene un tipo válido asignado: " + type);
                break;

        }
    }

    public void PlayMusic(SoundType type, int id)
    {
        /* if (MusicPool.Head == null || MusicPool.Count == 0)
         {
             Debug.Log("Se agrando la lista");
             ExpandSoundPlayerMusic(5);
             // PlayAudio(audioName);
             return;
         }
         AudioData data = musicDatabase.GetAudio(type, id);

         SoundPlayer soundPlayer = SoundPlayerList[0];
         soundPlayer = MusicPool.Dequeue();
         soundPlayer.gameObject.SetActive(true);
         soundPlayer.PlayAudio(data.Clip, data.Volume, type);*/



        if (currentMusic != null)
        {
            currentMusic.FadeOut(fadeOutDuration);
        }

        if (MusicPool.Head == null || MusicPool.Count == 0)
        {
            ExpandSoundPlayerMusic(5);
        }

        AudioData data = musicDatabase.GetAudio(type, id);

        currentMusic = MusicPool.Dequeue();
        currentMusic.gameObject.SetActive(true);

        currentMusic.FadeIn(data.Clip, data.Volume, fadeInDuration, type);
    }

    public void PlaySFX(SoundType type, int id)
    {
        if (SFXPool.Head == null || SFXPool.Count == 0)
        {
            Debug.Log("Se agrando la lista");
            ExpandSoundPlayerSFX(5);
            // PlayAudio(audioName);
            return;
        }
        AudioData data = musicDatabase.GetAudio(type, id);
        SoundPlayer soundPlayer = SoundPlayerList[1];
        soundPlayer = SFXPool.Dequeue();
        soundPlayer.gameObject.SetActive(true);
        soundPlayer.PlayAudio(data.Clip, data.Volume, type);
    }
    public void PlayUI(SoundType type, int id)
    {
        if (UIPool.Head == null || UIPool.Count == 0)
        {
            Debug.Log("Se agrando la lista");
            ExpandSoundPlayerUI(5);
            // PlayAudio(audioName);
            return;
        }
        AudioData data = musicDatabase.GetAudio(type, id);

        SoundPlayer soundPlayer = SoundPlayerList[2];
        soundPlayer = UIPool.Dequeue();
        soundPlayer.gameObject.SetActive(true);
        soundPlayer.PlayAudio(data.Clip, data.Volume, type);
    }
    public void PlayAmbient(SoundType type, int id)
    {
        if (AmbientPool.Head == null || AmbientPool.Count == 0)
        {
            Debug.Log("Se agrando la lista");
            ExpandSoundPlayerAmbient(5);
            // PlayAudio(audioName);
            return;
        }
        AudioData data = musicDatabase.GetAudio(type, id);

        SoundPlayer soundPlayer = SoundPlayerList[3];
        soundPlayer = AmbientPool.Dequeue();
        soundPlayer.gameObject.SetActive(true);
        soundPlayer.PlayAudio(data.Clip, data.Volume, type);
    }
    public void PlayVoice(SoundType type, int id)
    {
        if (VoicePool.Head == null || VoicePool.Count == 0)
        {
            Debug.Log("Se agrando la lista");
            ExpandSoundPlayerVoice(5);
            // PlayAudio(audioName);
            return;
        }
        AudioData data = musicDatabase.GetAudio(type, id);

        SoundPlayer soundPlayer = SoundPlayerList[4];
        soundPlayer = VoicePool.Dequeue();
        soundPlayer.gameObject.SetActive(true);
        soundPlayer.PlayAudio(data.Clip, data.Volume, type);
    }
    private void EnqueueAudio(SoundPlayer soundPlayer)
    {
        if (soundPlayer == currentMusic)
        {
            currentMusic = null;
        }
        soundPlayer.gameObject.SetActive(false);

        switch (soundPlayer.AudioType)
        {
            case SoundType.None:
                Debug.LogWarning("El SoundPlayer tiene un tipo None, no se puede encolar.");
                Destroy(soundPlayer.gameObject);
                break;
            case SoundType.Music:
                MusicPool.Enqueue(soundPlayer);
                break;
            case SoundType.SFX:
                SFXPool.Enqueue(soundPlayer);
                break;
            case SoundType.UI:
                UIPool.Enqueue(soundPlayer);
                break;
            case SoundType.Ambient:
                AmbientPool.Enqueue(soundPlayer);
                break;
            case SoundType.Voice:
                VoicePool.Enqueue(soundPlayer);
                break;
            default:
                Debug.LogWarning("El SoundPlayer no tiene un tipo válido asignado: " + soundPlayer.AudioType);
                Destroy(soundPlayer.gameObject);
                break;
        }
    }
    [Button]
    private void ExpandSoundPlayerMusic(int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            SoundPlayer Music = Instantiate(SoundPlayerList[0], transform);
            Music.gameObject.SetActive(false);
            MusicPool.Enqueue(Music);

        }

    }
    private void ExpandSoundPlayerSFX(int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            SoundPlayer SFX = Instantiate(SoundPlayerList[1], transform);
            SFX.gameObject.SetActive(false);
            SFXPool.Enqueue(SFX);

        }
    }
    private void ExpandSoundPlayerUI(int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            SoundPlayer UI = Instantiate(SoundPlayerList[2], transform);
            UI.gameObject.SetActive(false);
            UIPool.Enqueue(UI);

        }
    }
    private void ExpandSoundPlayerAmbient(int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            SoundPlayer Ambient = Instantiate(SoundPlayerList[3], transform);
            Ambient.gameObject.SetActive(false);
            AmbientPool.Enqueue(Ambient);

        }
    }
    private void ExpandSoundPlayerVoice(int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {

            SoundPlayer Voices = Instantiate(SoundPlayerList[4], transform);
            Voices.gameObject.SetActive(false);
            VoicePool.Enqueue(Voices);

        }
    }

    [Button]
    public void TestAudio(SoundType type, int id)
    {
        CheckTypeAudio(type, id);
        Debug.Log(SFXPool.Count);
    }
    [Button]
    public void TestCount()
    {

        Debug.Log(SFXPool.Count);
    }

    [Button]
    public void StopMusicBackground(float fadeDuration)
    {
        if (currentMusic == null)
            return;

        currentMusic.FadeOut(fadeDuration);

    }


    public SoundPlayer CurrentMusic => currentMusic;
}