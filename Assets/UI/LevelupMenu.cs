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
    public GameObject magicBtnParent; //레벨업 시 마법 버튼들의 부모 오브젝트
    public GameObject elementIcon; //마법 재료 원소 아이콘 프리팹
    public GameObject elementPlus; //마법 재료 사이 플러스 아이콘 프리팹

    // public List<MagicInfo> magicDB = null; //마법 정보 DB
    // public Sprite[] magicIcon = null; //마법 아이콘 리스트
    // public GameObject[] magicPrefab = null; //마법 프리팹 리스트

    List<MagicInfo> notHasMagic = new List<MagicInfo>(); //플레이어가 보유하지 않은 마법 리스트

    void Awake()
    {
        // magicDB = MagicDB.Instance.magicDB;
        // magicIcon = MagicDB.Instance.magicIcon;
        // magicPrefab = MagicDB.Instance.magicPrefab;

        // 보유하지 않은 마법만 DB에서 파싱
        notHasMagic.Clear();
        notHasMagic = MagicDB.Instance.magicDB.FindAll(x => x.hasMagic == false);
    }

    // 오브젝트가 active 될때 호출함수
    private void OnEnable()
    {
        // 레벨업 메뉴에 마법 정보 넣기
        if (MagicDB.Instance.loadDone)
            SetMenu();
    }

    private void Update()
    {
        if (MagicDB.Instance.loadDone && Input.GetKeyDown(KeyCode.Mouse0))
        {
            // 레벨업 메뉴에 마법 정보 넣기
            // SetMenu();
        }
    }

    void SetMenu()
    {
        // 보유하지 않은 마법만 DB에서 파싱
        notHasMagic = MagicDB.Instance.magicDB.FindAll(x => x.hasMagic == false);

        //랜덤 마법 인덱스 3개 뽑기
        int[] randomNum = RandomMagicIndex();

        for (int i = 0; i < randomNum.Length; i++)
        {
            // 획득 가능한 마법 없을때, 원소 젬 주기
            if (randomNum[i] == -1)
            {
                //TODO 원소 젬 정보 입력
            }
            else
            {
                int num = randomNum[i]; //미리 뽑아놓은 랜덤 인덱스 3개
                Transform magicBtn = magicBtnParent.transform.GetChild(i); //마법 버튼 UI

                // 마법 ID 넣기
                magicBtn.GetComponent<MagicBtn>().magicID = notHasMagic[num].id;

                // 마법 아이콘 넣기
                Image icon = magicBtn.Find("Background/MagicIcon").GetComponent<Image>();
                //! 마법 아이콘 스프라이트 그려지면 0에서 num으로 바꾸기
                icon.sprite = MagicDB.Instance.magicIcon.Find(x => x.name == notHasMagic[0].magicName.Replace(" ","") + "_Icon");

                // 마법 등급 넣기
                Image btnBackground = magicBtn.GetComponent<Image>();
                btnBackground.color = MagicDB.Instance.gradeColor[notHasMagic[num].grade - 1];

                // 마법 이름 넣기
                Text name = magicBtn.Find("Background/MagicDescript/Name").GetComponent<Text>();
                name.text = notHasMagic[num].magicName;

                // 마법 속성 넣기
                Transform elementParent = magicBtn.Find("Background/MagicDescript/Element").transform; //마법 속성 넣을 부모 찾기
                DestoryChildren(elementParent); //모든 자식 요소 제거

                // 해당 마법의 원소 배열
                List<string> elements = new List<string>();
                elements.Clear();
                ElementalSorting(elements, notHasMagic[num].element_A);
                ElementalSorting(elements, notHasMagic[num].element_B);
                // 배열 가나다순으로 정렬
                elements.Sort();

                //첫번째 원소 아이콘 넣기
                var elementIcon_0 = Instantiate(elementIcon, elementParent.position, Quaternion.identity); // 원소 아이콘
                elementIcon_0.transform.SetParent(elementParent.transform);
                elementIcon_0.transform.localScale = Vector3.one;
                elementIcon_0.GetComponent<Image>().color = MagicDB.Instance.ElementColor(elements[0]); //원소 색 넣기

                //두번째~마지막 원소 아이콘 넣기
                for (int j = 1; j < elements.Count; j++)
                {
                    //플러스 아이콘 넣기
                    var plus = Instantiate(elementPlus, elementParent.transform.position, Quaternion.identity); //플러스 모양 아이콘
                    plus.transform.SetParent(elementParent.transform);
                    plus.transform.localScale = Vector3.one;

                    //첫번째 원소 아이콘 넣기
                    var elementIcon = Instantiate(this.elementIcon, elementParent.position, Quaternion.identity); // 원소 아이콘
                    elementIcon.transform.SetParent(elementParent.transform);
                    elementIcon.transform.localScale = Vector3.one;
                    elementIcon.GetComponent<Image>().color = MagicDB.Instance.ElementColor(elements[j]); //원소 색 넣기
                }

                // 마법 설명 넣기
                Text descript = magicBtn.Find("Background/MagicDescript/Descript").GetComponent<Text>();
                descript.text = notHasMagic[num].description;
            }
        }
    }

    //랜덤 인덱스 3가지 뽑기
    int[] RandomMagicIndex()
    {
        //모든 마법 인덱스를 넣을 리스트
        List<int> magicIndex = new List<int>();

        //인덱스 모두 넣기
        for (int i = 0; i < notHasMagic.Count; i++)
        {
            magicIndex.Add(i);
        }

        //랜덤 인덱스 3개를 넣을 배열
        int[] randomNum = new int[3];

        for (int i = 0; i < 3; i++)
        {
            // 획득 가능한 마법 없을때
            if (magicIndex.Count == 0)
            {
                randomNum[i] = -1;
            }
            else
            {
                //인덱스 리스트에서 랜덤한 난수 생성
                int j = Random.Range(0, magicIndex.Count);
                int index = magicIndex[j];
                // print(magicIndex.Count + " : " + index);

                //랜덤 인덱스 숫자 넣기
                randomNum[i] = index;
                //이미 선택된 인덱스 제거
                magicIndex.RemoveAt(j);
            }
        }

        //인덱스 리스트 리턴
        return randomNum;
    }

    bool isBasicElement(string element)
    {
        //기본 원소 이름과 일치하는 요소가 있는지 확인
        bool isExist = System.Array.Exists(MagicDB.Instance.elementNames, x => x == element);

        return isExist;
    }

    void ElementalSorting(List<string> elements, string element)
    {
        //첫번째 원소가 기본 원소일때
        if (isBasicElement(element))
        {
            //이 마법 원소에 해당 원소 없을때
            if (!elements.Exists(x => x == element))
                elements.Add(element);
        }
        //첫번째 원소가 기본 원소 아닐때
        else
        {
            if (MagicDB.Instance.magicDB.Exists(x => x.magicName == element))
            {
                // 원소 이름을 마법 이름에 넣어 마법 찾기
                MagicInfo magicInfo = MagicDB.Instance.magicDB.Find(x => x.magicName == element);
                // 해당 마법의 원소 두가지 다시 정렬하기
                ElementalSorting(elements, magicInfo.element_A);
                ElementalSorting(elements, magicInfo.element_B);
            }
        }
    }

    //오브젝트의 모든 자식을 제거
    void DestoryChildren(Transform obj)
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
