using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lean.Pool;
using DG.Tweening;
using UnityEngine.EventSystems;
using DanielLochner.Assets.SimpleScrollSnap;
using System.Linq;

public class MergeMenu : MonoBehaviour
{
    #region Singleton
    private static MergeMenu instance;
    public static MergeMenu Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<MergeMenu>();
                if (obj != null)
                {
                    instance = obj;
                }
                // else
                // {
                //     var newObj = new GameObject().AddComponent<MergeMenu>();
                //     instance = newObj;
                // }
            }
            return instance;
        }
    }
    #endregion

    [Header("Refer")]
    [SerializeField] Sprite itemFrame;
    [SerializeField] Sprite magicFrame;
    public SimpleScrollSnap screenScroll; // 화면 스크롤
    public GameObject phonePanel; // 핸드폰 화면 패널
    [SerializeField] GameObject mergeScreen; // 머지 페이지
    [SerializeField] GameObject recipeScreen; // 레시피 페이지
    public Button recipeBtn;
    public Button backBtn;
    public Button homeBtn;
    public SpriteRenderer lightScreen; // 폰 스크린 전체 빛내는 HDR 이미지
    public Image blackScreen; // 폰 작아질때 검은 이미지로 화면 가리기
    public GameObject loadingPanel; //로딩 패널, 로딩중 뒤의 버튼 상호작용 막기
    bool btnsInteractable = true; // 버튼 상호작용 가능 여부

    [Header("Effect")]
    public Vector3 phonePosition; //핸드폰일때 위치 기억
    public Vector3 phoneRotation; //핸드폰일때 회전값 기억
    public Vector3 phoneScale; //핸드폰일때 고정된 스케일
    public Vector3 UIPosition; //팝업일때 위치
    public SlicedFilledImage backBtnFill; //뒤로가기 버튼
    float backBtnCount; //백버튼 더블클릭 카운트

    [Header("Chat List")]
    public GameObject chatPrefab; // 채팅 프리팹
    public ScrollRect chatScroll; // 채팅 스냅 스크롤
    [SerializeField] private RectTransform chatContentRect;
    public float sumChatHeights; // 채팅 스크롤 content의 높이

    [Header("Merge List")]
    public Transform mergeSlots;
    public List<Button> mergeList = new List<Button>(); //각각 슬롯 오브젝트
    public MergeSlot nowSelectSlot; //현재 선택된 슬롯
    public MergeSlot mergeWaitSlot; // 합성 대기중인 슬롯
    public int[] closeSlots = new int[4]; //선택된 슬롯 주변의 인덱스
    public int[] mergeResultMagics = new int[4]; //합성 가능한 마법 id
    public Transform mergeSignal;
    public Transform mergeIcon; //Merge domove 할때 대신 날아갈 아이콘 (슬롯간 레이어 문제로 사용)
    public int moveMagicIndex = -1; // 옮겨질 마법

    [Header("Stack List")]
    public Transform stackSlots;
    public TextMeshProUGUI stackAllNum; // 스택 마법 총 개수
    public List<GameObject> stackObjSlots = new List<GameObject>(); //각각 슬롯 오브젝트
    Vector2[] stackSlotPos = new Vector2[7]; //각각 슬롯의 초기 위치
    float scrollCoolCount; //스크롤 쿨타임 카운트
    float scrollCoolTime = 0.1f; //스크롤 쿨타임
    public Button selectedSlot;
    public ToolTipTrigger selectedTooltip;
    public Button leftScrollBtn;
    public Button rightScrollBtn;
    public MagicInfo selectedMagic; //현재 선택된 마법
    public Image selectedIcon; //마우스 따라다닐 아이콘
    RectTransform selectedIconRect;

    [Header("Recipe List")]
    public SimpleScrollSnap recipeScroll; // 레시피 슬롯 스크롤
    public GameObject recipePrefab; // 단일 레시피 프리팹
    public bool recipeInit = false;

    [Header("USB List")]
    public TextMeshProUGUI usbAllNum; // USB 총 개수 표시 텍스트
    public SimpleScrollSnap usbScroll; // USB 슬롯 스크롤
    public Transform anim_USB_Slot; // 애니메이션용 usb 아이콘
    public CanvasGroup randomScreen; // 뽑기 스크린
    public SimpleScrollSnap randomScroll; // USB 뽑기 랜덤 스크롤
    public Transform magicSlotPrefab; // 랜덤 마법 아이콘 프리팹
    public Animator slotRayEffect; // 슬롯 팡파레 이펙트
    public ParticleSystem getMagicEffect; // 뽑은 마법 획득 이펙트
    MagicInfo getMagic = null; // 랜덤 획득 마법 정보
    public float randomScrollSpeed = 15f; // 뽑기 스크롤 속도
    public float minScrollTime = 3f; // 슬롯머신 최소 시간
    public float maxScrollTime = 5f; // 슬롯머신 최대 시간

    private void Awake()
    {
        //머지 슬롯 오브젝트 모두 저장
        for (int i = 0; i < mergeSlots.childCount; i++)
        {
            mergeList.Add(mergeSlots.GetChild(i).GetComponent<Button>());
        }

        //스택 오브젝트 및 위치 모두 저장
        for (int i = 0; i < 7; i++)
        {
            // 모든 슬롯 오브젝트 넣기
            stackObjSlots.Add(stackSlots.transform.GetChild(i).gameObject);
            // 슬롯들의 초기 위치 넣기
            stackSlotPos[i] = stackObjSlots[i].GetComponent<RectTransform>().anchoredPosition;
            // print(slotPos[i]);
        }

        // 선택된 마법 rect 찾기
        selectedIconRect = selectedIcon.transform.parent.GetComponent<RectTransform>();

        // 키 입력 정리
        StartCoroutine(InputInit());

        // 스크롤 컨텐츠 모두 비우기
        SystemManager.Instance.DestroyAllChild(recipeScroll.Content);
    }

    IEnumerator InputInit()
    {
        // 플레이어 초기화 대기
        yield return new WaitUntil(() => PlayerManager.Instance.initFinish);

        //플레이어 인풋 끄기
        // PlayerManager.Instance.playerInput.Disable();

        // 방향키 입력
        UIManager.Instance.UI_Input.UI.NavControl.performed += val => NavControl(val.ReadValue<Vector2>());
        // 마우스 위치 입력
        UIManager.Instance.UI_Input.UI.MousePosition.performed += val => MousePos();
        // 마우스 클릭
        UIManager.Instance.UI_Input.UI.Click.performed += val =>
        {
            if (gameObject.activeSelf)
                StartCoroutine(Click());
        };

        // 스마트폰 버튼 입력
        UIManager.Instance.UI_Input.UI.PhoneMenu.performed += val =>
        {
            // 로딩 패널 꺼져있을때, 머지 선택 모드 아닐때
            if (!loadingPanel.activeSelf)
            {
                //백 버튼 액션 실행
                StartCoroutine(BackBtnAction());
            }
        };

        // 머지 패널 끄기
        phonePanel.SetActive(false);
        // 머지 캔버스 끄기
        gameObject.SetActive(false);
    }

    IEnumerator Click()
    {
        // 클릭시 select 오브젝트 바뀔때까지 1프레임 대기
        yield return new WaitForSeconds(Time.deltaTime);

        //마우스에 아이콘 들고 있을때
        if (selectedIcon.enabled)
        {
            // null 선택했을때, 메뉴버튼, 백버튼, 홈버튼 클릭했을때
            if (EventSystem.current.currentSelectedGameObject == null
            || EventSystem.current.currentSelectedGameObject == recipeBtn.gameObject
            || EventSystem.current.currentSelectedGameObject == backBtn.gameObject
            || EventSystem.current.currentSelectedGameObject == homeBtn.gameObject)
            {
                //커서 및 빈 스택 슬롯 초기화 하기
                SelectSlotToggle();

                // 마법 이동 중이었으면
                if (moveMagicIndex != -1)
                {
                    // 마우스에 선택된 마법 아이콘 다시 넣기
                    selectedIcon.sprite = MagicDB.Instance.GetMagicIcon(selectedMagic.id);

                    // 이동중이던 마법 슬롯 다시 켜기
                    mergeList[moveMagicIndex].transform.Find("Icon").GetComponent<Image>().enabled = true;

                    // 이동중 마법 인덱스 초기화
                    moveMagicIndex = -1;
                }
            }
        }
    }

    // 방향키 입력되면 실행
    void NavControl(Vector2 arrowDir)
    {
        // 머지 패널 꺼져있으면 리턴
        if (!gameObject.activeSelf)
            return;

        //마우스에 아이콘 들고 있을때
        if (selectedIcon.enabled)
            //커서 및 빈 스택 슬롯 초기화 하기
            SelectSlotToggle();

        //쿨타임 가능할때, 스택 슬롯 Select 됬을때
        if (scrollCoolCount <= 0f && nowSelectSlot != null && UIManager.Instance.lastSelected.gameObject == selectedSlot.gameObject)
        {
            // 왼쪽으로 스크롤하기
            if (arrowDir.x < 0)
            {
                Scroll_Stack(-1);
                scrollCoolCount = scrollCoolTime;
            }

            // 오른쪽으로 스크롤하기
            if (arrowDir.x > 0)
            {
                Scroll_Stack(1);
                scrollCoolCount = scrollCoolTime;
            }
        }
    }

    // 마우스 위치 입력되면 실행
    void MousePos()
    {
        // 머지 패널 꺼져있으면 리턴
        if (!gameObject.activeSelf)
            return;

        // print(mousePosInput);

        if (selectedIcon.enabled)
        {
            // 캔버스 스케일을 해상도로 나눈 비율을 곱해서 마우스 위치값 보정
            Vector3 mousePos = UIManager.Instance.nowMousePos * (GetComponent<CanvasScaler>().referenceResolution.x / Screen.width);
            mousePos.z = 0;

            // 선택된 마법 아이콘 마우스 따라다니기
            selectedIconRect.anchoredPosition = mousePos;
        }
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //시간 멈추기
        Time.timeScale = 0f;

        //팝업 UI 열림 표시
        // UIManager.Instance.popupUIOpened = true;

        // 휴대폰 로딩 화면으로 가리기
        LoadingToggle(true);
        blackScreen.color = new Color(70f / 255f, 70f / 255f, 70f / 255f, 1);

        //마법 DB 로딩 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //머지 보드 세팅
        Set_Merge();

        // 스택 슬롯 사이즈 및 위치 정렬
        Scroll_Stack(0);

        // 총 스택 개수 갱신
        stackAllNum.text = StackAmount().ToString();

        // 레시피 리스트 세팅
        Set_Recipe();

        // usb 리스트 세팅
        Set_USB();

        // 애니메이션용 슬롯 위치 초기화
        anim_USB_Slot.position = usbScroll.transform.position;

        // 팡파레 이펙트 끄기
        slotRayEffect.gameObject.SetActive(false);

        // 뽑기 슬롯 이펙트 끄기
        getMagicEffect.gameObject.SetActive(false);

        // 뽑기 스크린 투명하게 숨기기
        randomScreen.alpha = 0f;

        // 뽑기 스크린 끄기
        randomScreen.gameObject.SetActive(false);

        // 선택 아이콘 끄기
        selectedIcon.enabled = false;

        //Merge 인디케이터 끄기
        mergeSignal.gameObject.SetActive(false);

        // 핸드폰 켜기
        StartCoroutine(OpenPhone());

        // 총 USB 개수 활성화
        usbAllNum.transform.parent.gameObject.SetActive(true);
        // 총 스택 개수 비활성화
        stackAllNum.transform.parent.gameObject.SetActive(false);

        // 각각 스크린 켜기
        mergeScreen.SetActive(true);
        recipeScreen.SetActive(true);
    }

    public int StackAmount()
    {
        int stackAmount = 0;
        foreach (MagicInfo magic in PlayerManager.Instance.hasStackMagics)
        {
            stackAmount += magic.amount;
        }

        return stackAmount;
    }

    public IEnumerator OpenPhone()
    {
        //위치 기억하기
        phonePosition = CastMagic.Instance.transform.position;
        //회전값 기억하기
        phoneRotation = CastMagic.Instance.transform.rotation.eulerAngles;

        //카메라 위치
        Vector3 camPos = SystemManager.Instance.camParent.position;
        camPos.z = 0;

        // 화면 라이트 끄기
        lightScreen.DOColor(new Color(1f, 1f, 1f, 0f), 0.4f)
        .SetUpdate(true);

        float moveTime = 0.8f;

        // 팝업UI 위치,회전,스케일로 복구하기
        CastMagic.Instance.transform.DOMove(camPos + UIPosition, moveTime)
        .SetUpdate(true);
        CastMagic.Instance.transform.DOScale(Vector3.one, moveTime)
        .SetUpdate(true);
        CastMagic.Instance.transform.DORotate(new Vector3(0, 720f - phoneRotation.y, 0), moveTime, RotateMode.WorldAxisAdd)
        .SetUpdate(true);

        // 스마트폰 움직이는 트랜지션 끝날때까지 대기
        yield return new WaitUntil(() => CastMagic.Instance.transform.localScale == Vector3.one);

        // 핸드폰 화면 패널 켜기
        phonePanel.SetActive(true);

        // 검은화면 투명하게
        blackScreen.DOColor(Color.clear, 0.2f)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            // 휴대폰 로딩 화면 끄기
            LoadingToggle(false);
        });

        #region Interact_On

        // merge 슬롯 모두 켜기
        foreach (Button mergeSlot in mergeList)
        {
            mergeSlot.interactable = true;
        }
        //스택 가운데 버튼 켜기
        selectedSlot.interactable = true;
        //스택 좌,우 버튼 켜기
        leftScrollBtn.interactable = true;
        rightScrollBtn.interactable = true;
        //레시피 버튼 켜기
        recipeBtn.interactable = true;
        //뒤로 버튼 켜기
        backBtn.interactable = true;
        //홈 버튼 켜기
        homeBtn.interactable = true;

        // 첫번째 머지 슬롯 선택하기
        UIManager.Instance.lastSelected = mergeList[0];
        UIManager.Instance.targetOriginColor = mergeList[0].GetComponent<Image>().color;
        // mergeList[0].GetComponent<Button>().Select();

        //선택된 슬롯 네비 설정
        Navigation nav = selectedSlot.navigation;
        nav.selectOnUp = stackObjSlots[3].GetComponent<Button>().FindSelectable(Vector3.up);
        selectedSlot.navigation = nav;

        #endregion

        //트윈 끝날때까지 대기
        // yield return new WaitUntil(() => CastMagic.Instance.transform.localScale == Vector3.one);

        //모든 슬롯 shiny 효과 순차적으로 켜기
        // for (int i = 0; i < mergeSlots.childCount; i++)
        // {
        //     GameObject shinyObj = mergeList[i].transform.Find("ShinyMask").gameObject;
        //     shinyObj.SetActive(false);
        //     shinyObj.SetActive(true);

        //     yield return new WaitForSecondsRealtime(0.01f);
        // }

        // TODO 게임 시작할때는 기본 마법 1개 어느 슬롯에 놓을지 선택
        // TODO 선택하면 배경 사라지고, 휴대폰 플레이어 위로 작아지며 날아간 후에 게임 시작
    }

    void LoadingToggle(bool isLoading)
    {
        UIManager.Instance.phoneLoading = isLoading;

        loadingPanel.SetActive(isLoading);
    }

    void Set_Merge()
    {
        // 머지 리스트에 있는 마법들 머지 보드에 나타내기
        for (int i = 0; i < mergeSlots.childCount; i++)
        {
            Transform mergeSlot = mergeList[i].transform;

            // 마법 정보 찾기
            MagicInfo magic = PlayerManager.Instance.hasMergeMagics[i];

            //프레임 찾기
            Image frame = mergeSlot.Find("Frame").GetComponent<Image>();
            //아이콘 찾기
            Image icon = mergeSlot.Find("Icon").GetComponent<Image>();
            //레벨 찾기
            Image level = mergeSlot.Find("Level").GetComponent<Image>();
            //버튼 찾기
            Button button = mergeSlot.GetComponent<Button>();
            //툴팁 컴포넌트 찾기
            // ToolTipTrigger tooltip = mergeSlot.Find("Button").GetComponent<ToolTipTrigger>();

            //마법 정보 없으면 넘기기
            if (magic == null)
            {
                //프레임 색 초기화
                frame.color = Color.white;

                //아이콘 및 레벨 비활성화
                icon.enabled = false;
                level.gameObject.SetActive(false);

                continue;
            }

            //아이콘 및 레벨 활성화
            icon.enabled = true;
            level.gameObject.SetActive(true);

            //등급 프레임 색 넣기
            frame.color = MagicDB.Instance.GradeColor[magic.grade];
            //아이콘 넣기
            icon.sprite = MagicDB.Instance.GetMagicIcon(magic.id) == null ? SystemManager.Instance.questionMark : MagicDB.Instance.GetMagicIcon(magic.id);
            // 레벨 이미지 색에 등급색 넣기
            level.color = MagicDB.Instance.GradeColor[magic.grade];
            //레벨 넣기
            level.GetComponentInChildren<TextMeshProUGUI>(true).text = "Lv. " + magic.magicLevel.ToString();
            //TODO 슬롯에 툴팁 정보 넣기
            // tooltip.magic = magic;
        }
    }

    private void Update()
    {
        //뒤로가기 시간 카운트
        if (backBtnCount > 0)
            backBtnCount -= Time.unscaledDeltaTime;
        else
        {
            DOTween.To(() => backBtnFill.fillAmount, x => backBtnFill.fillAmount = x, 0f, 0.2f)
            .SetUpdate(true);
        }

        //스택 스크롤 시간 카운트
        if (scrollCoolCount > 0)
            scrollCoolCount -= Time.unscaledDeltaTime;

        //todo 채팅 스크롤의 컨텐츠 오브젝트 사이즈를 자식 모두의 높이 총합만큼 lerp로 설정

        // 
        if (sumChatHeights == 0)
            sumChatHeights = chatContentRect.sizeDelta.y;

        // Lerp로 사이즈 반영        
        chatContentRect.sizeDelta = new Vector2(chatContentRect.sizeDelta.x, Mathf.Lerp(chatContentRect.sizeDelta.y, sumChatHeights, Time.unscaledDeltaTime * 5f));
    }

    public IEnumerator ChatAdd(string message)
    {
        // 메시지 생성
        GameObject chat = LeanPool.Spawn(MergeMenu.Instance.chatPrefab,
        MergeMenu.Instance.chatScroll.content.rect.position - new Vector2(0, -10f),
        Quaternion.identity, MergeMenu.Instance.chatScroll.content);

        // 캔버스 그룹 찾고 알파값 0으로 낮춰서 숨기기
        CanvasGroup canvasGroup = chat.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;

        // 채팅 컬러 및 메시지 적용
        chat.GetComponent<Image>().color = new Color(1, 50f / 255f, 50f / 255f, 1);
        chat.GetComponentInChildren<TextMeshProUGUI>().text = message;

        // 1프레임 대기
        yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);

        float sumHeights = 0;

        // 메시지들의 총합 높이 갱신
        for (int i = 0; i < MergeMenu.Instance.chatScroll.content.childCount; i++)
        {
            RectTransform rect = MergeMenu.Instance.chatScroll.content.GetChild(i).GetComponent<RectTransform>();

            // 여백 합산
            sumHeights += 10;
            // 높이 합산
            sumHeights += rect.sizeDelta.y;

            // print(i + " : " + rect.sizeDelta.y);
        }

        MergeMenu.Instance.sumChatHeights = sumHeights;

        // 알파값 높여서 표시
        canvasGroup.alpha = 1;
    }

    public void SelectSlotToggle()
    {
        //빈 슬롯이면 리턴
        if (PlayerManager.Instance.hasStackMagics.Count == 0)
            return;

        //마우스 꺼져있으면 리턴
        if (Cursor.lockState == CursorLockMode.Locked)
            return;

        // 마법 이동 중이었으면
        if (moveMagicIndex != -1)
        {
            // 이동중이던 마법 슬롯 다시 켜기
            mergeList[moveMagicIndex].transform.Find("Icon").GetComponent<Image>().enabled = true;

            // 이동중 마법 인덱스 초기화
            moveMagicIndex = -1;
        }

        // 가운데 슬롯 아이콘 찾기
        Image targetImage = stackObjSlots[3].transform.Find("Icon").GetComponent<Image>();

        // 마우스에 현재 선택된 마법 아이콘 넣기
        if (selectedMagic != null)
            selectedIcon.sprite = targetImage.sprite;

        // 스택 가운데 슬롯 이미지 토글
        targetImage.enabled = !targetImage.enabled;

        // 마우스 커서에 아이콘 토글
        selectedIcon.enabled = !targetImage.enabled;

        //선택된 마법 아이콘 마우스 위치로 이동
        MousePos();
    }

    public void Scroll_Stack(int direction)
    {
        //마우스에 아이콘 들고 있을때 스크롤하면
        if (selectedIcon.enabled)
        {
            //커서 및 빈 스택 슬롯 초기화 하기
            SelectSlotToggle();
        }

        // 마법 이동 중이었으면
        if (moveMagicIndex != -1)
        {
            // 이동중이던 마법 슬롯 다시 켜기
            mergeList[moveMagicIndex].transform.Find("Icon").GetComponent<Image>().enabled = true;

            // 이동중 마법 인덱스 초기화
            moveMagicIndex = -1;
        }

        //모든 슬롯 domove 강제 즉시 완료
        foreach (var slot in stackObjSlots)
        {
            slot.transform.DOComplete();
        }

        int startSlotIndex = -1;
        int endSlotIndex = -1;

        // 방향이 있을때
        if (direction != 0)
            // 처음 로딩이 아닐때는 슬롯 인덱스 이동
            if (!loadingPanel.activeSelf)
            {
                //슬롯 오브젝트 리스트 인덱스 계산
                startSlotIndex = direction > 0 ? stackObjSlots.Count - 1 : 0;
                endSlotIndex = direction > 0 ? 0 : stackObjSlots.Count - 1;

                // 마지막 슬롯을 첫번째 인덱스 자리에 넣기
                GameObject targetSlot = stackObjSlots[startSlotIndex]; //타겟 오브젝트 얻기
                stackObjSlots.RemoveAt(startSlotIndex); //타겟 마법 삭제
                stackObjSlots.Insert(endSlotIndex, targetSlot); //타겟 마법 넣기

                // 마지막 슬롯은 반대편으로 즉시 이동
                targetSlot.GetComponent<RectTransform>().anchoredPosition = stackSlotPos[endSlotIndex];
            }

        // 모든 슬롯 오브젝트들을 slotPos 초기위치에 맞게 domove
        for (int i = 0; i < stackObjSlots.Count; i++)
        {
            RectTransform stackSlotRect = stackObjSlots[i].GetComponent<RectTransform>();

            Vector2 slotPos = stackSlotPos[i];

            //이미 domove 중이면 빠르게 움직이기
            // float moveTime = Vector2.Distance(rect.anchoredPosition, slotPos[i]) != 120f ? 0.1f : 0.5f;
            float moveTime = 0.2f;

            // Merge 슬롯으로 옮겨서 0번 마법이 스택에서 삭제됬을때
            if (selectedMagic != null && selectedMagic != PlayerManager.Instance.hasStackMagics[0] && direction != 0)
            {
                // 왼쪽 3개는 즉시 위치이동
                if (i == 0 || i == 1 || i == 2)
                {
                    // 정해진 위치로 즉시 이동
                    stackSlotRect.anchoredPosition = slotPos;

                    //자리에 맞게 사이즈 바꾸기
                    stackSlotRect.localScale = Vector3.one * 0.7f;
                }

                // 오른쪽 3개는 그대로 DOAnchorPos
                if (i == 3 || i == 4 || i == 5)
                {
                    // 한칸씩 옆으로 이동
                    stackSlotRect.DOAnchorPos(slotPos, moveTime)
                    .SetUpdate(true);

                    //자리에 맞게 사이즈 바꾸기
                    float scale = i == 3 ? 1f : 0.7f;
                    stackSlotRect.DOScale(scale, moveTime)
                    .SetUpdate(true);
                }

                //todo 가운데 애니메이션용 슬롯 활성화, 줄어들고 비활성화, 사이즈 초기화
            }
            // 0번 마법이 그대로 남아있을때
            else
            {
                // 한칸씩 옆으로 이동
                stackSlotRect.DOAnchorPos(slotPos, moveTime)
                .SetUpdate(true);

                //자리에 맞게 사이즈 바꾸기
                float scale = i == 3 ? 1f : 0.7f;
                stackSlotRect.DOScale(scale, moveTime)
                .SetUpdate(true);
            }

            //아이콘 알파값 바꾸기
            stackObjSlots[i].GetComponent<CanvasGroup>().alpha = i == 3 ? 1f : 0.5f;
        }

        // 스택이 비어있지 않을때
        if (PlayerManager.Instance.hasStackMagics.Count > 0)
        {
            // 0번 마법이 삭제 되지 않았을때
            if (selectedMagic == PlayerManager.Instance.hasStackMagics[0] && direction != 0)
            {
                //마법 데이터 리스트 인덱스 계산
                int startIndex = direction > 0 ? PlayerManager.Instance.hasStackMagics.Count - 1 : 0;
                int endIndex = direction > 0 ? 0 : PlayerManager.Instance.hasStackMagics.Count - 1;

                // 실제 데이터 hasStackMagics도 마지막 슬롯을 첫번째 인덱스 자리에 넣기
                MagicInfo targetMagic = PlayerManager.Instance.hasStackMagics[startIndex]; //타겟 마법 참조

                // 타겟 마법 정보 삭제
                PlayerManager.Instance.hasStackMagics.RemoveAt(startIndex);

                // 타겟 마법 정보 넣기
                PlayerManager.Instance.hasStackMagics.Insert(endIndex, targetMagic);
            }

            // 선택된 마법 입력
            selectedMagic = PlayerManager.Instance.hasStackMagics[0];
            // 선택된 마법 아이콘 이미지 넣기
            selectedIcon.sprite = MagicDB.Instance.GetMagicIcon(selectedMagic.id) == null ? SystemManager.Instance.questionMark : MagicDB.Instance.GetMagicIcon(selectedMagic.id);

            // 선택된 슬롯에 툴팁 넣어주기
            selectedTooltip.enabled = true;
            selectedTooltip.Magic = PlayerManager.Instance.hasStackMagics[0];
        }

        // 모든 아이콘 다시 넣기
        Set_Stack();
    }

    public void Set_Stack()
    {
        //마지막 이전 마법
        SetStackIcon(0, 4, PlayerManager.Instance.hasStackMagics.Count - 3);
        //마지막 마법
        SetStackIcon(1, 3, PlayerManager.Instance.hasStackMagics.Count - 2);
        //0번째 마법
        SetStackIcon(2, 2, PlayerManager.Instance.hasStackMagics.Count - 1);
        //1번째 마법
        SetStackIcon(3, 1, 0);
        //2번째 마법
        SetStackIcon(4, 2, 1);
        //3번째 마법
        SetStackIcon(5, 3, 2);
        //4번째 마법
        SetStackIcon(6, 4, 3);

        // 총 스택 개수 갱신
        stackAllNum.text = StackAmount().ToString();
    }

    void SetStackIcon(int objIndex, int num, int magicIndex)
    {
        // 프레임 찾기
        Image frame = stackObjSlots[objIndex].transform.Find("Frame").GetComponent<Image>();
        // 아이콘 찾기
        Image icon = stackObjSlots[objIndex].transform.Find("Icon").GetComponent<Image>();
        // 개수 텍스트 찾기
        TextMeshProUGUI amount = stackObjSlots[objIndex].transform.Find("Amount").GetComponentInChildren<TextMeshProUGUI>(true);

        // hasStackMagics의 보유 마법이 num 보다 많을때
        if (PlayerManager.Instance.hasStackMagics.Count >= num)
        {
            MagicInfo magic = PlayerManager.Instance.hasStackMagics[magicIndex];

            // 프레임 이미지 바꾸기, 0등급이면 아이템이므로 아이템 프레임
            frame.sprite = magic.grade == 0 ? itemFrame : magicFrame;
            //프레임 색 넣기
            frame.color = MagicDB.Instance.GradeColor[magic.grade];

            //아이콘 스프라이트 찾기
            Sprite sprite = MagicDB.Instance.GetMagicIcon(magic.id);
            //아이콘 넣기
            icon.enabled = true;
            icon.sprite = sprite == null ? SystemManager.Instance.questionMark : sprite;

            // 마법 개수 1개일때, 개수 오브젝트 비활성화
            if (magic.amount == 1)
                amount.transform.parent.gameObject.SetActive(false);
            else
                amount.transform.parent.gameObject.SetActive(true);

            // 마법 개수 넣기
            amount.enabled = true;
            amount.text = magic.amount.ToString();
        }
        //넣을 마법 없으면 아이콘 및 프레임 숨기기
        else
        {
            // 프레임, 아이콘, 레벨, 툴팁 끄기
            frame.color = Color.white;
            icon.enabled = false;
            // level.enabled = false;
        }
    }

    void Set_Recipe()
    {
        // 레시피 스크롤 컴포넌트 끄기
        recipeScroll.enabled = false;

        // 처음에만 오브젝트 생성
        if (!recipeInit)
            // 레시피 목록에 모든 마법 표시
            for (int i = 0; i < MagicDB.Instance.magicDB.Count; i++)
            {
                // 마법 정보 찾기
                MagicInfo magic = MagicDB.Instance.magicDB[i];

                // 0등급이면 넘기기
                if (magic.grade == 0)
                    continue;

                // 레시피 프리팹 생성
                LeanPool.Spawn(recipePrefab, recipeScroll.Content.transform);
            }

        // 레시피 목록에 모든 마법 표시
        for (int i = 0; i < recipeScroll.Content.childCount; i++)
        {
            // 마법 정보 찾기
            MagicInfo magic = MagicDB.Instance.magicDB[i + 6];

            // 레시피 아이템 찾기
            Transform recipe = recipeScroll.Content.GetChild(i);

            // 해당 마법 언락 여부 판단
            bool unlocked = MagicDB.Instance.unlockMagics.Exists(x => x == magic.id);

            // 재료들 정보 찾기
            MagicInfo elementA = MagicDB.Instance.GetMagicByName(magic.element_A);
            MagicInfo elementB = MagicDB.Instance.GetMagicByName(magic.element_B);

            // 메인 아이콘 및 프레임 찾기
            Image main_Icon = recipe.transform.Find("MagicSlot/Icon").GetComponent<Image>();
            Image main_Frame = recipe.transform.Find("MagicSlot/Frame").GetComponent<Image>();
            // 재료 아이콘 A,B 및 프레임 찾기
            Image elementA_Icon = recipe.transform.Find("Element_A/Icon").GetComponent<Image>();
            Image elementA_Frame = recipe.transform.Find("Element_A/Frame").GetComponent<Image>();
            Image elementB_Icon = recipe.transform.Find("Element_B/Icon").GetComponent<Image>();
            Image elementB_Frame = recipe.transform.Find("Element_B/Frame").GetComponent<Image>();

            // 메인 아이콘 컬러 초기화
            main_Icon.color = unlocked ? Color.white : Color.black;

            // 메인 아이콘 표시
            main_Icon.sprite = MagicDB.Instance.GetMagicIcon(magic.id);
            // 재료 아이콘 해금됬으면 표시, 아니면 물음표
            elementA_Icon.sprite = unlocked && elementA != null ? MagicDB.Instance.GetMagicIcon(elementA.id) : SystemManager.Instance.questionMark;
            elementB_Icon.sprite = unlocked && elementB != null ? MagicDB.Instance.GetMagicIcon(elementB.id) : SystemManager.Instance.questionMark;

            // 메인 아이콘 프레임 색 넣기
            main_Frame.color = MagicDB.Instance.GradeColor[magic.grade];
            // 재료 아이콘 해금됬으면 프레임 색 넣기
            elementA_Frame.color = unlocked && elementA != null ? MagicDB.Instance.GradeColor[elementA.grade] : Color.white;
            elementB_Frame.color = unlocked && elementB != null ? MagicDB.Instance.GradeColor[elementB.grade] : Color.white;

            // 메인 아이콘에 툴팁 넣기
            if (unlocked)
            {
                ToolTipTrigger main_tooltip = main_Icon.transform.parent.GetComponentInChildren<ToolTipTrigger>(true);
                ToolTipTrigger elementA_tooltip = elementA_Icon.transform.parent.GetComponentInChildren<ToolTipTrigger>(true);
                ToolTipTrigger elementB_tooltip = elementB_Icon.transform.parent.GetComponentInChildren<ToolTipTrigger>(true);

                main_tooltip.Magic = magic;
                main_tooltip.enabled = true;

                if (elementA != null && elementB != null)
                {
                    elementA_tooltip.Magic = elementA;
                    elementA_tooltip.enabled = true;
                    elementB_tooltip.Magic = elementB;
                    elementB_tooltip.enabled = true;
                }
            }
        }

        // 레시피 스크롤 컴포넌트 켜기
        recipeScroll.enabled = true;

        // 레시피 스크롤 위치 초기화
        recipeScroll.Content.localPosition = Vector2.zero;
        recipeScroll.GoToPanel(0);

        // 레시피 초기화 완료
        recipeInit = true;
    }

    public void Set_USB()
    {
        // USB 슬롯들을 담고있는 부모
        Transform usbParent = usbScroll.Content;

        int allUSBNum = 0;

        // 모든 USB 개수 갱신
        for (int i = 0; i < 6; i++)
        {
            int usbNum = 0;
            usbNum = PlayerManager.Instance.hasUSBList[i];

            //해당 등급의 usb 개수 합산
            allUSBNum += usbNum;

            // 슬롯에서 개수 text 찾아 개수 갱신
            usbParent.GetChild(i).Find("Amount").GetComponentInChildren<TextMeshProUGUI>().text = usbNum.ToString();
        }

        // 핸드폰 메뉴 버튼의 총 USB 개수 갱신
        usbAllNum.text = allUSBNum.ToString();

        // 화면 하단의 총 USB 개수 갱신
        UIManager.Instance.PhoneNotice(allUSBNum);

        // USB 슬롯들 사이즈 조절
        Scroll_USB();
    }

    public void Scroll_USB()
    {
        // 선택된 아이콘은 사이즈 크게, 나머지 아이콘은 사이즈 작게
        for (int i = 0; i < 6; i++)
        {
            // 선택된것만 큰 사이즈로
            float size = i == usbScroll.CenteredPanel ? 1f : 0.7f;

            // 모든 슬롯 사이즈 조절
            usbScroll.Content.GetChild(i).DOScale(size, 0.2f)
            .SetUpdate(true);
        }

        // 애니메이팅 아이콘에 색깔 갱신
        anim_USB_Slot.Find("Icon").GetComponent<Image>().color = MagicDB.Instance.GradeColor[usbScroll.CenteredPanel + 1];
        anim_USB_Slot.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.GradeColor[usbScroll.CenteredPanel + 1];
    }

    public void Use_USB()
    {
        StartCoroutine(GetUSBMagic());
    }

    public IEnumerator GetUSBMagic()
    {
        // 아이콘 찾기
        Transform icon = usbScroll.Content.GetChild(usbScroll.CenteredPanel).Find("Icon");
        // 아이콘 이미지 찾기
        Image iconImage = icon.GetComponent<Image>();

        // usb 개수 부족하면 리턴
        if (PlayerManager.Instance.hasUSBList[usbScroll.CenteredPanel] <= 0)
        {
            // 아이콘 트윈 정지            
            icon.DOPause();

            // 원래 위치 저장
            Vector2 originPos = icon.localPosition;

            // usb 아이콘 떨림 트윈
            icon.DOPunchPosition(Vector2.right * 30f, 1f, 10, 1)
            .SetEase(Ease.Linear)
            .OnPause(() =>
            {
                icon.localPosition = originPos;
            })
            .SetUpdate(true);

            yield break;
        }

        //! todo 나중에 메뉴 버튼도 단축키 대응 되면 뽑기 도중에 화면 스크롤 못하게 막아야함
        // 메뉴, 백 버튼 상호작용 및 키입력 막기
        InteractToggleBtns(false);

        // 뽑기 화면 전체 투명하게
        randomScreen.alpha = 0;
        // 뽑기 배경 활성화, 가려서 핸드폰 입력 막기
        randomScreen.gameObject.SetActive(true);

        // 뽑기 스크롤 그룹 투명하게
        CanvasGroup randomScrollGroup = randomScroll.GetComponent<CanvasGroup>();
        randomScrollGroup.alpha = 0;
        // 뽑기 스크롤 비활성화
        randomScroll.gameObject.SetActive(false);
        // 뽑기 스크롤 컴포넌트 비활성화
        randomScroll.enabled = false;

        // 해당 usb 개수 차감
        PlayerManager.Instance.hasUSBList[usbScroll.CenteredPanel]--;

        // 선택된 usb 개수, usb 총 개수, 화면 하단 usb 총 개수 UI 갱신
        Set_USB();

        // 모든 자식 비우기
        SystemManager.Instance.DestroyAllChild(randomScroll.Content);

        // 사용된 usb 아이콘 숨기기
        Color usbColor = iconImage.color;
        usbColor.a = 0;
        iconImage.DOColor(usbColor, 0.5f)
        .SetUpdate(true);

        // 애니메이션용 아이콘 스크린 가운데로 올라가기
        anim_USB_Slot.gameObject.SetActive(true);
        anim_USB_Slot.DOMove(randomScroll.transform.position, 0.5f)
        .SetEase(Ease.OutSine)
        .SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.5f);

        // 랜덤 마법 리스트
        List<MagicInfo> randomList = new List<MagicInfo>();

        // 모든 마법 정보 조회
        foreach (KeyValuePair<int, MagicInfo> value in MagicDB.Instance.magicDB)
        {
            // 선택된 usb와 등급이 같은 마법이면 
            if (value.Value.grade == usbScroll.CenteredPanel + 1)
            {
                // 랜덤 풀에 넣기
                randomList.Add(value.Value);
            }
        }

        //todo 해당 등급의 언락된 마법 리스트 불러오기
        // for (int i = 0; i < MagicDB.Instance.magicDB.Count; i++)
        // {
        // 언락된 마법의 id
        // int magicId = MagicDB.Instance.unlockMagics[i];

        // if (MagicDB.Instance.magicDB.TryGetValue(magicId, out MagicInfo magic))
        // {
        //     // 선택된 usb와 등급이 같은 마법이면 
        //     if (magic.grade == usbScroll.CenteredPanel)
        //     {
        //         // 랜덤 풀에 넣기
        //         randomList.Add(magic);
        //     }
        // }
        // }

        // 랜덤 마법 풀 개수만큼 반복
        for (int i = 0; i < randomList.Count; i++)
        {
            // 랜덤 스크롤 컨텐츠 자식으로 슬롯 넣기
            Transform magicSlot = LeanPool.Spawn(magicSlotPrefab, randomScroll.Content);

            // 마법 아이콘 넣기
            magicSlot.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.GetMagicIcon(randomList[i].id);
            // 프레임 색 넣기
            magicSlot.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.GradeColor[randomList[i].grade];
        }

        // 뽑기 화면 전체 나타내기
        DOTween.To(() => randomScreen.alpha, x => randomScreen.alpha = x, 1f, 0.5f)
        .SetUpdate(true);

        // 스냅 스크롤 컴포넌트 활성화
        randomScroll.enabled = true;
        // 뽑기 스크롤 활성화
        randomScroll.gameObject.SetActive(true);

        // 한번 굴려서 무한 스크롤 위치 초기화
        randomScroll.GoToNextPanel();

        // 애니메이션용 usb 슬롯 비활성화 및 위치 초기화
        anim_USB_Slot.gameObject.SetActive(false);
        anim_USB_Slot.position = usbScroll.transform.position;

        yield return new WaitForSecondsRealtime(0.5f);

        // 뽑기 스크롤 그룹 알파값 초기화
        DOTween.To(() => randomScrollGroup.alpha, x => randomScrollGroup.alpha = x, 1f, 0.5f)
        .SetUpdate(true);

        // 스크롤 끝나는 시간 계산
        float stopTime = Time.unscaledTime + Random.Range(minScrollTime, maxScrollTime);
        // 타이머 끝날때까지 빠르게 스크롤 반복 내리기
        while (stopTime > Time.unscaledTime)
        {
            // 끝날때쯤 점점 느려짐
            if (stopTime <= Time.unscaledTime + 1f)
            {
                // 스냅 스피드 계산
                float scrollSpeed = (stopTime - Time.unscaledTime) * randomScrollSpeed;
                scrollSpeed = Mathf.Clamp(scrollSpeed, 5f, randomScrollSpeed);

                randomScroll.SnapSpeed = scrollSpeed;
            }
            else
                randomScroll.SnapSpeed = randomScrollSpeed;

            randomScroll.GoToNextPanel();

            // 남은시간 1초 이상일때 1초로 만들기 (피지컬로 슬롯 맞추기 가능)
            if (UIManager.Instance.UI_Input.UI.Accept.triggered)
            {
                if (stopTime >= 1f)
                {
                    stopTime = Time.unscaledTime + 1f;
                    print("Skip : " + (stopTime - Time.unscaledTime));
                }
            }

            print(stopTime - Time.unscaledTime);

            yield return new WaitForSecondsRealtime(0.01f);
        }

        // 멈춘 후 딜레이
        yield return new WaitForSecondsRealtime(0.5f);

        // 랜덤풀에서 멈춘 시점 선택된 인덱스에 해당하는 마법 뽑기
        getMagic = randomList[randomScroll.CenteredPanel];
        print(getMagic.magicName);

        // 획득한 마법 스택에 넣기
        PlayerManager.Instance.GetMagic(getMagic);

        // 팡파레 이펙트 켜기
        slotRayEffect.gameObject.SetActive(true);
        // 애니메이터 속도 느리게
        slotRayEffect.speed = 0.1f;
        // 팡파레 이펙트 등급색으로 변경
        Image raySprite = slotRayEffect.GetComponent<Image>();
        raySprite.color = MagicDB.Instance.GradeColor[getMagic.grade];
        // 사이즈 키우기
        slotRayEffect.transform.localScale = Vector2.zero;
        slotRayEffect.transform.DOScale(Vector3.one * 2f, 0.2f)
        .SetUpdate(true);

        // 획득 파티클 색 변경
        ParticleSystem.MainModule particleMain = getMagicEffect.main;
        particleMain.startColor = MagicDB.Instance.GradeColor[getMagic.grade];

        // 획득 마법 초기화
        getMagic = null;

        // 끝난 후 아무키 누르거나 클릭하면 트랜지션 종료
        yield return new WaitUntil(() => UIManager.Instance.UI_Input.UI.Click.IsPressed() || UIManager.Instance.UI_Input.UI.Accept.IsPressed());

        // 팡파레 이펙트 끄기
        slotRayEffect.gameObject.SetActive(false);

        // 뽑힌 슬롯 끄기
        GameObject getSlot = randomScroll.Content.GetChild(randomScroll.CenteredPanel).gameObject;
        getSlot.SetActive(false);

        // 마법 획득 이펙트 켜기
        getMagicEffect.gameObject.SetActive(true);

        // 사용된 usb 아이콘 색깔 초기화
        usbColor.a = 1;
        iconImage.DOColor(usbColor, 0.5f)
        .SetUpdate(true);

        yield return new WaitForSecondsRealtime(1f);

        // 스택 개수 UI 갱신
        stackAllNum.text = StackAmount().ToString();

        // 뽑기 스크린 전체 투명하게
        DOTween.To(() => randomScreen.alpha, x => randomScreen.alpha = x, 0f, 1f)
        .SetUpdate(true);

        yield return new WaitForSecondsRealtime(1f);

        // 마법 획득 이펙트 끄기
        getMagicEffect.gameObject.SetActive(false);

        // 뽑기 스크린 비활성화
        randomScreen.gameObject.SetActive(false);

        // 뽑힌 슬롯 다시 켜기
        getSlot.SetActive(true);

        // 메뉴, 백 버튼 상호작용 및 키입력 막기 해제
        InteractToggleBtns(true);
    }

    #region ChooseModeToggle
    // public void ChooseModeToggle()
    // {
    //     //merge 선택 모드 토글
    //     mergeChooseMode = !mergeChooseMode;

    //     // 선택모드로 전환
    //     if (mergeChooseMode)
    //     {
    //         // 합성 가능한 슬롯 빼고 모두 상호작용 끄기
    //         for (int i = 0; i < mergeSlots.childCount; i++)
    //         {
    //             //버튼 찾기
    //             Button btn = mergeList[i].GetComponent<Button>();
    //             // 상호작용 끄기
    //             btn.interactable = false;

    //             foreach (int closeIndex in closeSlots)
    //             {
    //                 //주변 슬롯 인덱스와 같으면
    //                 if (closeIndex == i)
    //                 {
    //                     // 상호작용 켜기
    //                     btn.interactable = true;

    //                     // 배열 범위 내 인덱스일때
    //                     if (closeIndex >= 0 && closeIndex < PlayerManager.Instance.hasMergeMagics.Length)
    //                     {
    //                         //해당 방향의 슬롯 찾기
    //                         Transform closeSlot = mergeList[closeIndex].transform;
    //                         Transform closeIcon = closeSlot.Find("Icon");
    //                         Vector2 moveDir = mergeWaitSlot.transform.position - closeIcon.position;

    //                         // 아이콘이 타겟 슬롯 방향으로 조금씩 움직이려는 트윈
    //                         closeIcon.DOLocalMove(moveDir.normalized * 20f, 0.5f)
    //                         .OnKill(() =>
    //                         {
    //                             closeIcon.localPosition = Vector2.zero;
    //                         })
    //                         .SetEase(Ease.InOutSine)
    //                         .SetLoops(-1, LoopType.Yoyo)
    //                         .SetUpdate(true);

    //                         // 해당 슬롯들 반짝이기
    //                         Image backImg = closeSlot.GetComponent<Image>();
    //                         Color originColor = backImg.color;
    //                         backImg.DOColor(Color.white, 0.5f)
    //                         .SetUpdate(true)
    //                         .SetLoops(-1, LoopType.Yoyo)
    //                         .SetEase(Ease.InOutBack)
    //                         .OnKill(() =>
    //                         {
    //                             backImg.color = originColor;
    //                         });
    //                     }
    //                 }
    //             }
    //         }
    //         // 뒤로 버튼 반짝이기
    //         // Image backBtnImg = backBtn.GetComponent<Image>();
    //         // Color originBackColor = backBtnImg.color;
    //         // backBtnImg.DOColor(new Color(originBackColor.r, originBackColor.g * 2f, originBackColor.b * 2f, originBackColor.a), 0.5f)
    //         // .SetUpdate(true)
    //         // .SetLoops(-1, LoopType.Yoyo)
    //         // .SetEase(Ease.InOutBack)
    //         // .OnKill(() =>
    //         // {
    //         //     backBtnImg.color = originBackColor;
    //         // });

    //         // 백 버튼 상호작용 끄기
    //         backBtn.interactable = false;
    //         // 레시피 버튼 상호작용 끄기
    //         recipeBtn.interactable = false;
    //         // 홈 버튼 상호작용 끄기
    //         homeBtn.interactable = false;
    //         // 스택 가운데 버튼 상호작용 끄기
    //         selectedSlot.interactable = false;
    //         // 스택 좌우 스크롤 버튼 상호작용 끄기
    //         leftScrollBtn.interactable = false;
    //         rightScrollBtn.interactable = false;
    //     }
    //     // 선택모드 취소
    //     else
    //     {
    //         // 모든 Merge 슬롯 상호작용 켜기
    //         foreach (Transform slot in mergeSlots)
    //         {
    //             slot.GetComponent<Button>().interactable = true;
    //         }

    //         // 백 버튼 상호작용 켜기
    //         backBtn.interactable = true;
    //         // 레시피 버튼 상호작용 켜기
    //         recipeBtn.interactable = true;
    //         // 홈 버튼 상호작용 켜기
    //         homeBtn.interactable = true;
    //         //스택 가운데 버튼 상호작용 켜기
    //         selectedSlot.interactable = true;
    //         // 스택 좌우 스크롤 버튼 상호작용 끄기
    //         leftScrollBtn.interactable = true;
    //         rightScrollBtn.interactable = true;

    //         // 아이콘이 타겟 슬롯 방향으로 조금씩 움직이려는 트윈 종료
    //         foreach (int index in closeSlots)
    //         {
    //             // 배열 범위 내 인덱스일때
    //             if (index >= 0 && index < PlayerManager.Instance.hasMergeMagics.Length)
    //             {
    //                 // 해당 방향의 슬롯 찾기
    //                 Transform closeSlot = mergeList[index].transform;

    //                 // 슬롯 배경 찾아 트윈 멈추기
    //                 Image closeBack = closeSlot.GetComponent<Image>();
    //                 closeBack.DOKill();

    //                 Transform dirIcon = mergeSlots.GetChild(index).Find("Icon");
    //                 dirIcon.DOKill();
    //             }
    //         }

    //         // 스택 슬롯 초기화
    //         SelectSlotToggle();

    //         //백 버튼 깜빡임 종료
    //         // Image backBtnImg = backBtn.GetComponent<Image>();
    //         // backBtnImg.DOKill();
    //     }
    // }
    #endregion

    public void IconMoveStop()
    {
        // 아이콘이 타겟 슬롯 방향으로 조금씩 움직이려는 트윈 종료
        foreach (int index in closeSlots)
        {
            // 배열 범위 내 인덱스일때
            if (index >= 0 && index < PlayerManager.Instance.hasMergeMagics.Length)
            {
                // 아이콘 찾아 트윈 멈추기
                Transform closeIcon = mergeList[index].transform.Find("Icon");
                closeIcon.DOKill();
            }
        }
    }

    void InteractToggleBtns(bool able)
    {
        // 키 입력 막기 변수 토글
        btnsInteractable = able;

        // 메뉴 버튼 상호작용 토글
        recipeBtn.interactable = able;
        // 백 버튼 상호작용 토글
        backBtn.interactable = able;
    }

    public void ScreenScrollBtn()
    {
        // 키 입력 막기
        if (!btnsInteractable)
            return;

        // 선택된게 머지 스크린일때
        if (screenScroll.CenteredPanel == 0)
        {
            // 총 USB 개수 활성화
            usbAllNum.transform.parent.gameObject.SetActive(true);
            // 총 스택 개수 비활성화
            stackAllNum.transform.parent.gameObject.SetActive(false);
        }
        // 선택된게 레시피 스크린일때
        else
        {
            // 총 USB 개수 비활성화
            usbAllNum.transform.parent.gameObject.SetActive(false);
            // 총 스택 개수 활성화
            stackAllNum.transform.parent.gameObject.SetActive(true);

            // 레시피 갱신
            Set_Recipe();
        }
    }

    public void BackBtn()
    {
        //백 버튼 액션 실행
        StartCoroutine(BackBtnAction());
    }

    public IEnumerator BackBtnAction()
    {
        // 머지 패널 꺼져있으면 리턴
        if (!phonePanel.activeSelf)
            yield break;

        // 키 입력 막기
        if (!btnsInteractable)
            yield break;

        // MergeChoose 모드일때
        // if (mergeChooseMode)
        // {
        //     // 선택했던 Merge 슬롯 비우기
        //     mergeWaitSlot.frame.color = Color.white;
        //     mergeWaitSlot.icon.enabled = false;
        //     mergeWaitSlot.level.enabled = false;
        //     mergeWaitSlot.tooltip.enabled = false;
        //     PlayerManager.Instance.hasMergeMagics[mergeWaitSlot.transform.GetSiblingIndex()] = null;

        //     // 선택 모드 취소하기
        //     ChooseModeToggle();
        //     return;
        // }

        // 메인 Merge 화면일때
        if (backBtnCount <= 0)
        {
            //버튼 시간 카운트 시작
            backBtnCount = 1f;

            // 한번 누르면 시간 재면서 버튼 절반 색 채우기
            DOTween.To(() => backBtnFill.fillAmount, x => backBtnFill.fillAmount = x, 0.5f, 0.2f)
            .SetUpdate(true);
        }
        // 시간 내에 한번 더 누르면 팝업 종료
        else
        {
            //TODO 모든 버튼 상호작용 끄기
            // merge 슬롯 모두 끄기
            foreach (Button mergeSlot in mergeList)
            {
                mergeSlot.interactable = false;
            }
            //스택 가운데 버튼 끄기
            selectedSlot.interactable = false;
            //스택 좌,우 버튼 끄기
            leftScrollBtn.interactable = false;
            rightScrollBtn.interactable = false;
            //레시피 버튼 끄기
            recipeBtn.interactable = false;
            //뒤로 버튼 끄기
            backBtn.interactable = false;
            //홈 버튼 끄기
            homeBtn.interactable = false;

            //마우스로 아이콘 들고 있으면 복귀시키기
            if (selectedIcon.enabled)
                SelectSlotToggle();

            // UI 커서 미리 끄기
            UIManager.Instance.UICursorToggle(false);

            // 로딩 패널 켜기
            loadingPanel.SetActive(true);

            // 백버튼 색 채우기
            DOTween.To(() => backBtnFill.fillAmount, x => backBtnFill.fillAmount = x, 1f, 0.2f)
            .SetUpdate(true);

            // 화면 검은색으로
            blackScreen.DOColor(new Color(70f / 255f, 70f / 255f, 70f / 255f, 1), 0.2f)
            .SetUpdate(true);

            // 화면 꺼지는 동안 대기
            yield return new WaitForSecondsRealtime(0.2f);

            // 핸드폰 화면 패널 끄기
            phonePanel.SetActive(false);

            float moveTime = 0.8f;

            // 매직폰 위치,회전,스케일로 복구하기
            CastMagic.Instance.transform.DOMove(phonePosition, moveTime)
            .SetUpdate(true);
            CastMagic.Instance.transform.DOScale(phoneScale, moveTime)
            .SetUpdate(true);
            CastMagic.Instance.transform.DORotate(new Vector3(0, 360f, 0), moveTime, RotateMode.WorldAxisAdd)
            .SetUpdate(true);

            // 절반쯤 이동했을때 화면 라이트 켜기
            lightScreen.DOColor(new Color(30f / 255f, 1f, 1f, 100f / 255f), moveTime / 2f)
            .SetDelay(moveTime / 2f)
            .SetUpdate(true);

            // 핸드폰 이동하는 동안 대기
            yield return new WaitForSecondsRealtime(moveTime);

            //백 버튼 변수 초기화
            backBtnCount = 0f;
            backBtnFill.fillAmount = 0f;

            // 끝나면 시간 복구하기
            Time.timeScale = 1f;

            //스마트폰 캔버스 종료
            UIManager.Instance.PopupUI(UIManager.Instance.phoneCanvas);

            // Merge 리스트에서 확인해서 필요한 마법 시전하기
            CastMagic.Instance.CastCheck();
        }
    }
}
