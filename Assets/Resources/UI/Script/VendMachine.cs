using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;
using DG.Tweening;
using System.Linq;
using UnityEngine.EventSystems;
using TMPro;

public class VendMachine : MonoBehaviour
{
    enum ProductType { Item, Magic };
    private List<ItemInfo> items = new List<ItemInfo>(); //자판기에 들어갈 아이템 목록
    private List<MagicInfo> magics = new List<MagicInfo>(); //자판기에 들어갈 마법 목록
    Sequence outputSeq;

    [Header("Refer")]
    [SerializeField]
    private Sprite itemBack; //아이템 배경 스프라이트
    [SerializeField]
    private Sprite magicBack; //마법 배경 스프라이트
    [SerializeField]
    private Sprite itemFrame; //아이템 프레임 스프라이트
    [SerializeField]
    private Sprite magicFrame; //마법 프레임 스프라이트
    [SerializeField]
    private Transform productsParent; //상품 부모 오브젝트
    [SerializeField]
    private Transform productOutput; //상품 토출구 오브젝트
    [SerializeField]
    private GameObject productPrefab; //상품 소개 프리팹
    [SerializeField]
    private GameObject productObj; // 토출구에서 나올 상품 프리팹
    // private float rate = 0.5f; //아티팩트가 아닌 마법이 나올 확률
    [SerializeField]
    private TextMeshProUGUI productPriceType; // 현재 선택 상품 지불 수단 텍스트
    [SerializeField]
    private TextMeshProUGUI productPrice; // 현재 선택 상품 가격 텍스트
    [SerializeField]
    private Transform outputHole; // 아이템 나올 토출구

    private void OnEnable()
    {
        //시간 멈추기
        Time.timeScale = 0f;

        GetComponent<Animator>().enabled = true;

        // 자판기 상품 불러오기
        StartCoroutine(LoadProducts());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //ESC 누르면 자판기 종료
            Exit();
        }
    }

    IEnumerator LoadProducts()
    {
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //상품 모두 지우기
        UIManager.Instance.DestroyChildren(productsParent);

        // 상품9개 = 마법 최대 6개, 최대체력, 최대마나, 랜덤박스
        List<ProductType> productTypes = new List<ProductType>();
        int itemNum = Random.Range(0, 5); //아이템 개수
        int magicNum = 9 - itemNum; //마법 개수
        print(itemNum + " : " + magicNum);

        //마법 6개 추가
        for (int i = 0; i < magicNum; i++)
        {
            productTypes.Add(ProductType.Magic);
        }

        //아이템 3개 추가
        for (int i = 0; i < itemNum; i++)
        {
            productTypes.Add(ProductType.Item);
        }

        // 중복제거된 랜덤 마법 id 뽑기
        int[] magicIDs = MagicDB.Instance.RandomMagicIndex(magicNum);
        magics.Clear();
        for (int i = 0; i < magicIDs.Length; i++)
        {
            MagicInfo magic = MagicDB.Instance.GetMagicByID(magicIDs[i]);
            magics.Add(magic);
        }
        //높은 등급부터 내림차순
        List<MagicInfo> sortedMagics = magics.OrderByDescending(x => x.grade).ToList();

        // 중복제거된 랜덤 아이템 id 뽑기
        int[] itemIDs = ItemDB.Instance.RandomItemIndex(itemNum);
        //아이템 리스트에 등장할 아이템 추가
        items.Clear();
        for (int i = 0; i < itemIDs.Length; i++)
        {
            ItemInfo item = ItemDB.Instance.GetItemByID(itemIDs[i]);
            items.Add(item);
        }

        foreach (var type in productTypes)
        {
            // 상품 인스턴스 생성
            GameObject product = LeanPool.Spawn(productPrefab, transform.position, Quaternion.identity, productsParent);

            //품절 표시 비활성화
            Transform soldoutCover = product.transform.Find("Cover");
            Transform soldoutSlash = product.transform.Find("Button/Slash");
            soldoutCover.gameObject.SetActive(false);
            soldoutSlash.gameObject.SetActive(false);

            //상품 배경
            Image background = product.transform.GetComponent<Image>();
            //타입따라 다른 배경 스프라이트
            background.sprite = type == ProductType.Magic ? magicBack : itemBack;
            //품절 표시 커버 스프라이트 변경
            soldoutCover.GetComponent<Image>().sprite = type == ProductType.Magic ? magicBack : itemBack;

            // 상품 버튼 속성
            InfoHolder infoHolder = product.transform.GetComponent<InfoHolder>();
            //버튼 누르면 종료될 팝업창 오브젝트
            infoHolder.popupMenu = UIManager.Instance.vendMachinePanel;

            // 아이템,마법 각각 프레임
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

            // 마법일때 정보 넣기
            if (type == ProductType.Magic)
            {
                // 0번째 마법 복사 후 삭제 (다음 아이템으로 넘기기위함)
                magic = sortedMagics[0];
                sortedMagics.RemoveAt(0);

                //신규 여부
                isNew = magic.MagicLevel > 0 ? false : true;

                // 마법 아이콘 찾기
                productSprite = MagicDB.Instance.GetMagicIcon(magic.id);

                // 마법 등급 프레임 및 색깔
                frame.sprite = magicFrame;
                gradeColor = MagicDB.Instance.gradeColor[magic.grade];
                frame.color = gradeColor;

                //마법 포함 원소 UI 켜기
                // gemCircle.gameObject.SetActive(true);

                // // 해당 마법의 원소 배열 
                // List<string> elements = new List<string>();
                // elements.Clear();
                // MagicDB.Instance.ElementalSorting(elements, magic.element_A);
                // MagicDB.Instance.ElementalSorting(elements, magic.element_B);
                // // 배열 가나다순으로 정렬
                // elements.Sort();

                // // 마법 해당되는 원소 넣기
                // Transform[] elementIcons = gemCircle.GetComponentsInChildren<Transform>();

                // for (int i = 1; i < elementIcons.Length; i++)
                // {
                //     if (elements.Exists(x => x == elementIcons[i].name))
                //     {
                //         elementIcons[i].gameObject.SetActive(true);
                //     }
                //     else
                //     {
                //         elementIcons[i].gameObject.SetActive(false);
                //     }
                // }

                //가격 정보 넣기
                price = SetPrice(product, infoHolder, magic, item);

                // 해당 상품 타입 및 ID 넣기
                infoHolder.holderType = InfoHolder.HolderType.magicHolder;
                infoHolder.id = magic.id;
            }

            //아이템일때 정보 넣기
            if (type == ProductType.Item)
            {
                // 0번째 아이템 복사 후 삭제 (다음 아이템으로 넘기기위함)
                item = items[0];
                items.RemoveAt(0);

                //신규 여부
                isNew = item.amount > 0 ? false : true;

                //아이템 아이콘 찾기
                productSprite = ItemDB.Instance.itemIcon.Find(
                    x => x.name == item.itemName.Replace(" ", "") + "_Icon");

                //아이템 등급 프레임 및 색깔
                frame.sprite = itemFrame;
                gradeColor = MagicDB.Instance.gradeColor[item.grade];
                frame.color = gradeColor;

                //마법 원소 UI 끄기
                gemCircle.gameObject.SetActive(false);

                //가격 정보 넣기
                price = SetPrice(product, infoHolder, magic, item);

                // 해당 상품 타입 및 ID 넣기
                infoHolder.holderType = InfoHolder.HolderType.itemHolder;
                infoHolder.id = item.id;
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
            Transform Icon = product.transform.Find("Icon");
            //스프라이트 못찾으면 아이콘 오브젝트 비활성화
            if (productSprite == null)
                Icon.gameObject.SetActive(false);
            else
            {
                Icon.GetComponent<Image>().sprite = productSprite;
                Icon.gameObject.SetActive(true);
            }

            // 화폐 종류 다시 불러오기
            gemTypeIndex = infoHolder.gemType;

            //버튼 찾기
            Button button = product.transform.GetComponent<Button>();

            // 선택 했을때
            EventTrigger trigger = button.GetComponent<EventTrigger>(); //이벤트 트리거 참조
            EventTrigger.Entry entry = new EventTrigger.Entry(); //이벤트 트리거에 넣을 엔트리 생성            
            entry.eventID = EventTriggerType.Select; //Select 했을때로 지정
            entry.callback.AddListener((data) => { ProductSelect(product, gemTypeIndex, magic, item); }); //Select 했을때 넣을 함수 넣기
            trigger.triggers.Add(entry); //만들어진 엔트리를 이벤트 트리거에 넣어주기

            // 클릭 했을때
            button.onClick.AddListener(delegate
            {
                // 상품 획득 시도하기
                GetProduct(product, gemTypeIndex, infoHolder, price, magic, item);
            });

            // 툴팁에 상품 정보 넣기
            ToolTipTrigger tooltip = product.GetComponent<ToolTipTrigger>();
            tooltip.toolTipType = ToolTipTrigger.ToolTipType.ProductTip;
            tooltip.Magic = magic;
            tooltip.Item = item;
        }
    }

    int SetPrice(GameObject product, InfoHolder infoHolder, MagicInfo magic = null, ItemInfo item = null)
    {
        // 화폐 종류 넣기, 어떤 원소젬인지
        string priceType = magic != null ? magic.priceType : item.priceType;
        int gemTypeIndex = System.Array.FindIndex(MagicDB.Instance.elementNames, x => x == priceType); //지불 원소젬 이름을 인덱스로 치환

        //지불 수단 타입이 없을때
        if (gemTypeIndex == -1)
        {
            // 0~5 사이 랜덤
            gemTypeIndex = Random.Range(0, 6);
        }

        //상품의 화폐 종류 저장
        infoHolder.gemType = gemTypeIndex;

        // 화폐에 따라 색 바꾸기
        Color color = MagicDB.Instance.elementColor[gemTypeIndex];
        Image gem = product.transform.Find("Button/Gem").GetComponent<Image>();
        gem.color = color;

        // 고정된 가격에서 +- 범위내 랜덤 조정해서 가격 넣기, 10 단위로 반올림
        float originPrice = magic != null ? magic.price : item.price;
        float price = Mathf.Round(originPrice * Random.Range(0.51f, 1.5f) / 10f) * 10f;

        //아이템 가격 텍스트
        TextMeshProUGUI priceTxt = product.transform.Find("Button/Price").GetComponent<TextMeshProUGUI>();
        priceTxt.text = price.ToString();
        // 구매 가능하면 초록, 아니면 빨강
        priceTxt.color = PlayerManager.Instance.hasGems[gemTypeIndex] >= price ? Color.green : Color.red;

        // 할인 표시 오브젝트 찾기
        Transform discount = product.transform.Find("Discount");
        Transform discountRaise = discount.Find("Raise");
        Transform discountAmount = discount.Find("Amount");

        //가격 같을때
        if (price == originPrice)
        {
            //할인 스티커 비활성화
            discount.gameObject.SetActive(false);
        }
        else
        {
            //할인 스티커 활성화
            discount.gameObject.SetActive(true);

            //가격 업다운 여부 화살표
            RectTransform raiseRect = discountRaise.GetComponent<RectTransform>();
            Image raiseImg = discountRaise.GetComponent<Image>();

            //할인율 텍스트
            TextMeshProUGUI amountTxt = discountAmount.GetComponent<TextMeshProUGUI>();

            //할인일때
            if (price < originPrice)
            {
                //화살표 아래로
                raiseRect.rotation = Quaternion.Euler(Vector3.back * 90f);

                //화살표 파란색
                raiseImg.color = Color.blue;

                //가격 텍스트 파란색
                amountTxt.color = Color.blue;
            }
            else
            {
                //화살표 위로
                raiseRect.rotation = Quaternion.Euler(Vector3.forward * 90f);

                //화살표 빨간색
                raiseImg.color = Color.red;

                //가격 텍스트 빨간색
                amountTxt.color = Color.red;
            }

            //할인율 반영
            string amount = (Mathf.RoundToInt((price / originPrice) * 100f) - 100f).ToString() + "%";
            amountTxt.text = amount;
            // print(price + " / " + originPrice + " = " + amount);
        }

        return (int)price;
    }

    //애니메이션 끝나면 첫번째 상품 선택하기
    public void SelectFirst()
    {
        // 첫번째 아이템 Select 하기
        Selectable productBtn = productsParent.GetComponentInChildren<Selectable>();
        productBtn.Select();

        GetComponent<Animator>().enabled = false;
    }

    //상품 선택했을때
    public void ProductSelect(GameObject product, int gemTypeIndex, MagicInfo magic = null, ItemInfo item = null)
    {
        //해당 버튼의 위치에서 오른쪽 아래 모서리로 툴팁 위치하기
        Vector2 pos = (Vector2)product.transform.position + new Vector2(product.GetComponent<RectTransform>().sizeDelta.x, -product.GetComponent<RectTransform>().sizeDelta.y);

        // 해당 상품의 화폐과 같은 원소젬 개수 보여주기
        productPriceType.text = (MagicDB.Instance.elementNames[gemTypeIndex] + " Gem").ToString(); // 화폐 원소젬 이름
        productPrice.text = PlayerManager.Instance.hasGems[gemTypeIndex].ToString(); //지불할 가격

        // 상품 모서리에 툴팁 띄우고 고정
        ProductToolTip.Instance.OpenTooltip(magic, item, ProductToolTip.ToolTipCorner.LeftUp, pos);
    }

    //상품 획득
    void GetProduct(GameObject product, int gemTypeIndex, InfoHolder infoHolder, int price,
    MagicInfo magic = null, ItemInfo item = null)
    {
        // 지불 수단 넣기, 어떤 원소젬인지
        // string priceType = magic != null ? magic.priceType : item.priceType;
        // int gemTypeIndex = System.Array.FindIndex(MagicDB.Instance.elementNames, x => x == priceType); //지불 원소젬 이름을 인덱스로 치환
        Image frame = product.transform.Find("Frame").GetComponent<Image>();
        Button button = product.transform.GetComponent<Button>();

        // print(product.name + PlayerManager.Instance.GemAmount(gemTypeIndex) +" : "+ price);

        //구매 가능
        if (PlayerManager.Instance.hasGems[gemTypeIndex] >= price)
        {
            // 선택한 버튼의 상품 획득하기
            infoHolder.ChooseBtn();

            // 가격 지불하기
            PlayerManager.Instance.PayGem(gemTypeIndex, price);

            // 모든 상품 정보 찾기
            InfoHolder[] allInfo = productsParent.GetComponentsInChildren<InfoHolder>();
            for (int i = 0; i < productsParent.childCount; i++)
            {
                //해당 화폐 플레이어 소지금
                int _hasGem = PlayerManager.Instance.hasGems[allInfo[i].gemType];
                //해당 상품 가격
                int _price = allInfo[i].holderType == InfoHolder.HolderType.magicHolder
                ? MagicDB.Instance.GetMagicByID(allInfo[i].id).price
                : ItemDB.Instance.GetItemByID(allInfo[i].id).price;

                // 모든 상품 가격 색깔로 구매 가능여부 갱신
                allInfo[i].transform.Find("Button/Price").GetComponent<TextMeshProUGUI>().color
                = _hasGem >= _price ? Color.green : Color.red;

                // if (allInfo[i].holderType == InfoHolder.HolderType.magicHolder)
                //     print(MagicDB.Instance.GetMagicByID(allInfo[i].id).magicName + ":" + _hasGem + ":" + _price);
                // else
                //     print(ItemDB.Instance.GetItemByID(allInfo[i].id).itemName + ":" + _hasGem + ":" + _price);
            }

            // 자판기 떨림
            transform.GetChild(0).DOShakePosition(0.5f, 10f)
            .SetUpdate(true);

            //툴팁 끄기
            ProductToolTip.Instance.QuitTooltip();

            //신규 표시
            Transform newTxt = frame.transform.Find("New");
            //품절 표시
            Transform soldoutCover = product.transform.Find("Cover");
            Transform soldoutSlash = product.transform.Find("Button/Slash");

            // 해당 상품 비활성화
            button.interactable = false; // 상호작용 비활성화
            newTxt.gameObject.SetActive(false); //신규 표시 없에기
            soldoutCover.gameObject.SetActive(true); //아이콘 어둡게
            soldoutSlash.gameObject.SetActive(true); //가격 표시 사선 표시

            // 토출구에서 상품 떨어뜨리기
            GameObject outputObj = LeanPool.Spawn(productObj, product.transform.position, Quaternion.identity, productOutput);

            //배경 스프라이트 바꾸기
            Image outputBack = outputObj.transform.GetComponent<Image>();
            if (magic != null)
                outputBack.sprite = magicBack; //마법이면 마법 배경
            else
                outputBack.sprite = itemBack; //아이템이면 아이템 배경

            //아이콘 스프라이트 교체
            Image outputIcon = outputObj.transform.Find("Icon").GetComponent<Image>();
            outputIcon.sprite = product.transform.Find("Icon").GetComponent<Image>().sprite;

            //프레임 색깔 변경
            Image outputFrame = outputObj.transform.Find("Frame").GetComponent<Image>();
            if (magic != null)
                outputFrame.sprite = magicFrame; //마법이면 마법 프레임
            else
                outputFrame.sprite = itemFrame; //아이템이면 아이템 프레임

            outputObj.transform.localScale = Vector2.one;

            outputSeq = DOTween.Sequence();
            outputSeq
            .Append(
                // 토출구 가운데까지 DoMove 하기
                outputObj.transform.DOMove(outputHole.position, 1f)
            )
            .Append(
                // 점점 줄어들어 사라지기
                outputObj.transform.DOScale(Vector2.zero, 1f)
                .SetEase(Ease.InBack)
            )
            .SetUpdate(true)
            .OnComplete(() =>
            {
                LeanPool.Despawn(outputObj);
            });
        }
        //구매 불가능
        else
        {
            Color frameColor = frame.color;

            //프레임 깜빡이기
            FlickerObj(frame.gameObject, frameColor);

            //부족한 젬 타입 숫자 UI 깜빡이기
            FlickerObj(UIManager.Instance.gemUIs[gemTypeIndex].gameObject, Color.white);
        }
    }

    void FlickerObj(GameObject obj, Color originColor)
    {
        // 재화 부족할때 인디케이터 표시
        if (obj.TryGetComponent(out Image img))
        {
            img.DOComplete();

            // 이미지 색깔 깜빡이기
            img.DOColor(Color.red, 0.5f)
            .SetUpdate(true) //timescale 무시
            .SetEase(Ease.Flash, 3, 0)
            .OnComplete(() =>
            {
                img.color = originColor; //색깔 초기화하고 끝
            });
        }
        else if (obj.TryGetComponent(out Text txt))
        {
            txt.DOComplete();

            // 텍스트 색깔 깜빡이기
            txt.DOColor(Color.red, 0.5f)
            .SetUpdate(true) //timescale 무시
            .SetEase(Ease.Flash, 3, 0)
            .OnComplete(() =>
            {
                txt.color = originColor; //색깔 초기화하고 끝
            });
        }
    }

    public void Exit()
    {
        //아이템 받는 시퀀스 강제 완료시키기
        outputSeq.Complete();

        //팝업 메뉴 닫기
        UIManager.Instance.PopupUI(UIManager.Instance.vendMachinePanel);
    }
}
