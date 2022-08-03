using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Lean.Pool;

public class GatePortal : MonoBehaviour
{
    float maxGem; //필요 젬 개수
    float nowGem; //현재 젬 개수
    float delayCount; //상호작용 딜레이 카운트
    [SerializeField]
    float interactDelay = 1f; //상호작용 딜레이
    [SerializeField]
    float farDistance = 150f; //해당 거리 이상 벌어지면 포탈 이동

    [Header("Refer")]
    Interacter interacter; //상호작용 콜백 함수 클래스
    [SerializeField]
    GameObject showKey; //상호작용 키 표시 UI
    [SerializeField]
    TextMeshProUGUI GemNum; //젬 개수 표시 UI
    [SerializeField]
    TextMeshProUGUI pressKey; //상호작용 인디케이터
    [SerializeField]
    Animator anim; //포탈 이펙트 애니메이션
    [SerializeField]
    SpriteRenderer gaugeImg; //포탈 테두리 원형 게이지 이미지

    [Header("Debug")]
    public EnemyManager fixedBoss; //! 고정된 보스 소환

    private void Awake()
    {
        interacter = GetComponent<Interacter>();
    }

    private void OnEnable()
    {
        Init();
    }

    void Init()
    {
        maxGem = Random.Range(10, 100);

        //! 테스트 
        maxGem = 10;

        //젬 개수 UI 갱신
        UpdateGemNum();

        //상호작용 표시 비활성화
        showKey.SetActive(false);

        //포탈 이펙트 오브젝트 비활성화
        anim.gameObject.SetActive(false);

        // 상호작용 함수 콜백에 연결 시키기
        interacter.interactSubmitCallback += InteractSubmit;

        // 상호작용 트리거 함수 콜백에 연결 시키기
        interacter.interactTriggerCallback += InteractTrigger;
    }

    private void Update()
    {
        // 젬 넣기 딜레이 카운트 차감
        if (delayCount > 0)
            delayCount -= Time.deltaTime;

        //TODO 플레이어와 거리 너무 멀어지면 위치 이동
        MoveClose();
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
    public void InteractSubmit()
    {
        // 딜레이 중이면 리턴
        if (delayCount > 0)
            return;

        // 인디케이터 꺼져있으면 리턴
        if (!showKey.activeSelf)
            return;

        //첫번째 젬 넣을때
        if (nowGem == 0)
            //생성된 포탈 게이트 위치 보여주는 아이콘 화살표 UI
            StartCoroutine(UIManager.Instance.PointObject(gameObject, SystemManager.Instance.gateIcon));

        //todo 플레이어 젬 소모

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

        //상호작용 딜레이 초기화
        delayCount = interactDelay;
    }

    void UpdateGemNum()
    {
        //젬 개수 UI 갱신
        GemNum.text = nowGem.ToString() + " / " + maxGem.ToString();

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
        bossInfo = new EnemyInfo(EnemyDB.Instance.GetEnemyByName(fixedBoss.name.Split('_')[0]));

        //보스 프리팹 찾기
        GameObject bossPrefab = EnemyDB.Instance.GetPrefab(bossInfo.id);

        //보스 소환 위치
        Vector2 bossPos = (Vector2)transform.position + Random.insideUnitCircle * 10f;

        // 보스 소환
        GameObject bossObj = LeanPool.Spawn(bossPrefab, bossPos, Quaternion.identity, SystemManager.Instance.enemyPool);

        // 보스 enemyManager 참조
        EnemyManager enemyManager = bossObj.GetComponent<EnemyManager>();

        // 보스 초기화 시작
        enemyManager.initialStart = true;

        //포탈에서 보스 소환
        // StartCoroutine(EnemySpawn.Instance.PortalSpawn(bosses[Random.Range(0, bosses.Count)], false, bossPos, bossObj));

        // 보스 소환 후 포탈 이펙트 활성화
        anim.gameObject.SetActive(true);

        // 보스 죽을때까지 대기
        yield return new WaitUntil(() => enemyManager.isDead);

        print("boss dead");

        // 몬스터 스폰 멈추기
        EnemySpawn.Instance.spawnSwitch = false;

        // 남은 몬스터 화살표로 방향 표시해주기
        UIManager.Instance.enemyPointSwitch = true;

        // 모든 몬스터 죽을때까지 대기
        yield return new WaitUntil(() => EnemySpawn.Instance.spawnEnemyList.Count == 0f);

        print("all dead");

        // PortalOpen 트리거 true / Open, Idle 애니메이션 순서대로 시작
        anim.SetTrigger("PortalOpen");
    }

    void MoveClose()
    {
        // farDistance 보다 멀어지면
        float distance = Vector2.Distance(transform.position, PlayerManager.Instance.transform.position);
        if (distance >= farDistance)
        {
            //포탈이 생성될 위치
            Vector2 pos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * SystemManager.Instance.portalRange;

            // 플레이어 주변으로 재이동
            transform.position = pos;
        }
    }
}
