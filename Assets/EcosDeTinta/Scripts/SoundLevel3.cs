using System.Collections;
using UnityEngine;

public class SoundLevel3 : MonoBehaviour
{
    public float Time=3f;
    void Start()
    {
        StartCoroutine(LateStart(Time));
    }
    IEnumerator LateStart(float time)
    {
        yield return new WaitForSeconds(time);

        SoundManager.Instance.PlayAmbient(SoundType.Ambient, 0);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
