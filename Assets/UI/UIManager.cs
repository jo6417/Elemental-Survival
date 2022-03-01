using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Lean.Pool;

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

    [Header("PlayerUI")]
    public Image playerHp;
    public SlicedFilledImage playerExp;
    public Text playerLev;
    public Text EarthGem_UI;
    public Text FireGem_UI;
    public Text LifeGem_UI;
    public Text LightningGem_UI;
    public Text WaterGem_UI;
    public Text WindGem_UI;
    public GameObject statsUI; //일시정지 메뉴 스탯 UI
    public GameObject hasItemIcon; //플레이어 현재 소지 아이템 아이콘
    public Transform hasItemsUI; //플레이어 현재 소지 아이템 표시 UI


    private void Start()
    {
        Time.timeScale = 1; //시간값 초기화
    }

    private void Update()
    {
        // 일시정지 메뉴 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Resume();
        }
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
        updateExp();
        updateHp();
        updateStat();
    }

    public void updateExp()
    {
        // 경험치 바 갱신
        playerExp.fillAmount = PlayerManager.Instance.ExpNow / PlayerManager.Instance.ExpMax;

        // 레벨 갱신
        playerLev.text = "Lev. " + PlayerManager.Instance.Level.ToString();
    }

    public void updateHp()
    {
        playerHp.fillAmount = PlayerManager.Instance.hpNow / PlayerManager.Instance.hpMax;
    }

    public void updateGem()
    {
        EarthGem_UI.text = "x " + PlayerManager.Instance.Earth_Gem.ToString();
        FireGem_UI.text = "x " + PlayerManager.Instance.Fire_Gem.ToString();
        LifeGem_UI.text = "x " + PlayerManager.Instance.Life_Gem.ToString();
        LightningGem_UI.text = "x " + PlayerManager.Instance.Lightning_Gem.ToString();
        WaterGem_UI.text = "x " + PlayerManager.Instance.Water_Gem.ToString();
        WindGem_UI.text = "x " + PlayerManager.Instance.Wind_Gem.ToString();
    }

    public void updateItem()
    {
        //기존 아이콘 모두 없에기
        Image[] children = hasItemsUI.GetComponentsInChildren<Image>();
        print(children.Length);

        //모든 자식 오브젝트 비활성화
        if (children != null)
            for (int j = 0; j < children.Length; j++)
            {
                LeanPool.Despawn(children[j].gameObject);
            }

        foreach (var item in PlayerManager.Instance.hasItems)
        {
            print(item.itemName + " x" + item.hasNum);

            //아이템 아이콘 오브젝트 생성
            GameObject icon = LeanPool.Spawn(hasItemIcon, hasItemsUI.position, Quaternion.identity, hasItemsUI);

            //스프라이트 넣기
            icon.GetComponent<Image>().sprite =
            ItemDB.Instance.itemIcon.Find(x => x.name == item.itemName.Replace(" ", "") + "_Icon");

            //아이템 개수 넣기, 2개 이상부터 표시
            Text amount = icon.GetComponentInChildren<Text>(true);
            if (item.hasNum >= 2)
            {
                amount.gameObject.SetActive(true);
                amount.text = "x " + item.hasNum.ToString();
            }
            else
            {
                amount.gameObject.SetActive(false);
            }

        }
    }

    public void updateStat()
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
        stats[5].text = Mathf.Round(PlayerManager.Instance.rateFire * 100).ToString() + " %";
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
    public void DestoryChildren(Transform obj)
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
