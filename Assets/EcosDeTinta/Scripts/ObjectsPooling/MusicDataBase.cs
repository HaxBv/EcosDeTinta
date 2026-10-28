using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MusicDatabase", menuName = "EcosDeTinta/MusicDatabase")]
public class MusicDatabase : SerializedScriptableObject
{
    public Dictionary<SoundType, List<AudioData>> ClipsDataBase = new();

    public AudioData GetAudio(SoundType type, int id)
    {
        if (ClipsDataBase.TryGetValue(type, out List<AudioData> entities))
        {
            return entities[id];
        }
        else
        {
            throw new System.Exception("El id del audio al que intentas acceder no existe");
        }
    }
}
