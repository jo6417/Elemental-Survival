using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;
using System.Linq;

public class VendMachine : MonoBehaviour
{
    enum ProductType { Item, Magic };
    private List<ItemInfo> items = new List<ItemInfo>(); //자판기에 들어갈 아이템 목록
    private List<MagicInfo> magics = new List<MagicInfo>(); //자판기에 들어갈 마법 목록

    [Header("Refer")]
    [SerializeField]
    private Sprite itemFrame; //아이템 프레임 스프라이트
    [SerializeField]
    private Sprite magicFrame; //마법 프레임 스프라이트
    [SerializeField]
    private Transform productsParent; //상품 부모 오브젝트
    [SerializeField]
    private GameObject productPrefab; //상품 프리팹
    [SerializeField]
    private float rate = 0.5f; //아티팩트가 아닌 마법이 나올 확률

    private void OnEnable()
    {
        // 자판기 상품 불러오기
        if (MagicDB.Instance.loadDone)
            LoadProducts();
    }

    void LoadProducts()
    {
        //상품 모두 지우기
        UIManager.Instance.DestroyChildren(productsParent);

        // 랜덤 아티팩트 or 마법 9개 불러오기, 중복 제거
        List<ProductType> productTypes = new List<ProductType>();
        int itemNum = Random.Range(0,10);
        int magicNum = 9 - itemNum;

        // type == true 일때 아이템, 아니면 마법
        for (int i = 0; i < itemNum; i++)
        {
            productTypes.Add(ProductType.Item);
        }
        for (int i = 0; i < magicNum; i++)
        {
            productTypes.Add(ProductType.Magic);
        }

        // print(itemNum +":"+ magicNum);
        // string p = string.Join(", ", productTypes);
        // print(p);

        //TODO 등급마다 확률 다르게
        //아이템 타입이 아티팩트인 모든아이템 리스트
        List<ItemInfo> itemList = ItemDB.Instance.itemDB.FindAll(x => x.itemType == "Artifact");
        // 중복제거된 랜덤 아티팩트ID 뽑기
        int[] itemIDs = ItemDB.Instance.RandomArtifactIndex(itemList, itemNum);

        items.Clear();
        for (int i = 0; i < itemIDs.Length; i++)
        {
            ItemInfo item = ItemDB.Instance.GetItemByID(itemIDs[i]);
            items.Add(item);
        }

        //높은 등급부터 내림차순
        // if(items != null && items.Count > 2)
        // items.Sort((ItemInfo x, ItemInfo y) => y.grade.CompareTo(x.grade));
        List<ItemInfo> sortedItems = 
        items.OrderByDescending(x => x.grade).ToList();

        //TODO 등급마다 확률 다르게
        // 중복제거된 랜덤 마법 id 뽑기
        int[] magicIDs = MagicDB.Instance.RandomMagicIndex(MagicDB.Instance.magicDB, magicNum);

        magics.Clear();
        for (int i = 0; i < magicIDs.Length; i++)
        {
            MagicInfo magic = MagicDB.Instance.GetMagicByID(magicIDs[i]);
            magics.Add(magic);
        }

        //높은 등급부터 내림차순
        // if(magics != null && magics.Count > 2)
        // magics.Sort((MagicInfo x, MagicInfo y) => y.grade.CompareTo(x.grade));
        List<MagicInfo> sortedMagics = magics.OrderByDescending(x => x.grade).ToList();

        foreach (var type in productTypes)
        {
            // 상품 인스턴스 생성
            GameObject product = Instantiate(productPrefab, transform.position, Quaternion.identity, productsParent);

            PopupBtn btn = product.transform.Find("Button").GetComponent<PopupBtn>();
            //버튼 누르면 종료될 팝업창 오브젝트
            btn.popupMenu = gameObject;

            // 아이템,마법 각각 프레임 찾아놓기
            Image frame = product.transform.Find("Frame").GetComponent<Image>();
            Transform gemCircle = frame.transform.Find("GemCircle");

            //신규 여부
            bool isNew = false;
            //상품 이미지
            Sprite productSprite = null;
            //등급 색깔
            Color gradeColor = Color.white;
            //상품 가격
            string price = "";

            //아이템일때 정보 넣기
            if (type == ProductType.Item)
            {                
                //생성된 랜덤 아이템 리스트에서 가져와서 쓰고 삭제
                ItemInfo item = sortedItems[0];
                sortedItems.RemoveAt(0);

                //신규 여부
                isNew = item.hasNum > 0 ? false : true;

                //아이템 아이콘 찾기
                productSprite = ItemDB.Instance.itemIcon.Find(
                    x => x.name == item.itemName.Replace(" ", "") + "_Icon");

                //아이템 등급 프레임 및 색깔
                frame.sprite = itemFrame;
                frame.color = MagicDB.Instance.gradeColor[item.grade - 1];
                //마법 원소 UI 끄기
                gemCircle.gameObject.SetActive(false);

                // 지불 수단 넣기, 어떤 원소젬인지
                int index = -1;
                index = System.Array.FindIndex(MagicDB.Instance.elementNames, x => x == item.priceType); //지불 원소젬 이름을 인덱스로 치환
                // print(item.priceType + " : " + index);

                if (index != -1)
                {
                    Color color = MagicDB.Instance.elementColor[index];
                    Image gem = product.transform.Find("Button/Gem").GetComponent<Image>();
                    gem.color = color;
                }

                // 가격 넣기
                price = item.price.ToString();

                // 해당 상품 타입 및 ID 넣기
                btn.btnType = PopupBtn.BtnType.itemBtn;
                btn.id = item.id;
            }
            // 마법일때 정보 넣기
            else if (type == ProductType.Magic)
            {
                //생성된 랜덤 마법 리스트에서 가져와서 쓰고 삭제
                MagicInfo magic = sortedMagics[0];
                sortedMagics.RemoveAt(0);

                //신규 여부
                isNew = magic.magicLevel > 0 ? false : true;

                // 마법 아이콘 찾기
                productSprite = MagicDB.Instance.magicIcon.Find(
                    x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

                // 마법 등급 프레임 및 색깔
                frame.sprite = magicFrame;
                frame.color = MagicDB.Instance.gradeColor[magic.grade - 1];
                //마법 원소 UI 켜기
                gemCircle.gameObject.SetActive(true);

                // 해당 마법의 원소 배열 
                List<string> elements = new List<string>();
                elements.Clear();
                MagicDB.Instance.ElementalSorting(elements, magic.element_A);
                MagicDB.Instance.ElementalSorting(elements, magic.element_B);
                // 배열 가나다순으로 정렬
                elements.Sort();

                // 마법 해당되는 원소 넣기
                Transform[] elementIcons = gemCircle.GetComponentsInChildren<Transform>();

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

                // 지불 수단 넣기, 어떤 원소젬인지
                int index = -1;
                index = System.Array.FindIndex(MagicDB.Instance.elementNames, x => x == magic.priceType); //지불 원소젬 이름을 인덱스로 치환
                // print(magic.priceType + " : " + index);

                if (index != -1)
                {
                    Color color = MagicDB.Instance.elementColor[index];
                    Image gem = product.transform.Find("Button/Gem").GetComponent<Image>();
                    gem.color = color;
                }

                // 가격 넣기
                price = magic.price.ToString();

                // 해당 상품 타입 및 ID 넣기
                btn.btnType = PopupBtn.BtnType.magicBtn;
                btn.id = magic.id;
            }

            // 신규 여부 표시
            Transform newTxt = frame.transform.Find("New");
            if (!isNew)
            {
                //New 아이템 아님
                newTxt.gameObject.SetActive(false);
            }
            else
            {
                //New 아이템
                newTxt.gameObject.SetActive(true);
            }

            // 아이콘 넣기
            product.GetComponent<Image>().sprite = productSprite;

            //TODO 고정된 가격에서 +- 범위내 랜덤 조정해서 넣기
            Text priceTxt = product.transform.Find("Button/Price").GetComponent<Text>();
            priceTxt.text = price;

            //TODO 툴팁에 등급,이름,설명,버프 스탯 정보 넣기
        }
    }

}
