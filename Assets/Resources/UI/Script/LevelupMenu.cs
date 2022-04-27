using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using UnityEngine.UI;
using System.Linq;

public class LevelupMenu : MonoBehaviour
{
    [Header("Refer")]
    public GameObject btnParent; //레벨업 시 마법 버튼들의 부모 오브젝트
    // public GameObject elementIcon; //마법 재료 원소 아이콘 프리팹
    // public GameObject elementPlus; //마법 재료 사이 플러스 아이콘 프리팹

    // List<MagicInfo> notHasMagic = new List<MagicInfo>(); //플레이어가 보유하지 않은 마법 리스트

    void Awake()
    {
        // 보유하지 않은 마법만 DB에서 파싱
        // notHasMagic.Clear();
        // notHasMagic = MagicDB.Instance.magicDB.FindAll(x => x.magicLevel == 0);
    }

    // 오브젝트가 active 될때 호출함수
    private void OnEnable()
    {
        // 레벨업 메뉴에 마법 정보 넣기
        if (MagicDB.Instance.loadDone)
            SetArtifact();
    }

    void SetArtifact()
    {
        //아이템 타입이 아티팩트인 모든아이템 리스트
        List<ItemInfo> artifactList = ItemDB.Instance.itemDB.FindAll(x => x.itemType == "Artifact");
        // 랜덤 아티팩트ID 뽑기, 중복제거됨
        int[] randomIDs = ItemDB.Instance.RandomItemIndex(3);

        // 고정된 3개 아티팩트 버튼에 정보 (아티팩트ID, 아이콘, 등급색깔, 이름, 설명)
        for (int i = 0; i < randomIDs.Length; i++)
        {
            int itmeID = randomIDs[i];
            ItemInfo item = ItemDB.Instance.GetItemByID(randomIDs[i]);
            Transform magicBtnObj = btnParent.transform.GetChild(i); //마법 버튼 UI

            // 아티팩트 ID, 버튼타입 넣기
            InfoHolder infoHolder = magicBtnObj.GetComponent<InfoHolder>();
            infoHolder.holderType = InfoHolder.HolderType.itemHolder;
            infoHolder.id = itmeID;
            infoHolder.popupMenu = gameObject;

            // 신규 아이템 여부 표시
            Transform newTxt = magicBtnObj.transform.Find("Background/Icon/New");
            if (item.amount > 0)
            {
                //New 아이템 아님
                newTxt.gameObject.SetActive(false);
            }
            else
            {
                //New 아이템
                newTxt.gameObject.SetActive(true);
            }

            // 아티팩트 아이콘 넣기
            Image icon = magicBtnObj.Find("Background/Icon").GetComponent<Image>();
            //! 마법 아이콘 스프라이트 그려지면 0에서 num으로 바꾸기
            icon.sprite = ItemDB.Instance.itemIcon.Find(x => x.name == item.itemName.Replace(" ", "") + "_Icon");

            // 아티팩트 등급 넣기
            Image btnBackground = magicBtnObj.GetComponent<Image>();
            btnBackground.color = MagicDB.Instance.gradeColor[item.grade];

            // 아티팩트 이름 넣기
            Text name = magicBtnObj.Find("Background/Descript/Name").GetComponent<Text>();
            name.text = item.itemName;

            // 아티팩트 설명 넣기
            Text descript = magicBtnObj.Find("Background/Descript/Descript").GetComponent<Text>();
            descript.text = item.description;
        }
        //TODO 플레이어가 보유하지 않은 아이템은 New Item 표시
    }

    bool isBasicElement(string element)
    {
        //기본 원소 이름과 일치하는 요소가 있는지 확인
        bool isExist = System.Array.Exists(MagicDB.Instance.elementNames, x => x == element);

        return isExist;
    }

    // void ElementalSorting(List<string> elements, string element)
    // {
    //     //첫번째 원소가 기본 원소일때
    //     if (isBasicElement(element))
    //     {
    //         //이 마법 원소에 해당 원소 없을때
    //         if (!elements.Exists(x => x == element))
    //             elements.Add(element);
    //     }
    //     //첫번째 원소가 기본 원소 아닐때
    //     else
    //     {
    //         if (MagicDB.Instance.magicInfo.Exists(x => x.magicName == element))
    //         {
    //             // 원소 이름을 마법 이름에 넣어 마법 찾기
    //             MagicInfo magicInfo = MagicDB.Instance.magicInfo.Find(x => x.magicName == element);
    //             // 해당 마법의 원소 두가지 다시 정렬하기
    //             ElementalSorting(elements, magicInfo.element_A);
    //             ElementalSorting(elements, magicInfo.element_B);
    //         }
    //     }
    // }
}
