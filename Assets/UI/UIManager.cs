using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        
    }

    public void InitialStat(){
        updateExp();
        updateHp();
    }

    public void updateExp(){
        // 경험치 바 갱신
        playerExp.fillAmount = PlayerManager.Instance.ExpNow / PlayerManager.Instance.ExpMax;
        
        // 레벨 갱신
        playerLev.text = "Lev. " + PlayerManager.Instance.Level.ToString();
    }

    public void updateHp(){
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
}
