using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class SystemManager : MonoBehaviour
{
    public delegate void EnemyDeadCallback(Vector2 deadPos);
    public EnemyDeadCallback enemyDeadCallback;
    // public static event EnemyDeadCallback EnemyDeadEvent;

    #region Singleton
    private static SystemManager instance;
    public static SystemManager Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<SystemManager>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<SystemManager>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    public float playerTimeScale = 1f; //플레이어만 사용하는 타임스케일
    public float timeScale = 1f; //전역으로 사용하는 타임스케일

    [Header("Refer")]
    public List<Camera> camList = new List<Camera>();
    MagicInfo lifeSeedMagic;

    [Header("Material")]
    public Material spriteMat; //일반 스프라이트 머터리얼
    public Material outLineMat; //아웃라인 머터리얼
    public Material hitMat; //맞았을때 단색 머터리얼

    [Header("Color")]
    public Color stopColor; //시간 멈췄을때 색깔
    public Color hitColor; //맞았을때 깜빡일 색깔
    public Color DeadColor; //죽을때 점점 변할 색깔

    public void AllTimeScale(float scale)
    {
        playerTimeScale = scale;
        timeScale = scale;
    }

    public void AddDropSeedEvent(MagicInfo magic)
    {
        enemyDeadCallback += DropLifeSeed;

        // Heal Seed 마법 찾기
        lifeSeedMagic = magic;
    }

    public void DropLifeSeed(Vector2 dropPos)
    {
        // 크리티컬 확률 = 드랍 확률
        // print(MagicDB.Instance.MagicCritical(magic));
        bool isDrop = MagicDB.Instance.MagicCritical(lifeSeedMagic);
        //크리티컬 데미지만큼 회복
        int healAmount = Mathf.RoundToInt(MagicDB.Instance.MagicCriticalPower(lifeSeedMagic));
        healAmount = (int)Mathf.Clamp(healAmount, 1f, healAmount); //최소 회복량 1f 보장

        // HealSeed 마법 크리티컬 확률에 따라 드랍
        if (isDrop)
        {
            Transform itemPool = ObjectPool.Instance.transform.Find("ItemPool");
            GameObject healSeed = LeanPool.Spawn(ItemDB.Instance.heartSeed, dropPos, Quaternion.identity, itemPool);

            // 아이템에 체력 회복량 넣기
            healSeed.GetComponent<ItemManager>().amount = healAmount;
        }
    }
}
