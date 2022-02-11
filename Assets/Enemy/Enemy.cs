using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="New Enemy", menuName ="Enemy")]
public class Enemy : ScriptableObject
{
    public string EnemyID;
    public string EnemyName = "New Enemy";
    public Sprite icon = null;
    public GameObject EnemyPrefab = null;

    [Header("Stat")]
    public float atkPower = 1; //공격력
    public float dropRate = 0.5f; //아이템 드롭 확률
    public float hitDelay = 0.2f; //맞은 후 경직시간
    public float HpMax = 2; //최대 체력
    public float knockbackForce = 1; //넉백 되는 힘

    [Header("Element Type")] //몬스터 원소속성에 따라 피해 계수 추가
    public bool Earth_Type;
    public bool Fire_Type;
    public bool Life_Type;
    public bool Lightning_Type;
    public bool Water_Type;
    public bool Wind_Type;    
}
