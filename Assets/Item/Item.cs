using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="New Item", menuName ="Item")]
public class Item : ScriptableObject
{
    public string itemID;
    public string itemName = "New item";
    public int MaxAmount = 1;
    public float coolTimeNow = 0;
    public Sprite icon = null;
    public GameObject ItemPrefab = null;

    public enum ItemType { Gem, Heart, Scroll, Artifact, etc};
    public ItemType itemType;

    [Header("Element Type")]
    public bool Earth_Type;
    public bool Fire_Type;
    public bool Life_Type;
    public bool Lightning_Type;
    public bool Water_Type;
    public bool Wind_Type;    
}
