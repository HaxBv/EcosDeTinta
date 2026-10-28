using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioData", menuName = "EcosDeTinta/AudioData")]
[InlineEditor]
public class AudioData : ScriptableObject
{
    #region Privates/Properties
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0f, 1f)] private float volume = 1.0f;

    #endregion

    #region Getters
    public AudioClip Clip => clip;
    public float Volume => volume;

    #endregion
}
