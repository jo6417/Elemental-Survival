using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Pool;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class MagicMixMenu : MonoBehaviour
{
    [Header("Refer")]
    public GameObject classPrefab; //클래스 전체 프리팹
    public GameObject iconGridPrefab; //아이콘 들어갈 그리드 프리팹
    public GameObject magicIconPrefab; //마법 아이콘 프리팹
    public GameObject recipePrefab; //마법 도감 레시피 프리팹
    public TextMeshProUGUI noMagicTxt; //합성 불가시 텍스트
    public TextMeshProUGUI scrollAmount; // 플레이어 보유 스크롤 개수

    [Header("ScrollRect")]
    public ScrollRect leftScroll; //왼쪽 스크롤
    public ScrollRect rightScroll; //오른쪽 스크롤
    public ScrollRect recipeScroll; //레시피 스크롤바

    [Header("List")]
    public Transform leftContainer; //왼쪽 마법 리스트
    public Transform rightContainer; //오른쪽 마법 리스트
    public Transform recipeContainer; //마법 레시피 리스트
    List<MagicInfo> notHasMagic = new List<MagicInfo>(); //플레이어가 보유하지 않은 마법 리스트

    private MagicInfo leftMagic; //왼쪽에서 선택된 마법
    private MagicInfo rightMagic; //오른쪽에서 선택된 마법
    private MagicInfo mixedMagic = null;

    [Header("FloatingIcon")]
    public Transform leftIcon; //왼쪽에서 선택된 마법 오브젝트
    public Transform rightIcon; //오른쪽에서 선택된 마법 오브젝트

    [Header("Panel")]
    public Transform leftInfoPanel; //왼쪽에서 선택된 마법 정보창
    public Transform rightInfoPanel; //오른쪽에서 선택된 마법 정보창
    public Transform mixInfoPanel; //합성된 마법 정보창
    public Transform recipePanel; //레시피 패널
    public Transform recipeInfoPanel; //레시피 항목 정보 패널

    [Header("Button")]
    public Transform scroll_Index; // 스크롤 개수 인덱스
    public Transform leftBackBtn; //왼쪽 back 버튼
    public Transform rightBackBtn; //오른쪽 back 버튼
    public Transform exitBtn; // 마법 합성 종료 버튼
    public Transform recipeBtn; // 마법 조합법 버튼
    public Button acceptBtn; // 조합 성공 확인 버튼

    // private Button lastSelected; //마지막 선택되었던 버튼 기억
    private Button leftSelected; //좌측 마지막 선택되었던 마법
    private Button rightSelected; //우측 마지막 선택되었던 마법
    private Button firstRecipe = null; //첫번째 레시피

    private void OnEnable()
    {
        // 레벨업 메뉴에 마법 정보 넣기
        if (MagicDB.Instance.loadDone)
            StartCoroutine(SetMenu());
    }

    IEnumerator SetMenu()
    {
        //시간 멈추기
        Time.timeScale = 0f;

        //플레이어 보유 스크롤 개수 업데이트
        int amount = 0;
        if (PlayerManager.Instance.hasItems.Exists(x => x.itemType == "Scroll"))
        {
            amount = PlayerManager.Instance.hasItems.Find(x => x.itemType == "Scroll").amount;
        }
        scrollAmount.text = "x " + amount.ToString();

        // 보유하지 않은 마법만 DB에서 파싱
        notHasMagic.Clear();
        notHasMagic = MagicDB.Instance.magicDB.Values.ToList().FindAll(x => x.magicLevel == 0);

        //Vertical 레이아웃 켜기
        leftContainer.GetComponent<VerticalLayoutGroup>().enabled = true;
        rightContainer.GetComponent<VerticalLayoutGroup>().enabled = true;
        //fitter 켜기
        leftContainer.GetComponent<ContentSizeFitter>().enabled = true;
        rightContainer.GetComponent<ContentSizeFitter>().enabled = true;

        //양쪽 뒤로 버튼 넣기
        ToggleBackBtn(true, 0);
        ToggleBackBtn(false, 0);

        //마법 레시피 버튼 띄우기
        ToggleRecipeBtn(true);

        //팝업 종료 버튼 띄우기
        ToggleExitBtn(true);

        // 페이지 로딩 끝날때까지 대기
        yield return new WaitUntil(() => 
        SetPage(true) && 
        SetPage(false) && 
        SetRecipe() && 
        leftSelected != null);

        //그리드 레이아웃 업데이트
        List<GridLayoutUI> leftGrid = leftContainer.GetComponentsInChildren<GridLayoutUI>().ToList();
        List<GridLayoutUI> rightGrid = rightContainer.GetComponentsInChildren<GridLayoutUI>().ToList();
        List<GridLayoutUI> recipeGrid = recipeContainer.GetComponentsInChildren<GridLayoutUI>().ToList();
        foreach (var grid in leftGrid)
        {
            grid.GridUpdate();
            grid.enabled = false;
        }
        foreach (var grid in rightGrid)
        {
            grid.GridUpdate();
            grid.enabled = false;
        }
        foreach (var grid in recipeGrid)
        {
            grid.GridUpdate();
            grid.enabled = false;
        }

        // UI 레이아웃 리빌드하기
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)leftContainer.transform);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)rightContainer.transform);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)recipeContainer.transform);

        //Vertical 레이아웃 끄기
        leftContainer.GetComponent<VerticalLayoutGroup>().enabled = false;
        rightContainer.GetComponent<VerticalLayoutGroup>().enabled = false;
        //fitter 끄기
        leftContainer.GetComponent<ContentSizeFitter>().enabled = false;
        rightContainer.GetComponent<ContentSizeFitter>().enabled = false;

        // 최상단으로 스크롤하기
        leftScroll.verticalNormalizedPosition = 1f;
        rightScroll.verticalNormalizedPosition = 1f;

        //좌측 첫번째 아이콘 버튼 선택하기
        leftSelected.Select();
    }

    bool SetPage(bool isLeft)
    {
        //합성 실패 텍스트 비활성화
        noMagicTxt.gameObject.SetActive(false);

        //합성 마법 페이지 닫기
        mixInfoPanel.gameObject.SetActive(false);

        //해당 페이지의 선택된 마법 아이콘
        Transform selectIcon = isLeft ? leftIcon : rightIcon;
        //마법 아이콘 비활성화
        selectIcon.gameObject.SetActive(false);

        //해당 페이지의 선택된 마법 정보창
        Transform selectInfoPanel = isLeft ? leftInfoPanel : rightInfoPanel;
        //좌,우 정보창 비활성화
        selectInfoPanel.gameObject.SetActive(false);

        //마법 리스트
        Transform container = isLeft ? leftContainer : rightContainer;
        // 리스트의 자식 모두 제거
        UIManager.Instance.DestroyChildren(container);
        // 리스트 활성화
        container.gameObject.SetActive(true);

        //아이콘 들어가는 스크롤
        ScrollRect scroll = isLeft ? leftScroll : rightScroll;

        // 모든 마법 정보 비우기
        leftMagic = null;
        rightMagic = null;
        // mixedMagic = null;

        // 플레이어 보유중인 마법 참조
        List<MagicInfo> playerMagics = PlayerManager.Instance.hasMagics;

        //최상단 첫번째 마법 아이콘 선택하기
        // Button firstIcon = null;

        // 해당 등급 마법 있으면 class 숫자 수정하고 마법 아이콘 추가
        for (int i = 6; i >= 0; i--)
        {
            // 6 ~ 1 등급 마법 있으면 아이콘 생성
            if (playerMagics.Exists(x => x.grade == i))
            {
                //클래스명 오브젝트 생성
                GameObject classTitle = LeanPool.Spawn(classPrefab, container);

                //아이콘 넣을 그리드 오브젝트 생성
                Transform magicList = LeanPool.Spawn(iconGridPrefab, container).transform;

                //그리드 내부 아이템 사이즈 수정
                GridLayoutUI grid = magicList.GetComponent<GridLayoutUI>();
                grid.cellSize = new Vector2(150f, 150f);
                grid.spacing = new Vector2(10f, 10f);

                //등급 타이틀
                TextMeshProUGUI classTxt = classTitle.GetComponentInChildren<TextMeshProUGUI>();

                //등급 색깔 넣기
                classTitle.GetComponent<Image>().color = MagicDB.Instance.gradeColor[i];
                //등급 텍스트 넣기
                classTxt.text = "Class " + i;

                //해당 등급 마법 모두 찾기
                List<MagicInfo> magics = new List<MagicInfo>();
                magics = playerMagics.FindAll(x => x.grade == i);

                //해당 등급 모든 마법 아이콘 넣기
                foreach (var magic in magics)
                {
                    //해당 등급 리스트에 마법 아이콘 스폰
                    GameObject magicIcon = LeanPool.Spawn(magicIconPrefab, magicList);

                    //자동 스크롤 스크립트 참조시키기
                    magicIcon.GetComponent<ButtonEvents>().autoScroll = scroll.GetComponent<OnSelectAutoScroll>();

                    //좌측 패널 첫번째 마법 아이콘 기억하기
                    if (isLeft && leftSelected == null)
                    {
                        leftSelected = magicIcon.GetComponent<Button>();
                    }

                    //우측 패널 첫번째 마법 아이콘 기억하기
                    if (!isLeft && rightSelected == null)
                    {
                        rightSelected = magicIcon.GetComponent<Button>();
                    }

                    // 마법 등급 넣기
                    magicIcon.transform.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[i];

                    // 마법 아이콘 이미지 넣기
                    magicIcon.transform.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.GetMagicIcon(magic.id);
                    // MagicDB.Instance.magicIcon.Find(x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

                    // 아이콘의 마법 정보 넣기
                    magicIcon.GetComponent<MagicHolder>().magic = magic;

                    // 툴팁 컴포넌트에 마법 정보 넣기
                    ToolTipTrigger tooltip = magicIcon.GetComponent<ToolTipTrigger>();
                    tooltip.toolTipType = ToolTipTrigger.ToolTipType.ProductTip;
                    tooltip.magic = magic;

                    // 아이콘 마우스 클릭할때 이벤트
                    Button btn = magicIcon.GetComponent<Button>();
                    btn.onClick.AddListener(delegate
                    {
                        // print(selectMagic.position);

                        // 마법 아이콘에 등급 넣기
                        selectIcon.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[magic.grade];

                        // 마법 아이콘 이미지 넣기
                        selectIcon.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.GetMagicIcon(magic.id);
                        // MagicDB.Instance.magicIcon.Find(x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

                        // 이름 테두리에 등급 색깔 넣기
                        Transform title = selectInfoPanel.Find("TitleFrame");
                        title.gameObject.SetActive(true);
                        title.GetComponent<Image>().color = MagicDB.Instance.gradeColor[magic.grade];
                        //이름 넣기
                        title.Find("Title").GetComponent<TextMeshProUGUI>().text = magic.magicName;

                        //아이콘 찾기
                        Transform infoIcon = selectInfoPanel.Find("MagicIcon");

                        //마법 설명 넣기
                        Transform descript = selectInfoPanel.Find("Descript");
                        descript.gameObject.SetActive(true);
                        descript.GetComponent<TextMeshProUGUI>().text = "마법정보 : " + magic.description;

                        // 페이지의 리스트 비활성화
                        //TODO 리스트 오브젝트 투명해지며 사라지기
                        container.gameObject.SetActive(false);
                        // 아이콘 활성화
                        selectIcon.gameObject.SetActive(true);

                        // 아이콘 사이즈 줄이기
                        selectIcon.localScale = Vector3.one;
                        // 아이콘 사이즈 키우기
                        selectIcon.DOScale(Vector3.one * 2f, 0.5f)
                        .SetUpdate(true);

                        // 해당 아이콘 자리로 selectIcon가 이동
                        selectIcon.position = btn.transform.position;
                        // 선택한 아이콘이 페이지 가운데로 domove
                        selectIcon
                        .DOMove(infoIcon.position, 0.5f)
                        .SetUpdate(true)
                        .SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            // 정보창 활성화
                            selectInfoPanel.gameObject.SetActive(true);

                            //백 버튼 튀어나오기
                            ToggleBackBtn(isLeft);

                            //좌,우 각각 마법 정보 넣기
                            if (isLeft)
                                leftMagic = magic;
                            else
                                rightMagic = magic;

                            // 양쪽 마법 둘다 선택되면
                            if (leftMagic != null && rightMagic != null)
                            {
                                //마법 합성 시작
                                MixMagic(isLeft);
                            }
                        });

                        //트윈 재시작
                        selectIcon.DORestart();

                        //버튼 선택 초기화
                        EventSystem.current.SetSelectedGameObject(null);

                        //좌우 각각 마지막 버튼 기억에 해당 아이콘 넣기
                        if (isLeft)
                        {
                            leftSelected = btn;
                        }
                        else
                        {
                            rightSelected = btn;
                        }

                        //한쪽만 선택했을때 다른 페이지 아이콘으로 커서 이동
                        if (!leftIcon.gameObject.activeSelf || !rightIcon.gameObject.activeSelf)
                        {
                            if (isLeft)
                            {
                                if (rightSelected != null && rightSelected.gameObject.activeInHierarchy)
                                {
                                    rightSelected.Select();
                                }
                            }
                            else
                            {
                                if (leftSelected != null && leftSelected.gameObject.activeInHierarchy)
                                {
                                    leftSelected.Select();
                                }
                            }
                        }
                    });
                }
                
            }
        }

        return true;
    }

    bool SetRecipe()
    {
        // 리스트의 자식 모두 제거
        UIManager.Instance.DestroyChildren(recipeContainer);

        //모든 마법 리스트 불러오기
        List<MagicInfo> allMagics = MagicDB.Instance.magicDB.Values.ToList();

        // 해당 등급 마법 있으면 class 숫자 수정하고 마법 아이콘 추가
        for (int i = 6; i >= 1; i--)
        {
            // 6 ~ 1 등급 마법 있으면 아이콘 생성
            if (allMagics.Exists(x => x.grade == i))
            {
                //등급 타이틀
                GameObject classTitle = LeanPool.Spawn(classPrefab, recipeContainer);

                //아이콘 넣을 그리드 오브젝트 생성
                Transform magicList = LeanPool.Spawn(iconGridPrefab, recipeContainer).transform;

                //그리드 내부 아이템 사이즈 수정
                GridLayoutUI grid = magicList.GetComponent<GridLayoutUI>();
                grid.cellSize = new Vector2(650f, 170f);
                grid.spacing = new Vector2(0f, 0f);

                //등급 타이틀
                // Transform classTitle = classMenu.transform.Find("ClassTitle");
                TextMeshProUGUI classTxt = classTitle.GetComponentInChildren<TextMeshProUGUI>();

                //등급 색깔 넣기
                classTitle.GetComponent<Image>().color = MagicDB.Instance.gradeColor[i];
                //등급 텍스트 넣기
                classTxt.text = "Class " + i;

                //해당 등급 마법 모두 찾기
                List<MagicInfo> magics = new List<MagicInfo>();
                magics = allMagics.FindAll(x => x.grade == i);

                //해당 등급 모든 마법 아이콘 넣기
                foreach (var magic in magics)
                {
                    //재료 마법들 찾기
                    MagicInfo elementA = MagicDB.Instance.GetMagicByName(magic.element_A);
                    MagicInfo elementB = MagicDB.Instance.GetMagicByName(magic.element_A);

                    //합성 성공 내역 있는 마법만 보여주기
                    bool unlockMagic = MagicDB.Instance.unlockMagics.Exists(x => x == magic.id);

                    //해당 등급 리스트에 레시피 스폰
                    GameObject recipe = LeanPool.Spawn(recipePrefab, magicList);

                    //자동 스크롤 스크립트 참조시키기
                    recipe.GetComponent<ButtonEvents>().autoScroll = recipeScroll.GetComponent<OnSelectAutoScroll>();

                    //첫번째 레시피일때 기억하기
                    if (firstRecipe == null)
                    {
                        firstRecipe = recipe.GetComponent<Button>();
                    }

                    //마법 아이콘
                    Transform magicIcon = recipe.transform.Find("MagicIcon");
                    // 마법 등급 넣기
                    magicIcon.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[i];

                    //마법 아이콘 찾기
                    Sprite iconSprite = MagicDB.Instance.GetMagicIcon(magic.id);
                    // MagicDB.Instance.magicIcon.Find(x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

                    //! 미구현 아이콘은 물음표 넣기
                    if (iconSprite == null)
                        iconSprite = SystemManager.Instance.questionMark;

                    // 마법 아이콘 이미지 넣기
                    Image magicIcon_icon = magicIcon.Find("Icon").GetComponent<Image>();
                    magicIcon_icon.sprite = iconSprite;

                    // 잠금해제한 마법일때
                    if (unlockMagic)
                        magicIcon_icon.color = Color.white;
                    // 잠긴 마법일때
                    else
                        magicIcon_icon.color = Color.black;

                    // 툴팁 컴포넌트에 마법 정보 넣기
                    // ToolTipTrigger tooltip = magicIcon.GetComponent<ToolTipTrigger>();
                    // tooltip.toolTipType = ToolTipTrigger.ToolTipType.ProductTip;
                    // tooltip.magic = magic;

                    //마법 재료 A 아이콘
                    Transform element_A = recipe.transform.Find("Element_A");
                    // 마법 등급 넣기
                    element_A.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[elementA.grade];
                    // 마법 아이콘 이미지 넣기, 미획득이면 물음표 스프라이트 넣기
                    element_A.Find("Icon").GetComponent<Image>().sprite = unlockMagic
                    ? MagicDB.Instance.GetMagicIcon(magic.id)
                    : SystemManager.Instance.questionMark;

                    // 툴팁 컴포넌트에 마법 정보 넣기
                    ToolTipTrigger tooltip_A = element_A.GetComponent<ToolTipTrigger>();
                    tooltip_A.toolTipType = ToolTipTrigger.ToolTipType.ProductTip;
                    tooltip_A.magic = MagicDB.Instance.GetMagicByName(magic.element_A);

                    //마법 재료 B 아이콘
                    Transform element_B = recipe.transform.Find("Element_B");
                    // 마법 등급 넣기
                    element_B.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[elementB.grade];
                    // 마법 아이콘 이미지 넣기
                    element_B.Find("Icon").GetComponent<Image>().sprite = unlockMagic
                    ? MagicDB.Instance.GetMagicIcon(magic.id)
                    : SystemManager.Instance.questionMark;

                    // 툴팁 컴포넌트에 마법 정보 넣기
                    ToolTipTrigger tooltip_B = element_B.GetComponent<ToolTipTrigger>();
                    tooltip_B.toolTipType = ToolTipTrigger.ToolTipType.ProductTip;
                    tooltip_B.magic = MagicDB.Instance.GetMagicByName(magic.element_B);

                    // 아이콘 눌렀을때 일어날 이벤트 넣기
                    Button btn = recipe.GetComponent<Button>();

                    btn.onClick.AddListener(delegate
                    {
                        // print("Recipe : " + magic.magicName);

                        //마법 타이틀 찾기
                        Transform title = recipeInfoPanel.Find("TitleFrame");
                        //마법 등급 색깔 넣기
                        title.GetComponent<Image>().color = MagicDB.Instance.gradeColor[magic.grade];
                        //마법 이름 넣기
                        title.Find("Title").GetComponent<TextMeshProUGUI>().text = magic.magicName;

                        //마법 아이콘 찾기
                        Transform magicIcon = recipeInfoPanel.Find("MagicIcon");

                        // 마법 아이콘에 등급 넣기
                        magicIcon.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[magic.grade];
                        // 마법 아이콘 이미지 넣기
                        Image iconImg = magicIcon.Find("Icon").GetComponent<Image>();
                        iconImg.sprite = iconSprite;
                        // 획득한 마법일때
                        if (unlockMagic)
                            iconImg.color = Color.white;
                        // 미획득한 마법일때
                        else
                            iconImg.color = Color.black;

                        //마법 설명 넣기
                        recipeInfoPanel.Find("Descript").GetComponent<TextMeshProUGUI>().text = "마법정보 : " + magic.description;
                    });
                }
                // UI 레이아웃 리빌드하기
                // LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)classMenu.transform);
            }
        }

        //레시피 리스트 및 정보 패널 비활성화
        recipePanel.gameObject.SetActive(false);
        recipeInfoPanel.gameObject.SetActive(false);

        //완료 여부 리턴
        return true;
    }

    void MixMagic(bool isLeft)
    {
        Sequence mixSeq = DOTween.Sequence();

        int scrollAmount = 0;
        if (PlayerManager.Instance.hasItems.Exists(x => x.itemType == "Scroll"))
        {
            scrollAmount = PlayerManager.Instance.hasItems.Find(x => x.itemType == "Scroll").amount;
        }

        //두 재료 모든 갖고 있는 마법 찾기
        mixedMagic = MagicDB.Instance.magicDB.Values.ToList().Find(y => y.element_A == leftMagic.magicName && y.element_B == rightMagic.magicName);

        if (mixedMagic == null)
            mixedMagic = MagicDB.Instance.magicDB.Values.ToList().Find(y => y.element_A == rightMagic.magicName && y.element_B == leftMagic.magicName);

        // 합성 실패
        if (scrollAmount == 0 || mixedMagic == null)
        {
            // 마지막 선택된 패널 백버튼 선택하기
            if (isLeft)
            {
                leftBackBtn.GetComponent<Selectable>().Select();
            }
            else
            {
                rightBackBtn.GetComponent<Selectable>().Select();
            }

            //좌,우 재료로 합성 가능한 마법 없을때
            if (mixedMagic == null)
            {
                print("합성 가능한 마법이 없습니다");
                noMagicTxt.text = "합성 가능한 마법이 없습니다";

                // 양쪽 아이콘 떨리기
                leftIcon.DOComplete(); //트윈 강제로 끝내기
                leftIcon.DOPunchPosition(Vector2.right * 10f, 1f)
                    .SetUpdate(true);
                rightIcon.DOComplete(); //트윈 강제로 끝내기
                rightIcon.DOPunchPosition(Vector2.right * 10f, 1f)
                    .SetUpdate(true);
            }

            //스크롤이 부족할때
            if (scrollAmount == 0)
            {
                print("스크롤이 부족합니다");
                noMagicTxt.text = "스크롤이 부족합니다";

                // 스크롤 인덱스 떨림으로 강조하기
                scroll_Index.DOComplete(); //트윈 강제로 끝내기
                scroll_Index.DOPunchPosition(Vector2.right * 10f, 1f)
                .SetUpdate(true);
            }

            //트윈 강제로 끝내기
            noMagicTxt.DOComplete();

            // 합성 불가 메시지 띄운 뒤 사라지기
            noMagicTxt.gameObject.SetActive(true);

            Color color = noMagicTxt.color;
            noMagicTxt.DOColor(new Color(color.r, color.g, color.b, 0), 1f)
            .SetDelay(1f)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                noMagicTxt.gameObject.SetActive(false);
                noMagicTxt.color = color;
            });

            print("합성 실패");
            return;
        }

        print("합성 성공 : " + mixedMagic.magicName);

        // 양쪽 마법 정보 비우기
        leftMagic = null;
        rightMagic = null;

        //보유 스크롤 개수 줄이기 및 업데이트
        PlayerManager.Instance.hasItems.Find(x => x.itemType == "Scroll").amount -= 1;
        scrollAmount = PlayerManager.Instance.hasItems.Find(x => x.itemType == "Scroll").amount;
        this.scrollAmount.text = "x " + scrollAmount.ToString();

        //좌,우 정보창 끄기
        leftInfoPanel.gameObject.SetActive(false);
        rightInfoPanel.gameObject.SetActive(false);

        //좌,우 뒤로 버튼 끄기
        ToggleBackBtn(true);
        ToggleBackBtn(false);

        //레시피 버튼 끄기
        ToggleRecipeBtn(false);

        //팝업 종료 버튼 끄기
        ToggleExitBtn(false);

        //합성 마법 정보창 찾기
        Transform effectMask = mixInfoPanel.Find("EffectMask");

        // 이름 테두리에 등급 색깔 넣기
        Transform title = effectMask.Find("TitleFrame");
        title.GetComponent<Image>().color = MagicDB.Instance.gradeColor[mixedMagic.grade];
        //이름 넣기
        title.Find("Title").GetComponent<TextMeshProUGUI>().text = mixedMagic.magicName;

        //아이콘 및 등급색 넣기
        Transform mixedIcon = effectMask.Find("MagicIcon");
        mixedIcon.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.GetMagicIcon(mixedMagic.id);
        // MagicDB.Instance.magicIcon.Find(x => x.name == mixedMagic.magicName.Replace(" ", "") + "_Icon");
        mixedIcon.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[mixedMagic.grade];
        mixedIcon.localScale = Vector2.zero;

        //마법 설명 넣기
        effectMask.Find("Descript").GetComponent<TextMeshProUGUI>().text = "마법정보 : " + mixedMagic.description;

        //좌,우 아이콘 사이즈 줄이면서 가운데로 모으기
        float seqDuration = 0.5f;
        mixSeq.Append(
            leftIcon.DOMove(mixInfoPanel.position, seqDuration)
            .SetEase(Ease.InBack)
        )
        .Join(
            leftIcon.DOScale(Vector3.one, seqDuration)
        )
        .Join(
            rightIcon.DOMove(mixInfoPanel.position, seqDuration)
            .SetEase(Ease.InBack)
        )
        .Join(
            rightIcon.DOScale(Vector3.one, seqDuration)
        )
        .OnComplete(() =>
        {
            //양쪽 아이콘 숨기기
            leftIcon.gameObject.SetActive(false);
            rightIcon.gameObject.SetActive(false);

            //궁극기일때
            if (mixedMagic.castType == "ultimate")
            {
                //궁극기 팝업창 열기
                UIManager.Instance.PopupUI(UIManager.Instance.ultimateMagicPanel);

                //마법 정보 넣기
                UltimateMagic.Instance.newMagic = mixedMagic;

                //아이콘 숨기기
                mixedIcon.DOScale(Vector3.zero, 0.5f);
            }
            //일반 마법일때
            else
            {
                //합성된 마법 정보창 활성화
                mixInfoPanel.gameObject.SetActive(true);

                // Accept 버튼 선택하기
                acceptBtn.Select();

                //아이콘 커지면서 등장
                mixedIcon.DOScale(Vector3.one * 2f, 0.5f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);

                // 마법 획득하기
                PlayerManager.Instance.GetMagic(mixedMagic);
            }

            // 마법 도감에 없으면 추가
            if (!MagicDB.Instance.unlockMagics.Exists(x => x == mixedMagic.id))
            {
                // 해당 마법을 도감에서 해금하기
                MagicDB.Instance.unlockMagics.Add(mixedMagic.id);

                // 변경된 도감 저장하기
                StartCoroutine(SaveManager.Instance.Save());
            }

            // 합성된 마법 정보 삭제
            mixedMagic = null;
        })
        .SetUpdate(true);
        // mixSeq.Restart(); //시퀀스 재시작
    }

    public void GoListPage(bool isLeft)
    {
        //해당 페이지의 선택된 마법 아이콘
        Transform selectIcon = isLeft ? leftIcon : rightIcon;
        //해당 페이지의 선택된 마법 정보창
        Transform selectInfoPanel = isLeft ? leftInfoPanel : rightInfoPanel;
        //마법 리스트
        Transform parent = isLeft ? leftContainer : rightContainer;

        // 마법 아이콘 비활성화
        selectIcon.gameObject.SetActive(false);

        // 정보창 비활성화
        selectInfoPanel.gameObject.SetActive(false);

        //뒤로 버튼 끄기 위치로 움직이기
        ToggleBackBtn(isLeft);

        // 마법 리스트 활성화
        parent.gameObject.SetActive(true);

        //마지막 선택했던 아이콘 선택
        if (isLeft)
        {
            if (leftSelected != null)
            {
                leftSelected.Select();
            }
        }
        else
        {
            if (rightSelected != null)
            {
                rightSelected.Select();
            }
        }
    }

    void ToggleBackBtn(bool isLeft, float duration = 0.5f)
    {
        //해당 페이지의 뒤로가기 버튼
        Transform backBtn = isLeft ? leftBackBtn : rightBackBtn;
        //해당 페이지의 선택된 마법 아이콘
        Transform selectPage = isLeft ? leftInfoPanel : rightInfoPanel;

        // 버튼 끌때
        if (!selectPage.gameObject.activeSelf)
        {
            // 좌,우 각각 마법 정보 없에기
            if (isLeft)
                leftMagic = null;
            else
                rightMagic = null;
        }

        // 움직일때는 상호작용 비활성화
        backBtn.GetComponent<Button>().interactable = false;

        //아이콘 트윈 강제로 끝내기
        selectPage.DOComplete();

        //뒤로 버튼 트윈 강제로 끝내기
        backBtn.DOComplete();

        float startY = 140f;
        float endY = 0;
        //왼쪽 백 버튼일때
        if (isLeft)
        {
            endY = selectPage.gameObject.activeSelf ? startY - 100f : startY;
        }
        //오른쪽 백 버튼일때
        else
        {
            endY = selectPage.gameObject.activeSelf ? startY - 100f : startY;
        }

        //켤때, 끌때 다른 Ease
        Ease ease = selectPage.gameObject.activeSelf ? Ease.OutBack : Ease.InBack;

        //원래 위치로 돌아오기
        backBtn.DOLocalMoveY(endY, duration)
        .OnStart(() =>
        {
            backBtn.gameObject.SetActive(true);
        })
        .SetEase(ease)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            //버튼 안보일땐 비활성화
            if (!selectPage.gameObject.activeSelf)
                backBtn.gameObject.SetActive(false);

            //상호작용 토글
            backBtn.GetComponent<Button>().interactable = selectPage.gameObject.activeSelf;
        });
    }

    public void ClickRecipeBtn()
    {
        StartCoroutine(ToggleRecipePage());
    }

    IEnumerator ToggleRecipePage()
    {
        //레시피 리스트 토글
        recipePanel.gameObject.SetActive(!recipePanel.gameObject.activeSelf);
        //레시피 정보 패널 토글
        recipeInfoPanel.gameObject.SetActive(recipePanel.gameObject.activeSelf);

        //레시피 리스트 켤때는 첫번째 레시피 선택
        if (recipePanel.gameObject.activeSelf)
        {
            // 첫번째 항목 버튼 클릭하기
            // firstRecipe.onClick.Invoke();

            //! 스크롤 아래 끝까지 내리는 버그 있음
            firstRecipe.Select();
        }

        // 재료 리스트 패널 토글
        leftContainer.gameObject.SetActive(!recipePanel.gameObject.activeSelf);
        rightContainer.gameObject.SetActive(!recipePanel.gameObject.activeSelf);

        // 마법 정보 패널 비활성화
        leftInfoPanel.gameObject.SetActive(false);
        rightInfoPanel.gameObject.SetActive(false);

        //마법 정보 아이콘 비활성화
        if (leftIcon.gameObject.activeSelf)
        {
            leftIcon.gameObject.SetActive(false);
            ToggleBackBtn(true);
        }

        if (rightIcon.gameObject.activeSelf)
        {
            rightIcon.gameObject.SetActive(false);
            ToggleBackBtn(false);
        }

        //레시피 버튼 올리기
        ToggleRecipeBtn(false);

        //올라가는 동안 대기
        yield return new WaitForSecondsRealtime(0.5f);

        //레시피 버튼 다시 내리기
        ToggleRecipeBtn(true);
    }

    void ToggleRecipeBtn(bool isActive, float duration = 0.5f)
    {
        //버튼 상호작용 비활성화
        recipeBtn.GetComponent<Button>().interactable = isActive;

        //버튼 트윈 강제로 끝내기
        recipeBtn.DOComplete();

        float endX = isActive ? 40f : 240f;

        //켤때, 끌때 다른 Ease
        Ease ease = isActive ? Ease.OutBack : Ease.InBack;

        //버튼 이동
        recipeBtn.GetComponent<RectTransform>().DOAnchorPosX(endX, duration)
        .OnStart(() =>
        {
            //버튼 내려갈때만
            if (isActive)
            {
                //레시피 켤때는 Back, 레시피 끌때는 Recipe로 전환
                recipeBtn.GetComponentInChildren<TextMeshProUGUI>().text = recipePanel.gameObject.activeSelf ? "Back" : "Recipe";
                //레시피 켤때는 빨간색, 레시피 끌때는 초록색으로 전환
                recipeBtn.GetComponent<Image>().color = recipePanel.gameObject.activeSelf ?
                SystemManager.Instance.HexToRGBA("F06464") : SystemManager.Instance.HexToRGBA("3742CC");
            }
        })
        .SetEase(ease)
        .SetUpdate(true);
    }

    void ToggleExitBtn(bool isActive, float duration = 0.5f)
    {
        exitBtn.GetComponent<Button>().interactable = isActive;

        //뒤로 버튼 트윈 강제로 끝내기
        exitBtn.DOComplete();
        
        float endX = isActive ? -40f : -240f;

        //켤때, 끌때 다른 Ease
        Ease ease = isActive ? Ease.OutBack : Ease.InBack;

        //버튼 이동
        exitBtn.GetComponent<RectTransform>().DOAnchorPosX(endX, duration)
        .SetEase(ease)
        .SetUpdate(true);
    }

    public void ExitPopup()
    {
        if (!mixInfoPanel.gameObject.activeSelf)
        {
            //레시피 버튼 즉시 끄기
            ToggleRecipeBtn(false, 0);

            //팝업 종료 버튼 즉시 끄기
            ToggleExitBtn(false, 0);
        }

        //합성 마법 페이지 닫기
        mixInfoPanel.gameObject.SetActive(false);

        // 마법 정보 패널 비활성화
        leftInfoPanel.gameObject.SetActive(false);
        rightInfoPanel.gameObject.SetActive(false);

        //마법 정보 아이콘 비활성화
        if (leftIcon.gameObject.activeSelf)
        {
            leftIcon.gameObject.SetActive(false);
            ToggleBackBtn(true);
        }

        if (rightIcon.gameObject.activeSelf)
        {
            rightIcon.gameObject.SetActive(false);
            ToggleBackBtn(false);
        }

        //마지막으로 기억된 버튼 없에기
        UIManager.Instance.lastSelected = null;
        leftSelected = null;
        rightSelected = null;
        firstRecipe = null;

        //선택 정보 삭제
        EventSystem.current.SetSelectedGameObject(null);

        //해당 팝업 끄기
        UIManager.Instance.PopupUI(UIManager.Instance.magicMixPanel);
    }
}
