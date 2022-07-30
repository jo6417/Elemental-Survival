using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lean.Pool;
using DG.Tweening;
using UnityEngine.EventSystems;

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
                else
                {
                    var newObj = new GameObject().AddComponent<MergeMenu>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    [Header("Refer")]
    public GameObject mergePanel; // 핸드폰 화면 패널
    public Button recipeBtn;
    public Button backBtn;
    public Button homeBtn;
    public SpriteRenderer lightScreen; // 폰 스크린 전체 빛내는 HDR 이미지
    public Image blackScreen; // 폰 작아질때 검은 이미지로 화면 가리기
    public GameObject loadingPanel; //로딩 패널, 로딩중 뒤의 버튼 상호작용 막기

    [Header("Effect")]
    public Vector3 phonePosition; //핸드폰일때 위치 기억
    public Vector3 phoneRotation; //핸드폰일때 회전값 기억
    public Vector3 phoneScale; //핸드폰일때 고정된 스케일
    public Vector3 UIPosition; //팝업일때 위치
    public SlicedFilledImage backBtnFill; //뒤로가기 버튼
    float backBtnCount; //백버튼 더블클릭 카운트

    [Header("Merge Board")]
    public Transform mergeSlots;
    public List<Button> mergeList = new List<Button>(); //각각 슬롯 오브젝트
    public MergeSlot nowSelectSlot; //현재 선택된 슬롯
    public MergeSlot mergeWaitSlot; // 합성 대기중인 슬롯
    public int[] closeSlots = new int[4]; //선택된 슬롯 주변의 인덱스
    public int[] mergeResultMagics = new int[4]; //합성 가능한 마법 id
    public Transform mergeSignal;
    public Transform mergeIcon; //Merge domove 할때 대신 날아갈 아이콘 (슬롯간 레이어 문제로 사용)
    public bool mergeChooseMode = false; //마법 놓았을때 합성 가능성이 여러개일때, 선택모드 켜기

    [Header("Stack List")]
    public Transform stackSlots;
    public List<GameObject> stackList = new List<GameObject>(); //각각 슬롯 오브젝트
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
            stackList.Add(stackSlots.transform.GetChild(i).gameObject);
            // 슬롯들의 초기 위치 넣기
            stackSlotPos[i] = stackList[i].GetComponent<RectTransform>().anchoredPosition;
            // print(slotPos[i]);
        }

        selectedIconRect = selectedIcon.GetComponent<RectTransform>();

        // 키 입력 정리
        InputInit();
    }

    void InputInit()
    {
        //플레이어 인풋 끄기
        PlayerManager.Instance.playerInput.Disable();

        // 방향키 입력
        UIManager.Instance.UI_Input.UI.NavControl.performed += val => NavControl(val.ReadValue<Vector2>());
        // 마우스 위치 입력
        UIManager.Instance.UI_Input.UI.MousePosition.performed += val => MousePos(val.ReadValue<Vector2>());

        // 스마트폰 버튼 입력
        UIManager.Instance.UI_Input.UI.PhoneMenu.performed += val =>
        {
            // 로딩 패널 꺼져있을때, 머지 선택 모드 아닐때
            if (!loadingPanel.activeSelf && !mergeChooseMode)
            {
                //백 버튼 액션 실행
                StartCoroutine(BackBtnAction());
            }
        };
    }

    // 방향키 입력되면 실행
    void NavControl(Vector2 arrowDir)
    {
        // 머지 패널 꺼져있으면 리턴
        if (!gameObject.activeSelf)
            return;

        //마우스에 아이콘 들고 있을때
        if (selectedIcon.enabled)
        {
            //커서 및 빈 스택 슬롯 초기화 하기
            ToggleStackSlot();
        }

        //쿨타임 가능할때, 스택 슬롯 Select 됬을때
        if (scrollCoolCount <= 0f && nowSelectSlot != null && UIManager.Instance.lastSelected.gameObject == selectedSlot.gameObject)
        {
            // 왼쪽으로 스크롤하기
            if (arrowDir.x < 0)
            {
                ScrollSlots(false);
                scrollCoolCount = scrollCoolTime;
            }

            // 오른쪽으로 스크롤하기
            if (arrowDir.x > 0)
            {
                ScrollSlots(true);
                scrollCoolCount = scrollCoolTime;
            }
        }
    }

    // 마우스 위치 입력되면 실행
    void MousePos(Vector2 mousePosInput)
    {
        // 머지 패널 꺼져있으면 리턴
        if (!gameObject.activeSelf)
            return;

        // print(mousePosInput);

        if (selectedIcon.enabled)
        {
            //선택된 마법 아이콘 마우스 따라다니기
            Vector3 mousePos = mousePosInput;
            mousePos.z = 0;

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

        //마법 DB 로딩 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //머지 보드 세팅
        MergeSet();

        // 스택 슬롯 세팅
        SetSlots();
        // 스택 슬롯 사이즈 및 위치 정렬
        ScrollSlots(true);

        // 선택 아이콘 끄기
        selectedIcon.enabled = false;

        //Merge 인디케이터 끄기
        mergeSignal.gameObject.SetActive(false);

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
        mergePanel.SetActive(true);

        // 검은화면 투명하게
        blackScreen.DOColor(Color.clear, 0.2f)
        .SetUpdate(true);

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
        nav.selectOnUp = stackList[3].GetComponent<Button>().FindSelectable(Vector3.up);
        selectedSlot.navigation = nav;

        //트윈 끝날때까지 대기
        yield return new WaitUntil(() => CastMagic.Instance.transform.localScale == Vector3.one);

        //모든 슬롯 shiny 효과 순차적으로 켜기
        for (int i = 0; i < mergeSlots.childCount; i++)
        {
            GameObject shinyObj = mergeList[i].transform.Find("ShinyMask").gameObject;
            shinyObj.SetActive(false);
            shinyObj.SetActive(true);

            yield return new WaitForSecondsRealtime(0.01f);
        }

        // 휴대폰 로딩 화면 끄기
        LoadingToggle(false);

        // TODO 게임 시작할때는 기본 마법 1개 어느 슬롯에 놓을지 선택
        // TODO 선택하면 배경 사라지고, 휴대폰 플레이어 위로 작아지며 날아간 후에 게임 시작
    }

    void LoadingToggle(bool isLoading)
    {
        UIManager.Instance.phoneLoading = isLoading;

        loadingPanel.SetActive(isLoading);
    }

    void MergeSet()
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
            TextMeshProUGUI level = icon.transform.Find("Level").GetComponent<TextMeshProUGUI>();
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
                level.enabled = false;

                continue;
            }
            else
            {
                //아이콘 및 레벨 활성화
                icon.enabled = true;
                level.enabled = true;
            }

            //등급 프레임 색 넣기
            frame.color = MagicDB.Instance.gradeColor[magic.grade];
            //아이콘 넣기
            icon.sprite = MagicDB.Instance.GetMagicIcon(magic.id) == null ? SystemManager.Instance.questionMark : MagicDB.Instance.GetMagicIcon(magic.id);
            //레벨 넣기
            level.text = "Lv. " + magic.magicLevel.ToString();
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
    }

    public void ToggleStackSlot()
    {
        //빈 슬롯이면 리턴
        if (PlayerManager.Instance.hasStackMagics.Count == 0)
            return;

        //마우스 꺼져있으면 리턴
        if (Cursor.lockState == CursorLockMode.Locked)
            return;

        Image targetImage = stackList[3].transform.Find("Icon").GetComponent<Image>();

        // 스택 가운데 슬롯 이미지 토글
        targetImage.enabled = !targetImage.enabled;

        // 마우스 커서에 아이콘 토글
        selectedIcon.enabled = !targetImage.enabled;

        //선택된 마법 아이콘 마우스 위치로 이동
        Vector3 mousePos = UIManager.Instance.nowMousePos;
        mousePos.z = 0;
        selectedIconRect.anchoredPosition = mousePos;
    }

    public void ScrollSlots(bool isLeft)
    {
        //마우스에 아이콘 들고 있을때 스크롤하면
        if (selectedIcon.enabled)
        {
            //커서 및 빈 스택 슬롯 초기화 하기
            ToggleStackSlot();
        }

        //모든 슬롯 domove 강제 즉시 완료
        foreach (var slot in stackList)
        {
            slot.transform.DOComplete();
        }

        int startSlotIndex = -1;
        int endSlotIndex = -1;

        // 처음 로딩이 아닐때는 슬롯 인덱스 이동
        if (!loadingPanel.activeSelf)
        {
            //슬롯 오브젝트 리스트 인덱스 계산
            startSlotIndex = isLeft ? stackList.Count - 1 : 0;
            endSlotIndex = isLeft ? 0 : stackList.Count - 1;

            // 마지막 슬롯을 첫번째 인덱스 자리에 넣기
            GameObject targetSlot = stackList[startSlotIndex]; //타겟 오브젝트 얻기
            stackList.RemoveAt(startSlotIndex); //타겟 마법 삭제
            stackList.Insert(endSlotIndex, targetSlot); //타겟 마법 넣기

            // 마지막 슬롯은 slotPos[0] 으로 이동
            targetSlot.GetComponent<RectTransform>().anchoredPosition = stackSlotPos[endSlotIndex];
        }

        // 모든 슬롯 오브젝트들을 slotPos 초기위치에 맞게 domove
        for (int i = 0; i < stackList.Count; i++)
        {
            RectTransform rect = stackList[i].GetComponent<RectTransform>();

            //이미 domove 중이면 빠르게 움직이기
            // float moveTime = Vector2.Distance(rect.anchoredPosition, slotPos[i]) != 120f ? 0.1f : 0.5f;
            float moveTime = 0.2f;

            //한칸 옆으로 위치 이동
            if (endSlotIndex != i && endSlotIndex != -1)
                rect.DOAnchorPos(stackSlotPos[i], moveTime)
                .SetUpdate(true);

            //자리에 맞게 사이즈 바꾸기
            float scale = i == 3 ? 1f : 0.7f;
            rect.DOScale(scale, moveTime)
            .SetUpdate(true);

            //아이콘 알파값 바꾸기
            stackList[i].GetComponent<CanvasGroup>().alpha = i == 3 ? 1f : 0.5f;
        }

        if (PlayerManager.Instance.hasStackMagics.Count > 0)
        {
            //마법 데이터 리스트 인덱스 계산
            int startIndex = isLeft ? PlayerManager.Instance.hasStackMagics.Count - 1 : 0;
            int endIndex = isLeft ? 0 : PlayerManager.Instance.hasStackMagics.Count - 1;

            // 실제 데이터 hasStackMagics도 마지막 슬롯을 첫번째 인덱스 자리에 넣기
            MagicInfo targetMagic = PlayerManager.Instance.hasStackMagics[startIndex]; //타겟 마법 참조
            PlayerManager.Instance.hasStackMagics.RemoveAt(startIndex); //타겟 마법 정보 삭제
            PlayerManager.Instance.hasStackMagics.Insert(endIndex, targetMagic); //타겟 마법 정보 넣기

            // 선택된 마법 입력
            selectedMagic = PlayerManager.Instance.hasStackMagics[0];
            // 선택된 마법 아이콘 이미지 넣기
            selectedIcon.sprite = MagicDB.Instance.GetMagicIcon(selectedMagic.id) == null ? SystemManager.Instance.questionMark : MagicDB.Instance.GetMagicIcon(selectedMagic.id);

            // 선택된 슬롯에 툴팁 넣어주기
            selectedTooltip.enabled = true;
            selectedTooltip.Magic = PlayerManager.Instance.hasStackMagics[0];
        }

        // 모든 아이콘 다시 넣기
        SetSlots();
    }

    void SetSlots()
    {
        //마지막 이전 마법
        SetIcon(0, 4, PlayerManager.Instance.hasStackMagics.Count - 3);
        //마지막 마법
        SetIcon(1, 3, PlayerManager.Instance.hasStackMagics.Count - 2);
        //0번째 마법
        SetIcon(2, 2, PlayerManager.Instance.hasStackMagics.Count - 1);
        //1번째 마법
        SetIcon(3, 1, 0);
        //2번째 마법
        SetIcon(4, 2, 1);
        //3번째 마법
        SetIcon(5, 3, 2);
        //4번째 마법
        SetIcon(6, 4, 3);
    }

    void SetIcon(int objIndex, int num, int magicIndex)
    {
        //프레임 찾기
        Image frame = stackList[objIndex].transform.Find("Frame").GetComponent<Image>();
        //아이콘 찾기
        Image icon = stackList[objIndex].transform.Find("Icon").GetComponent<Image>();
        //레벨 찾기
        TextMeshProUGUI level = icon.transform.Find("Level").GetComponent<TextMeshProUGUI>();

        // hasStackMagics의 보유 마법이 num 보다 많을때
        if (PlayerManager.Instance.hasStackMagics.Count >= num)
        {
            MagicInfo magic = PlayerManager.Instance.hasStackMagics[magicIndex];

            //프레임 색 넣기
            frame.color = MagicDB.Instance.gradeColor[magic.grade];

            //아이콘 스프라이트 찾기
            Sprite sprite = MagicDB.Instance.GetMagicIcon(magic.id);
            //아이콘 넣기
            icon.enabled = true;
            icon.sprite = sprite == null ? SystemManager.Instance.questionMark : sprite;

            //레벨 넣기
            level.enabled = true;
            level.text = "Lv. " + magic.magicLevel;
        }
        //넣을 마법 없으면 아이콘 및 프레임 숨기기
        else
        {
            // 프레임, 아이콘, 레벨, 툴팁 끄기
            frame.color = Color.white;
            icon.enabled = false;
            level.enabled = false;
        }
    }

    public void ChooseModeToggle()
    {
        //merge 선택 모드 토글
        mergeChooseMode = !mergeChooseMode;

        // 선택모드로 전환
        if (mergeChooseMode)
        {
            // 합성 가능한 슬롯 빼고 모두 상호작용 끄기
            for (int i = 0; i < mergeSlots.childCount; i++)
            {
                //버튼 찾기
                Button btn = mergeList[i].GetComponent<Button>();
                // 상호작용 끄기
                btn.interactable = false;

                foreach (int closeIndex in closeSlots)
                {
                    //주변 슬롯 인덱스와 같으면
                    if (closeIndex == i)
                    {
                        // 상호작용 켜기
                        btn.interactable = true;

                        // 배열 범위 내 인덱스일때
                        if (closeIndex >= 0 && closeIndex < PlayerManager.Instance.hasMergeMagics.Length)
                        {
                            //해당 방향의 슬롯 찾기
                            Transform closeSlot = mergeList[closeIndex].transform;
                            Transform closeIcon = closeSlot.Find("Icon");
                            Vector2 moveDir = mergeWaitSlot.transform.position - closeIcon.position;

                            // 아이콘이 타겟 슬롯 방향으로 조금씩 움직이려는 트윈
                            closeIcon.DOLocalMove(moveDir.normalized * 20f, 0.5f)
                            .OnKill(() =>
                            {
                                closeIcon.localPosition = Vector2.zero;
                            })
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetUpdate(true);

                            // 해당 슬롯들 반짝이기
                            Image backImg = closeSlot.GetComponent<Image>();
                            Color originColor = backImg.color;
                            backImg.DOColor(Color.white, 0.5f)
                            .SetUpdate(true)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetEase(Ease.InOutBack)
                            .OnKill(() =>
                            {
                                backImg.color = originColor;
                            });
                        }
                    }
                }
            }
            // 뒤로 버튼 반짝이기
            // Image backBtnImg = backBtn.GetComponent<Image>();
            // Color originBackColor = backBtnImg.color;
            // backBtnImg.DOColor(new Color(originBackColor.r, originBackColor.g * 2f, originBackColor.b * 2f, originBackColor.a), 0.5f)
            // .SetUpdate(true)
            // .SetLoops(-1, LoopType.Yoyo)
            // .SetEase(Ease.InOutBack)
            // .OnKill(() =>
            // {
            //     backBtnImg.color = originBackColor;
            // });

            // 백 버튼 상호작용 끄기
            backBtn.interactable = false;
            // 레시피 버튼 상호작용 끄기
            recipeBtn.interactable = false;
            // 홈 버튼 상호작용 끄기
            homeBtn.interactable = false;
            // 스택 가운데 버튼 상호작용 끄기
            selectedSlot.interactable = false;
            // 스택 좌우 스크롤 버튼 상호작용 끄기
            leftScrollBtn.interactable = false;
            rightScrollBtn.interactable = false;
        }
        // 선택모드 취소
        else
        {
            // 모든 Merge 슬롯 상호작용 켜기
            foreach (Transform slot in mergeSlots)
            {
                slot.GetComponent<Button>().interactable = true;
            }

            // 백 버튼 상호작용 켜기
            backBtn.interactable = true;
            // 레시피 버튼 상호작용 켜기
            recipeBtn.interactable = true;
            // 홈 버튼 상호작용 켜기
            homeBtn.interactable = true;
            //스택 가운데 버튼 상호작용 켜기
            selectedSlot.interactable = true;
            // 스택 좌우 스크롤 버튼 상호작용 끄기
            leftScrollBtn.interactable = true;
            rightScrollBtn.interactable = true;

            // 아이콘이 타겟 슬롯 방향으로 조금씩 움직이려는 트윈 종료
            foreach (int index in closeSlots)
            {
                // 배열 범위 내 인덱스일때
                if (index >= 0 && index < PlayerManager.Instance.hasMergeMagics.Length)
                {
                    // 해당 방향의 슬롯 찾기
                    Transform closeSlot = mergeList[index].transform;

                    // 슬롯 배경 찾아 트윈 멈추기
                    Image closeBack = closeSlot.GetComponent<Image>();
                    closeBack.DOKill();

                    Transform dirIcon = mergeSlots.GetChild(index).Find("Icon");
                    dirIcon.DOKill();
                }
            }

            // 스택 슬롯 초기화
            ToggleStackSlot();

            //백 버튼 깜빡임 종료
            // Image backBtnImg = backBtn.GetComponent<Image>();
            // backBtnImg.DOKill();
        }
    }

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

    // Back 버튼 누르면
    public IEnumerator BackBtnAction()
    {
        // 머지 패널 꺼져있으면 리턴
        if (!mergePanel.activeSelf)
            yield break;

        //TODO 레시피 화면일때
        //TODO 메인화면으로 전환

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
                ToggleStackSlot();

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
            mergePanel.SetActive(false);

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
            UIManager.Instance.PopupUI(UIManager.Instance.mergeMagicPanel);

            // Merge 리스트에서 확인해서 필요한 마법 시전하기
            CastMagic.Instance.CastCheck();
        }
    }
}
