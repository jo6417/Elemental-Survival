using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UltimateMagic : MonoBehaviour
{
    #region Singleton
    private static UltimateMagic instance;
    public static UltimateMagic Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<UltimateMagic>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<UltimateMagic>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    public MagicInfo newMagic;

    public RectTransform magicPanel; //얻은 궁극기 정보 팝업창
    public GameObject acceptBtn; //확인 버튼
    public GameObject chooseMagicPanel; //기존 마법 있을때 마법 선택창

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    private void Start()
    {
        // newMagic = MagicDB.Instance.GetMagicByID(49);
    }

    IEnumerator Init()
    {
        //모든 오브젝트 끄고 대기
        magicPanel.gameObject.SetActive(false);
        chooseMagicPanel.SetActive(false);
        acceptBtn.SetActive(false);

        //패널 둘다 사이즈 제로
        magicPanel.transform.localScale = Vector2.zero;
        chooseMagicPanel.transform.localScale = Vector2.zero;

        //magic 정보 들어올때까지 대기
        yield return new WaitUntil(() => newMagic != null);

        MagicInfo oldMagic = PlayerManager.Instance.ultimateList[0];

        // 마법 정보창에 모든 정보 넣기
        Transform magicIcon = magicPanel.transform.Find("NewMagic");
        //새 마법 등급 색 넣기
        magicIcon.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.GradeColor[newMagic.grade];
        //새 마법 아이콘 넣기
        magicIcon.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.GetMagicIcon(newMagic.id);

        //새 마법 이름,설명 넣기
        magicPanel.transform.Find("MagicName").GetComponent<TextMeshProUGUI>().text = newMagic.magicName;
        magicPanel.transform.Find("MagicDescript").GetComponent<TextMeshProUGUI>().text = newMagic.description;

        //마법 스탯 리스트화 하기
        List<string> statAmounts = new List<string>();
        statAmounts.Add(newMagic.power.ToString());
        statAmounts.Add(newMagic.speed.ToString());
        statAmounts.Add(newMagic.range.ToString());
        statAmounts.Add(newMagic.duration.ToString());
        statAmounts.Add(newMagic.critical.ToString());
        statAmounts.Add(newMagic.criticalPower.ToString());
        statAmounts.Add(newMagic.pierce.ToString());
        statAmounts.Add(newMagic.projectile.ToString());
        statAmounts.Add(newMagic.coolTime.ToString());

        //모든 마법 스탯 넣기
        Transform stats = magicPanel.transform.Find("Stat");
        for (int i = 0; i < stats.childCount; i++)
        {
            stats.GetChild(i).Find("Amount").GetComponent<TextMeshProUGUI>().text = statAmounts[i];
        }

        // 기존 마법 없을때 화면 가운데로
        if (oldMagic == null)
        {
            magicPanel.anchoredPosition = Vector2.zero;
            magicPanel.gameObject.SetActive(true);
            chooseMagicPanel.SetActive(false);
            //확인 버튼 활성화
            acceptBtn.SetActive(true);
            //확인 버튼 선택하기
            acceptBtn.GetComponent<Button>().Select();

            //패널 사이즈 키우기
            magicPanel.transform.DOScale(Vector3.one, 0.5f)
            .SetUpdate(true);
        }
        //TODO 기존 마법 있을때 위로 올리기, 마법 선택창 띄우기
        else
        {
            magicPanel.anchoredPosition = Vector2.zero + Vector2.up * 200f;
            magicPanel.gameObject.SetActive(true);
            chooseMagicPanel.SetActive(true);
            acceptBtn.SetActive(false);

            //패널 사이즈 키우기
            magicPanel.transform.DOScale(Vector3.one, 0.5f)
            .SetUpdate(true);
            chooseMagicPanel.transform.DOScale(Vector3.one, 0.5f)
            .SetUpdate(true);

            //TODO 마법 선택창에 magicInfo 및 아이콘 바꾸기
            //새 마법 아이콘 바꾸기
            Transform newMagicIcon = chooseMagicPanel.transform.Find("NewMagic");
            newMagicIcon.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.GradeColor[newMagic.grade];
            newMagicIcon.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.GetMagicIcon(newMagic.id);

            //기존 마법 아이콘 바꾸기
            Transform oldMagicIcon = chooseMagicPanel.transform.Find("OldMagic");
            newMagicIcon.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.GradeColor[oldMagic.grade];
            newMagicIcon.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.GetMagicIcon(oldMagic.id);
        }
    }

    public void AcceptBtn()
    {
        // 팝업 닫기
        UIManager.Instance.PopupUI(UIManager.Instance.ultimateMagicPanel);
        //마법 합성 팝업 닫기
        UIManager.Instance.PopupUI(UIManager.Instance.mixMagicPanel, false);

        // 궁극기 장착
        PlayerManager.Instance.EquipUltimate();

        //선택 정보 삭제
        EventSystem.current.SetSelectedGameObject(null);
    }
}
