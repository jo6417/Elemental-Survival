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

    [Header("ReferUI")]
    public GameObject pauseMenu;
    public GameObject magicMixUI;
    public GameObject chestUI;
    public GameObject vendMachineUI;
    public GameObject slotMachineUI;
    public GameObject magicUpgradeUI;
    public GameObject ultimateMagicUI;
    public TextMeshProUGUI timerUI;
    public Transform overlayCanvas;

    [Header("UI Cursor")]
    public GameObject UI_Cursor; //선택된 UI 따라다니는 UI커서
    public GameObject lastSelected; //마지막 선택된 오브젝트
    public Color lastOriginColor; //마지막 선택된 오브젝트 원래 selected 색깔
    public float UI_CursorPadding; //UI 커서 여백
    bool isFlicking = false; //현재 깜빡임 여부
    // Sequence flickSeq; //깜빡임 시퀀스

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
        // VarManager.Instance.AllTimeScale(1);

        // GemUI 전부 찾기
        TextMeshProUGUI[] gems = gemUIParent.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var gemUI in gems)
        {
            gemUIs.Add(gemUI);
        }

        // GemUI Light2D 전부 찾기
        Light2D[] lights = gemUIParent.GetComponentsInChildren<Light2D>();
        foreach (var light in lights)
        {
            //리스트에 추가
            gemUILights.Add(light);
            //밝기 0으로 낮추기
            light.intensity = 0;
        }
    }

    private void Update()
    {
        // 일시정지 메뉴 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Resume();

            // 보유한 모든 마법 아이콘 갱신
            UpdateMagics();
            // 보유한 모든 아이템 아이콘 갱신
            // UpdateItems();
        }

        //게임시간 타이머 업데이트
        UpdateTimer();

        //선택된 UI 따라다니기
        FollowUICursor();
    }

    void FollowUICursor()
    {
        // lastSelected와 현재 선택버튼이 같으면 버튼 깜빡임 코루틴 시작
        if (lastSelected == EventSystem.current.currentSelectedGameObject && EventSystem.current.currentSelectedGameObject != null)
        {
            //깜빡이는 코루틴 시작
            if (!isFlicking)
            {
                StartCoroutine(FlickButtonColor());
            }
        }
        else
        {
            // null이 아닌것을 선택했을때 lastSelected에 기억하기
            if (EventSystem.current.currentSelectedGameObject != null && !isFlicking)
                lastSelected = EventSystem.current.currentSelectedGameObject;
        }

        // 선택된 UI 있으면 해당 UI와 위치,사이즈 똑같이 맞추기
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            //위치 맞추기
            UI_Cursor.transform.position = EventSystem.current.currentSelectedGameObject.transform.position;

            //크기 맞추기, 여백 추가
            UI_Cursor.GetComponent<RectTransform>().sizeDelta =
            EventSystem.current.currentSelectedGameObject.GetComponent<RectTransform>().sizeDelta
            + Vector2.one * UI_CursorPadding;

            UI_Cursor.SetActive(true);
        }
        else
        {
            // null 선택하면 끄기
            UI_Cursor.SetActive(false);
        }
    }

    IEnumerator FlickButtonColor()
    {
        //코루틴 시작
        isFlicking = true;

        Image image = lastSelected.GetComponentInChildren<Image>();
        lastOriginColor = lastSelected.GetComponentInChildren<Image>().color; //원본 컬러
        float rate = 0.5f;
        Color flickColor = new Color(lastOriginColor.r * rate, lastOriginColor.g * rate, lastOriginColor.b * rate, 1f); //깜빡일 컬러

        // print(lastOriginColor + ":" + flickColor);

        while (EventSystem.current.currentSelectedGameObject != null && lastSelected == EventSystem.current.currentSelectedGameObject)
        {
            image.DOColor(flickColor, 0.5f)
            .SetUpdate(true)
            .OnUpdate(() =>
            {
                //도중에 선택 버튼 바뀌면 즉시 완료
                if (lastSelected != EventSystem.current.currentSelectedGameObject)
                    image.DOComplete();
            });

            //색 바뀔때까지 대기
            yield return new WaitUntil(() => image.color == flickColor);

            image.DOColor(lastOriginColor, 0.5f)
            .SetUpdate(true)
            .OnUpdate(() =>
            {
                //도중에 선택 버튼 바뀌면 즉시 완료
                if (lastSelected != EventSystem.current.currentSelectedGameObject)
                    image.DOComplete();
            });

            //색 바뀔때까지 대기
            yield return new WaitUntil(() => image.color == lastOriginColor);
        }

        //코루틴 끝
        isFlicking = false;
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
        pauseMenu.SetActive(!pauseMenu.activeSelf);

        // 시간 정지 토글
        Time.timeScale = pauseMenu.activeSelf ? 0 : 1;
        // if(pauseMenu.activeSelf)
        // VarManager.Instance.AllTimeScale(0);
        // else
        // VarManager.Instance.AllTimeScale(1);
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
        timerUI.text = "00:00";
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
        timerUI.text = hour + minute + second;

        //TODO 시간 UI 색깔 변경
        //TODO 색깔에 따라 난이도 변경
    }

    public void UpdateExp()
    {
        // 경험치 바 갱신
        playerExp.fillAmount = PlayerManager.Instance.ExpNow / PlayerManager.Instance.ExpMax;

        // 레벨 갱신
        playerLev.text = "Lev. " + PlayerManager.Instance.Level.ToString();
    }

    public void UpdateHp()
    {
        playerHp.fillAmount = PlayerManager.Instance.hpNow / PlayerManager.Instance.hpMax;
        playerHpText.text = (int)PlayerManager.Instance.hpNow + " / " + (int)PlayerManager.Instance.hpMax;
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
        //기존 아이콘 모두 없에기
        Image[] children = hasMagicsUI.GetComponentsInChildren<Image>();
        // print(children.Length);

        //모든 자식 오브젝트 비활성화
        if (children != null)
            for (int j = 0; j < children.Length; j++)
            {
                LeanPool.Despawn(children[j].gameObject);
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

            // 오브젝트에 마법 정보 저장
            ToolTipTrigger toolTipTrigger = magicIcon.GetComponent<ToolTipTrigger>();
            toolTipTrigger.toolTipType = ToolTipTrigger.ToolTipType.HasStuffTip;
            toolTipTrigger.magic = magic;

            //스프라이트 넣기
            magicIcon.GetComponent<Image>().sprite = MagicDB.Instance.GetMagicIcon(magic.id);
            // MagicDB.Instance.magicIcon.Find(x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

            //마법 개수 넣기, 2개 이상부터 표시
            Text amount = magicIcon.GetComponentInChildren<Text>(true);
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

        stats[0].text = PlayerManager.Instance.hpMax.ToString();
        stats[1].text = Mathf.Round(PlayerManager.Instance.power * 100).ToString() + " %";
        stats[2].text = Mathf.Round(PlayerManager.Instance.armor * 100).ToString() + " %";
        stats[3].text = Mathf.Round(PlayerManager.Instance.moveSpeed * 100).ToString() + " %";
        stats[4].text = Mathf.Round(PlayerManager.Instance.projectileNum * 100).ToString() + " %";
        stats[5].text = Mathf.Round(PlayerManager.Instance.speed * 100).ToString() + " %";
        stats[6].text = Mathf.Round(PlayerManager.Instance.coolTime * 100).ToString() + " %";
        stats[7].text = Mathf.Round(PlayerManager.Instance.duration * 100).ToString() + " %";
        stats[8].text = Mathf.Round(PlayerManager.Instance.range * 100).ToString() + " %";
        stats[9].text = Mathf.Round(PlayerManager.Instance.luck * 100).ToString() + " %";
        stats[10].text = Mathf.Round(PlayerManager.Instance.expGain * 100).ToString() + " %";
        stats[11].text = Mathf.Round(PlayerManager.Instance.moneyGain * 100).ToString() + " %";

        stats[12].text = Mathf.Round(PlayerManager.Instance.earth_atk * 100).ToString() + " %";
        stats[13].text = Mathf.Round(PlayerManager.Instance.fire_atk * 100).ToString() + " %";
        stats[14].text = Mathf.Round(PlayerManager.Instance.life_atk * 100).ToString() + " %";
        stats[15].text = Mathf.Round(PlayerManager.Instance.lightning_atk * 100).ToString() + " %";
        stats[16].text = Mathf.Round(PlayerManager.Instance.water_atk * 100).ToString() + " %";
        stats[17].text = Mathf.Round(PlayerManager.Instance.wind_atk * 100).ToString() + " %";
    }

    public void PopupUI(GameObject popup)
    {
        // 팝업 UI 토글
        popup.SetActive(!popup.activeSelf);

        // 시간 정지 토글
        Time.timeScale = popup.activeSelf ? 0 : 1;
        // if(popup.activeSelf)
        // VarManager.Instance.AllTimeScale(0);
        // else
        // VarManager.Instance.AllTimeScale(1);
    }

    public void PopupUI(GameObject popup, bool forceSwitch = true)
    {
        // 팝업 UI 토글
        popup.SetActive(forceSwitch);

        // 시간 정지 토글
        Time.timeScale = popup.activeSelf ? 0 : 1;
        // if(popup.activeSelf)
        // VarManager.Instance.AllTimeScale(0);
        // else
        // VarManager.Instance.AllTimeScale(1);

        //TODO 팝업 꺼질때 UI 커서 끄기
        UI_Cursor.SetActive(false);
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
}
