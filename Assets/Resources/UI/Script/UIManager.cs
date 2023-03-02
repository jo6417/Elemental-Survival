using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Lean.Pool;
using DG.Tweening;
using TMPro;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.EventSystems;
using System.Linq;

public class UIManager : MonoBehaviour
{
    #region Singleton
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                return null;
                // var obj = FindObjectOfType<UIManager>();
                // if (obj != null)
                // {
                //     instance = obj;
                // }
                // else
                // {
                //     var newObj = new GameObject().AddComponent<UIManager>();
                //     instance = newObj;
                // }
            }
            return instance;
        }
    }
    #endregion

    public bool enemyPointSwitch = false; //화면 밖의 적 방향 표시 여부

    [Header("State")]
    [SerializeField] float fill_Max = 600f; // 단일 난이도 최대치, 기본 10분
    public enum DamageType { Damaged, Heal, Miss, Block }

    [Header("Camera")]
    public Transform camFollowTarget;
    public float camFollowSpeed = 10f; // 캠 따라오는 속도
    public float defaultCamSize = 16.875f; // 기본 캠 사이즈
    private Tween zoomTween = null; // 현재 진행중인 줌인 및 줌아웃 트윈

    [Header("Input")]
    public NewInput UI_Input; // UI 인풋 받기
    public Vector2 nowMousePos; // 마우스 마지막 위치 기억

    [Header("PopupUI")]
    [ReadOnly] public GameObject nowOpenPopup; //현재 열려있는 팝업 UI
    public Transform popupUIparent; //팝업 UI 담는 부모 오브젝트
    RectTransform UIRect;
    public GameObject phonePanel;
    public GameObject defaultPanel;
    public GameObject chestPanel;
    public GameObject vendMachinePanel;
    public GameObject magicMachinePanel;
    public GameObject levelupPanel;
    public GameObject pausePanel;
    public GameObject optionPanel;
    public GameObject gameoverPanel;

    [Header("Refer")]
    public Transform camParent; // 카메라 이동 오브젝트
    public GameObject dmgTxtPrefab; //데미지 텍스트 UI
    public Transform gameoverScreen;
    public Transform gameLog; // 게임 플레이 기록
    public GameObject gameoverSlot; //게임 오버 창에 들어갈 마법 슬롯
    public TextMeshProUGUI timer; // 누적 시간
    [SerializeField] Image timerFrame; // 타이머 프레임 및 뒷배경
    [SerializeField] Image timerEffect; // 타이머 뒷배경 이펙트
    [SerializeField] SlicedFilledImage timerInside; // 타이머 내부 배경
    public TextMeshProUGUI killCountTxt;
    public GameObject bossHp;
    public GameObject arrowPrefab; //적 방향 가리킬 화살표 UI
    public GameObject iconArrowPrefab; //오브젝트 방향 기리킬 아이콘 화살표 UI
    public Image phoneNoticeIcon; //스마트폰 알람 아이콘 UI
    int noticeNum; // 현재 스마트폰 알림 개수
    Sequence iconJumpSeq;
    public bool phoneLoading; //스마트폰 로딩중 여부
    public Image nowHoldSlot; // 현재 선택중인 슬롯 아이콘
    public InventorySlot activeSlot_A;
    public InventorySlot activeSlot_B;
    public InventorySlot activeSlot_C;
    public GameObject inGameBindKeyList; // 인게임 사용 키 리스트
    public GameObject tabletBindKeyList; // 태블릿 사용 키 리스트
    [SerializeField] Transform bindKeyList; // 현재 사용 가능한 키 안내 리스트
    [SerializeField] Transform bindKeyPrefab; // 현재 사용 가능한 키 액션 프리팹

    [Header("PlayerUI")]
    [SerializeField] PlayerManager playerManager;
    public CanvasGroup dodgeBar;
    public SlicedFilledImage playerHp;
    public TextMeshProUGUI playerHpText;
    public SlicedFilledImage playerExp;
    public TextMeshProUGUI playerLev;
    public List<TextMeshProUGUI> gemAmountUIs = new List<TextMeshProUGUI>();
    public List<Image> gemIndicators = new List<Image>();
    public Transform gemUIParent;
    public GameObject statsUI; //일시정지 메뉴 스탯 UI
    public TextMeshProUGUI pauseScrollAmt; //일시정지 메뉴 스크롤 개수 UI
    public GameObject hasItemIcon; //플레이어 현재 소지 아이템 아이콘
    public Transform hasItemsUI; //플레이어 현재 소지한 모든 아이템 UI
    public GridLayoutUI hasMagicGrid; //플레이어 현재 소지한 모든 마법 UI

    public Transform ultimateMagicSlot; //궁극기 마법 슬롯 UI
    public Image ultimateIndicator; //궁극기 슬롯 인디케이터 이미지

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

        //입력 초기화
        StartCoroutine(InputInit());

        // 시작할때 머지 캔버스 켜놓기
        phonePanel.SetActive(true);

        // 새 아이템 개수 알림 갱신
        PhoneNotice(0);

        // 선택중인 아이콘 끄기
        nowHoldSlot.enabled = false;
        // 선택중인 슬롯 머터리얼 인스턴싱
        Material holdMat = new Material(nowHoldSlot.material);
        nowHoldSlot.material = holdMat;
    }

    IEnumerator InputInit()
    {
        UI_Input = new NewInput();

        // null 이 아닐때까지 대기
        yield return new WaitUntil(() => SystemManager.Instance != null && UI_Input != null);

        // 방향키 입력시
        UI_Input.UI.NavControl.performed += val =>
        {
            // 오브젝트 켜져있을때
            if (UIManager.Instance)
                NavControl(val.ReadValue<Vector2>());
        };
        // 마우스 위치 입력
        UI_Input.UI.MousePosition.performed += val =>
        {
            // 오브젝트 켜져있을때
            if (UIManager.Instance)
                MousePos(val.ReadValue<Vector2>());
        };
        // 확인 입력
        UI_Input.UI.Accept.performed += val =>
        {
            // 오브젝트 켜져있을때
            if (UIManager.Instance)
                Submit();
        };
        // 취소 입력
        UI_Input.UI.Cancel.performed += val =>
        {
            // 오브젝트 켜져있을때
            if (UIManager.Instance)
                Cancel();
        };
        // 스마트폰 버튼 입력
        UI_Input.UI.PhoneMenu.performed += val =>
        {
            // 오브젝트 켜져있을때
            if (UIManager.Instance)
                PhoneOpen();
        };
        // 마우스 클릭
        UI_Input.UI.Click.performed += val =>
        {
            // // 선택된 오브젝트 넣기
            // if (EventSystem.current.currentSelectedGameObject != null
            // && EventSystem.current.currentSelectedGameObject.TryGetComponent(out Selectable selectable))
            //     lastSelected = selectable;

            // 오브젝트 켜져있을때
            if (UIManager.Instance)
                Click();
        };

        // UI 인풋 활성화
        UI_Input.Enable();
    }

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());
    }

    private void OnDestroy()
    {
        if (UI_Input != null)
        {
            UI_Input.Disable();
            UI_Input.Dispose();
        }
    }

    IEnumerator Init()
    {
        yield return null;

        camFollowTarget = playerManager.transform;

        // 난이도 등급 변수 초기화
        WorldSpawner.Instance.nowDifficultGrade = 1;

        // 타이머 프레임 색 변경
        timerFrame.color = MagicDB.Instance.GradeColor[WorldSpawner.Instance.nowDifficultGrade - 1];
        // 타이머 내부 배경 색 변경
        Color InsideColor = MagicDB.Instance.GradeColor[WorldSpawner.Instance.nowDifficultGrade];
        InsideColor.a = 200f / 255f;
        timerInside.color = InsideColor;

        // 에디터에서만
#if UNITY_EDITOR
        // 기본 마법 패널 열기
        PopupUI(defaultPanel, true);
#endif

        // 인게임 바인딩 리스트 켜기
        inGameBindKeyList.SetActive(true);
        tabletBindKeyList.SetActive(false);

        //todo 키설정으로 바뀐 바인딩 키 불러와서 목록 직접 만들기
        // // 모든 자식 디스폰
        // SystemManager.Instance.DespawnAllChild(bindKeyList);
        // // 화면 구석에 바인딩된 키 역할 표시
        // ShowBindKey("Tab", "인벤토리");
        // ShowBindKey("ESC", "일시정지");

        // for (int i = 0; i < 3; i++)
        // {
        //     bindKeyList.gameObject.SetActive(false);
        //     yield return new WaitForSeconds(Time.deltaTime);
        //     bindKeyList.gameObject.SetActive(true);
        //     Canvas.ForceUpdateCanvases();
        // }
    }

    // 방향키 입력되면 실행
    void NavControl(Vector2 arrowDir)
    {
        // print(arrowDir);

        // 시간 멈춰있을때만
        if (Time.timeScale == 0)
        {
            // 마우스 커서 끄기
            UICursor.Instance.arrowCursor.SetActive(false);

            //마우스 잠겨있지않을때
            if (Cursor.lockState == CursorLockMode.None)
            {
                //모든 툴팁 끄기
                HasStuffToolTip.Instance.QuitTooltip();
                ProductToolTip.Instance.QuitTooltip();

                //마우스 숨기기
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    // 마우스 위치 입력되면 실행
    void MousePos(Vector2 mousePos)
    {
        // print(mousePos);

        //마지막 마우스 위치 기억
        nowMousePos = mousePos;

        // 마우스 잠겨있을때
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            // 마우스 고정인데 툴팁 떠있으면 끄기
            HasStuffToolTip.Instance.QuitTooltip();
            ProductToolTip.Instance.QuitTooltip();

            //마우스 고정해제
            Cursor.lockState = CursorLockMode.None;

            // UI 커서 끄기
            UICursor.Instance.UICursorToggle(false);

            // 마우스 커서 켜기
            UICursor.Instance.arrowCursor.SetActive(true);
        }
    }

    // 확인 입력
    public void Click()
    {
        //! 테스트, 현재 선택된 UI 이름 표시
        if (EventSystem.current.currentSelectedGameObject == null)
            SystemManager.Instance.nowSelectUI.text = "Last Select : null";
        else
            SystemManager.Instance.nowSelectUI.text = "Last Select : " + EventSystem.current.currentSelectedGameObject.name;
    }

    // 확인 입력
    public void Submit()
    {
        //선택된 UI 따라다니기
        // FollowUICursor();

        // print("submit");

        // 현재 선택된 버튼 누르기
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            Button btn = EventSystem.current.currentSelectedGameObject.GetComponent<Button>();
            if (btn != null)
                btn.onClick.Invoke();
        }
    }

    // 취소 입력
    void Cancel()
    {
        //일시정지 패널 켜기
        Resume();
    }

    public void PhoneOpen(Vector3 modifyPos = default)
    {
        // 폰메뉴 켜져있으면 리턴
        if (PhoneMenu.Instance.isOpen)
            return;

        // 시간 멈추지 않았을때
        if (Time.timeScale > 0)
        {
            //시간 멈추기
            SystemManager.Instance.TimeScaleChange(0f);

            // 핸드폰 열기
            StartCoroutine(PhoneMenu.Instance.OpenPhone(modifyPos));

            // UI 입력 켜기
            SystemManager.Instance.ToggleInput(true);

            //현재 열려있는 팝업 갱신
            nowOpenPopup = phonePanel;
        }
    }

    private void Start()
    {
        UIRect = GetComponent<RectTransform>();

        // GemUI 전부 찾기
        for (int i = 0; i < gemUIParent.childCount; i++)
        {
            // 원소젬 인디케이터 찾기
            Image indicator = gemUIParent.GetChild(i).GetComponent<Image>();
            gemIndicators.Add(indicator);

            // 인디케이터 컬러 투명하게 초기화
            indicator.color = new Color(1, 0, 0, 0);

            // 원소젬 개수 텍스트 찾기
            gemAmountUIs.Add(gemUIParent.GetChild(i).GetComponentInChildren<TextMeshProUGUI>());
        }

        //킬 카운트 초기화
        UpdateKillCount();

        // 보스 체력 UI 초기화
        bossHp.SetActive(false);

        //화면밖 적 방향 미표시
        enemyPointSwitch = false;
    }

    private void Update()
    {
        if (camFollowTarget != null)
        {
            // 카메라 타겟 부드럽게 따라가기
            Vector3 targetPos = camFollowTarget.position;
            targetPos.z = -50f;
            camParent.position = Vector3.Lerp(camParent.position, targetPos, Time.deltaTime * camFollowSpeed);
        }

        //게임시간 타이머 업데이트
        if (SystemManager.Instance.playerTimeScale != 0)
            UpdateTimer();
        else
            ResumeTimer();
    }

    public Vector2 GetMousePos()
    {
        return UI_Input.UI.MousePosition.ReadValue<Vector2>();
    }

    #region Camera

    public void CameraShake(float duration, float strength = 1, int vibrato = 10,
    float randomness = 90, bool snapping = false, bool fadeOut = true)
    {
        // 메인 카메라 위치 초기화
        SystemManager.Instance.MainCamera.transform.localPosition = Vector3.back;

        // 카메라 흔들기
        SystemManager.Instance.MainCamera.transform.DOShakePosition(duration, strength, vibrato, randomness, snapping, fadeOut)
        .OnComplete(() =>
        {
            // 메인 카메라 위치 초기화
            SystemManager.Instance.MainCamera.transform.localPosition = Vector3.back;
        });
    }

    public void CameraZoom(float zoomTime, float amount = 0)
    {
        // 기존 트윈 죽이기
        if (zoomTween != null)
            zoomTween.Kill();

        // 입력된 사이즈대로 줌인/줌아웃 트윈 실행
        zoomTween = DOTween.To(() => SystemManager.Instance.MainCamera.orthographicSize, x => SystemManager.Instance.MainCamera.orthographicSize = x, defaultCamSize + amount, zoomTime);
    }

    public void CameraMove(Vector2 targetPos, float time, bool isUnscaledTime)
    {
        // 카메라 이동
        camParent.DOMove(new Vector3(targetPos.x, targetPos.y, -50f), time)
        .SetUpdate(isUnscaledTime);
    }

    #endregion

    //옵션 메뉴 띄우기
    public void Option()
    {

    }

    //게임 일시정지,재개
    public void Resume()
    {
        // 핸드폰 패널 켜져있을때
        if (nowOpenPopup == phonePanel
        || nowOpenPopup == magicMachinePanel)
            // 핸드폰 닫기
            PhoneMenu.Instance.BackBtn();
        // 기본 마법 패널일때
        else if (nowOpenPopup == defaultPanel
        // 게임 오버 패널일때
        || nowOpenPopup == gameoverPanel)
            return;
        else
        {
            // 현재 팝업 패널 없을때
            if (nowOpenPopup == null)
                //일시정지 메뉴 켜기
                PopupUI(pausePanel, true);
            // 현재 팝업 패널 있을때
            else
            {
                // 옵션 패널이 아닐때
                if (nowOpenPopup != optionPanel)
                    // 켜져있는 패널 끄기
                    PopupUI(nowOpenPopup, false);
            }
        }
    }

    public void InitialStat()
    {
        UpdateExp();
        UpdateHp();
        UpdateStat();
        ResetTimer();
    }

    public void ResetTimer()
    {
        SystemManager.Instance.time_start = Time.time;
        SystemManager.Instance.time_current = 0;
        timer.text = "00:00";
    }

    public void ResumeTimer()
    {
        SystemManager.Instance.time_start = Time.time - SystemManager.Instance.time_current;
    }

    public string UpdateTimer()
    {
        // 현재 시간 갱신
        float nowTime = (int)(Time.time - SystemManager.Instance.time_start) + SystemManager.Instance.modifyTime;
        SystemManager.Instance.time_current = nowTime;

        //시간을 3600으로 나눈 몫
        string hour = 0 < (int)(nowTime / 3600f) ? string.Format("{0:00}", Mathf.FloorToInt(nowTime / 3600f)) + ":" : "";
        //시간을 60으로 나눈 몫을 60으로 나눈 나머지
        string minute = 0 < (int)(nowTime / 60f % 60f) ? string.Format("{0:00}", Mathf.FloorToInt(nowTime / 60f % 60f)) + ":" : "00:";
        //시간을 60으로 나눈 나머지
        string second = string.Format("{0:00}", nowTime % 60f);

        // 누적 시간 텍스트 출력
        timer.text = hour + minute + second;

        // 총 난이도 시간 최대치, 단일 난이도 최대치를 6단계로 곱셈
        float difficult_Max = fill_Max * 6f;

        // 누적시간이 1시간 이하일때만
        if (nowTime <= difficult_Max)
        {
            // 현재 난이도 남은시간
            float difficult_Amount = (nowTime % fill_Max) / fill_Max;
            // 현재시간을 난이도 최대치로 나눈 나머지만큼 시간 UI 뒷배경 차오르기
            timerInside.fillAmount = difficult_Amount;

            // 현재 난이도 등급
            int difficult_Grade = (int)(nowTime / fill_Max) + 1;
            // 기존 난이도 보다 오르면 프레임 변경
            if (difficult_Grade > WorldSpawner.Instance.nowDifficultGrade)
            {
                // 난이도 등급 변수 상승
                WorldSpawner.Instance.nowDifficultGrade = difficult_Grade;

                // 난이도 등급 상승 트랜지션
                TimeGradeChange();
            }

            // print(nowTime + " : " + difficult_Amount + "% : Grade " + difficult_Grade);
        }

        // 스테이지 시작시간부터 gateSpawnTime 시간 이후일때
        if (SystemManager.Instance.time_current - WorldSpawner.Instance.stageStartTime > WorldSpawner.Instance.GateSpawnTime)
        {
            // 이제부터 포탈게이트 근처에서 몬스터 스폰
            WorldSpawner.Instance.gateSpawn = true;
            // 몬스터 반대편으로 옮기기 정지
            WorldSpawner.Instance.dragSwitch = false;
        }

        return hour + minute + second;
    }

    void TimeGradeChange()
    {
        int nowDifficultGrade = WorldSpawner.Instance.nowDifficultGrade;

        // 내부 배경 색 변경
        if (nowDifficultGrade < MagicDB.Instance.GradeColor.Length)
        {
            Color InsideColor = MagicDB.Instance.GradeColor[nowDifficultGrade];
            InsideColor.a = 150f / 255f;
            timerInside.color = InsideColor;
        }

        // 크기 초기화
        timerEffect.transform.localScale = Vector3.one;
        // 타이머 뒷배경 커지는 이펙트 재생
        timerEffect.transform.DOScale(Vector3.one * 2f, 1f)
        .SetEase(Ease.OutCubic);

        // 이펙트 색 초기화
        Color difficultColor = MagicDB.Instance.GradeColor[nowDifficultGrade - 1];
        timerEffect.color = difficultColor;
        // 타이머 뒷배경 투명해지며 사라지는 이펙트 재생
        difficultColor.a = 0f;
        timerEffect.DOColor(difficultColor, 1f)
        .SetEase(Ease.OutCubic);

        // 타이머 뒷배경 현재 난이도 색으로 변하기
        Color frameColor = MagicDB.Instance.GradeColor[nowDifficultGrade - 1];
        timerFrame.DOColor(frameColor, 1f)
        .SetEase(Ease.OutCubic);
    }

    public void UpdateKillCount()
    {
        //킬 카운트 표시
        killCountTxt.text = SystemManager.Instance.killCount.ToString();
    }

    public void UpdateExp()
    {
        // 경험치 바 갱신
        playerExp.fillAmount = playerManager.ExpNow / playerManager.ExpMax;

        // 레벨 갱신
        playerLev.text = "Lev. " + playerManager.characterStat.Level.ToString();
    }

    public IEnumerator UpdateBossHp(Character bossManager)
    {
        //보스 몬스터 정보 들어올때까지 대기
        yield return new WaitUntil(() => bossManager.enemy != null);

        float bossHpNow = bossManager.characterStat.hpNow;
        float bossHpMax = bossManager.characterStat.hpMax;

        //보스 체력 UI 활성화
        bossHp.SetActive(true);

        //보스 체력 게이지 갱신
        bossHp.GetComponentInChildren<SlicedFilledImage>().fillAmount
        = bossHpNow / bossHpMax;

        //보스 체력 텍스트 갱신
        bossHp.transform.Find("HpText").GetComponent<TextMeshProUGUI>().text
        = Mathf.CeilToInt(bossHpNow).ToString() + " / " + Mathf.CeilToInt(bossHpMax).ToString();

        //보스 이름 갱신, 체력 0이하면 공백
        bossHp.transform.Find("BossName").GetComponent<TextMeshProUGUI>().text
        = bossHpNow <= 0 ? "" : bossManager.enemy.name;

        //체력 0 이하면 체력 UI 끄기
        if (bossHpNow <= 0)
        {
            //보스 체력 UI 비활성화
            bossHp.SetActive(false);
        }
    }

    public void UpdateHp()
    {
        playerHp.fillAmount = playerManager.characterStat.hpNow / playerManager.characterStat.hpMax;
        playerHpText.text = (int)playerManager.characterStat.hpNow + " / " + (int)playerManager.characterStat.hpMax;
    }

    public void UpdateGem(int gemTypeIndex)
    {
        // 해당 타입의 젬 UI 업데이트
        gemAmountUIs[gemTypeIndex].text = playerManager.hasGem[gemTypeIndex].amount.ToString();
    }

    public void UpdateGem()
    {
        // 모든 젬 UI 업데이트
        for (int i = 0; i < 6; i++)
        {
            gemAmountUIs[i].text = playerManager.hasGem[i].amount.ToString();
        }
    }

    public void GemIndicator(int gemIndex, Color indicateColor)
    {
        Image indicator = gemIndicators[gemIndex];

        // 기존 트윈 끄기
        indicator.DOKill();
        // 투명하게 초기화
        Color originColor = indicateColor;
        originColor.a = 0;
        indicator.color = originColor;

        // 투명도 복구
        originColor.a = 1f;

        // 2회 점멸하기
        indicator.DOColor(originColor, 0.2f)
        .SetLoops(4, LoopType.Yoyo);
    }

    public void UpdateItems()
    {
        //기존 아이콘 모두 없에기
        Image[] children = hasItemsUI.GetComponentsInChildren<Image>();
        // print(children.Length);

        //모든 자식 오브젝트 비활성화
        if (children != null)
            for (int j = 0; j < children.Length; j++)
            {
                LeanPool.Despawn(children[j].gameObject);
            }

        foreach (var item in playerManager.hasGem)
        {
            // print(item.itemName + " x" + item.hasNum);

            //아티팩트 아니면 넘기기
            if (item.itemType != ItemDB.ItemType.Artifact.ToString())
                continue;

            //아이템 아이콘 오브젝트 생성
            GameObject itemIcon = LeanPool.Spawn(hasItemIcon, hasItemsUI.position, Quaternion.identity, hasItemsUI);

            // 오브젝트에 아이템 정보 저장
            ToolTipTrigger toolTipTrigger = itemIcon.GetComponent<ToolTipTrigger>();
            toolTipTrigger.toolTipType = ToolTipTrigger.ToolTipType.HasStuffTip;
            toolTipTrigger._slotInfo = item;

            //스프라이트 넣기
            itemIcon.GetComponent<Image>().sprite =
            ItemDB.Instance.itemIcon.Find(x => x.name == item.name.Replace(" ", "") + "_Icon");

            //아이템 개수 넣기, 2개 이상부터 표시
            Text amount = itemIcon.GetComponentInChildren<Text>(true);
            if (item.amount >= 2)
            {
                amount.gameObject.SetActive(true);
                amount.text = "x " + item.amount.ToString();
            }
            else
            {
                amount.gameObject.SetActive(false);
            }
        }
    }

    public IEnumerator UpdateMagics(List<MagicInfo> magicList)
    {
        // 목록 투명하게 숨기기
        hasMagicGrid.GetComponent<CanvasGroup>().alpha = 0f;

        //모든 자식 오브젝트 비활성화
        int childNum = hasMagicGrid.transform.childCount;
        for (int i = 0; i < childNum; i++)
        {
            LeanPool.Despawn(hasMagicGrid.transform.GetChild(0).gameObject);
        }

        foreach (MagicInfo slot in magicList)
        {
            //0등급은 원소젬이므로 표시 안함
            if (slot.grade == 0)
                continue;

            MagicInfo magic = slot as MagicInfo;

            //마법 아이콘 오브젝트 생성
            GameObject magicIcon = LeanPool.Spawn(hasItemIcon, hasMagicGrid.transform.position, Quaternion.identity, hasMagicGrid.transform);

            //툴팁에 마법 정보 저장
            ToolTipTrigger toolTipTrigger = magicIcon.GetComponent<ToolTipTrigger>();
            toolTipTrigger.toolTipType = ToolTipTrigger.ToolTipType.HasStuffTip;
            toolTipTrigger._slotInfo = magic;

            // 전역 마법 정보 찾기
            MagicInfo sharedMagic = MagicDB.Instance.GetMagicByID(slot.id);
            // 전역 마법 정보의 쿨타임 보여주기
            ShowMagicCooltime showCool = magicIcon.GetComponent<ShowMagicCooltime>();
            showCool.magic = sharedMagic;

            //아이콘 넣기
            magicIcon.GetComponent<Image>().sprite = MagicDB.Instance.GetIcon(slot.id);

            //마법 레벨 넣기
            TextMeshProUGUI amount = magicIcon.GetComponentInChildren<TextMeshProUGUI>(true);
            amount.gameObject.SetActive(true);
            amount.text = "Lev." + magic.magicLevel.ToString();
        }

        //그리드 업데이트 명령하기
        hasMagicGrid.isChanged = true;

        yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);

        // 목록 나타내기
        hasMagicGrid.GetComponent<CanvasGroup>().alpha = 1f;
    }

    public void AddMagicUI(MagicInfo magic)
    {
        Transform matchIcon = null;

        //모든 자식 오브젝트 비활성화
        for (int j = 0; j < hasMagicGrid.transform.childCount; j++)
        {
            // TooltipTrigger의 magic이 같은 아이콘 찾기
            if (hasMagicGrid.transform.GetChild(j).GetComponent<ToolTipTrigger>()._slotInfo == magic)
            {
                matchIcon = hasMagicGrid.transform.GetChild(j);
                break;
            }
        }

        // 못찾으면 1렙 새 아이콘 추가
        if (matchIcon == null)
        {
            //0등급은 원소젬이므로 표시 안함
            if (magic.grade == 0)
                return;

            //마법 아이콘 오브젝트 생성
            GameObject magicIcon = LeanPool.Spawn(hasItemIcon, hasMagicGrid.transform.position, Quaternion.identity, hasMagicGrid.transform);

            //툴팁에 마법 정보 저장
            ToolTipTrigger toolTipTrigger = magicIcon.GetComponent<ToolTipTrigger>();
            toolTipTrigger.toolTipType = ToolTipTrigger.ToolTipType.HasStuffTip;
            toolTipTrigger._slotInfo = magic;

            //스프라이트 넣기
            magicIcon.GetComponent<Image>().sprite = MagicDB.Instance.GetIcon(magic.id);
            // MagicDB.Instance.magicIcon.Find(x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

            //마법 개수 넣기, 2개 이상부터 표시
            TextMeshProUGUI amount = magicIcon.GetComponentInChildren<TextMeshProUGUI>(true);
            amount.gameObject.SetActive(true);
            amount.text = "Lev." + magic.magicLevel.ToString();
        }
        // 찾으면 해당 아이콘에 레벨 텍스트 갱신
        else
        {
            //마법 개수 넣기, 2개 이상부터 표시
            TextMeshProUGUI amount = matchIcon.GetComponentInChildren<TextMeshProUGUI>(true);
            amount.gameObject.SetActive(true);
            amount.text = "Lev." + magic.magicLevel.ToString();
        }

    }

    public void UpdateStat()
    {
        // 스탯 입력할 UI
        Text[] stats = statsUI.GetComponentsInChildren<Text>();

        // print("stats.Length : " + stats.Length);

        // 스탯 입력값
        List<float> statAmount = new List<float>();

        stats[0].text = playerManager.characterStat.hpMax.ToString();
        stats[1].text = Mathf.Round(playerManager.characterStat.power * 100).ToString() + " %";
        stats[2].text = Mathf.Round(playerManager.characterStat.armor * 100).ToString() + " %";
        stats[3].text = Mathf.Round(playerManager.characterStat.moveSpeed * 100).ToString() + " %";
        stats[4].text = Mathf.Round(playerManager.characterStat.atkNum * 100).ToString() + " %";
        stats[5].text = Mathf.Round(playerManager.characterStat.speed * 100).ToString() + " %";
        stats[6].text = Mathf.Round(playerManager.characterStat.coolTime * 100).ToString() + " %";
        stats[7].text = Mathf.Round(playerManager.characterStat.duration * 100).ToString() + " %";
        stats[8].text = Mathf.Round(playerManager.characterStat.range * 100).ToString() + " %";
        stats[9].text = Mathf.Round(playerManager.characterStat.luck * 100).ToString() + " %";
        stats[10].text = Mathf.Round(playerManager.characterStat.expGain * 100).ToString() + " %";
        stats[11].text = Mathf.Round(playerManager.characterStat.getRage * 100).ToString() + " %";

        stats[12].text = Mathf.Round(playerManager.characterStat.earth_atk * 100).ToString() + " %";
        stats[13].text = Mathf.Round(playerManager.characterStat.fire_atk * 100).ToString() + " %";
        stats[14].text = Mathf.Round(playerManager.characterStat.life_atk * 100).ToString() + " %";
        stats[15].text = Mathf.Round(playerManager.characterStat.lightning_atk * 100).ToString() + " %";
        stats[16].text = Mathf.Round(playerManager.characterStat.water_atk * 100).ToString() + " %";
        stats[17].text = Mathf.Round(playerManager.characterStat.wind_atk * 100).ToString() + " %";
    }

    public void PhoneNotice(int fixNum = -1)
    {
        // 알람 숫자 아이콘 배경
        Image notice = phoneNoticeIcon.transform.Find("Notice").GetComponent<Image>();
        // 알람 개수 텍스트
        TextMeshProUGUI stackNum = notice.transform.Find("Text").GetComponent<TextMeshProUGUI>();

        // noticeNum 입력이 들어오면 해당 개수로 갱신
        if (fixNum >= 0)
            noticeNum = fixNum;
        else
            // 알림 개수 1개 높여서 갱신
            noticeNum++;

        //  개수 넣기
        stackNum.text = noticeNum.ToString();

        // 아이콘 점프 트윈 생성 및 멈추기
        if (iconJumpSeq == null)
        {
            iconJumpSeq = DOTween.Sequence();

            // 알림 기본 위치
            Vector3 defaultPos = notice.rectTransform.localPosition;

            // 알림 아이콘 주기적으로 두번씩 튀는 트윈
            iconJumpSeq
            .Append(notice.rectTransform.DOLocalJump(defaultPos, 10f, 1, 0.5f))
            .Append(notice.rectTransform.DOLocalJump(defaultPos, 10f, 1, 0.5f))
            .AppendInterval(1f)
            // 루프간 1초 대기
            .SetLoops(-1)
            .OnPause(() =>
            {
                // 초기 위치로 복귀
                notice.transform.localPosition = defaultPos;
            });
        }

        // 0개일때
        if (noticeNum == 0)
        {
            // 아이콘 점프 트윈 끝내기
            iconJumpSeq.Pause();

            // 화면 밝기 내려서 끄기
            phoneNoticeIcon.DOColor(new Color(1, 1, 1, 0), 1f);

            //알림 아이콘 끄기
            notice.gameObject.SetActive(false);
        }
        // 1개 이상일때
        else
        {
            // 스마트폰 UI 화면 밝히기
            phoneNoticeIcon.DOColor(new Color(1, 1, 1, 1), 1f);

            // 알림 아이콘 켜기
            notice.gameObject.SetActive(true);

            iconJumpSeq.Restart();
        }
    }

    // public void ChangePopup(GameObject closePopup, GameObject openPopup)
    // {
    //     // 기존 팝업 끄기
    //     closePopup.SetActive(false);

    //     //todo 새로운 팝업 켜기
    //     PopupUI(openPopup, true);
    // }

    public void PopupUI(GameObject popup)
    {
        // 이미 다른 팝업 열려있는데 팝업 키려고하면 리턴
        if (!popup.activeSelf && nowOpenPopup != null)
        {
            return;
        }

        // 팝업 UI 토글
        popup.SetActive(!popup.activeSelf);

        //팝업 세팅
        PopupSet(popup);
    }

    public void PopupUI(GameObject popup, bool forceSwitch = true)
    {
        // 이미 다른 팝업 열려있는데 팝업 키려고하면 리턴
        if (!popup.activeSelf && nowOpenPopup != null)
            return;

        // 팝업 UI 토글
        popup.SetActive(forceSwitch);

        //팝업 세팅
        PopupSet(popup);
    }

    public void PopupSet(GameObject popup)
    {
        // 시간 정지 토글
        float scale = popup.activeSelf ? 0 : 1;
        SystemManager.Instance.TimeScaleChange(scale);

        //팝업 off
        if (!popup.activeSelf)
        {
            // 팝업 꺼질때 UI 커서 끄기
            UICursor.Instance.UICursorToggle(false);

            //null 선택하기
            EventSystem.current.SetSelectedGameObject(null);

            //플레이어 입력 켜기
            playerManager.player_Input.Enable();

            //현재 열려있는 팝업 비우기
            nowOpenPopup = null;

            //마우스 고정해제
            Cursor.lockState = CursorLockMode.None;

            // 버튼 Select 해제
            UICursor.Instance.UpdateLastSelect(null);
        }
        //팝업 on
        else
        {
            // UI 입력 켜기
            SystemManager.Instance.ToggleInput(true);

            //현재 열려있는 팝업 갱신
            nowOpenPopup = popup;
        }

        // 마우스 커서 전환
        UICursor.Instance.CursorChange(popup.activeSelf);
    }

    public void SelectObject(GameObject gameObject)
    {
        // 오브젝트 선택하기
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    // 화면 밖 오브젝트 방향 표시 UI
    public IEnumerator PointObject(Renderer targetRenderer, Sprite icon)
    {
        // 오버레이 풀에서 화살표 UI 생성
        GameObject arrowUI = LeanPool.Spawn(iconArrowPrefab, targetRenderer.transform.position, Quaternion.identity, ObjectPool.Instance.overlayPool);

        //rect 찾기
        RectTransform rect = arrowUI.GetComponent<RectTransform>();

        // 화살표의 아이콘 이미지 바꾸기
        arrowUI.transform.Find("Icon").GetComponent<Image>().sprite = icon;

        // 방향 가리킬 화살표
        Transform arrow = arrowUI.transform.Find("Arrow");

        // 화살표 이동 딜레이
        float followDelay = 0.001f;

        //오브젝트 활성화 되어있으면
        while (targetRenderer.gameObject.activeSelf)
        {
            // 화살표의 카메라 내부 위치 산출 (좌하단에서 우상단까지 0 ~ 1)
            Vector3 arrowPos = Camera.main.WorldToViewportPoint(targetRenderer.transform.position);
            // if (arrowPos.x < 0f || arrowPos.x > 1f
            // || arrowPos.y < 0f || arrowPos.y > 1f)

            // 해당 렌더러가 화면 밖으로 나갔을때
            if (!targetRenderer.isVisible)
            {
                // 화살표 켜기
                arrowUI.SetActive(true);

                // 화살표 위치가 화면 밖으로 벗어나지않게 제한
                arrowPos.x = Mathf.Clamp(arrowPos.x, 0f, 1f);
                arrowPos.y = Mathf.Clamp(arrowPos.y, 0f, 1f);

                // 화면 가장짜리쪽으로 피벗 변경
                rect.pivot = arrowPos;

                // 아이콘 화살표 위치 이동
                arrowUI.transform.DOMove(Camera.main.ViewportToWorldPoint(arrowPos), followDelay);

                // 오브젝트 방향 가리키기
                Vector2 dir = targetRenderer.transform.position - playerManager.transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 225f;
                arrow.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
            // 해당 렌더러가 화면 안에 보일때
            else
                // 화살표 끄기
                arrowUI.SetActive(false);

            yield return new WaitForSeconds(followDelay);
        }

        //화살표 디스폰
        LeanPool.Despawn(arrowUI);
    }

    public void DamageUI(DamageType damageType, float damage, bool isCritical, Vector2 hitPos, bool isPlayer = false)
    {
        // 데미지 표시 가능할때만
        if (SystemManager.Instance.showDamage)
            // 데미지 UI 재생
            StartCoroutine(DamageText(damageType, damage, isCritical, hitPos));
    }

    IEnumerator DamageText(DamageType damageType, float damage, bool isCritical, Vector2 hitPos, bool isPlayer = false)
    {
        // 데미지 UI 띄우기
        GameObject damageUI = LeanPool.Spawn(UIManager.Instance.dmgTxtPrefab, hitPos, Quaternion.identity, ObjectPool.Instance.overlayPool);
        TextMeshProUGUI dmgTxt = damageUI.GetComponent<TextMeshProUGUI>();

        switch (damageType)
        {
            // 데미지 있을때
            case DamageType.Damaged:

                // 플레이어일때
                if (isPlayer)
                {
                    // 크리티컬 떴을때
                    if (isCritical)
                        // 보라색
                        dmgTxt.color = new Color(200f / 255f, 30f / 255f, 200f / 255f);
                    else
                        // 빨간색
                        dmgTxt.color = new Color(200f / 255f, 30f / 255f, 30f / 255f);
                }
                // 몬스터일때
                else
                {
                    // 크리티컬 떴을때
                    if (isCritical)
                        // 노란색
                        dmgTxt.color = Color.yellow;
                    else
                        // 흰색
                        dmgTxt.color = Color.white;
                }

                dmgTxt.text = damage.ToString();
                break;

            // 데미지가 마이너스일때 (체력회복일때)
            case DamageType.Heal:
                dmgTxt.color = Color.green;
                dmgTxt.text = "+" + (-damage).ToString();
                break;

            // 회피 했을때
            case DamageType.Miss:
                dmgTxt.color = new Color(200f / 255f, 30f / 255f, 30f / 255f);
                dmgTxt.text = "MISS";
                break;

            // 방어 했을때
            case DamageType.Block:
                dmgTxt.color = new Color(50f / 255f, 180f / 255f, 255f / 255f);
                dmgTxt.text = "BLOCK";
                break;
        }

        // 데미지 양수일때
        if (damage > 0)
            // 오른쪽으로 DOJump
            damageUI.transform.DOJump((Vector2)damageUI.transform.position + Vector2.right * 2f, 1f, 1, 0.5f)
            .SetEase(Ease.OutBounce);
        // 데미지 음수일때
        else
            // 위로 DoMove
            damageUI.transform.DOMove((Vector2)damageUI.transform.position + Vector2.up * 2f, 0.5f)
            .SetEase(Ease.OutSine);

        //제로 사이즈로 시작
        damageUI.transform.localScale = Vector3.zero;

        //원래 크기로 늘리기
        damageUI.transform.DOScale(Vector3.one, 0.5f);
        yield return new WaitForSeconds(0.8f);

        //줄어들어 사라지기
        damageUI.transform.DOScale(Vector3.zero, 0.2f);
        yield return new WaitForSeconds(0.2f);

        // 데미지 텍스트 디스폰
        LeanPool.Despawn(damageUI);
    }

    public void ToggleHoldSlot(bool toggle, Sprite changeSprite = null)
    {
        // 스프라이트 들어오면 교체
        if (changeSprite != null)
            nowHoldSlot.sprite = changeSprite;

        // 선택중인 슬롯 켜고끄기
        nowHoldSlot.enabled = toggle;

        // 켜질때는
        if (toggle)
        {
            // 기존 트윈 종료
            nowHoldSlot.material.DOKill();

            // 색깔 흰색으로 깜빡이기
            nowHoldSlot.material.DOColor(Color.white, "_Tint", 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .OnStart(() =>
            {
                // 투명하게 초기화
                nowHoldSlot.material.SetColor("_Tint", new Color(1, 1, 1, 0));
            })
            .OnKill(() =>
            {
                // 투명하게 초기화
                nowHoldSlot.material.SetColor("_Tint", new Color(1, 1, 1, 0));
            });
        }
        else
            // 깜빡임 트윈 종료
            nowHoldSlot.material.DOKill();
    }

    // Transform ShowBindKey(string keyName, string actionName)
    // {
    //     // 키 액션 프리팹 생성
    //     Transform bindKey = LeanPool.Spawn(bindKeyPrefab, bindKeyList);

    //     // 키 이름 텍스트 찾기
    //     TextMeshProUGUI keyText = bindKey.Find("KeyImage/KeyName").GetComponent<TextMeshProUGUI>();
    //     // 액션 텍스트 찾기
    //     TextMeshProUGUI actionText = bindKey.Find("Action").GetComponent<TextMeshProUGUI>();

    //     // 키 이름 넣기
    //     keyText.text = keyName;
    //     // 액션 이름 넣기
    //     actionText.text = actionName;

    //     // UI 강제 갱신
    //     Canvas.ForceUpdateCanvases();

    //     // 생성된 UI 리턴
    //     return bindKey;
    // }

    public void HoldIcon(Transform parent)
    {
        // 홀드 중인 아이콘을 마우스 커서의 자식으로 넣기 및 위치 초기화
        nowHoldSlot.transform.SetParent(parent);
        // 첫번째 자식으로 넣기
        nowHoldSlot.transform.SetSiblingIndex(0);

        // 사이즈 초기화
        nowHoldSlot.transform.localScale = Vector2.one;
        // 로컬 위치 초기화
        nowHoldSlot.transform.localPosition = Vector2.zero;
    }
}
