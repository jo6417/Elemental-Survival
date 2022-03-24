using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Lean.Pool;
using DG.Tweening;
using TMPro;
using UnityEngine.Experimental.Rendering.Universal;

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

    [Header("SystemUI")]
    public GameObject pauseMenu;
    public GameObject scrollMenu;
    public GameObject chestUI;
    public GameObject vendMachineUI;
    public GameObject slotMachineUI;
    public GameObject magicUpgradeUI;
    public TextMeshProUGUI TimerUI;
    float time_start;
    float time_current;

    [Header("PlayerUI")]
    public SlicedFilledImage playerHp;
    public TextMeshProUGUI playerHpText;
    public SlicedFilledImage playerExp;
    public TextMeshProUGUI playerLev;

    public List<TextMeshProUGUI> gemUIs = new List<TextMeshProUGUI>();
    public List<Light2D> gemUILights = new List<Light2D>();
    public GameObject gemUIParent;

    public GameObject statsUI; //일시정지 메뉴 스탯 UI
    public GameObject hasItemIcon; //플레이어 현재 소지 아이템 아이콘
    public Transform hasItemsUI; //플레이어 현재 소지한 모든 아이템 UI
    public Transform hasMagicsUI; //플레이어 현재 소지한 모든 마법 UI


    private void Start()
    {
        Time.timeScale = 1; //시간값 초기화

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
            UIManager.Instance.UpdateMagics();
            // 보유한 모든 아이템 아이콘 갱신
            UIManager.Instance.UpdateItems();
        }

        UpdateTimer();
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
        TimerUI.text = "00:00";
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
        TimerUI.text = hour + minute + second;

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
        playerHpText.text = PlayerManager.Instance.hpNow + " / " + PlayerManager.Instance.hpMax;
    }

    public void UpdateGem(int gemTypeIndex)
    {
        gemUIs[gemTypeIndex].text = "x " + PlayerManager.Instance.hasGems[gemTypeIndex].ToString();
    }

    public void GemIndicator(int gemIndex)
    {
        Light2D gemLight = gemUILights[gemIndex];
        
        //밝기 0으로 초기화
        gemLight.intensity = 0;

        //밝기 1까지 부드럽게 올렸다 내리기
        DOTween.To(() => gemLight.intensity, x => gemLight.intensity = x, 1, 0.2f)
        .OnComplete(() => {
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
            //마법 아이콘 오브젝트 생성
            GameObject magicIcon = LeanPool.Spawn(hasItemIcon, hasMagicsUI.position, Quaternion.identity, hasMagicsUI);

            // 오브젝트에 마법 정보 저장
            ToolTipTrigger toolTipTrigger = magicIcon.GetComponent<ToolTipTrigger>();
            toolTipTrigger.toolTipType = ToolTipTrigger.ToolTipType.HasStuffTip;
            toolTipTrigger.magic = magic;

            //스프라이트 넣기
            magicIcon.GetComponent<Image>().sprite =
            MagicDB.Instance.magicIcon.Find(x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

            //마법 개수 넣기, 2개 이상부터 표시
            Text amount = magicIcon.GetComponentInChildren<Text>(true);
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
    }

    public void PopupUI(GameObject popup, bool setToggle = true)
    {
        // 팝업 UI 토글
        popup.SetActive(setToggle);

        // 시간 정지 토글
        Time.timeScale = popup.activeSelf ? 0 : 1;
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
