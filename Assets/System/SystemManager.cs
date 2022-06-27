using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Experimental.Rendering.Universal;

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
    public float globalTimeScale = 1f; //전역으로 사용하는 타임스케일
    public float portalRange = 100f; //포탈게이트 생성될 범위
    public float time_start; //시작 시간
    public float time_current; // 현재 스테이지 플레이 타임
    public int killCount; //몬스터 킬 수
    public float globalLightDefault = 0.9f; //글로벌 라이트 기본값

    [Header("Refer")]
    public Light2D globalLight;
    public Transform enemyPool;
    public Transform itemPool;
    public Transform overlayPool;
    public Transform magicPool;
    public Transform effectPool;
    public Transform camParent;
    MagicInfo lifeSeedMagic;
    public Sprite gateIcon; //포탈게이트 아이콘
    public Sprite questionMark; //물음표 스프라이트
    public Button timeBtn; //! 시간 속도 토글 버튼
    public Button godModBtn; //! 갓모드 토글 버튼

    [Header("Prefab")]
    public GameObject portalGate; //다음 맵 넘어가는 포탈게이트 프리팹
    public GameObject dmgTxtPrefab; //데미지 텍스트 UI
    public GameObject ghostPrefab; // 잔상 효과 프리팹
    public GameObject pointPrefab; // 위치 디버깅용 프리팹

    [Header("Material")]
    public Material spriteMat; //일반 스프라이트 머터리얼
    public Material outLineMat; //아웃라인 머터리얼
    public Material hitMat; //맞았을때 단색 머터리얼
    public Material ghostHDRMat; //고스팅 마법 HDR 머터리얼
    public Material verticalFillMat; // Vertical Fill Sprite 머터리얼

    [Header("Color")]
    public Color stopColor; //시간 멈췄을때 컬러
    public Color hitColor; //맞았을때 flash 컬러
    public Color poisonColor; //독 데미지 flash 컬러
    public Color DeadColor; //죽을때 서서히 변할 컬러

    private void Awake()
    {
        //초기화
        // StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        Time.timeScale = 0f;

        //TODO 로딩 UI 띄우기
        print("로딩 시작");

        //모두 로드 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);
        yield return new WaitUntil(() => ItemDB.Instance.loadDone);
        yield return new WaitUntil(() => EnemyDB.Instance.loadDone);

        //TODO 로딩 UI 끄기
        print("로딩 완료");

        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        //다음맵으로 넘어가는 포탈게이트 생성하기
        SpawnPortalGate();
    }

    void SpawnPortalGate()
    {
        //포탈이 생성될 위치
        Vector2 pos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * portalRange;

        //포탈 게이트 생성
        GameObject gate = LeanPool.Spawn(portalGate, pos, Quaternion.identity);
    }

    public Color HexToRGBA(string hex)
    {
        Color color;
        ColorUtility.TryParseHtmlString("#" + hex, out color);

        return color;
    }

    public void AllTimeScale(float scale)
    {
        playerTimeScale = scale;
        globalTimeScale = scale;
    }

    public void TimeStopToggle()
    {
        Image timeImg = timeBtn.GetComponent<Image>();
        TextMeshProUGUI timeTxt = timeBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>();

        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;

            timeImg.color = Color.green;
        }
        else
        {
            Time.timeScale = 0f;

            timeImg.color = Color.red;
        }

        timeTxt.text = "TimeSpeed = " + Time.timeScale;
    }

    public void GodModeToggle()
    {
        Image godModImg = godModBtn.GetComponent<Image>();
        TextMeshProUGUI godModTxt = godModBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>();

        PlayerManager.Instance.godMod = !PlayerManager.Instance.godMod;

        if (PlayerManager.Instance.godMod)
        {
            godModImg.color = Color.green;
            godModTxt.text = "GodMod On";
        }
        else
        {
            godModImg.color = Color.red;
            godModTxt.text = "GodMod Off";
        }
    }

    // public void AddDropSeedEvent(MagicInfo magic)
    // {
    //     //적이 죽을때 함수를 호출하도록 델리게이트에 넣기
    //     enemyDeadCallback += DropLifeSeed;

    //     // Heal Seed 마법 찾기
    //     lifeSeedMagic = magic;
    // }

    // // Life Seed 드랍하기
    // public void DropLifeSeed(Vector2 dropPos)
    // {
    //     // print(MagicDB.Instance.MagicCritical(magic));

    //     // 크리티컬 확률 = 드랍 확률
    //     bool isDrop = MagicDB.Instance.MagicCritical(lifeSeedMagic);

    //     //크리티컬 데미지 = 회복량
    //     int healAmount = Mathf.RoundToInt(MagicDB.Instance.MagicCriticalPower(lifeSeedMagic));
    //     healAmount = (int)Mathf.Clamp(healAmount, 1f, healAmount); //최소 회복량 1f 보장

    //     // HealSeed 마법 크리티컬 확률에 따라 드랍
    //     if (isDrop)
    //     {
    //         GameObject mushroom = LeanPool.Spawn(ItemDB.Instance.lifeMushroom, dropPos, Quaternion.identity, itemPool);

    //         // 아이템에 체력 회복량 넣기
    //         mushroom.GetComponent<ItemManager>().amount = healAmount;

    //         //아이템 리지드 찾기
    //         Rigidbody2D itemRigid = mushroom.GetComponent<Rigidbody2D>();

    //         // 랜덤 방향으로 아이템 날리기
    //         itemRigid.velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * Random.Range(3f, 5f);

    //         // 아이템 랜덤 회전 시키기
    //         itemRigid.angularVelocity = Random.value < 0.5f ? 360f : -360f;
    //     }
    // }
}
