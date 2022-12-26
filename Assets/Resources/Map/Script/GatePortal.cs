using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Lean.Pool;
using UnityEngine.UI;

public class GatePortal : MonoBehaviour
{
    #region Singleton
    private static GatePortal instance;
    public static GatePortal Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<GatePortal>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    // var newObj = new GameObject().AddComponent<GatePortal>();
                    // instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    [Header("State")]
    int gemType; // 필요 젬 타입
    float maxGem; //필요 젬 개수
    float nowGem; //현재 젬 개수
    float refundRate = 0.2f; // 클리어시 원소젬 환불 계수
    float delayCount; //상호작용 딜레이 카운트
    [SerializeField] float interactDelay = 0.1f; //상호작용 딜레이
    [SerializeField] float farDistance = 150f; //해당 거리 이상 벌어지면 포탈 이동
    [SerializeField] bool nowPay = false; // 젬 넣고 있는지 여부
    IEnumerator payCoroutine;
    public Character bossCharacter;

    [Header("Refer")]
    Interacter interacter; //상호작용 콜백 함수 클래스
    [SerializeField] GameObject showKey; //상호작용 키 표시 UI
    [SerializeField] TextMeshProUGUI gemNum; //젬 개수 표시 UI
    [SerializeField] TextMeshProUGUI pressKey; //상호작용 인디케이터
    [SerializeField] Animator anim; //포탈 이펙트 애니메이션
    [SerializeField] SpriteRenderer gaugeImg; //포탈 테두리 원형 게이지 이미지

    [Header("Debug")]
    public Character fixedBoss; //! 고정된 보스 소환

    private void Awake()
    {
        interacter = GetComponent<Interacter>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 보스 변수 초기화
        bossCharacter = null;

        // 필요한 젬 타입 지정
        gemType = Random.Range(0, 6);
        // 필요한 젬 개수 지정
        maxGem = Random.Range(30, 50);

        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 젬 타입 UI 색깔 갱신
        gemNum.GetComponentInChildren<Image>().color = MagicDB.Instance.GetElementColor(gemType);

        //젬 개수 UI 갱신
        UpdateGemNum();

        //상호작용 표시 비활성화
        showKey.SetActive(false);

        //포탈 이펙트 오브젝트 비활성화
        anim.gameObject.SetActive(false);

        // 상호작용 트리거 함수 콜백에 연결 시키기
        interacter.interactTriggerCallback += InteractTrigger;

        // 상호작용 함수 콜백에 연결 시키기
        interacter.interactSubmitCallback += InteractSubmit;
    }

    private void Update()
    {
        // 플레이어와 거리 너무 멀어지면 위치 이동
        MoveClose();
    }

    void MoveClose()
    {
        // farDistance 보다 멀어지면
        float distance = Vector2.Distance(transform.position, PlayerManager.Instance.transform.position);
        if (distance >= farDistance)
        {
            //포탈이 생성될 위치
            Vector2 pos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * MapManager.Instance.portalRange;

            // 플레이어 주변으로 재이동
            transform.position = pos;
        }
    }

    public void InteractTrigger(bool able)
    {
        //todo 플레이어 상호작용 키가 어떤 키인지 표시
        // pressKey.text = 

        if (able && nowGem < maxGem)
            showKey.SetActive(true);
        else
            showKey.SetActive(false);
    }

    // 포탈 상호작용
    public void InteractSubmit(bool isPress = true)
    {
        // 인디케이터 꺼져있으면 리턴
        if (!showKey.activeSelf)
            return;

        // 지불 여부 갱신
        nowPay = isPress;

        // 젬 넣기 시작
        if (nowPay && payCoroutine == null)
        {
            payCoroutine = PayGem();
            StartCoroutine(payCoroutine);
        }
        // 젬 넣기 종료
        else
        {
            StopCoroutine(payCoroutine);
            payCoroutine = null;
        }

        //첫번째 젬 넣을때
        if (nowGem == 0)
            //생성된 포탈 게이트 위치 보여주는 아이콘 화살표 UI 켜기
            StartCoroutine(UIManager.Instance.PointObject(gameObject, SystemManager.Instance.gateIcon));
    }

    IEnumerator PayGem()
    {
        float payDelay = 0.1f;
        // 계속 지불 중이면 반복
        while (nowPay && nowGem < maxGem)
        {
            // 플레이어 젬 하나씩 소모
            PlayerManager.Instance.PayGem(gemType, 1);

            // 젬 하나씩 넣기
            nowGem++;

            //젬 개수 UI 갱신
            UpdateGemNum();

            if (nowGem == maxGem)
            {
                // 상호작용 인디케이터 끄기
                showKey.SetActive(false);

                //보스 소환
                StartCoroutine(SummonBoss());
            }

            // 페이 딜레이 감소 및 값제한
            payDelay -= 0.01f;
            payDelay = Mathf.Clamp(payDelay, Time.deltaTime, 1f);

            yield return new WaitForSeconds(payDelay);
        }
    }

    void UpdateGemNum()
    {
        //젬 개수 UI 갱신
        gemNum.text = nowGem.ToString() + " / " + maxGem.ToString();

        //젬 개수만큼 테두리 도넛 게이지 갱신
        float gaugeFill = ((maxGem - nowGem) / maxGem) * 360f;
        gaugeFill = Mathf.Clamp(gaugeFill, 0, 360f);

        gaugeImg.material.SetFloat("_Arc2", gaugeFill);
    }

    IEnumerator SummonBoss()
    {
        //보스 리스트
        List<EnemyInfo> bosses = new List<EnemyInfo>();

        //타입이 보스인 몬스터 찾기
        foreach (KeyValuePair<int, EnemyInfo> value in EnemyDB.Instance.enemyDB)
        {
            //타입이 보스면
            if (value.Value.enemyType == EnemyDB.EnemyType.Boss.ToString())
            {
                //리스트에 포함
                bosses.Add(value.Value);
            }
        };

        // 보스 정보 찾기
        EnemyInfo bossInfo = bosses[Random.Range(0, bosses.Count)];

        //! 보스 정보 고정
        if (fixedBoss != null)
            bossInfo = new EnemyInfo(EnemyDB.Instance.GetEnemyByName(fixedBoss.name.Split('_')[0]));

        //보스 소환 위치
        Vector2 bossPos = (Vector2)transform.position + Random.insideUnitCircle * 10f;

        //보스 프리팹 찾기
        GameObject bossPrefab = EnemyDB.Instance.GetPrefab(bossInfo.id);

        print(bossInfo.name);

        // 보스 소환
        StartCoroutine(WorldSpawner.Instance.PortalSpawn(bossInfo, false, bossPos, bossPrefab, true));

        // 보스 죽을때까지 대기
        yield return new WaitUntil(() => bossCharacter != null && bossCharacter.isDead);

        // 보스 변수 초기화
        bossCharacter = null;

        print("boss dead");

        // 몬스터 스폰 멈추기
        WorldSpawner.Instance.spawnSwitch = false;

        // 남은 몬스터 화살표로 방향 표시해주기
        UIManager.Instance.enemyPointSwitch = true;

        // 모든 몬스터 죽을때까지 대기
        yield return new WaitUntil(() => WorldSpawner.Instance.spawnEnemyList.Count == 0f);

        print("all dead");

        // PortalOpen 트리거 true / Open, Idle 애니메이션 순서대로 시작
        anim.SetTrigger("PortalOpen");

        // 클리어 보상 드랍
        StartCoroutine(ClearReward());
    }

    IEnumerator ClearReward()
    {
        // 드랍 시킬 원소젬 뽑기
        ItemInfo gemInfo = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Gem);
        // 게이트에 투입된 원소젬 개수에 환불 계수 곱하기
        int dropNum = Mathf.RoundToInt(Random.Range(maxGem * (1 - refundRate), maxGem * (1 + refundRate)));

        // 트럭 버튼 드랍
        ItemInfo truckBtnInfo = new ItemInfo(ItemDB.Instance.GetItemByName("TruckButton"));
        StartCoroutine(ItemDB.Instance.ItemDrop(truckBtnInfo, transform.position, Vector2.down * Random.Range(30f, 40f)));

        // 원소젬 순차적 드랍
        WaitForSeconds singleDropTime = new WaitForSeconds(Time.deltaTime);
        while (dropNum > 0)
        {
            // 게이트 위치에서 아래 방향으로 하나씩 드랍
            StartCoroutine(ItemDB.Instance.ItemDrop(gemInfo, transform.position, Vector2.down * Random.Range(30f, 40f)));

            dropNum--;

            yield return singleDropTime;
        }
    }
}
