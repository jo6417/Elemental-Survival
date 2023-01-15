using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    void SoundPlayGlobal(string soundName)
    {
        SoundManager.Instance.PlaySound(soundName);
    }

    void SoundPlayHere(string soundName)
    {
        SoundManager.Instance.PlaySound(soundName, transform.position);
    }
}
