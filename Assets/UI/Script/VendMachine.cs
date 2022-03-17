using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;
using DG.Tweening;
using System.Linq;
using UnityEngine.EventSystems;

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
        int itemNum = Random.Range(0, 10);
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

            // 상품 버튼 속성
            InfoHolder productBtn = product.transform.Find("Button").GetComponent<InfoHolder>();
            //버튼 누르면 종료될 팝업창 오브젝트
            productBtn.popupMenu = gameObject;

            Button btn = product.transform.Find("Button").GetComponent<Button>();

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
            int price = 0;
            //상품 비용 젬 타입 인덱스
            int gemTypeIndex = -1;

            // 상품에 담길 아이템 or 마법
            ItemInfo item = null;
            MagicInfo magic = null;

            //아이템일때 정보 넣기
            if (type == ProductType.Item)
            {
                //생성된 랜덤 아이템 리스트에서 가져와서 쓰고 삭제
                item = sortedItems[0];
                sortedItems.RemoveAt(0);

                //신규 여부
                isNew = item.hasNum > 0 ? false : true;

                //아이템 아이콘 찾기
                productSprite = ItemDB.Instance.itemIcon.Find(
                    x => x.name == item.itemName.Replace(" ", "") + "_Icon");

                //아이템 등급 프레임 및 색깔
                frame.sprite = itemFrame;
                gradeColor = MagicDB.Instance.gradeColor[item.grade - 1];
                frame.color = gradeColor;

                //마법 원소 UI 끄기
                gemCircle.gameObject.SetActive(false);

                // 지불 수단 넣기, 어떤 원소젬인지
                gemTypeIndex = System.Array.FindIndex(MagicDB.Instance.elementNames, x => x == item.priceType); //지불 원소젬 이름을 인덱스로 치환
                // print(item.priceType + " : " + index);

                if (gemTypeIndex != -1)
                {
                    Color color = MagicDB.Instance.elementColor[gemTypeIndex];
                    Image gem = product.transform.Find("Button/Gem").GetComponent<Image>();
                    gem.color = color;
                }

                // 가격 넣기
                price = item.price;

                // 해당 상품 타입 및 ID 넣기
                productBtn.holderType = InfoHolder.HolderType.itemHolder;
                productBtn.id = item.id;
            }
            // 마법일때 정보 넣기
            else if (type == ProductType.Magic)
            {
                //생성된 랜덤 마법 리스트에서 가져와서 쓰고 삭제
                magic = sortedMagics[0];
                sortedMagics.RemoveAt(0);

                //신규 여부
                isNew = magic.magicLevel > 0 ? false : true;

                // 마법 아이콘 찾기
                productSprite = MagicDB.Instance.magicIcon.Find(
                    x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

                // 마법 등급 프레임 및 색깔
                frame.sprite = magicFrame;
                gradeColor = MagicDB.Instance.gradeColor[magic.grade - 1];
                frame.color = gradeColor;

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

                //지불 원소젬 이름을 인덱스로 반환
                gemTypeIndex = System.Array.FindIndex(MagicDB.Instance.elementNames, x => x == magic.priceType);
                // print(magic.priceType + " : " + index);

                if (gemTypeIndex != -1)
                {
                    Color color = MagicDB.Instance.elementColor[gemTypeIndex];
                    Image gem = product.transform.Find("Button/Gem").GetComponent<Image>();
                    gem.color = color;
                }

                // 가격 넣기
                price = magic.price;

                // 해당 상품 타입 및 ID 넣기
                productBtn.holderType = InfoHolder.HolderType.magicHolder;
                productBtn.id = magic.id;
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

            // 상품 버튼 눌렀을때 이벤트 넣기
            // print(product.name + PlayerManager.Instance.GemAmount(gemTypeIndex) +" : "+ price);
            if (PlayerManager.Instance.GemAmount(gemTypeIndex) >= price)
            {
                // 상품 구매할 돈 있으면 초록
                priceTxt.color = Color.green;

                //버튼 클릭시 이벤트 함수
                btn.onClick.AddListener(delegate
                    {
                        //아이템일때
                        if (type == ProductType.Item)
                        {
                            // 선택한 버튼의 상품 획득하기
                            GetProduct(productBtn, gemTypeIndex, price);
                        }
                        else if (type == ProductType.Magic)
                        {
                            // 새 마법일때
                            if (isNew)
                            {
                                // 선택한 버튼의 상품 획득하기
                                GetProduct(productBtn, gemTypeIndex, price);
                            }
                            // 기존 마법 업그레이드일때
                            else
                            {
                                // 마법 업그레이드 팝업
                                GameObject magicUpgradePopup = UIManager.Instance.magicUpgradeUI;
                                MagicUpgradeMenu magicUp = magicUpgradePopup.GetComponent<MagicUpgradeMenu>();
                                magicUp.magic = magic; //마법 데이터
                                magicUp.magicIcon.sprite = productSprite; //마법 아이콘
                                magicUp.magicFrame.color = gradeColor; //프레임 색상
                                magicUp.magicName.text = magic.magicName; //마법 이름

                                // 팝업 띄우기
                                UIManager.Instance.PopupUI(magicUpgradePopup);
                            }
                        }

                        //툴팁 끄기
                        ProductToolTip.Instance.QuitTooltip();
                    });
            }
            else
            {
                // 상품 구매할 돈 없으면 빨강
                priceTxt.color = Color.red;

                Color frameColor = frame.color;

                //버튼 클릭시 이벤트 함수
                btn.onClick.AddListener(delegate
                    {
                        //프레임 깜빡이기
                        FlickerObj(frame.gameObject, frameColor);

                        //부족한 젬 타입 숫자 UI 깜빡이기
                        FlickerObj(UIManager.Instance.gemUIs[gemTypeIndex].gameObject, Color.white);
                    });
            }

            priceTxt.text = price.ToString();

            // 툴팁에 상품 정보 넣기
            ToolTipTrigger tooltip = product.GetComponent<ToolTipTrigger>();
            tooltip.toolTipType = ToolTipTrigger.ToolTipType.ProductTip;
            tooltip.magic = magic;
            tooltip.item = item;
        }
    }

    void GetProduct(InfoHolder productBtn, int gemTypeIndex, int price)
    {
        // 선택한 버튼의 상품 획득하기
        productBtn.ChooseBtn(false);

        //팝업 메뉴 닫기
        UIManager.Instance.PopupUI(productBtn.popupMenu);

        // 가격 지불하기
        PlayerManager.Instance.PayGem(gemTypeIndex, price);
    }

    void FlickerObj(GameObject obj, Color originColor)
    {
        // 재화 부족할때 인디케이터 표시
        if (obj.TryGetComponent(out Image img))
        {
            // 이미지 색깔 깜빡이기
            img.DOColor(Color.red, 0.5f)
            .SetUpdate(true) //timescale 무시
            .SetEase(Ease.Flash, 3, 0)
            .OnStart(() =>
            {
                img.color = originColor; //색깔 초기화하고 시작
            })
            .OnComplete(() =>
            {
                img.color = originColor; //색깔 초기화하고 끝
            });
        }
        else if (obj.TryGetComponent(out Text txt))
        {
            // 텍스트 색깔 깜빡이기
            txt.DOColor(Color.red, 0.5f)
            .SetUpdate(true) //timescale 무시
            .SetEase(Ease.Flash, 3, 0)
            .OnStart(() =>
            {
                txt.color = originColor; //색깔 초기화하고 시작
            })
            .OnComplete(() =>
            {
                txt.color = originColor; //색깔 초기화하고 끝
            });
        }
    }

}
