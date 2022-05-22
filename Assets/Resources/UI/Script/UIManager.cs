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

    // public delegate void OnGemChanged();
    // public OnGemChanged onGemChangedCallback;
    float time_start;
    public float time_current; // 현재 스테이지 플레이 타임
    public int killCount;
    public bool enemyPointSwitch = false; //화면 밖의 적 방향 표시 여부

    [Header("ReferUI")]
    RectTransform UIRect;
    public GameObject magicMixPanel;
    public GameObject chestPanel;
    public GameObject vendMachinePanel;
    public GameObject slotMachinePanel;
    public GameObject magicUpgradePanel;
    public GameObject ultimateMagicPanel;
    public GameObject pausePanel;
    public TextMeshProUGUI timer;
    public TextMeshProUGUI killCountTxt;
    public GameObject bossHp;
    public GameObject arrowPrefab; //적 방향 가리킬 화살표 UI
    public GameObject iconArrowPrefab; //오브젝트 방향 기리킬 아이콘 화살표 UI

    //! 테스트, 선택된 UI 이름
    public TextMeshProUGUI nowSelectUI;

    [Header("UI Cursor")]
    public GameObject UI_Cursor; //선택된 UI 따라다니는 UI커서
    public Selectable lastSelected; //마지막 선택된 오브젝트
    public Color lastOriginColor; //마지막 선택된 오브젝트 원래 selected 색깔
    public float UI_CursorPadding; //UI 커서 여백
    bool isFlicking = false; //커서 깜빡임 여부
    bool isMove = false; //커서 이동중 여부
    Sequence cursorSeq; //깜빡임 시퀀스
    Vector2 lastMousePos; //마지막 마우스 위치
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
    public Transform hasMagicsUI; //플레이어 현재 소지한 모든 마법 UI
    public Transform ultimateMagicIcon; //궁극기 마법 슬롯 UI
    public Image ultimateIndicator; //궁극기 슬롯 인디케이터 이미지

    private void Start()
    {
        Time.timeScale = 1; //시간값 초기화

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
        // 일시정지 메뉴 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Resume();
        }

        //게임시간 타이머 업데이트
        if (SystemManager.Instance.playerTimeScale != 0)
            UpdateTimer();
        else
            ResumeTimer();

        //마우스 숨기기 토글
        HideToggleMouseCursor();

        //선택된 UI 따라다니기
        FollowUICursor();
    }

    void HideToggleMouseCursor()
    {
        // 방향키 누르면
        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            if (Cursor.lockState == CursorLockMode.None)
            {
                HasStuffToolTip.Instance.QuitTooltip();
                ProductToolTip.Instance.QuitTooltip();
                //마우스 숨기기
                Cursor.lockState = CursorLockMode.Locked;
            }

            //마지막 마우스 위치 기억
            lastMousePos = Input.mousePosition;
        }

        // 마우스 움직이면
        if (Input.GetAxisRaw("Mouse X") != 0 || Input.GetAxisRaw("Mouse Y") != 0)
        {
            // 마우스 고정인데 툴팁 떠있으면 끄기
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                HasStuffToolTip.Instance.QuitTooltip();
                ProductToolTip.Instance.QuitTooltip();
                //마우스 고정해제
                Cursor.lockState = CursorLockMode.None;

                // UI 커서 끄기
                UI_Cursor.SetActive(false);
            }
        }
    }

    void FollowUICursor()
    {
        // lastSelected와 현재 선택버튼이 같으면 버튼 깜빡임 코루틴 시작
        if (EventSystem.current.currentSelectedGameObject == null
        || !EventSystem.current.currentSelectedGameObject.activeSelf
        || !EventSystem.current.currentSelectedGameObject.activeInHierarchy
        || lastSelected != EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>()
        || Cursor.lockState == CursorLockMode.None
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
                    lastSelected.GetComponent<Image>().color = lastOriginColor;

                //커서 애니메이션 끝
                isFlicking = false;
            }

            // lastSelected 새로 갱신해서 기억하기
            if (EventSystem.current.currentSelectedGameObject)
            {
                //마지막 버튼 기억 갱신
                lastSelected = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();

                //원본 컬러 기억하기
                lastOriginColor = EventSystem.current.currentSelectedGameObject.GetComponentInChildren<Image>().color;
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
                CursorAnim();
            }

            //ui커서 따라가기
            if (!isMove)
                UI_Cursor.transform.position = EventSystem.current.currentSelectedGameObject.transform.position;
        }

        //방향키 입력 들어왔을때
        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            // UI커서가 꺼져있고 lastSelected가 있으면 lastSelected 선택
            if (!UI_Cursor.activeSelf && lastSelected)
                lastSelected.Select();
        }
    }

    void HideUICursor()
    {
        //UI커서 비활성화
        UI_Cursor.SetActive(false);

        //UI커서 크기 및 위치 초기화
        // print(new Vector2(Screen.width, Screen.height));
        // UI_Cursor.transform.localScale = new Vector2(1920f, 1080f);
        // UI_Cursor.transform.position = transform.position;
    }

    void CursorAnim()
    {
        Image image = EventSystem.current.currentSelectedGameObject.GetComponentInChildren<Image>();
        RectTransform cursorRect = UI_Cursor.GetComponent<RectTransform>();
        RectTransform lastRect = EventSystem.current.currentSelectedGameObject.GetComponent<RectTransform>();

        //깜빡일 시간
        float flickTime = 0.3f;
        //깜빡일 컬러 강조 비율
        float colorRate = 1.4f;
        //깜빡일 컬러
        Color flickColor = new Color(lastOriginColor.r * colorRate, lastOriginColor.g * colorRate, lastOriginColor.b * colorRate, 1f);
        //커서 사이즈 + 여백 추가
        Vector2 size = lastRect.sizeDelta + Vector2.one * UI_CursorPadding;
        //이동할 버튼 위치
        Vector3 btnPos = EventSystem.current.currentSelectedGameObject.transform.position;

        UI_Cursor.SetActive(true); //UI 커서 활성화

        //원래 트윈 죽이기
        UI_Cursor.transform.DOKill();
        cursorRect.DOKill();
        cursorSeq.Kill();

        //이동 시간 카운트
        float moveCount = 0f;
        //버튼 위치로 UI커서 이동
        UI_Cursor.transform.DOMove(btnPos, flickTime)
        .OnStart(() =>
        {
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
            .SetLoops(-1)
            .PrependCallback(() =>
            {
                // 선택된 버튼과 커서 크기 맞추기
                cursorRect.sizeDelta = size;
            })
            // 깜빡이는 색으로 변경, 해당 버튼 사이즈보다 확대
            .Append(
                image.DOColor(flickColor, flickTime)
            )
            .Join(
                cursorRect.DOSizeDelta(size + Vector2.one * 20f, flickTime)
            )
            // 원본 색깔로 복구, 해당 버튼 사이즈 원본 사이즈 복구
            .Append(
                image.DOColor(lastOriginColor, flickTime)
            )
            .Join(
                cursorRect.DOSizeDelta(size, flickTime)
            )
            .OnKill(() =>
            {
                image.color = lastOriginColor;
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
        time_start = Time.time;
        time_current = 0;
        timer.text = "00:00";
    }

    public void ResumeTimer()
    {
        time_start = Time.time - time_current;
    }

    public void UpdateTimer()
    {
        time_current = (int)(Time.time - time_start);

        //시간을 3600으로 나눈 몫
        string hour = 0 < (int)(time_current / 3600f) ? string.Format("{0:00}", Mathf.FloorToInt(time_current / 3600f)) + ":" : "";
        //시간을 60으로 나눈 몫을 60으로 나눈 나머지
        string minute = 0 < (int)(time_current / 60f % 60f) ? string.Format("{0:00}", Mathf.FloorToInt(time_current / 60f % 60f)) + ":" : "00:";
        //시간을 60으로 나눈 나머지
        string second = string.Format("{0:00}", time_current % 60f);

        //시간 출력
        timer.text = hour + minute + second;

        //TODO 시간 UI 색깔 변경
        //TODO 색깔에 따라 난이도 변경
    }

    public void UpdateKillCount()
    {
        //킬 카운트 표시
        killCountTxt.text = killCount.ToString();
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

    public void UpdateMagics()
    {
        //모든 자식 오브젝트 비활성화
        for (int j = 0; j < hasMagicsUI.childCount; j++)
        {
            LeanPool.Despawn(hasMagicsUI.GetChild(j).gameObject);
        }

        foreach (var magic in PlayerManager.Instance.hasMagics)
        {
            //0등급은 원소젬이므로 표시 안함
            if (magic.grade == 0)
                continue;

            //궁극기는 표시 안함
            if (magic.castType == "ultimate")
                continue;

            //마법 아이콘 오브젝트 생성
            GameObject magicIcon = LeanPool.Spawn(hasItemIcon, hasMagicsUI.position, Quaternion.identity, hasMagicsUI);

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
    }

    public void AddMagicUI(MagicInfo magic)
    {
        Transform matchIcon = null;

        //모든 자식 오브젝트 비활성화
        for (int j = 0; j < hasMagicsUI.childCount; j++)
        {
            // TooltipTrigger의 magic이 같은 아이콘 찾기
            if (hasMagicsUI.GetChild(j).GetComponent<ToolTipTrigger>().magic == magic)
            {
                matchIcon = hasMagicsUI.GetChild(j);
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
            GameObject magicIcon = LeanPool.Spawn(hasItemIcon, hasMagicsUI.position, Quaternion.identity, hasMagicsUI);

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
        MagicInfo ultimateMagic = PlayerManager.Instance.ultimateMagic;

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
        float coolTimeRate
        = PlayerManager.Instance.ultimateMagic != null
        ? PlayerManager.Instance.ultimateCoolCount / MagicDB.Instance.MagicCoolTime(PlayerManager.Instance.ultimateMagic)
        : 0;

        ultimateMagicIcon.Find("CoolTime").GetComponent<Image>().fillAmount = coolTimeRate;
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

    public void PopupUI(GameObject popup)
    {
        // 팝업 UI 토글
        popup.SetActive(!popup.activeSelf);

        // 시간 정지 토글
        Time.timeScale = popup.activeSelf ? 0 : 1;

        if (!popup.activeSelf)
        {
            // 팝업 꺼질때 UI 커서 끄기
            UI_Cursor.SetActive(false);
            //UI커서 크기 및 위치 초기화
            cursorRect.sizeDelta = UIRect.sizeDelta;
            UI_Cursor.transform.position = transform.position;

            //null 선택하기
            EventSystem.current.SetSelectedGameObject(null);
        }

    }

    public void PopupUI(GameObject popup, bool forceSwitch = true)
    {
        // 팝업 UI 토글
        popup.SetActive(forceSwitch);

        // 시간 정지 토글
        Time.timeScale = popup.activeSelf ? 0 : 1;

        if (!popup.activeSelf)
        {
            // 팝업 꺼질때 UI 커서 끄기
            UI_Cursor.SetActive(false);
            //UI커서 크기 및 위치 초기화
            cursorRect.sizeDelta = UIRect.sizeDelta;
            UI_Cursor.transform.position = transform.position;

            //null 선택하기
            EventSystem.current.SetSelectedGameObject(null);
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
}
