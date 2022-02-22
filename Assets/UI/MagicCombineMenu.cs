using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using UnityEngine.UI;

public class MagicCombineMenu : MonoBehaviour
{    
    public GameObject magicRecipeBtn; //마법 합성 버튼
    public GameObject recipeParent; //버튼 들어갈 부모 오브젝트
    private void OnEnable()
    {
        // 레벨업 메뉴에 마법 정보 넣기
        if (MagicDB.Instance.loadDone)
            SetMenu();
    }

    void SetMenu()
    {
        //오브젝트의 자식 모두 제거
        DestroyChildren(recipeParent.transform);

        // 마법DB에서 재료 둘다 갖고 있는 마법 찾기
        foreach (var magic in MagicDB.Instance.magicDB)
        {
            // 마법 재료A와 B를 플레이어가 갖고있는지
            MagicInfo magicA = MagicDB.Instance.GetMagicByName(magic.element_A);
            MagicInfo magicB = MagicDB.Instance.GetMagicByName(magic.element_B);
            if (
                magicA != null &&
                magicB != null &&
                PlayerManager.Instance.hasMagics.Exists(x => x == magicA.id) &&
                PlayerManager.Instance.hasMagics.Exists(x => x == magicB.id)
            )
            {
                // print(magic.magicName);

                // 마법 합성 버튼 만들기
                GameObject recipe = LeanPool.Spawn(magicRecipeBtn, transform.position, Quaternion.identity);
                recipe.transform.parent = recipeParent.transform;
                recipe.transform.localScale = Vector3.one;

                MagicBtn btn = recipe.GetComponent<MagicBtn>();
                //닫을 팝업 메뉴 넣기
                btn.popupMenu = gameObject;
                // 마법 id 넣기
                btn.magicID = magic.id;

                // 마법 아이콘 넣기
                Transform icon = recipe.transform.Find("MagicIcon");
                icon.GetComponent<Image>().sprite = MagicDB.Instance.magicIcon.Find(
                    x => x.name == magic.magicName.Replace(" ", "") + "_Icon");
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
            }
        }
    }

    //오브젝트의 모든 자식을 제거
    void DestroyChildren(Transform obj)
    {
        Transform[] children = obj.GetComponentsInChildren<Transform>();

        print(children.Length);
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
