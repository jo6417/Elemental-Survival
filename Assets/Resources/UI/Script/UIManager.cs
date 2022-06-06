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
                var obj = FindObjectOfType<UIManager>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<UIManager>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    public bool enemyPointSwitch = false; //화면 밖의 적 방향 표시 여부

    [Header("<Input>")]
    public NewInput UI_Input; // UI 인풋 받기
    public Vector2 nowMousePos; // 마우스 마지막 위치 기억

    [Header("PopupUI")]
    public GameObject nowOpenPopup; //현재 열려있는 팝업 UI
    public Transform popupUIparent; //팝업 UI 담는 부모 오브젝트
    RectTransform UIRect;
    public GameObject mixMagicPanel;
    public GameObject mergeMagicPanel;
    public GameObject chestPanel;
    public GameObject vendMachinePanel;
    public GameObject slotMachinePanel;
    public GameObject magicUpgradePanel;
    public GameObject ultimateMagicPanel;
    public GameObject pausePanel;
    public GameObject gameoverPanel;

    [Header("ReferUI")]
    public Transform gameoverScreen;
    public GameObject gameoverSlot; //게임 오버 창에 들어갈 마법 슬롯
    public TextMeshProUGUI timer;
    public TextMeshProUGUI killCountTxt;
    public GameObject bossHp;
    public GameObject arrowPrefab; //적 방향 가리킬 화살표 UI
    public GameObject iconArrowPrefab; //오브젝트 방향 기리킬 아이콘 화살표 UI
    public Image phoneScreen; //스마트폰 알람 UI
    public bool phoneLoading; //스마트폰 로딩중 여부

    //! 테스트, 선택된 UI 이름
    public TextMeshProUGUI nowSelectUI;

    [Header("UI Cursor")]
    public GameObject UI_Cursor; //선택된 UI 따라다니는 UI커서
    public Canvas UI_CursorCanvas; //UI커서 전용 캔버스
    public Selectable lastSelected; //마지막 선택된 오브젝트
    public Color targetOriginColor; //마지막 선택된 오브젝트 원래 selected 색깔
    public float UI_CursorPadding; //UI 커서 여백
    bool isFlicking = false; //커서 깜빡임 여부
    bool isMove = false; //커서 이동중 여부
    Sequence cursorSeq; //깜빡임 시퀀스
    RectTransform cursorRect;

    [Header("PlayerUI")]
    public SlicedFilledImage playerHp;
    public TextMeshProUGUI playerHpText;
    public SlicedFilledImage playerExp;
    public TextMeshProUGUI playerLev;

    public List<TextMeshProUGUI> gemUIs = new List<TextMeshProUGUI>();
    public List<Light2D> gemUILights = new List<Light2D>();
    public GameObject gemUIParent;

    public GameObject statsUI; //일시정지 메뉴 스탯 UI
    public TextMeshProUGUI pauseScrollAmt; //일시정지 메뉴 스크롤 개수 UI
    public GameObject hasItemIcon; //플레이어 현재 소지 아이템 아이콘
    public Transform hasItemsUI; //플레이어 현재 소지한 모든 아이템 UI
    public GridLayoutUI hasMagicGrid; //플레이어 현재 소지한 모든 마법 UI
    public Transform ultimateMagicIcon; //궁극기 마법 슬롯 UI
    public Image ultimateIndicator; //궁극기 슬롯 인디케이터 이미지

    private void Awake()
    {
        //입력 초기화
        InputInitial();
    }

    void InputInitial()
    {
        UI_Input = new NewInput();

        // 방향키 입력
        UI_Input.UI.NavControl.performed += val => NavControl(val.ReadValue<Vector2>());
        // 마우스 위치 입력
        UI_Input.UI.MousePosition.performed += val => MousePos(val.ReadValue<Vector2>());
        // 확인 입력
        UI_Input.UI.Accept.performed += val => Accept();
        // 취소 입력
        UI_Input.UI.Cancel.performed += val => Cancel();
        // 스마트폰 버튼 입력
        UI_Input.UI.PhoneMenu.performed += val => PhoneOpen();
    }

    private void OnEnable()
    {
        UI_Input.Enable();
    }

    private void OnDisable()
    {
        UI_Input.Disable();
    }

    // 방향키 입력되면 실행
    void NavControl(Vector2 arrowDir)
    {
        // print(arrowDir);

        //마우스 잠겨있지않을때
        if (Cursor.lockState == CursorLockMode.None)
        {
            //모든 툴팁 끄기
            HasStuffToolTip.Instance.QuitTooltip();
            ProductToolTip.Instance.QuitTooltip();

            //마우스 숨기기
            Cursor.lockState = CursorLockMode.Locked;
        }

        // UI 커서 컨트롤
        // UI커서가 꺼져있고 lastSelected가 있으면 lastSelected 선택
        if (!UI_Cursor.activeSelf && lastSelected)
            lastSelected.Select();
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
            UICursorToggle(false);
        }
    }

    // 확인 입력
    void Accept()
    {

    }

    // 취소 입력
    void Cancel()
    {
        //일시정지 패널 켜기
        Resume();
    }

    void PhoneOpen()
    {
        //스마트폰 패널 꺼져있을때
        if (!mergeMagicPanel.activeSelf)
            PopupUI(mergeMagicPanel);
    }

    private void Start()
    {
        // Time.timeScale = 1; //시간값 초기화

        UIRect = GetComponent<RectTransform>();
        cursorRect = UI_Cursor.GetComponent<RectTransform>();

        // GemUI 전부 찾기
        TextMeshProUGUI[] gems = gemUIParent.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI gemUI in gems)
        {
            gemUIs.Add(gemUI);
        }

        // GemUI Light2D 전부 찾기
        Light2D[] lights = gemUIParent.GetComponentsInChildren<Light2D>();
        foreach (Light2D light in lights)
        {
            //리스트에 추가
            gemUILights.Add(light);
            //밝기 0으로 낮추기
            light.intensity = 0;
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
        //게임시간 타이머 업데이트
        if (SystemManager.Instance.playerTimeScale != 0)
            UpdateTimer();
        else
            ResumeTimer();

        //선택된 UI 따라다니기
        FollowUICursor();
    }

    void FollowUICursor()
    {
        // lastSelected와 현재 선택버튼이 같으면 버튼 깜빡임 코루틴 시작
        if (EventSystem.current.currentSelectedGameObject == null //현재 선택 버튼이 없을때
        || !EventSystem.current.currentSelectedGameObject.activeSelf //현재 선택 버튼 비활성화 체크 됬을때
        || !EventSystem.current.currentSelectedGameObject.activeInHierarchy //현재 선택 버튼 실제로 비활성화 됬을때
        || lastSelected != EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>() //다른 버튼 선택 됬을때
        || !lastSelected.interactable //버튼 상호작용 꺼졌을때
        || Cursor.lockState == CursorLockMode.None //마우스 켜져있을때
        )
        {
            // UI커서 애니메이션 켜져있으면
            if (isFlicking)
            {
                //깜빡임 시퀀스 종료
                cursorSeq.Pause();
                cursorSeq.Kill();

                //기억하고 있는 버튼 있으면 색 복구하기
                if (lastSelected)
                    lastSelected.targetGraphic.color = targetOriginColor;

                //커서 애니메이션 끝
                isFlicking = false;
            }

            // lastSelected 새로 갱신해서 기억하기
            if (EventSystem.current.currentSelectedGameObject)
            {
                //마지막 버튼 기억 갱신
                lastSelected = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();

                //원본 컬러 기억하기
                targetOriginColor = lastSelected.targetGraphic.color;
            }
        }
        //선택된 버튼이 바뀌었을때
        else
        {
            //! 테스트, 현재 선택된 UI 이름 표시
            nowSelectUI.text = "Last Select : " + EventSystem.current.currentSelectedGameObject.name;

            if (!isFlicking)
            {
                //커서 애니메이션 시작
                isFlicking = true;
                isMove = true;

                //커서 애니메이션 시작
                StartCoroutine(NewCursorAnim());
            }

            // domove 끝났으면 타겟 위치 따라가기
            if (!isMove)
                UI_Cursor.transform.position = lastSelected.transform.position;
        }
    }

    public void UICursorToggle(bool setToggle)
    {
        // 커서 켜져있을때
        if (!setToggle && UI_Cursor.activeSelf)
        {
            //UI커서 비활성화
            UI_Cursor.SetActive(false);
        }

        // 커서 꺼져있을때
        if (setToggle && !UI_Cursor.activeSelf)
        {
            //UI커서 크기 및 위치 초기화
            RectTransform cursorCanvasRect = UI_CursorCanvas.GetComponent<RectTransform>();
            cursorRect.sizeDelta = cursorCanvasRect.sizeDelta;
            print("cursorCanvasRect.sizeDelta : " + cursorCanvasRect.sizeDelta);

            cursorRect.position = Vector2.zero;

            //UI커서 활성화
            UI_Cursor.SetActive(true);
        }

    }

    IEnumerator NewCursorAnim()
    {
        // 선택된 타겟 이미지
        Image image = lastSelected.targetGraphic.GetComponent<Image>();
        // 선택된 이미지 Rect
        RectTransform lastRect = lastSelected.GetComponent<RectTransform>();

        //깜빡일 시간
        float flickTime = 0.3f;
        //깜빡일 컬러 강조 비율
        float colorRate = 1.4f;
        //깜빡일 컬러
        Color flickColor = new Color(targetOriginColor.r * colorRate, targetOriginColor.g * colorRate, targetOriginColor.b * colorRate, 1f);
        //이동할 버튼 위치
        Vector3 btnPos = EventSystem.current.currentSelectedGameObject.transform.position;

        //커서 사이즈 + 여백 추가
        Vector2 size = lastRect.sizeDelta + lastRect.sizeDelta * 0.1f;

        //마지막 선택된 버튼의 캔버스
        Canvas selectedCanvas = lastRect.GetComponentInParent<Canvas>();

        // UI커서 부모 캔버스와 선택된 버튼 부모 캔버스의 렌더모드가 다를때
        if (UI_CursorCanvas.renderMode != selectedCanvas.renderMode)
        {
            //렌더 모드 일치 시키기
            UI_CursorCanvas.renderMode = selectedCanvas.renderMode;

            // 바뀐 렌더모드에 따라 커서 스케일 정의
            RectTransform cursorCanvasRect = UI_CursorCanvas.GetComponent<RectTransform>();
            if (UI_CursorCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                cursorCanvasRect.localScale = Vector2.one;
            }
            else
            {
                cursorCanvasRect.localScale = Vector2.one / 64f;

                //캔버스 z축을 선택된 캔버스에 맞추기
                Vector3 canvasPos = cursorCanvasRect.position;
                canvasPos.z = selectedCanvas.transform.position.z;
                cursorCanvasRect.position = canvasPos;
            }
        }

        //UI 커서 활성화
        UICursorToggle(true);
        // UI_Cursor.SetActive(true);

        //원래 트윈 있으면 죽이기
        if (image != null && DOTween.IsTweening(image))
            image.DOKill();

        if (UI_Cursor.transform != null && DOTween.IsTweening(UI_Cursor.transform))
            UI_Cursor.transform.DOKill();

        if (cursorRect != null && DOTween.IsTweening(cursorRect))
            cursorRect.DOKill();

        if (cursorSeq.IsActive())
            cursorSeq.Kill();

        //TODO 타겟 위치로 이동
        UI_Cursor.transform.DOMove(btnPos, flickTime)
        .SetUpdate(true);

        //TODO 타겟과 사이즈 맞추기
        cursorRect.DOSizeDelta(size, flickTime)
        .SetUpdate(true);

        //이동 시간 카운트
        float moveCount = flickTime;
        while (isMove && EventSystem.current.currentSelectedGameObject != null)
        {
            //남은 이동 시간 차감
            moveCount -= Time.deltaTime;

            //타겟 위치 변경되면
            //이동 중 버튼 위치가 바뀌면
            if (btnPos != EventSystem.current.currentSelectedGameObject.transform.position)
            {
                //버튼 위치 갱신
                btnPos = EventSystem.current.currentSelectedGameObject.transform.position;

                //원래 트윈 죽이기
                UI_Cursor.transform.DOKill();

                // 새롭게 이동 트윈
                UI_Cursor.transform.DOMove(btnPos, moveCount)
                .SetUpdate(true);
            }

            yield return new WaitForSeconds(Time.unscaledDeltaTime);
        }

        //이동 시간 대기
        yield return new WaitForSecondsRealtime(flickTime);

        //이동 끝
        isMove = false;
        //깜빡임 시작
        isFlicking = true;

        //사이즈 초기화
        cursorRect.sizeDelta = size;
        // 사이즈 커졌다 작아졌다 무한 반복
        cursorRect.DOSizeDelta(size + size * 0.1f, flickTime)
        .SetLoops(-1, LoopType.Yoyo)
        .SetUpdate(true)
        .OnKill(() =>
        {
            //사이즈 초기화
            cursorRect.sizeDelta = size;
        });

        // 컬러 초기화
        image.color = targetOriginColor;
        // 컬러 깜빡이기 무한 반복
        image.DOColor(flickColor, flickTime)
        .SetLoops(-1, LoopType.Yoyo)
        .SetUpdate(true)
        .OnKill(() =>
        {
            // 컬러 초기화
            image.color = targetOriginColor;
        });
    }

    void CursorAnim()
    {
        Image image = lastSelected.targetGraphic.GetComponent<Image>();
        RectTransform lastRect = lastSelected.GetComponent<RectTransform>();

        //깜빡일 시간
        float flickTime = 0.3f;
        //깜빡일 컬러 강조 비율
        float colorRate = 1.4f;
        //깜빡일 컬러
        Color flickColor = new Color(targetOriginColor.r * colorRate, targetOriginColor.g * colorRate, targetOriginColor.b * colorRate, 1f);
        //이동할 버튼 위치
        Vector3 btnPos = EventSystem.current.currentSelectedGameObject.transform.position;

        //커서 사이즈 + 여백 추가
        Vector2 size = lastRect.sizeDelta + lastRect.sizeDelta * 0.1f;

        //마지막 선택된 버튼의 캔버스
        Canvas selectedCanvas = lastRect.GetComponentInParent<Canvas>();

        // UI커서 부모 캔버스와 선택된 버튼 부모 캔버스의 렌더모드가 다를때
        if (UI_CursorCanvas.renderMode != selectedCanvas.renderMode)
        {
            //렌더 모드 일치 시키기
            UI_CursorCanvas.renderMode = selectedCanvas.renderMode;

            // 바뀐 렌더모드에 따라 커서 스케일 정의
            RectTransform cursorCanvasRect = UI_CursorCanvas.GetComponent<RectTransform>();
            if (UI_CursorCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                cursorCanvasRect.localScale = Vector2.one;
            }
            else
            {
                cursorCanvasRect.localScale = Vector2.one / 64f;

                //캔버스 z축을 선택된 캔버스에 맞추기
                Vector3 canvasPos = cursorCanvasRect.position;
                canvasPos.z = selectedCanvas.transform.position.z;
                cursorCanvasRect.position = canvasPos;
            }
        }

        //UI 커서 활성화
        UICursorToggle(true);
        // UI_Cursor.SetActive(true);

        //원래 트윈 있으면 죽이기
        if (image != null && DOTween.IsTweening(image))
            image.DOKill();

        if (UI_Cursor.transform != null && DOTween.IsTweening(UI_Cursor.transform))
            UI_Cursor.transform.DOKill();

        if (cursorRect != null && DOTween.IsTweening(cursorRect))
            cursorRect.DOKill();

        if (cursorSeq.IsActive())
            cursorSeq.Kill();

        //이동 시간 카운트
        float moveCount = 0f;
        //버튼 위치로 UI커서 이동
        UI_Cursor.transform.DOMove(btnPos, flickTime)
        .OnStart(() =>
        {
            //커서 애니메이션 시작
            isFlicking = true;
            isMove = true;

            float moveCount = flickTime;
        })
        .OnUpdate(() =>
        {
            //남은 이동 시간
            moveCount -= Time.deltaTime;

            //이동 중 버튼 위치가 바뀌면
            if (btnPos != EventSystem.current.currentSelectedGameObject.transform.position)
            {
                //버튼 위치 갱신
                btnPos = EventSystem.current.currentSelectedGameObject.transform.position;

                // 남은 이동시간동안 새로운 위치로 재이동
                UI_Cursor.transform.DOMove(btnPos, moveCount)
                .SetUpdate(true);
            }
        })
        .SetUpdate(true)
        .OnComplete(() =>
        {
            //커서 이동 끝
            isMove = false;

            cursorSeq = DOTween.Sequence();
            cursorSeq
            .OnStart(() =>
            {
                //커서 애니메이션 시작
                isFlicking = true;
                isMove = true;
            })
            .SetLoops(-1)
            .PrependCallback(() =>
            {
                // 선택된 버튼과 커서 크기 맞추기
                cursorRect.sizeDelta = size;
            })
            // 깜빡이는 색으로 변경, 해당 버튼 사이즈보다 확대
            .Append(
                image.DOColor(flickColor, flickTime)
                .OnKill(() =>
                {
                    image.color = targetOriginColor;
                })
            )
            .Join(
                cursorRect.DOSizeDelta(size + size * 0.1f, flickTime)
            )
            // 원본 색깔로 복구, 해당 버튼 사이즈 원본 사이즈 복구
            .Append(
                image.DOColor(targetOriginColor, flickTime)
                .OnKill(() =>
                {
                    image.color = targetOriginColor;
                })
            )
            .Join(
                cursorRect.DOSizeDelta(size, flickTime)
            )
            .OnKill(() =>
            {
                image.DOKill();
                UI_Cursor.transform.DOKill();
                cursorRect.DOKill();
                image.color = targetOriginColor;
            })
            .SetUpdate(true);
        });

        // 선택된 버튼과 커서 크기 맞추기
        cursorRect.DOSizeDelta(size, flickTime)
        .SetUpdate(true);
    }

    public void QuitMainMenu()
    {
        // 메인메뉴 씬 불러오기
        SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
    }

    //옵션 메뉴 띄우기
    public void Option()
    {

    }

    //게임 재개
    public void Resume()
    {
        //일시정지 메뉴 UI 토글
        PopupUI(pausePanel);
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
        SystemManager.Instance.time_current = (int)(Time.time - SystemManager.Instance.time_start);

        //시간을 3600으로 나눈 몫
        string hour = 0 < (int)(SystemManager.Instance.time_current / 3600f) ? string.Format("{0:00}", Mathf.FloorToInt(SystemManager.Instance.time_current / 3600f)) + ":" : "";
        //시간을 60으로 나눈 몫을 60으로 나눈 나머지
        string minute = 0 < (int)(SystemManager.Instance.time_current / 60f % 60f) ? string.Format("{0:00}", Mathf.FloorToInt(SystemManager.Instance.time_current / 60f % 60f)) + ":" : "00:";
        //시간을 60으로 나눈 나머지
        string second = string.Format("{0:00}", SystemManager.Instance.time_current % 60f);

        //시간 출력
        timer.text = hour + minute + second;

        return hour + minute + second;

        //TODO 시간 UI 색깔 변경
        //TODO 색깔에 따라 난이도 변경
    }

    public void UpdateKillCount()
    {
        //킬 카운트 표시
        killCountTxt.text = SystemManager.Instance.killCount.ToString();
    }

    public void UpdateExp()
    {
        // 경험치 바 갱신
        playerExp.fillAmount = PlayerManager.Instance.PlayerStat_Now.ExpNow / PlayerManager.Instance.PlayerStat_Now.ExpMax;

        // 레벨 갱신
        playerLev.text = "Lev. " + PlayerManager.Instance.PlayerStat_Now.Level.ToString();
    }

    public void UpdateBossHp(float bossHpNow, float bossHpMax, string bossName)
    {
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
        = bossHpNow <= 0 ? "" : bossName;

        //체력 0 이하면 체력 UI 끄기
        if (bossHpNow <= 0)
        {
            //보스 체력 UI 비활성화
            bossHp.SetActive(false);
        }
    }

    public void UpdateHp()
    {
        playerHp.fillAmount = PlayerManager.Instance.PlayerStat_Now.hpNow / PlayerManager.Instance.PlayerStat_Now.hpMax;
        playerHpText.text = (int)PlayerManager.Instance.PlayerStat_Now.hpNow + " / " + (int)PlayerManager.Instance.PlayerStat_Now.hpMax;
    }

    public void UpdateGem(int gemTypeIndex)
    {
        gemUIs[gemTypeIndex].text = PlayerManager.Instance.hasGems[gemTypeIndex].ToString();
    }

    public void GemIndicator(int gemIndex)
    {
        Light2D gemLight = gemUILights[gemIndex];

        //밝기 0으로 초기화
        gemLight.intensity = 0;

        //밝기 1까지 부드럽게 올렸다 내리기
        DOTween.To(() => gemLight.intensity, x => gemLight.intensity = x, 1, 0.2f)
        .OnComplete(() =>
        {
            DOTween.To(() => gemLight.intensity, x => gemLight.intensity = x, 0, 0.2f);
        });
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

        foreach (var item in PlayerManager.Instance.hasItems)
        {
            // print(item.itemName + " x" + item.hasNum);

            //스크롤일때
            if (item.itemType == "Scroll")
            {
                pauseScrollAmt.text = "x " + item.amount.ToString();
                continue;
            }

            //아티팩트 아니면 넘기기
            if (item.itemType != "Artifact")
                continue;

            //아이템 아이콘 오브젝트 생성
            GameObject itemIcon = LeanPool.Spawn(hasItemIcon, hasItemsUI.position, Quaternion.identity, hasItemsUI);

            // 오브젝트에 아이템 정보 저장
            ToolTipTrigger toolTipTrigger = itemIcon.GetComponent<ToolTipTrigger>();
            toolTipTrigger.toolTipType = ToolTipTrigger.ToolTipType.HasStuffTip;
            toolTipTrigger.item = item;

            //스프라이트 넣기
            itemIcon.GetComponent<Image>().sprite =
            ItemDB.Instance.itemIcon.Find(x => x.name == item.itemName.Replace(" ", "") + "_Icon");

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

    public void UpdateMagics(List<MagicInfo> magicList)
    {
        //모든 자식 오브젝트 비활성화
        int childNum = hasMagicGrid.transform.childCount;
        for (int i = 0; i < childNum; i++)
        {
            LeanPool.Despawn(hasMagicGrid.transform.GetChild(0).gameObject);
        }

        foreach (MagicInfo magic in magicList)
        {
            //0등급은 원소젬이므로 표시 안함
            if (magic.grade == 0)
                continue;

            //궁극기는 표시 안함
            if (magic.castType == "ultimate")
                continue;

            //마법 아이콘 오브젝트 생성
            GameObject magicIcon = LeanPool.Spawn(hasItemIcon, hasMagicGrid.transform.position, Quaternion.identity, hasMagicGrid.transform);

            //툴팁에 마법 정보 저장
            ToolTipTrigger toolTipTrigger = magicIcon.GetComponent<ToolTipTrigger>();
            toolTipTrigger.toolTipType = ToolTipTrigger.ToolTipType.HasStuffTip;
            toolTipTrigger.magic = magic;

            //아이콘 넣기
            magicIcon.GetComponent<Image>().sprite = MagicDB.Instance.GetMagicIcon(magic.id);
            // MagicDB.Instance.magicIcon.Find(x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

            //마법 레벨 넣기
            TextMeshProUGUI amount = magicIcon.GetComponentInChildren<TextMeshProUGUI>(true);
            amount.gameObject.SetActive(true);
            amount.text = "Lev." + magic.magicLevel.ToString();
        }

        //그리드 업데이트 명령하기
        hasMagicGrid.isChanged = true;
    }

    public void AddMagicUI(MagicInfo magic)
    {
        Transform matchIcon = null;

        //모든 자식 오브젝트 비활성화
        for (int j = 0; j < hasMagicGrid.transform.childCount; j++)
        {
            // TooltipTrigger의 magic이 같은 아이콘 찾기
            if (hasMagicGrid.transform.GetChild(j).GetComponent<ToolTipTrigger>().magic == magic)
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

            //궁극기는 표시 안함
            if (magic.castType == "ultimate")
                return;

            //마법 아이콘 오브젝트 생성
            GameObject magicIcon = LeanPool.Spawn(hasItemIcon, hasMagicGrid.transform.position, Quaternion.identity, hasMagicGrid.transform);

            //툴팁에 마법 정보 저장
            ToolTipTrigger toolTipTrigger = magicIcon.GetComponent<ToolTipTrigger>();
            toolTipTrigger.toolTipType = ToolTipTrigger.ToolTipType.HasStuffTip;
            toolTipTrigger.magic = magic;

            //스프라이트 넣기
            magicIcon.GetComponent<Image>().sprite = MagicDB.Instance.GetMagicIcon(magic.id);
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

    public void UpdateUltimateIcon()
    {
        //현재 보유중인 궁극기 마법 불러오기
        MagicInfo ultimateMagic = null;
        if (PlayerManager.Instance.ultimateList.Count > 0)
            ultimateMagic = PlayerManager.Instance.ultimateList[0];

        Image frame = ultimateMagicIcon.Find("Frame").GetComponent<Image>();
        Image icon = ultimateMagicIcon.Find("Icon").GetComponent<Image>();

        //궁극기 마법 등급 및 아이콘 넣기
        if (ultimateMagic != null)
        {
            frame.color = MagicDB.Instance.gradeColor[ultimateMagic.grade];
            icon.sprite = MagicDB.Instance.GetMagicIcon(ultimateMagic.id);
            icon.gameObject.SetActive(true);
        }
        else
        {
            frame.color = Color.white;
            icon.gameObject.SetActive(false);
        }
    }

    public void UltimateCooltime()
    {
        // 남은 쿨타임
        float coolTimeRate = 0f;

        // 쿨타임 이미지 불러오기
        Image coolTimeImg = ultimateMagicIcon.Find("CoolTime").GetComponent<Image>();

        // 마법이 없으면 쿨타임 이미지 비우기
        if (PlayerManager.Instance.ultimateList.Count <= 0)
            coolTimeImg.fillAmount = 0;
        //마법이 있으면 쿨타임만큼 채우기
        else
        {
            coolTimeRate
            = PlayerManager.Instance.ultimateList[0] != null
            ? PlayerManager.Instance.ultimateCoolCount / MagicDB.Instance.MagicCoolTime(PlayerManager.Instance.ultimateList[0])
            : 0;

            coolTimeImg.fillAmount = coolTimeRate;
        }
    }

    public void UpdateStat()
    {
        // 스탯 입력할 UI
        Text[] stats = statsUI.GetComponentsInChildren<Text>();

        // print("stats.Length : " + stats.Length);

        // 스탯 입력값
        List<float> statAmount = new List<float>();

        stats[0].text = PlayerManager.Instance.PlayerStat_Now.hpMax.ToString();
        stats[1].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.power * 100).ToString() + " %";
        stats[2].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.armor * 100).ToString() + " %";
        stats[3].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.moveSpeed * 100).ToString() + " %";
        stats[4].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.projectileNum * 100).ToString() + " %";
        stats[5].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.speed * 100).ToString() + " %";
        stats[6].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.coolTime * 100).ToString() + " %";
        stats[7].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.duration * 100).ToString() + " %";
        stats[8].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.range * 100).ToString() + " %";
        stats[9].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.luck * 100).ToString() + " %";
        stats[10].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.expGain * 100).ToString() + " %";
        stats[11].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.moneyGain * 100).ToString() + " %";

        stats[12].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.earth_atk * 100).ToString() + " %";
        stats[13].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.fire_atk * 100).ToString() + " %";
        stats[14].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.life_atk * 100).ToString() + " %";
        stats[15].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.lightning_atk * 100).ToString() + " %";
        stats[16].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.water_atk * 100).ToString() + " %";
        stats[17].text = Mathf.Round(PlayerManager.Instance.PlayerStat_Now.wind_atk * 100).ToString() + " %";
    }

    public void PhoneNotice()
    {
        Image notice = UIManager.Instance.phoneScreen.transform.Find("Notice").GetComponent<Image>();
        TextMeshProUGUI stackNum = notice.transform.Find("Text").GetComponent<TextMeshProUGUI>();

        //TODO 스택 0개일때
        if (PlayerManager.Instance.hasStackMagics.Count == 0)
        {
            //화면 켜져있을때 끄기
            if (UIManager.Instance.phoneScreen.color.a > 0f)
            {
                //이미 반짝이는 트윈 중이면 트윈 킬
                if (DOTween.IsTweening(UIManager.Instance.phoneScreen))
                {
                    UIManager.Instance.phoneScreen.DOKill();
                }

                //화면 밝기 0으로
                UIManager.Instance.phoneScreen.DOColor(new Color(1, 1, 1, 0), 0.5f);
            }

            //알림 아이콘 끄기
            notice.gameObject.SetActive(false);
        }
        //TODO 스택 1개 이상일때
        else
        {
            //이미 반짝이는 트윈 중이면 트윈 킬
            if (DOTween.IsTweening(UIManager.Instance.phoneScreen))
            {
                UIManager.Instance.phoneScreen.DOKill();
            }

            //스마트폰 UI 화면 밝히기
            UIManager.Instance.phoneScreen.color = Color.white; //색 초기화
            UIManager.Instance.phoneScreen.DOColor(new Color(1, 1, 1, 0), 1f)
            .SetLoops(-1, LoopType.Yoyo);

            //알림 아이콘 켜기
            notice.gameObject.SetActive(true);
            //스택 개수 넣기
            stackNum.text = PlayerManager.Instance.hasStackMagics.Count.ToString();
        }
    }

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
        {
            return;
        }

        // 팝업 UI 토글
        popup.SetActive(forceSwitch);

        //팝업 세팅
        PopupSet(popup);
    }

    void PopupSet(GameObject popup)
    {
        // 시간 정지 토글
        Time.timeScale = popup.activeSelf ? 0 : 1;

        //팝업 off
        if (!popup.activeSelf)
        {
            // 팝업 꺼질때 UI 커서 끄기
            UICursorToggle(false);

            //null 선택하기
            EventSystem.current.SetSelectedGameObject(null);

            //플레이어 입력 켜기
            PlayerManager.Instance.playerInput.Enable();

            //현재 열려있는 팝업 비우기
            nowOpenPopup = null;
        }
        //팝업 on
        else
        {
            //플레이어 입력 끄기
            PlayerManager.Instance.playerInput.Disable();

            //현재 열려있는 팝업 갱신
            nowOpenPopup = popup;
        }
    }

    //오브젝트의 모든 자식을 제거
    public void DestroyChildren(Transform obj)
    {
        Transform[] children = obj.GetComponentsInChildren<Transform>();
        //모든 자식 오브젝트 제거
        if (children != null)
            for (int j = 1; j < children.Length; j++)
            {
                if (children[j] != transform)
                {
                    Destroy(children[j].gameObject);
                }
            }
    }

    // 화면 밖 오브젝트 방향 표시 Nav UI
    public IEnumerator PointObject(GameObject targetObj, Sprite icon)
    {
        // 오버레이 풀에서 화살표 UI 생성
        GameObject arrowUI = LeanPool.Spawn(iconArrowPrefab, targetObj.transform.position, Quaternion.identity, SystemManager.Instance.overlayPool);

        //rect 찾기
        RectTransform rect = arrowUI.GetComponent<RectTransform>();

        // 화살표의 아이콘 이미지 바꾸기
        arrowUI.transform.Find("Icon").GetComponent<Image>().sprite = icon;

        // 방향 가리킬 화살표
        Transform arrow = arrowUI.transform.Find("Arrow");

        //오브젝트 활성화 되어있으면
        while (targetObj.activeSelf)
        {
            // 오브젝트가 화면 안에 있으면 화살표 비활성화, 밖에 있으면 활성화
            Vector3 arrowPos = Camera.main.WorldToViewportPoint(targetObj.transform.position);
            if (arrowPos.x < 0f
            || arrowPos.x > 1f
            || arrowPos.y < 0f
            || arrowPos.y > 1f)
            {
                arrowUI.SetActive(true);

                // 화살표 위치가 화면 밖으로 벗어나지않게 제한
                arrowPos.x = Mathf.Clamp(arrowPos.x, 0f, 1f);
                arrowPos.y = Mathf.Clamp(arrowPos.y, 0f, 1f);

                // 화면 가장짜리쪽으로 피벗 변경
                rect.pivot = arrowPos;

                // 아이콘 화살표 위치 이동
                arrowUI.transform.position = Camera.main.ViewportToWorldPoint(arrowPos);

                // 오브젝트 방향 가리키기
                Vector2 dir = targetObj.transform.position - PlayerManager.Instance.transform.position;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 225f;
                arrow.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
            else
            {
                arrowUI.SetActive(false);
            }

            yield return new WaitForSeconds(Time.deltaTime);
        }

        //오브젝트 비활성화되면
        yield return new WaitUntil(() => !targetObj.activeSelf);

        //화살표 디스폰
        LeanPool.Despawn(arrowUI);
    }

    public void GameOver(bool isClear = false)
    {
        // 시간 멈추기
        Time.timeScale = 0f;
        // 게임 오버 UI 켜기
        gameoverPanel.SetActive(true);

        // 클리어 여부에 따라 타이틀 바꾸기
        TextMeshProUGUI title = gameoverScreen.Find("Title").GetComponent<TextMeshProUGUI>();
        // 클리어 여부에 따라 스크린 배경색 바꾸기
        Image background = gameoverScreen.Find("ScreenBackground").GetComponent<Image>();
        if (isClear)
        {
            title.text = "CLEAR";
            background.color = SystemManager.Instance.HexToRGBA("00903E");
        }
        else
        {
            title.text = "GAME OVER";
            background.color = SystemManager.Instance.HexToRGBA("006090");
        }

        //TODO 캐릭터 넣기
        gameoverScreen.Find("Stat/Character/Amount").GetComponent<TextMeshProUGUI>().text = "캐릭터 완료";
        //TODO 맵 넣기
        gameoverScreen.Find("Stat/Map/Amount").GetComponent<TextMeshProUGUI>().text = "맵 완료";
        // 현재 시간 넣기
        gameoverScreen.Find("Stat/Time/Amount").GetComponent<TextMeshProUGUI>().text = UpdateTimer();
        // 재화 넣기
        gameoverScreen.Find("Stat/Money/Amount").GetComponent<TextMeshProUGUI>().text = "재화 완료";
        // 킬 수 넣기
        gameoverScreen.Find("Stat/KillCount/Amount").GetComponent<TextMeshProUGUI>().text = SystemManager.Instance.killCount.ToString();
        //TODO 사망원인 넣기
        gameoverScreen.Find("Stat/KilledBy/Amount").GetComponent<TextMeshProUGUI>().text = "사망원인 완료";

        //TODO id 순으로(등급순) 정렬하기
        // 이번 게임에서 보유 했었던 마법 전부 넣기
        Transform hasMagics = gameoverScreen.Find("HasMagic");
        DestroyChildren(hasMagics); //모든 자식 제거
        for (int i = 0; i < MagicDB.Instance.touchedMagics.Count; i++)
        {
            //마법 찾기
            MagicInfo magic = MagicDB.Instance.GetMagicByID(MagicDB.Instance.touchedMagics[i]);
            print(magic.magicName);
            //마법 슬롯 생성
            Transform slot = LeanPool.Spawn(gameoverSlot, hasMagics.position, Quaternion.identity, hasMagics).transform;

            //프레임 색 넣기
            slot.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[magic.grade];
            //아이콘 넣기
            slot.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.GetMagicIcon(magic.id);
            //레벨 넣기
            slot.Find("Level").GetComponent<TextMeshProUGUI>().text = "Lv. " + magic.magicLevel.ToString();
        }
    }
}
