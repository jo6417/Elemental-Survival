using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Pool;
using UnityEngine;
using UnityEngine.UI;

public class ScrollMenu : MonoBehaviour
{
    public GameObject noMagicText; //합성 가능한 마법 없다는 텍스트
    public GameObject magicRecipeBtn; //마법 합성 버튼
    public GameObject recipeParent; //버튼 들어갈 부모 오브젝트
    List<MagicInfo> notHasMagic = new List<MagicInfo>(); //플레이어가 보유하지 않은 마법 리스트

    private void OnEnable()
    {
        // 레벨업 메뉴에 마법 정보 넣기
        if (MagicDB.Instance.loadDone)
            SetMenu();
    }

    void SetMenu()
    {
        // 보유하지 않은 마법만 DB에서 파싱
        notHasMagic.Clear();
        notHasMagic = MagicDB.Instance.magicDB.FindAll(x => x.magicLevel == 0);

        //오브젝트의 자식 모두 제거
        UIManager.Instance.DestroyChildren(recipeParent.transform);

        // 플레이어 보유중인 마법 참조
        List<MagicInfo> playerMagics = PlayerManager.Instance.hasMagics;

        // 마법DB에서 재료 둘다 갖고 있는 마법 찾기
        List<MagicInfo> combineMagics = MagicDB.Instance.magicDB.FindAll(x =>
        MagicDB.Instance.GetMagicByName(x.element_A) != null &&
        MagicDB.Instance.GetMagicByName(x.element_B) != null &&
        playerMagics.Exists(y => y == MagicDB.Instance.GetMagicByName(x.element_A)) &&
        playerMagics.Exists(y => y == MagicDB.Instance.GetMagicByName(x.element_B))
        );

        // 높은 등급부터 내림차순 하기
        string line = "";
        foreach (var m in combineMagics)
        {
            line += ", " + m.grade.ToString();
        }
        // print(line);

        // 높은 등급부터 내림차순 하기
        combineMagics.Sort((MagicInfo x, MagicInfo y) => y.grade.CompareTo(x.grade));

        line = "";
        foreach (var m in combineMagics)
        {
            line += ", " + m.grade.ToString();
        }
        // print(line);

        // 합성 가능한 마법 없을때
        noMagicText.SetActive(false);
        if (combineMagics.Count == 0)
        {
            noMagicText.SetActive(true);
            return;
        }

        // 마법DB에서 재료 둘다 갖고 있는 마법 찾기
        foreach (var magic in combineMagics)
        {
            // 해당 마법의 재료들
            MagicInfo magicA = MagicDB.Instance.GetMagicByName(magic.element_A);
            MagicInfo magicB = MagicDB.Instance.GetMagicByName(magic.element_B);

            // print(magic.magicName);

            // 마법 합성 버튼 만들기
            GameObject recipe = LeanPool.Spawn(magicRecipeBtn, transform.position, Quaternion.identity, recipeParent.transform);
            // recipe.transform.SetParent();
            recipe.transform.localScale = Vector3.one;

            InfoHolder btn = recipe.GetComponent<InfoHolder>();
            //닫을 팝업 메뉴 넣기
            btn.popupMenu = gameObject;
            // 마법 id 넣기
            btn.id = magic.id;
            btn.holderType = InfoHolder.HolderType.magicHolder;

            // 해당 마법의 원소 배열
            List<string> elements = new List<string>();
            elements.Clear();
            MagicDB.Instance.ElementalSorting(elements, magic.element_A);
            MagicDB.Instance.ElementalSorting(elements, magic.element_B);
            // 배열 가나다순으로 정렬
            elements.Sort();

            // 마법 아이콘 넣기
            Transform icon = recipe.transform.Find("MagicIcon");
            icon.GetComponent<Image>().sprite = MagicDB.Instance.magicIcon.Find(
                x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

            // 마법 등급 색깔 넣기
            Image frame = recipe.transform.Find("MagicIcon/MagicFrame").GetComponent<Image>();
            frame.color = MagicDB.Instance.gradeColor[magic.grade - 1];

            // 마법 해당되는 원소 넣기
            Transform[] elementIcons = recipe.transform.Find("MagicIcon/GemCircle").GetComponentsInChildren<Transform>();

            for (int i = 1; i < elementIcons.Length; i++)
            {
                if (elements.Exists(x => x == elementIcons[i].name))
                {
                    elementIcons[i].gameObject.SetActive(true);
                }
                else
                {
                    elementIcons[i].gameObject.SetActive(false);
                }
            }

            // 신규 마법 여부 표시
            Transform newTxt = recipe.transform.Find("MagicIcon/New");
            if (magic.magicLevel > 0)
            {
                //New 아이템 아님
                newTxt.gameObject.SetActive(false);
            }
            else
            {
                //New 아이템
                newTxt.gameObject.SetActive(true);
            }

            // 마법 이름 넣기
            Transform name = recipe.transform.Find("MagicDescript/Name");
            name.GetComponent<Text>().text = magic.magicName;
            // 마법 설명 넣기
            Transform descript = recipe.transform.Find("MagicDescript/Description");
            descript.GetComponent<Text>().text = magic.description;

            // 버튼에서 재료A 이미지 오브젝트 찾기
            Transform elementA = recipe.transform.Find("MagicDescript/Element/Element_A");
            // 재료A의 이미지 넣기
            elementA.GetComponent<Image>().sprite = MagicDB.Instance.magicIcon.Find(
                x => x.name == magicA.magicName.Replace(" ", "") + "_Icon");

            // 버튼에서 재료B 이미지 오브젝트 찾기
            Transform elementB = recipe.transform.Find("MagicDescript/Element/Element_B");
            // 재료B의 이미지 넣기
            elementB.GetComponent<Image>().sprite = MagicDB.Instance.magicIcon.Find(
                x => x.name == magicB.magicName.Replace(" ", "") + "_Icon");
            
            //재료 A,B 등급 색상 넣기
            Image gradeA = recipe.transform.Find("MagicDescript/Element/Element_A/Frame").GetComponent<Image>();
            gradeA.color = MagicDB.Instance.gradeColor[magicA.grade - 1];
            Image gradeB = recipe.transform.Find("MagicDescript/Element/Element_B/Frame").GetComponent<Image>();
            gradeB.color = MagicDB.Instance.gradeColor[magicB.grade - 1];
        }
    }
}
