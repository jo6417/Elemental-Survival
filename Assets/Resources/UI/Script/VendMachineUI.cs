using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lean.Pool;
using DG.Tweening;
using System.Linq;
using UnityEngine.EventSystems;
using TMPro;

public class VendMachineUI : MonoBehaviour
{
    #region Singleton
    private static VendMachineUI instance;
    public static VendMachineUI Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<VendMachineUI>(true);
                if (obj != null)
                {
                    instance = obj;
                }
                // else
                // {
                //     var newObj = new GameObject().AddComponent<VendMachineUI>();
                //     instance = newObj;
                // }
            }
            return instance;
        }
    }
    #endregion

    [Header("State")]
    Sequence outputSeq;
    public List<SlotInfo> productList = null; // 판매 상품 정보 리스트
    public float[] discountList = new float[9]; // 각 상품들의 할인율 배열
    public bool[] soldOutList = new bool[9]; // 상품 판매 여부 배열
    [SerializeField, ReadOnly] bool fieldDrop; // 아이템 필드드랍 할지 여부

    [Header("Refer")]
    [SerializeField] private RectTransform vendMachineObj;
    [SerializeField] private Transform productsParent; // 상품들 부모 오브젝트
    [SerializeField] public Transform itemDropper; //상품 토출구 오브젝트
    [SerializeField] private GameObject productPrefab; //상품 소개 프리팹
    [SerializeField] private GameObject outputObj; // 토출구에서 나올 상품 프리팹
    // private float rate = 0.5f; //아티팩트가 아닌 마법이 나올 확률
    [SerializeField] private TextMeshProUGUI productPriceType; // 현재 선택 상품 지불 수단 텍스트
    [SerializeField] private TextMeshProUGUI productPrice; // 현재 선택 상품 가격 텍스트
    [SerializeField] private Transform outputHole; // 아이템 나올 토출구
    [SerializeField] Button exitBtn; // 종료 버튼
    NewInput input;

    private void Awake()
    {
        input = new NewInput();
        // // 취소 입력
        // input.UI.Cancel.performed += val =>
        // {
        //     // 종료버튼 상호작용 켜져있으면
        //     if (exitBtn.interactable)
        //         // 자판기 종료
        //         StartCoroutine(ExitTransition());
        // };

        // 닫기 버튼 입력
        input.UI.PhoneMenu.performed += val =>
        {
            // 자판기 패널 켜져있을때
            if (gameObject.activeSelf)
                // 종료버튼 상호작용 켜져있으면
                if (exitBtn.interactable)
                    // 자판기 종료
                    StartCoroutine(ExitTransition());
        };

        input.Enable();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 필드드랍 여부 초기화
        fieldDrop = false;

        // 화면 위로 이동
        vendMachineObj.anchoredPosition = new Vector2(0, 1100f);

        // 자판기 상품 불러오기
        StartCoroutine(SetProducts());

        // 원래 위치로 떨어지기
        vendMachineObj.DOAnchorPos(new Vector2(0, 20f), 0.5f)
        .SetEase(Ease.OutBounce)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            // 종료 버튼 상호작용 켜기
            exitBtn.interactable = true;
        });

        yield return new WaitForSecondsRealtime(0.1f);
        // 쾅하고 떨어지는 소리 재생
        SoundManager.Instance.PlaySound("Vend_Fall", 0, 0, 1, false);
    }

    IEnumerator SetProducts()
    {
        yield return new WaitUntil(() => MagicDB.Instance.initDone);

        //상품 모두 지우기
        // SystemManager.Instance.DestroyAllChild(productsParent);

        for (int i = 0; i < productList.Count; i++)
        {
            // 인덱스 인스턴싱
            int index = i;
            // 상품 오브젝트 참조
            Transform productObj = productsParent.GetChild(i);

            // 아이콘 버튼 찾기
            Button productButton = productObj.GetComponent<Button>();
            // 가격 버튼
            Transform priceButton = productObj.Find("Price");
            // 품절 표시 빨간줄
            Transform soldOutSlash = priceButton.transform.Find("Slash");
            soldOutSlash.gameObject.SetActive(false);
            // 아이템,마법 각각 프레임
            Image frame = productObj.Find("Frame").GetComponent<Image>();
            // 신규 표시
            Transform newTxt = productObj.Find("New");

            // 상품 정보 캐싱
            SlotInfo slotInfo = productList[i];

            //신규 여부
            bool isNew = false;
            //상품 이미지
            Sprite productSprite = null;
            //등급 색깔
            Color gradeColor = Color.white;
            //상품 가격
            int price = 0;

            // 상품에 담길 아이템 or 마법
            ItemInfo item = slotInfo as ItemInfo;
            MagicInfo magic = slotInfo as MagicInfo;

            // 가격 타입 인덱스, 정보 없으면 랜덤으로 부여
            int priceType = 0;
            if (productList[i].priceType != "None")
                // 가격 타입 있으면 해당 타입
                priceType = MagicDB.Instance.ElementType(slotInfo);
            else
            {
                // 정해진 타입 없으면 랜덤 타입 원소젬
                priceType = Random.Range(0, 6);

                // 해당 가격 타입을 아이템 타입에 저장
                productList[i].priceType = MagicDB.Instance.ElementNames[priceType];
            }

            // 마법일때 정보 넣기
            if (magic != null)
            {
                //todo 신규 여부 (인벤토리에 없을때)
                // isNew = magic.magicLevel > 0 ? false : true;

                // 마법 아이콘 찾기
                productSprite = MagicDB.Instance.GetIcon(magic.id);
            }
            //아이템일때 정보 넣기
            if (item != null)
            {
                //todo 신규 여부 (인벤토리에 없을때)
                // isNew = item.amount > 0 ? false : true;

                //아이템 아이콘 찾기
                productSprite = ItemDB.Instance.GetIcon(item.id);
            }

            // 마법 등급 프레임 및 색깔
            gradeColor = MagicDB.Instance.GradeColor[slotInfo.grade];
            frame.color = gradeColor;

            // 기존 가격에서 랜덤 할인된 가격
            price = SetPrice(index, productObj, priceType);

            // 신규 여부 표시
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
            Transform Icon = productObj.transform.Find("Icon");
            //스프라이트 못찾으면 임시로 물음표 넣기
            Icon.GetComponent<Image>().sprite = productSprite != null ? productSprite : SystemManager.Instance.questionMark;

            //todo 자판기 초기화 후에도 제대로 작동 되는지 검사할것
            // 버튼 마우스 Enter시 이벤트 작성
            EventTrigger.Entry selectEntry = new EventTrigger.Entry(); //이벤트 트리거에 넣을 엔트리 생성
            selectEntry.eventID = EventTriggerType.PointerEnter; //Select 했을때로 지정
            selectEntry.callback.AddListener((data) => { ProductSelect(priceType, GetPrice(index)); }); //Select 했을때 넣을 함수 넣기

            // 아이콘 버튼 선택 했을때
            productButton.GetComponent<EventTrigger>().triggers.Add(selectEntry);
            // 가격 버튼 선택 했을때
            priceButton.GetComponent<EventTrigger>().triggers.Add(selectEntry);

            // 아이콘 버튼 클릭 이벤트 비우기
            productButton.onClick.RemoveAllListeners();
            // 아이콘 버튼 클릭 이벤트 넣기
            productButton.onClick.AddListener(delegate
            {
                // 상품 획득 시도하기
                StartCoroutine(GetProduct(index, productObj, priceType, index));
            });

            // 툴팁에 상품 정보 넣기
            ToolTipTrigger tooltip = productButton.GetComponent<ToolTipTrigger>();
            tooltip.toolTipType = ToolTipTrigger.ToolTipType.ProductTip;
            if (magic != null)
                tooltip._slotInfo = magic;
            if (item != null)
                tooltip._slotInfo = item;

            // 해당 상품 품절일때
            if (soldOutList[i])
            {
                // 해당 상품 비활성화
                // productButton.interactable = false; // 아이콘 버튼 상호작용 비활성화
                newTxt.gameObject.SetActive(false); //신규 표시 없에기
                soldOutSlash.gameObject.SetActive(true); //가격 표시 사선 표시
            }
            else
            {
                // 해당 상품 활성화
                // productButton.interactable = true; // 아이콘 버튼 상호작용 비활성화
                newTxt.gameObject.SetActive(true); //신규 표시 없에기
                soldOutSlash.gameObject.SetActive(false); //가격 표시 사선 표시
            }
        }
    }

    void UpdatePrice()
    {
        // 모든 상품 가격 색깔 바꿔서 구매 가능 여부 갱신
        for (int i = 0; i < 9; i++)
        {
            // 가격 텍스트
            TextMeshProUGUI amount = productsParent.GetChild(i).Find("Price/Amount").GetComponent<TextMeshProUGUI>();
            // 해당 상품 정보
            SlotInfo slotInfo = productList[i];
            // 가격 타입
            int priceType = MagicDB.Instance.ElementType(slotInfo);

            // 가격 텍스트 기본 색은 빨강
            Color amountColor = Color.red;
            // 구매 가능한 경우 초록색으로 변경
            if (PlayerManager.Instance.hasGem[priceType].amount >= GetPrice(i) && !soldOutList[i])
                amountColor = Color.green;

            // 가격 텍스트 색 넣기
            amount.color = amountColor;
        }
    }

    int SetPrice(int index, Transform product, int priceType)
    {
        // 화폐에 따라 색 바꾸기
        Color color = MagicDB.Instance.GetElementColor(priceType);
        Image gem = product.Find("Price/Gem").GetComponent<Image>();
        gem.color = color;

        // 원래 가격
        float originPrice = productList[index].price;
        // 할인율 불러오기
        float discountRate = discountList[index];
        // 상품의 최종 가격
        float price = originPrice;

        // 할인율 계산된 가격
        price = Mathf.Round(originPrice * (1f - discountRate) / 10f) * 10f;

        //아이템 가격 텍스트
        TextMeshProUGUI priceTxt = product.Find("Price/Amount").GetComponent<TextMeshProUGUI>();
        priceTxt.text = price.ToString();

        // 가격 텍스트 기본 색은 빨강
        Color amountColor = Color.red;
        // 구매 가능한 경우 초록색으로 변경
        if (PlayerManager.Instance.hasGem[priceType].amount >= GetPrice(index) && !soldOutList[index])
            amountColor = Color.green;
        // 가격 텍스트 색 넣기
        priceTxt.color = amountColor;

        // 할인 표시 오브젝트 찾기
        Transform discount = product.Find("Discount");
        Transform discountRaise = discount.Find("Raise");
        Transform discountAmount = discount.Find("Amount");

        // 가격이 그대로일때
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
            TextMeshProUGUI discountText = discountAmount.GetComponent<TextMeshProUGUI>();

            //할인일때
            if (price < originPrice)
            {
                //화살표 아래로
                raiseRect.rotation = Quaternion.Euler(Vector3.back * 90f);

                //화살표 파란색
                raiseImg.color = Color.blue;

                //가격 텍스트 파란색
                discountText.color = Color.blue;
            }
            else
            {
                //화살표 위로
                raiseRect.rotation = Quaternion.Euler(Vector3.forward * 90f);

                //화살표 빨간색
                raiseImg.color = Color.red;

                //가격 텍스트 빨간색
                discountText.color = Color.red;
            }

            //할인율 반영
            string amount = (-discountRate * 100f).ToString() + "%";
            discountText.text = amount;
            // print(price + " / " + originPrice + " = " + amount);
        }

        return (int)price;
    }

    int GetPrice(int index)
    {
        // 할인율 계산된 가격
        float price = Mathf.Round(productList[index].price * (1f - discountList[index]) / 10f) * 10f;

        return (int)price;
    }

    //애니메이션 끝나면 첫번째 상품 선택하기
    public void SelectFirst()
    {
        // 첫번째 아이템 Select 하기
        Selectable productBtn = productsParent.GetComponentInChildren<Selectable>();
        UICursor.Instance.UpdateLastSelect(productBtn);

        GetComponent<Animator>().enabled = false;
    }

    // 상품 선택했을때
    public void ProductSelect(int priceType, int priceAmount)
    {
        // 해당 상품의 화폐과 같은 원소젬 개수 보여주기
        productPriceType.text = (MagicDB.Instance.ElementNames[priceType] + " Gem").ToString(); // 화폐 원소젬 이름
        productPrice.text = priceAmount.ToString(); //지불할 가격
    }

    //상품 획득
    IEnumerator GetProduct(int index, Transform productObj, int priceType, int productIndex)
    {
        yield return null;

        // 상품 정보 캐싱
        SlotInfo slotInfo = productList[productIndex];

        Image frame = productObj.Find("Frame").GetComponent<Image>();
        // 아이콘 버튼
        Button iconBtn = productObj.GetComponent<Button>();
        // 가격 버튼
        Transform priceBtn = productObj.Find("Price");
        // 품절 표시
        Transform soldOutSlash = productObj.Find("Price/Slash");
        // 신규 표시
        Transform newTxt = productObj.Find("New");
        // 깜빡이기 인디케이터
        Image indicator = productObj.Find("Indicator").GetComponent<Image>();

        // print(product.name + PlayerManager.Instance.GemAmount(gemTypeIndex) +" : "+ price);

        // 품절 아닐때, 충분한 화폐가 있을때
        if (!soldOutList[productIndex] && PlayerManager.Instance.hasGem[priceType].amount >= GetPrice(index))
        {
            // 해당 상품 품절 처리
            soldOutList[productIndex] = true;

            // 현재 툴팁 끄기
            ProductToolTip.Instance.QuitTooltip();

            // iconBtn.interactable = false; // 아이콘 버튼 상호작용 비활성화
            newTxt.gameObject.SetActive(false); //신규 표시 없에기
            soldOutSlash.gameObject.SetActive(true); //가격 표시 사선 표시

            // 토출구에 드랍될 상품 위치,크기 초기화
            outputObj.transform.position = productsParent.position;
            outputObj.transform.localScale = Vector2.one;
            // 상품 오브젝트 켜기
            outputObj.SetActive(true);

            // 가격 지불하기
            PlayerManager.Instance.PayGem(priceType, GetPrice(index));

            // 돈 나가는 소리 재생
            // SoundManager.Instance.PlaySound("Vend_Pay", 0, 0, 1, false);
            // yield return new WaitForSecondsRealtime(0.1f);

            // 자판기 상품 투하 소리 재생
            SoundManager.Instance.PlaySound("Vend_Purchase", 0, 0, 1, false);

            // 모든 상품 가격 색깔 업데이트하여 구매가능 여부 표시
            UpdatePrice();

            //아이콘 스프라이트 교체
            Image outputIcon = outputObj.transform.Find("Icon").GetComponent<Image>();
            outputIcon.sprite = productObj.transform.Find("Icon").GetComponent<Image>().sprite;

            //프레임 색깔 변경
            Image outputFrame = outputObj.transform.Find("Frame").GetComponent<Image>();
            outputFrame.color = productObj.transform.Find("Frame").GetComponent<Image>().color;

            // yield return new WaitForSecondsRealtime(0.2f);

            // 기존 시퀀스 있으면 완료시키기
            if (outputSeq != null)
                outputSeq.Kill();
            // 자판기 이미지 원래 위치 저장
            Vector3 vendOriginPos = vendMachineObj.position;

            outputSeq = DOTween.Sequence();
            outputSeq
            .SetUpdate(true)
            .AppendCallback(() =>
            {
                // 상품 착지 소리 재생, 덜컹덜컹
                SoundManager.Instance.PlaySound("Vend_Product_Land_0", 0, 0.3f, 1, false);
                SoundManager.Instance.PlaySound("Vend_Product_Land_1", 0, 0.7f, 1, false);
                SoundManager.Instance.PlaySound("Vend_Product_Land_2", 0, 0.9f, 1, false);
            })
            .Join(
                // 토출구 가운데까지 DoMove 하기
                outputObj.transform.DOMove(outputHole.position, 1f)
                .SetEase(Ease.OutBounce)
            )
            .Join(
                // 자판기 떨림
                vendMachineObj.DOShakePosition(0.7f, 5f)
                .SetDelay(0.3f)
                .OnComplete(() =>
                {
                    vendMachineObj.position = vendOriginPos;
                })
                .OnKill(() =>
                {
                    vendMachineObj.position = vendOriginPos;
                })
            )
            .AppendCallback(() =>
            {
                // 상품 획득 소리 재생
                SoundManager.Instance.PlaySound("Vend_Product_Get", 0, 0, 1, false);
            })
            .Join(
                // 점점 줄어들어 사라지기
                outputObj.transform.DOScale(Vector2.zero, 0.5f)
                .SetEase(Ease.InBack)
            )
            .OnComplete(() =>
            {
                // 상품 오브젝트 끄기
                outputObj.SetActive(false);
            });

            // 필드드랍 1개 이상 있으면 켜기
            fieldDrop = true;

            // 아이템 드롭
            ItemDB.Instance.ItemDrop(slotInfo, itemDropper.position);
        }
        // 구매 불가능
        else
        {
            FlickerObj(indicator, Color.clear);

            //부족한 젬 타입 숫자 UI 깜빡이기
            UIManager.Instance.GemIndicator(priceType, Color.red);
        }
    }

    void FlickerObj(Image image, Color originColor)
    {
        // 거부 사운드 재생
        SoundManager.Instance.PlaySound("Denied");

        image.DOKill();
        image.color = originColor; //색깔 초기화

        // 해당 슬롯 빨갛게 blinkNum 만큼 깜빡이기
        image.DOColor(new Color(1, 0, 0, 0.5f), 0.1f)
        .SetLoops(4, LoopType.Yoyo)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            image.color = originColor; //색깔 초기화
        });
    }

    public void Exit()
    {
        StartCoroutine(ExitTransition());
    }

    public IEnumerator ExitTransition()
    {
        // 종료 버튼 상호작용 끄기
        exitBtn.interactable = false;

        //아이템 받는 시퀀스 강제 완료시키기
        outputSeq.Complete();

        // 필드드랍 아이템 1개 이상 있을때
        if (fieldDrop)
        {
            // 트럭 전조등 이펙트 켜기
            ParticleSystem truckParticle = itemDropper.GetComponent<ParticleSystem>();
            if (truckParticle)
                truckParticle.Play();
        }

        // 드롭퍼 변수 초기화
        itemDropper = null;

        //todo 종료 트랜지션

        //todo 종료 사운드 재생
        SoundManager.Instance.PlaySound("Vend_Exit", 0, 0, 1, false);

        // 트랜지션 대기
        // yield return new WaitForSecondsRealtime(1f);

        //팝업 메뉴 닫기
        UIManager.Instance.PopupUI(UIManager.Instance.vendMachinePanel);

        yield return null;
    }
}
