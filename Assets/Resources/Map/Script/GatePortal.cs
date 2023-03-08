using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Lean.Pool;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

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
    [SerializeField] float minGem = 0f; // 필요 젬 최소량
    [SerializeField] float maxGem = 100f; // 필요 젬 최대량
    [SerializeField] int needGem; //필요 젬 개수
    [SerializeField] int nowGem; //현재 젬 개수
    float refundRate = 0.2f; // 클리어시 원소젬 환불 계수
    float delayCount; //상호작용 딜레이 카운트
    [SerializeField] float interactDelay = 0.1f; //상호작용 딜레이
    [SerializeField] float farDistance = 150f; //해당 거리 이상 벌어지면 포탈 이동
    IEnumerator payCoroutine;
    [SerializeField] Character bossCharacter;
    [SerializeField] Character fixedBoss; // 고정된 보스 소환
    IEnumerator closeMoveCoroutine; // 플레이어와 거리 멀면 재이동 코루틴

    [Header("Refer")]
    [SerializeField] CanvasGroup showKeyUI; //상호작용 키 표시 UI
    Interacter interacter; //상호작용 콜백 함수 클래스
    [SerializeField] TextMeshProUGUI pressAction; // 상호작용 기능 설명 텍스트
    [SerializeField] Image gemBackground; // 젬 텍스트 배경
    [SerializeField] CanvasGroup gemNum; //젬 개수 표시 텍스트
    [SerializeField] Image GemIcon; // 젬 아이콘
    [SerializeField] TextMeshProUGUI pressKey; // 바인딩 된 키 이름
    [SerializeField] Transform portalPivot; // 게이트 피벗 오브젝트
    [SerializeField] Animator portalAnim; //포탈 이펙트 애니메이션
    // [SerializeField] SpriteRenderer gateFrame; // 포탈 테두리
    [SerializeField] SpriteRenderer gaugeImg; //포탈 테두리 원형 게이지 이미지
    [SerializeField] SpriteRenderer portalCover; // 포탈 빛날때 이미지
    [SerializeField] SpriteRenderer beam; // 포탈 전송시 나타나는 빔
    [SerializeField] GameObject beamParticle; // 빔 사라질때 남기는 파티클
    [SerializeField] ParticleManager gatherParticle; // 에너지 충전시 시작되는 파티클
    [SerializeField] ParticleManager teleportParticle; // 포탈 전송시 빔이 남기는 파티클
    [SerializeField] Animator bossBtnAnim; // 보스 버튼 이펙트 애니메이션
    [SerializeField] ParticleManager insertGemEffect; // 젬 넣을때 이펙트
    [SerializeField] GameObject gemMaxEffect; // 젬 max일때 이펙트
    [SerializeField] GameObject portalOpenEffect; // 포탈 오픈시 이펙트

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
            // DontDestroyOnLoad(gameObject);
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

        // 빔 및 이펙트 꺼서 초기화
        beam.transform.localScale = new Vector2(0, 10f);
        beamParticle.SetActive(false);

        // 포탈 이펙트 초기화
        gatherParticle.gameObject.SetActive(false);
        teleportParticle.gameObject.SetActive(false);

        // 젬 개수 UI 비활성화
        gemNum.alpha = 0;

        // 보스 버튼 이미지 끄기
        showKeyUI.GetComponent<Image>().enabled = false;
        // 버튼 테두리 이펙트 애니메이션 끄기
        bossBtnAnim.gameObject.SetActive(false);

        yield return new WaitUntil(() => SystemManager.Instance != null);

        // 맵 속성 따라서 필요한 젬 타입 초기화
        // gemType = Random.Range(0, 6);
        gemType = (int)SystemManager.Instance.NowMapElement;

        // 필요한 젬 개수 초기화
        needGem = (int)(Random.Range(minGem, maxGem) / 10f) * 10;
        nowGem = 0;

        // 젬 게이지 머터리얼 초기화
        gaugeImg.material.SetFloat("_Angle", 302f); // 시작지점 각도 갱신
        gaugeImg.material.SetFloat("_Arc1", 64f); // 마지막 지점 각도 갱신
        // 상호작용 텍스트 초기화
        ActionText("Pay Gem");
        // 젬 개수 UI 갱신
        UpdateGemNum();

        //상호작용 표시 비활성화
        showKeyUI.alpha = 0;

        //포탈 이펙트 오브젝트 비활성화
        // portalAnim.gameObject.SetActive(false);

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

        // 카메라에 포탈이 보이면 아이콘 화살표 띄워서 가리키기
        StartCoroutine(VisibleAction());

        // 플레이어와 멀어지면 위치 재이동
        closeMoveCoroutine = MoveClose();
        StartCoroutine(closeMoveCoroutine);
    }

    IEnumerator VisibleAction()
    {
        // 포탈 내부 스프라이트
        SpriteRenderer portalSprite = portalAnim.GetComponent<SpriteRenderer>();

        // 해당 스프라이트가 카메라에 보일때까지 대기
        yield return new WaitUntil(() => portalSprite.isVisible);

        // 아이콘 화살표가 게이트 위치 계속 가리키기
        StartCoroutine(UIManager.Instance.PointObject(portalSprite, SystemManager.Instance.gateIcon));

        // 멀어지면 위치 이동하는 코루틴 정지
        StopCoroutine(closeMoveCoroutine);
    }

    IEnumerator MoveClose()
    {
        // 플레이어까지 거리가 farDistance 보다 멀어지면
        yield return new WaitUntil(() => Vector2.Distance(transform.position, PlayerManager.Instance.transform.position) >= farDistance);

        //포탈이 생성될 위치
        Vector2 pos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * MapManager.Instance.portalRange;

        // 플레이어 주변으로 재이동
        transform.position = pos;

        // 코루틴 재실행
        closeMoveCoroutine = MoveClose();
        StartCoroutine(closeMoveCoroutine);
    }

    public void InteractTrigger(bool isTrigger)
    {
        //todo 플레이어 상호작용 키가 어떤 키인지 표시
        // pressKey.text = 

        // 젬이 부족할때
        if (nowGem < needGem)
            ActionText("Pay Gem");

        // 젬이 최대치일때
        if (nowGem == needGem)
            ActionText("Boss Summon", isTrigger);

        // 맵 클리어시
        if (portalState == PortalState.Clear)
            ActionText("Enter Portal");

        // 상호작용 키 표시 켜기
        if (isTrigger)
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

    void ActionText(string description, bool isTrigger = true)
    {
        // 텍스트 입력
        pressAction.text = description;

        //todo 바인딩된 키 입력
        // pressKey.text = "E";

        // UI 사이즈 강제 초기화
        Canvas.ForceUpdateCanvases();

        // 원소젬 최대일때
        if (portalState == PortalState.GemReceive && nowGem == needGem)
        {
            // 보스 버튼 이미지 켜기
            showKeyUI.GetComponent<Image>().enabled = isTrigger;
            // 버튼 테두리 이펙트 애니메이션 켜기
            bossBtnAnim.gameObject.SetActive(isTrigger);
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

            // 이제부터 포탈게이트 근처에서 몬스터 스폰
            WorldSpawner.Instance.gateSpawn = true;
            // 몬스터 반대편으로 옮기기 정지
            // WorldSpawner.Instance.dragSwitch = false;
        }

        // 젬 넣는 상태일때
        if (portalState == PortalState.Idle
        || portalState == PortalState.GemReceive)
        {
            // 젬이 최대치일때
            if (nowGem == needGem)
            {
                // 키를 눌렀을때
                if (isPress)
                {
                    //보스 소환
                    StartCoroutine(SummonBoss());
                }
            }
            // 젬이 최대치 이하로 들어있을때
            else
            {
                // 젬 넣기 시작
                if (payCoroutine == null)
                {
                    payCoroutine = InsertGem();
                    StartCoroutine(payCoroutine);
                }
                // 젬 넣기 종료
                else
                {
                    StopCoroutine(payCoroutine);
                    payCoroutine = null;
                }
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

    IEnumerator InsertGem()
    {
        // float payDelay = 0.1f;
        // 계속 지불 중이면 반복
        while (portalState == PortalState.GemReceive && nowGem < needGem)
        {
            // 플레이어 젬 부족시, 상호작용 범위 벗어나면
            if (PlayerManager.Instance.GetGem(gemType) <= 0
            || showKeyUI.alpha == 0)
            {
                // 젬 부족 효과음 재생
                SoundManager.Instance.PlaySound("Denied");

                // 젬 부족 인디케이터 재생
                // 기존 트윈 끄기
                gemBackground.DOKill();
                gemBackground.color = new Color(1, 0, 0, 0);
                // 빨갛게 2회 점멸하기
                gemBackground.DOColor(new Color(1, 0, 0, 0.5f), 0.2f)
                .SetLoops(4, LoopType.Yoyo)
                .OnKill(() =>
                {
                    // 투명하게 초기화
                    gemBackground.color = new Color(1, 0, 0, 0);
                });

                // 플레이어 젬 인디케이터 재생
                UIManager.Instance.GemIndicator(gemType, Color.red);

                // 코루틴 초기화
                payCoroutine = null;
                break;
            }

            // 플레이어 젬 하나씩 소모
            PlayerManager.Instance.PayGem(gemType, 1);

            // 젬 하나씩 넣기
            nowGem++;

            // 원소젬 넣을때 이펙트 생성
            LeanPool.Spawn(insertGemEffect, portalAnim.transform.position, Quaternion.identity, portalAnim.transform);
            // 젬 넣기 사운드 재생
            SoundManager.Instance.PlaySound("Gate_InsertGem", transform.position);

            // 포탈 준비 파티클 시작
            if (nowGem > 0)
                gatherParticle.gameObject.SetActive(true);

            //젬 개수 UI 갱신
            UpdateGemNum();

            // // 페이 딜레이 감소 및 값제한
            // payDelay -= 0.01f;
            // payDelay = Mathf.Clamp(payDelay, Time.deltaTime, 1f);

            yield return new WaitForSeconds(Time.deltaTime);
        }

        // 젬이 최대치일때
        if (nowGem == needGem)
        {
            // 원소젬 max 이펙트 재생
            gemMaxEffect.SetActive(true);
            // 젬 max 사운드 재생
            SoundManager.Instance.PlaySound("Gate_GemMax", transform.position);

            // 보스 소환 메시지로 전환
            ActionText("Boss Summon");
        }
    }

    void UpdateGemNum()
    {
        //젬 개수 UI 갱신
        gemNum.GetComponent<TextMeshProUGUI>().text = nowGem.ToString() + " / " + needGem.ToString();

        // 시작 지점 각도 수집
        float startAngle = gaugeImg.material.GetFloat("_Arc1");

        //젬 개수만큼 테두리 도넛 게이지 갱신
        float gaugeFill = (((float)needGem - (float)nowGem) / (float)needGem) * (360f - startAngle);
        gaugeFill = Mathf.Clamp(gaugeFill, 0, (360f - startAngle));

        // 시작부터 마지막 지점까지 채우는 양 갱신
        gaugeImg.material.SetFloat("_Arc2", gaugeFill);
    }

    IEnumerator SummonBoss()
    {
        // 상호작용 키 끄기
        showKeyUI.alpha = 0;
        // 젬 개수 UI 비활성화
        gemNum.alpha = 0;

        // 버튼 이펙트 끄기
        ActionText("", false);

        //보스 리스트
        // List<EnemyInfo> bosses = new List<EnemyInfo>();

        // 소환할 보스 정보
        EnemyInfo bossInfo = null;

        //타입이 보스인 몬스터 찾기
        foreach (KeyValuePair<int, EnemyInfo> enemy in EnemyDB.Instance.enemyDB)
        {
            // 타입이 보스면
            if (enemy.Value.enemyType == EnemyDB.EnemyType.Boss.ToString())
            {
                // 해당 몹의 원소 속성 반환
                int enemyElement = System.Array.FindIndex(MagicDB.Instance.ElementNames, x => x == enemy.Value.elementType);

                // 해당 월드 속성과 같으면
                if (enemyElement == (int)SystemManager.Instance.NowMapElement)
                    // 보스 정보 넣기
                    bossInfo = enemy.Value;
                // 보스 풀에 넣기
                // bosses.Add(enemy.Value);
            }
        };

        // // 보스풀에서 하나 랜덤 선택
        // EnemyInfo bossInfo = bosses[Random.Range(0, bosses.Count)];
        // // 보스 정보 고정
        // if (fixedBoss != null)
        //     bossInfo = new EnemyInfo(EnemyDB.Instance.GetEnemyByName(fixedBoss.name.Split('_')[0]));

        //보스 소환 위치
        Vector2 bossPos = (Vector2)transform.position + Random.insideUnitCircle.normalized * Random.Range(10f, 15f);

        //보스 프리팹 찾기
        GameObject bossPrefab = EnemyDB.Instance.GetPrefab(bossInfo.id);

        // 보스 소환
        Character bossCharacter = WorldSpawner.Instance.EnemySpawn(bossInfo, bossPos, bossPrefab, 2f).GetComponent<Character>();

        // 보스 소환 상태로 전환
        portalState = PortalState.BossAlive;

        // 보스 죽을때까지 대기
        yield return new WaitUntil(() => bossCharacter != null && bossCharacter.isDead);

        // 보스 변수 초기화
        bossCharacter = null;

        print("boss dead");

        // 몬스터 스폰 멈추기
        SystemManager.Instance.spawnSwitch = false;

        // 남은 몬스터 화살표로 방향 표시해주기
        UIManager.Instance.enemyPointSwitch = true;

        // 몬스터 생존 상태로 전환
        portalState = PortalState.MobRemain;

        // 모든 몬스터 방향 UI 표시
        foreach (Character character in WorldSpawner.Instance.spawnEnemyList)
            StartCoroutine(WorldSpawner.Instance.PointEnemyDir(character.gameObject));

        // 모든 몬스터 죽을때까지 대기
        yield return new WaitUntil(() => WorldSpawner.Instance.spawnEnemyList.Count == 0f);

        // 클리어 상태로 전환
        portalState = PortalState.Clear;

        // 포탈 준비 파티클 끄기
        gatherParticle.SmoothDisable();

        // 포탈 오픈 이펙트 재생
        portalOpenEffect.SetActive(true);
        // 포탈 오픈 사운드 재생
        SoundManager.Instance.PlaySound("Gate_Open", transform.position);

        print("Map Clear");

        // PortalOpen 트리거 true / Open, Idle 애니메이션 순서대로 시작
        portalAnim.SetTrigger("PortalOpen");

        // 클리어 보상 드랍
        StartCoroutine(ClearReward());
    }

    IEnumerator ClearReward()
    {
        // 드랍 시킬 원소젬 뽑기
        ItemInfo gemInfo = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Gem);
        // 게이트에 투입된 원소젬 개수에 환불 계수 곱하기
        int dropNum = Mathf.RoundToInt(Random.Range(needGem * (1 - refundRate), needGem * (1 + refundRate)));

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
        //todo 텔레포트 사운드 재생
        SoundManager.Instance.PlaySound("Gate_Teleport", transform.position);

        // 상호작용 키 끄기
        showKeyUI.alpha = 0;

        // 시간 멈추고
        SystemManager.Instance.TimeScaleChange(0f);
        // 플레이어 컨트롤 끄기
        PlayerManager.Instance.player_Input.Disable();
        // UI 컨트롤 끄기
        UIManager.Instance.UI_Input.Disable();

        // 포탈 준비 파티클 시작
        gatherParticle.gameObject.SetActive(true);

        float tweenTime = 2f;

        // 포탈로 카메라 이동
        UIManager.Instance.CameraMove(transform.position, 0.5f, true);

        // 천천히 줌인
        UIManager.Instance.CameraZoom(tweenTime, 3f);

        // 텔레포트 파티클 켜기
        teleportParticle.gameObject.SetActive(true);

        // 플레이어가 포탈 가운데로 들어가면서
        PlayerManager.Instance.transform.DOMove(transform.position, tweenTime)
        .SetUpdate(true);

        // 플레이어 하얗게 변화
        PlayerManager.Instance.playerSprite.material.DOColor(Color.white, "_Tint", tweenTime)
        .SetUpdate(true)
        .SetEase(Ease.InCubic);
        // 포탈 하얗게
        portalCover.DOColor(Color.white, tweenTime)
        .SetUpdate(true)
        .SetEase(Ease.InCubic);

        // 하얗게 변하는 시간
        yield return new WaitForSecondsRealtime(tweenTime - 0.5f);

        // 텔레포트 파티클 끄기
        teleportParticle.SmoothDisable();

        // 플레이어 얇고 길쭉해짐
        PlayerManager.Instance.transform.DOScale(new Vector3(0f, 2f), 1f)
        .SetUpdate(true)
        .SetEase(Ease.InExpo);

        // 포탈 얇고 길쭉해짐
        portalPivot.DOScale(new Vector3(0f, 2f), 1f)
        .SetUpdate(true)
        .SetEase(Ease.InExpo);

        // 포탈 중심으로 빛 기둥 넓어지며 등장
        beam.transform.DOScale(new Vector2(2f, 1f), 1f)
        .SetUpdate(true)
        .SetEase(Ease.InExpo);

        // 빛 기둥 넓어지는 시간
        yield return new WaitForSecondsRealtime(1f);

        // 빛 기둥 빠르게 얇아지면서 사라짐
        beam.transform.DOScale(new Vector2(0f, 1f), 0.5f)
        .SetUpdate(true)
        .SetEase(Ease.OutExpo);

        // 빛 기둥 모양으로 천천히 날리는 빛 파티클 조금 남김
        beamParticle.SetActive(true);

        // 카메라 줌 초기화
        UIManager.Instance.CameraZoom(3f, 1f);

        // 흩날리는 파티클 사라질때까지 대기
        yield return new WaitUntil(() => !beamParticle.activeSelf);

        // 현재 맵 속성 조회해서 int로 변환
        int nowIndex = (int)SystemManager.Instance.NowMapElement;
        // 마지막 스테이지가 아닐때
        if (nowIndex < 5)
        {
            // 다음 맵 인덱스로 증가
            SystemManager.Instance.NowMapElement = (MapElement)(nowIndex + 1);

            // 트랜지션 이후 새로운 인게임 씬 켜기
            SystemManager.Instance.StartGame();

            // 포탈 게이트 디스폰
            LeanPool.Despawn(gameObject);
        }
        // 마지막 스테이지일때
        else
        {
            // 게임 오버 UI 켜기
            SystemManager.Instance.GameOverPanelOpen(true);

            //todo 게임오버 BGM 재생

            // 포탈 게이트 디스폰
            LeanPool.Despawn(gameObject);
        }
    }
}
