using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Lean.Pool;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    public PortalState portalState; // 현재 포탈상태
    public enum PortalState { Idle, GemReceive, BossAlive, MobRemain, Clear };
    int gemType; // 필요 젬 타입
    [SerializeField] float maxGem; //필요 젬 개수
    [SerializeField] float nowGem; //현재 젬 개수
    float refundRate = 0.2f; // 클리어시 원소젬 환불 계수
    float delayCount; //상호작용 딜레이 카운트
    [SerializeField] float interactDelay = 0.1f; //상호작용 딜레이
    [SerializeField] float farDistance = 150f; //해당 거리 이상 벌어지면 포탈 이동
    IEnumerator payCoroutine;
    [SerializeField] Character bossCharacter;
    [SerializeField] Character fixedBoss; // 고정된 보스 소환

    [Header("Refer")]
    Interacter interacter; //상호작용 콜백 함수 클래스
    [SerializeField] CanvasGroup showKeyUI; //상호작용 키 표시 UI
    [SerializeField] TextMeshProUGUI pressAction; // 상호작용 기능 설명 텍스트
    [SerializeField] CanvasGroup gemNum; //젬 개수 표시 텍스트
    [SerializeField] Image GemIcon; // 젬 아이콘
    [SerializeField] TextMeshProUGUI pressKey; //상호작용 인디케이터
    [SerializeField] Animator anim; //포탈 이펙트 애니메이션
    [SerializeField] SpriteRenderer gaugeImg; //포탈 테두리 원형 게이지 이미지


    private void Awake()
    {
        // 다른 오브젝트가 이미 있을때
        if (instance != null)
        {
            // 파괴 후 리턴
            Destroy(gameObject);
            return;
        }
        // 최초 생성 됬을때
        else
        {
            instance = this;

            // 파괴되지 않게 설정
            DontDestroyOnLoad(gameObject);
        }

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

        // 맵 속성 따라서 필요한 젬 타입 초기화
        // gemType = Random.Range(0, 6);
        gemType = (int)SystemManager.Instance.nowMapElement;

        // 필요한 젬 개수 초기화
        maxGem = Random.Range(30, 50);
        nowGem = 0;

        // 젬 게이지 머터리얼 초기화
        gaugeImg.material.SetFloat("_Angle", 302f); // 시작지점 각도 갱신
        gaugeImg.material.SetFloat("_Arc1", 64f); // 마지막 지점 각도 갱신
        // 상호작용 텍스트 초기화
        pressAction.text = "Pay Gem";
        // 젬 개수 UI 갱신
        UpdateGemNum();

        //상호작용 표시 비활성화
        showKeyUI.alpha = 0;

        //포탈 이펙트 오브젝트 비활성화
        anim.gameObject.SetActive(false);

        // 상호작용 트리거 함수 콜백에 연결 시키기
        if (interacter.interactTriggerCallback == null)
            interacter.interactTriggerCallback += InteractTrigger;

        // 상호작용 함수 콜백에 연결 시키기
        if (interacter.interactSubmitCallback == null)
            interacter.interactSubmitCallback += InteractSubmit;

        // 원소젬 받기 상태로 초기화
        portalState = PortalState.Idle;

        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 젬 타입 UI 색깔 갱신
        GemIcon.color = MagicDB.Instance.GetElementColor(gemType);
    }

    private void Update()
    {
        if (!SystemManager.Instance.loadDone)
            return;

        // 플레이어와 거리 너무 멀어지면 위치 이동
        if (PlayerManager.Instance != null)
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

        // 젬이 부족할때
        if (nowGem < maxGem)
            pressAction.text = "Pay Gem";

        // 맵 클리어시
        if (portalState == PortalState.Clear)
            pressAction.text = "Enter Portal";

        // 상호작용 키 표시 켜기
        if (able)
        {
            // 젬이 부족할때 
            if (portalState == PortalState.Idle
            || portalState == PortalState.GemReceive)
            {
                // 상호작용 키 표시
                showKeyUI.alpha = 1;
                // 젬 개수 표시
                gemNum.alpha = 1;
            }
            // 클리어시
            if (portalState == PortalState.Clear)
                // 상호작용 키 표시
                showKeyUI.alpha = 1;
        }
        // 끌때는 언제나 끄기
        else
        {
            // print($"끄기 : {Time.time}");
            showKeyUI.alpha = 0;
            // 젬 개수 UI 비활성화
            gemNum.alpha = 0;
        }
    }

    // 포탈 상호작용
    public void InteractSubmit(bool isPress = true)
    {
        // 인디케이터 꺼져있으면 리턴
        if (showKeyUI.alpha == 0)
            return;

        // 첫번째 젬 넣을때
        if (portalState == PortalState.Idle)
        {
            // 젬 받기 이후 상태로 전환
            portalState = PortalState.GemReceive;

            //생성된 포탈 게이트 위치 보여주는 아이콘 화살표 UI 켜기
            StartCoroutine(UIManager.Instance.PointObject(gameObject, SystemManager.Instance.gateIcon));
        }

        // 젬 넣는 상태일때
        if (portalState == PortalState.Idle
        || portalState == PortalState.GemReceive)
        {
            // 젬 넣기 시작
            if (payCoroutine == null)
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
        }

        // 보스 및 잔여 몹 다 잡고 클리어 일때
        if (portalState == PortalState.Clear)
        {
            // 포탈 상태 초기화
            portalState = PortalState.Idle;

            // 상호작용시 포탈 게이트 트랜지션
            StartCoroutine(ClearTeleport());
        }
    }

    IEnumerator PayGem()
    {
        float payDelay = 0.1f;
        // 계속 지불 중이면 반복
        while (portalState == PortalState.GemReceive && nowGem < maxGem)
        {
            // 플레이어 젬 하나씩 소모
            PlayerManager.Instance.PayGem(gemType, 1);

            // 젬 하나씩 넣기
            nowGem++;

            //젬 개수 UI 갱신
            UpdateGemNum();

            // 최대치만큼 넣으면
            if (nowGem == maxGem)
            {
                // 상호작용 키 끄기
                showKeyUI.alpha = 0;
                // 젬 개수 UI 비활성화
                gemNum.alpha = 0;

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
        gemNum.GetComponent<TextMeshProUGUI>().text = nowGem.ToString() + " / " + maxGem.ToString();

        // 시작 지점 각도 수집
        float startAngle = gaugeImg.material.GetFloat("_Arc1");

        //젬 개수만큼 테두리 도넛 게이지 갱신
        float gaugeFill = ((maxGem - nowGem) / maxGem) * (360f - startAngle);
        gaugeFill = Mathf.Clamp(gaugeFill, 0, (360f - startAngle));

        // 시작부터 마지막 지점까지 채우는 양 갱신
        gaugeImg.material.SetFloat("_Arc2", gaugeFill);
    }

    IEnumerator SummonBoss()
    {
        //보스 리스트
        List<EnemyInfo> bosses = new List<EnemyInfo>();

        //타입이 보스인 몬스터 찾기
        foreach (KeyValuePair<int, EnemyInfo> enemy in EnemyDB.Instance.enemyDB)
        {
            // 타입이 보스면
            if (enemy.Value.enemyType == EnemyDB.EnemyType.Boss.ToString())
            {
                // 해당 몹의 원소 속성 반환
                int enemyElement = System.Array.FindIndex(MagicDB.Instance.ElementNames, x => x == enemy.Value.elementType);

                // 해당 월드 속성과 같으면
                if (enemyElement == (int)SystemManager.Instance.nowMapElement)
                    // 보스 풀에 넣기
                    bosses.Add(enemy.Value);
            }
        };

        // 보스풀에서 하나 랜덤 선택
        EnemyInfo bossInfo = bosses[Random.Range(0, bosses.Count)];

        //! 보스 정보 고정
        if (fixedBoss != null)
            bossInfo = new EnemyInfo(EnemyDB.Instance.GetEnemyByName(fixedBoss.name.Split('_')[0]));

        //보스 소환 위치
        Vector2 bossPos = (Vector2)transform.position + Random.insideUnitCircle.normalized * Random.Range(5f, 10f);

        //보스 프리팹 찾기
        GameObject bossPrefab = EnemyDB.Instance.GetPrefab(bossInfo.id);

        // 보스 소환
        // StartCoroutine(WorldSpawner.Instance.PortalSpawn(bossInfo, false, bossPos, bossPrefab, true));
        GameObject bossInstance = LeanPool.Spawn(bossPrefab, bossPos, Quaternion.identity, ObjectPool.Instance.enemyPool);
        bossCharacter = bossInstance.GetComponent<Character>();

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

        // 클리어 트리거 켜기
        portalState = PortalState.Clear;
    }

    IEnumerator ClearTeleport()
    {
        yield return null;

        // 시간 멈추고
        SystemManager.Instance.TimeScaleChange(0f);

        // 현재 맵 속성 조회해서 int로 변환
        int nowIndex = (int)SystemManager.Instance.nowMapElement;

        // 마지막 스테이지가 아닐때
        if (nowIndex < 5)
        {
            //todo 포탈 가운데로 빛 모이는 파티클
            //todo 플레이어가 포탈 가운데로 들어가면서 포탈로 카메라 고정
            //todo 플레이어 및 포탈 하얗게 변하고
            //todo 포탈 왜곡되면서 디스폰
            //todo 플레이어 및 포탈 길쭉해지며 승천
            //todo 천천히 날리는 빛 파티클 조금 남김
            //todo 화면 암전 했다가 풀면서
            //todo 왜곡되면서 플레이어 스폰
            //todo 새로운 맵에 길쭉한 하얀 오브젝트 내려와서
            //todo 하얀 플레이어로 변하고 플레이어 초기화
            //todo 새 맵 초기화

            // 다음 맵 인덱스로 증가
            SystemManager.Instance.nowMapElement = (SystemManager.MapElement)(nowIndex + 1);

            // 트랜지션 이후 새로운 인게임 씬 켜기
            SystemManager.Instance.StartGame();

            // 포탈 게이트 디스폰
            LeanPool.Despawn(gameObject);
        }
        // 마지막 스테이지일때
        else
        {
            // 플레이어 컨트롤 끄기
            SystemManager.Instance.ToggleInput(true);

            // 게임 오버 UI 켜기
            SystemManager.Instance.GameOverPanelOpen(true);

            //todo 게임오버 BGM 재생

            // 포탈 게이트 디스폰
            LeanPool.Despawn(gameObject);
        }
    }
}
