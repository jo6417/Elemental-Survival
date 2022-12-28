using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable, CreateAssetMenu(fileName = "SoundBundleList", menuName = "Scriptable Objects/SoundBundleList", order = 1)]
public class SoundBundleList : ScriptableObject
{
    public List<SoundBundle> soundBundles;
}
