using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private void Start() {
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

    public void QuitMainMenu(){
        // 메인메뉴 씬 불러오기
        SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Single);
    }

    //옵션 메뉴 띄우기
    public void Option(){

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

    public void ScrollMenu(){
        // 마법 합성 메뉴 UI 토글
        scrollMenu.SetActive(!scrollMenu.activeSelf);

        // 시간 정지 토글
        Time.timeScale = scrollMenu.activeSelf ? 0 : 1;

        //TODO 현재 합성 가능한 마법 리스트 만들기

    }
}
