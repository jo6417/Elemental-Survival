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
using System.Text;

public class PhoneMenu : MonoBehaviour
{
    #region Singleton
    private static PhoneMenu instance;
    public static PhoneMenu Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<PhoneMenu>();
                if (obj != null)
                {
                    instance = obj;
                }
                // else
                // {
                //     var newObj = new GameObject().AddComponent<PhoneMenu>();
                //     instance = newObj;
                // }
            }
            return instance;
        }
    }
    #endregion

    [Header("State")]
    public bool phoneOpen = false; // 현재 핸드폰 메뉴 켬 여부
    private bool btnsInteractable = true; // 버튼 상호작용 가능 여부
    float backBtnCount; //백버튼 더블클릭 카운트
    bool isSkipped = false;// 스킵 버튼 누름 여부
    public Vector3 phonePosition; //핸드폰일때 위치 기억
    public Vector3 phoneRotation; //핸드폰일때 회전값 기억
    public Vector3 phoneScale; //핸드폰일때 고정된 스케일
    public Vector3 UIPosition; //팝업일때 위치

    [Header("Refer")]
    public NewInput Phone_Input; // 입력 받기
    public SimpleScrollSnap screenScroll; // 화면 스크롤
    public GameObject phonePanel; // 핸드폰 화면 패널
    [SerializeField] GameObject invenScreen; // 머지 페이지
    [SerializeField] GameObject recipeScreen; // 레시피 페이지
    public Button recipeBtn;
    public Button backBtn;
    public Button homeBtn;
    public SpriteRenderer lightScreen; // 폰 스크린 전체 빛내는 HDR 이미지
    public Image blackScreen; // 폰 작아질때 검은 이미지로 화면 가리기
    public GameObject loadingPanel; //로딩 패널, 로딩중 뒤의 버튼 상호작용 막기
    public SlicedFilledImage backBtnFill; //뒤로가기 버튼

    [Header("Chat List")]
    public float sumChatHeights; // 채팅 스크롤 content의 높이
    public GameObject chatPrefab; // 채팅 프리팹
    public ScrollRect chatScroll; // 채팅 스냅 스크롤
    [SerializeField] private RectTransform chatContentRect;

    [Header("Inventory")]
    int mergeAbleNum; // 현재 합성 가능한 마법 개수
    public Image invenBackground; // 인벤토리 뒷배경 이미지
    public Transform invenParent; // 인벤토리 슬롯들 부모 오브젝트
    public List<InventorySlot> invenSlots = new List<InventorySlot>(); //각각 슬롯 오브젝트
    public InventorySlot nowSelectSlot; // 현재 선택된 슬롯
    public SlotInfo nowSelectSlotInfo; // 현재 선택된 슬롯 정보    
    RectTransform nowSelectIconRect;

    [Header("Merge Panel")]
    public Transform mergePanel;
    public InventorySlot L_MergeSlot; // 왼쪽 재료 슬롯
    public InventorySlot R_MergeSlot; // 오른쪽 재료 슬롯
    public InventorySlot mergedSlot; // 합성된 마법 슬롯
    public GameObject plusIcon; // 가운데 플러스 아이콘
    public ParticleManager mergeBeforeEffect; // 합성 준비 이펙트
    public ParticleSystem mergeFailEffect; // 합성 실패 이펙트
    public Image mergeAfterEffect; // 합성 완료 이펙트

    [Header("Recipe List")]
    public bool recipeInit = false;
    public SimpleScrollSnap recipeScroll; // 레시피 슬롯 스크롤
    public GameObject recipePrefab; // 단일 레시피 프리팹
    [SerializeField] public Button recipeUpBtn; // 레시피 위로 스크롤
    [SerializeField] public Button recipeDownBtn; // 레시피 아래로 스크롤

    [Header("Random Panel")]
    public float randomScroll_Speed = 15f; // 뽑기 스크롤 속도
    public float minScrollTime = 3f; // 슬롯머신 최소 시간
    public float maxScrollTime = 5f; // 슬롯머신 최대 시간
    public Transform animSlot; // 애니메이션용 슬롯
    public CanvasGroup randomScreen; // 뽑기 스크린
    public SimpleScrollSnap randomScroll; // 마법 뽑기 랜덤 스크롤
    public Transform magicSlotPrefab; // 랜덤 마법 아이콘 프리팹
    public Animator slotRayEffect; // 슬롯 팡파레 이펙트
    public ParticleSystem rankUpEffect; // 등급 업 직전 이펙트
    public ParticleSystem getMagicEffect; // 뽑은 마법 획득 이펙트
    public GameObject particleAttractor; // getMagicEffect 파티클 빨아들이는 오브젝트
    public ParticleSystem rankUpSuccessEffect; // 랭크업 성공 이펙트
    public ParticleSystem rankUpFailEffect; // 랭크업 실패 이펙트

    private void Awake()
    {
        StartCoroutine(AwakeInit());
    }

    IEnumerator AwakeInit()
    {
        // 키 입력 정리
        StartCoroutine(InputInit());

        // 인벤토리 슬롯 컴포넌트 모두 저장
        for (int i = 0; i < invenParent.childCount; i++)
        {
            InventorySlot invenSlot = invenParent.GetChild(i).GetComponent<InventorySlot>();

            invenSlots.Add(invenSlot);
        }

        yield return new WaitUntil(() => UIManager.Instance != null);

        // 선택된 마법 rect 찾기
        nowSelectIconRect = UIManager.Instance.nowSelectIcon.transform.parent.GetComponent<RectTransform>();
    }

    private void Start()
    {
        // 스크롤 컨텐츠 모두 비우기
        SystemManager.Instance.DestroyAllChild(recipeScroll.Content);
    }

    IEnumerator InputInit()
    {
        Phone_Input = new NewInput();

        // 플레이어 초기화 대기
        yield return new WaitUntil(() => PlayerManager.Instance && PlayerManager.Instance.initFinish);

        // 방향키 입력
        Phone_Input.UI.NavControl.performed += val =>
        {
            // 핸드폰 오브젝트 있을때
            if (PhoneMenu.Instance != null)
                NavControl(val.ReadValue<Vector2>());
        };
        // 마우스 위치 입력
        Phone_Input.UI.MousePosition.performed += val =>
        {
            // 핸드폰 오브젝트 있을때
            if (PhoneMenu.Instance != null)
                MousePos();
        };
        // 마우스 클릭
        Phone_Input.UI.Click.performed += val =>
        {
            // 핸드폰 오브젝트 있을때
            if (PhoneMenu.Instance != null)
                StartCoroutine(PhoneMenu.Instance.CancelMoveItem());
        };
        // 마우스 휠 스크롤
        Phone_Input.UI.MouseWheel.performed += val =>
        {
            // 핸드폰 오브젝트 있을때
            if (PhoneMenu.Instance != null)
                // 마우스 휠 입력하면 레시피 스크롤 하기
                if (val.ReadValue<Vector2>().y > 0)
                    recipeUpBtn.onClick.Invoke();
                else
                    recipeDownBtn.onClick.Invoke();
        };

        // 스마트폰 버튼 입력
        Phone_Input.UI.PhoneMenu.performed += val =>
        {
            // 핸드폰 오브젝트 있을때
            if (PhoneMenu.Instance != null)
                // 로딩 패널 꺼져있을때
                if (!loadingPanel.activeSelf)
                {
                    //백 버튼 액션 실행
                    StartCoroutine(BackBtnAction());
                }
        };

        Phone_Input.Enable();

        // 핸드폰 패널 끄기
        phonePanel.SetActive(false);
    }

    public IEnumerator CancelMoveItem()
    {
        // 클릭시 select 오브젝트 바뀔때까지 1프레임 대기
        yield return new WaitForSeconds(Time.deltaTime);

        //마우스에 아이콘 들고 있을때
        if (UIManager.Instance.nowSelectIcon.enabled)
        {
            // null 선택했을때, 메뉴버튼, 백버튼, 홈버튼 클릭했을때
            if (EventSystem.current.currentSelectedGameObject == null
            || EventSystem.current.currentSelectedGameObject == recipeBtn.gameObject
            || EventSystem.current.currentSelectedGameObject == backBtn.gameObject
            || EventSystem.current.currentSelectedGameObject == homeBtn.gameObject)
            {
                //커서 및 빈 스택 슬롯 초기화 하기
                CancelSelectSlot();
            }
        }
    }

    // 방향키 입력되면 실행
    public void NavControl(Vector2 arrowDir)
    {
        // 머지 패널 꺼져있으면 리턴
        if (!gameObject.activeSelf)
            return;

        //마우스에 아이콘 들고 있을때
        if (UIManager.Instance.nowSelectIcon.enabled)
            //커서 및 빈 스택 슬롯 초기화 하기
            CancelSelectSlot();
    }

    // 마우스 위치 입력되면 실행
    public void MousePos()
    {
        // print(mousePosInput);

        if (UIManager.Instance.nowSelectIcon.enabled)
        {
            // 캔버스 스케일을 해상도로 나눈 비율을 곱해서 마우스 위치값 보정
            Vector3 mousePos = UIManager.Instance.nowMousePos * (GetComponent<CanvasScaler>().referenceResolution.x / Screen.width);
            mousePos.z = 0;

            // 선택된 마법 아이콘 마우스 따라다니기
            nowSelectIconRect.anchoredPosition = mousePos;
        }
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    private void OnDestroy()
    {
        if (Phone_Input != null)
        {
            Phone_Input.Disable();
            Phone_Input.Dispose();
        }
    }

    IEnumerator Init()
    {
        //마법 DB 로딩 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 인벤토리 세팅
        Set_Inventory();

        // 레시피 리스트 세팅
        Set_Recipe();

        // 애니메이션용 슬롯 위치 초기화
        animSlot.position = mergePanel.transform.position;
        animSlot.gameObject.SetActive(false);

        // 선택 아이콘 끄기
        UIManager.Instance.nowSelectIcon.enabled = false;

        // 팡파레 이펙트 끄기
        slotRayEffect.gameObject.SetActive(false);

        // 뽑기 슬롯 이펙트 끄기
        getMagicEffect.gameObject.SetActive(false);

        // 뽑기 스크린 끄기
        randomScreen.gameObject.SetActive(false);

        // 합성 슬롯 비활성화
        // mergedSlot.gameObject.SetActive(false);
        mergedSlot.transform.localScale = Vector3.zero;

        // 각각 스크린 켜기
        invenScreen.SetActive(true);
        recipeScreen.SetActive(true);

        // 뽑기 스크린 투명하게 숨기기
        randomScreen.alpha = 0f;

        // 핸드폰 키입력 켜기
        Phone_Input.Enable();
    }

    public IEnumerator OpenPhone(Vector3 modifyPos = default)
    {
        //초기화
        StartCoroutine(Init());

        //마법 DB 로딩 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 핸드폰 토글 사운드 재생
        SoundManager.Instance.PlaySound("PhoneToggle");

        // 휴대폰 로딩 화면으로 가리기
        LoadingToggle(true);
        blackScreen.color = new Color(70f / 255f, 70f / 255f, 70f / 255f, 1);

        //위치 기억하기
        phonePosition = CastMagic.Instance.phone.position;
        //회전값 기억하기
        phoneRotation = CastMagic.Instance.phone.rotation.eulerAngles;

        //카메라 위치
        Vector3 camPos = Camera.main.transform.parent.position;
        camPos.z = 0;

        // 캠 변경된 스케일값
        float camScale = Camera.main.orthographicSize / UIManager.Instance.defaultCamSize;

        // 메인 카메라의 변경된 스케일 반영한 스케일값
        Vector3 UIscale = Vector3.one * camScale;

        // 화면 라이트 끄기
        lightScreen.DOColor(new Color(1f, 1f, 1f, 0f), 0.4f)
        .SetUpdate(true);

        float moveTime = 0.8f;

        // 팝업UI 상태일때 위치,회전,스케일로 이동
        CastMagic.Instance.phone.DOMove(camPos + modifyPos + UIPosition * camScale, moveTime)
        .SetUpdate(true);
        CastMagic.Instance.phone.DOScale(UIscale, moveTime)
        .SetUpdate(true);
        CastMagic.Instance.phone.DORotate(new Vector3(0, 720f - phoneRotation.y, 0), moveTime, RotateMode.WorldAxisAdd)
        .SetUpdate(true);

        // 스마트폰 움직이는 트랜지션 끝날때까지 대기
        yield return new WaitForSecondsRealtime(moveTime);

        // 버튼 상호작용 켜기
        InteractBtnsToggle(true);

        // 액티브 슬롯 3개 상호작용 켜기
        UIManager.Instance.activeSlot_A.slotButton.interactable = true;
        UIManager.Instance.activeSlot_B.slotButton.interactable = true;
        UIManager.Instance.activeSlot_C.slotButton.interactable = true;

        // 핸드폰 화면 패널 켜기
        phonePanel.SetActive(true);

        // 스크린 토글 사운드 재생
        SoundManager.Instance.PlaySound("ScreenToggle");

        // 검은화면 투명하게
        blackScreen.DOColor(Color.clear, 0.2f)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            // 휴대폰 로딩 화면 끄기
            LoadingToggle(false);
        });

        #region Interact_On

        // 인벤 슬롯 모두 켜기
        foreach (InventorySlot invenSlot in invenSlots)
        {
            invenSlot.slotButton.interactable = true;
        }

        // 첫번째 인벤 슬롯 선택하기
        UICursor.Instance.UpdateLastSelect(invenSlots[0].slotButton);

        // //선택된 슬롯 네비 설정
        // Navigation nav = selectedSlot.navigation;
        // nav.selectOnUp = stackObjSlots[3].GetComponent<Button>().FindSelectable(Vector3.up);
        // selectedSlot.navigation = nav;

        // 핸드폰 알림 개수 초기화
        UIManager.Instance.PhoneNotice(0);

        #endregion
    }

    void LoadingToggle(bool isLoading)
    {
        UIManager.Instance.phoneLoading = isLoading;

        loadingPanel.SetActive(isLoading);
    }

    void Set_Inventory()
    {
        // 머지 리스트에 있는 마법들 머지 보드에 나타내기
        for (int i = 0; i < invenSlots.Count; i++)
        {
            // 각 슬롯 세팅
            invenSlots[i].Set_Slot();
        }

        // 합성 가능 여부 체크
        MergeNumCheck();
    }

    public void MergeNumCheck()
    {
        // 합성 가능한 마법 있으면 인디케이터 켜기
        if (MergeNum() > 0)
        {
            invenBackground.color = new Color(50f / 255f, 50f / 255f, 50f / 255f, 1f);
            invenBackground.DOKill();
            invenBackground.DOColor(new Color(200f / 255f, 200f / 255f, 200f / 255f, 1f), 1f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
        }
        else
        {
            invenBackground.DOKill();
            invenBackground.DOColor(new Color(50f / 255f, 50f / 255f, 50f / 255f, 1f), 1f)
            .SetUpdate(true);
        }
    }

    int MergeNum()
    {
        // 인벤토리에 있는 모든 마법 리스트
        List<MagicInfo> magicList = new List<MagicInfo>();
        // 현재 합성 가능한 모든 마법 리스트
        List<MagicInfo> mergeAbleList = new List<MagicInfo>();

        // 가중치 배열 초기화
        for (int i = 0; i < SystemManager.Instance.elementWeitght.Length; i++)
            SystemManager.Instance.elementWeitght[i] = 1;

        // 머지 리스트에 있는 마법들 머지 보드에 나타내기
        for (int i = 0; i < invenSlots.Count; i++)
        {
            // 각 슬롯 정보 마법 정보로 변환
            MagicInfo magic = invenSlots[i].slotInfo as MagicInfo;

            // 해당 슬롯의 정보가 마법 정보일때
            if (magic != null)
            {
                magicList.Add(magic);

                // 해당 마법 원소의 인덱스 가중치 증가
                SystemManager.Instance.elementWeitght[MagicDB.Instance.ElementType(magic)]++;
            }
        }

        for (int i = 0; i < magicList.Count; i++)
        {
            // 해당 마법이 재료에 포함된 마법들 찾기
            List<MagicInfo> hasA_List = MagicDB.Instance.magicDB.Values.ToList().FindAll(
                x => x.element_A == magicList[i].name
                || x.element_B == magicList[i].name);

            // 나머지 재료도 인벤토리에 있는지 검사
            for (int j = 0; j < hasA_List.Count; j++)
            {
                // 현재 조회중인 슬롯이 아닌 슬롯만 조회
                if (i != j)
                {
                    // A 재료를 갖고 있는 마법중 나머지 B 재료가 인벤토리에 있는지 검사
                    List<MagicInfo> hasBoth_List = hasA_List.FindAll(x =>
                       (x.element_A == magicList[i].name && magicList.Exists(y => y.name == x.element_B))
                    || (x.element_B == magicList[i].name && magicList.Exists(y => y.name == x.element_A)));

                    foreach (MagicInfo magic in hasBoth_List)
                    {
                        // mergeAbleList 에 없는 마법이면 넣기
                        if (!mergeAbleList.Exists(x => x.name == magic.name))
                            mergeAbleList.Add(magic);
                    }
                }
            }
        }

        // 현재 합성 가능한 개수 저장
        mergeAbleNum = mergeAbleList.Count;

        // 최종 합성 가능한 마법 개수 반환
        return mergeAbleList.Count;
    }

    public IEnumerator MergeMagic(MagicInfo mergedMagic = null, int grade = 0)
    {
        // 스킵 스위치 초기화
        isSkipped = false;

        // 상호작용 비활성화
        InteractBtnsToggle(false);

        // 머지슬롯 Rect 찾기
        RectTransform L_MergeSlotRect = L_MergeSlot.GetComponent<RectTransform>();
        RectTransform R_MergeSlotRect = R_MergeSlot.GetComponent<RectTransform>();

        // 좌측 슬롯 원래 위치 저장
        Vector3 L_originPos = L_MergeSlotRect.anchoredPosition;
        // 우측 슬롯 원래 위치 저장
        Vector3 R_originPos = R_MergeSlotRect.anchoredPosition;
        // 가운데 합성된 슬롯 위치
        Vector3 centerPos = mergedSlot.GetComponent<RectTransform>().anchoredPosition;
        // 슬롯 원래 사이즈 저장
        Vector3 originScale = L_MergeSlot.transform.localScale;

        // 합성 준비 이펙트 재생
        mergeBeforeEffect.gameObject.SetActive(true);

        // 플러스 아이콘 줄이기
        plusIcon.transform.localScale = Vector3.zero;

        // 합성 시작 사운드 재생
        SoundManager.Instance.PlaySound("MergeStart");

        // 좌측 슬롯 가운데로 이동
        L_MergeSlotRect.DOAnchorPos(centerPos, 1f)
        .SetEase(Ease.InBack)
        .SetUpdate(true);
        // 우측 슬롯 가운데로 이동
        R_MergeSlotRect.DOAnchorPos(centerPos, 1f)
        .SetEase(Ease.InBack)
        .SetUpdate(true);

        // 좌측 슬롯 작아지기
        L_MergeSlotRect.DOScale(Vector3.zero, 0.5f)
        .SetEase(Ease.InQuad)
        .SetDelay(0.5f)
        .SetUpdate(true)
        .OnUpdate(() =>
        {
            // 스킵 스위치 켜졌을때
            if (isSkipped)
            {
                // 즉시 완료
                L_MergeSlotRect.DOComplete();

                // 합성 준비 이펙트 끄기
                mergeBeforeEffect.gameObject.SetActive(false);
            }
        })
        .OnComplete(() =>
        {
            // 슬롯 비우기
            L_MergeSlot.slotInfo = null;
            // 슬롯 UI 초기화
            L_MergeSlot.Set_Slot();
        });
        // 우측 슬롯 작아지기
        R_MergeSlotRect.DOScale(Vector3.zero, 0.5f)
        .SetEase(Ease.InQuad)
        .SetDelay(0.5f)
        .SetUpdate(true)
        .OnUpdate(() =>
        {
            // 스킵 스위치 켜졌을때
            if (isSkipped)
            {
                // 즉시 완료
                R_MergeSlotRect.DOComplete();

                // 합성 준비 이펙트 끄기
                mergeBeforeEffect.gameObject.SetActive(false);
            }
        })
        .OnComplete(() =>
        {
            // 슬롯 비우기
            R_MergeSlot.slotInfo = null;
            // 슬롯 UI 초기화
            R_MergeSlot.Set_Slot();
        });

        // 랜덤 뽑기일때
        if (grade > 0)
        {
            // 스킵 스위치 꺼져있을때
            if (!isSkipped)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                // 합성 준비 이펙트 정지
                mergeBeforeEffect.SmoothDisable();
                yield return new WaitForSecondsRealtime(0.5f);
            }

            // 해당 등급으로 랜덤 뽑기 시작
            yield return StartCoroutine(GetGradeMagic(grade));
        }
        // 일반 합성일때
        else
        {
            // 합성된 마법 아이콘 넣기
            mergedSlot.slotIcon.sprite = MagicDB.Instance.GetIcon(mergedMagic.id);
            // 합성된 마법 프레임 색 넣기
            mergedSlot.slotFrame.color = MagicDB.Instance.GradeColor[mergedMagic.grade];
            // 합성된 마법 레벨 합산
            mergedSlot.slotLevel.GetComponentInChildren<TextMeshProUGUI>(true).text = "Lv." + mergedMagic.magicLevel.ToString();
            // 합성된 마법 툴팁 넣기
            mergedSlot.slotTooltip._slotInfo = mergedMagic;

            bool isNew = false;
            // 언락된 마법중에 없으면
            if (!MagicDB.Instance.unlockMagics.Exists(x => x == mergedMagic.id))
            {
                // 새로운 마법 표시
                isNew = true;

                // 언락 리스트에 추가
                MagicDB.Instance.unlockMagics.Add(mergedMagic.id);
                // 저장
                StartCoroutine(SaveManager.Instance.Save());
            }
            // 새로운 마법일때만 New 표시하기
            mergedSlot.newSign.SetActive(isNew);

            // 스킵 스위치 꺼져있을때
            if (!isSkipped)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                // 합성 준비 이펙트 정지
                mergeBeforeEffect.SmoothDisable();
                yield return new WaitForSecondsRealtime(0.5f);
            }

            // 합성 완료 사운드 재생
            SoundManager.Instance.PlaySound("MergeComplete");

            // 합성된 슬롯 켜기
            mergedSlot.gameObject.SetActive(true);

            // 합성된 슬롯 사이즈 제로
            mergedSlot.transform.localScale = Vector3.zero;

            // 합성된 슬롯 사이즈 키우기
            mergedSlot.transform.DOScale(originScale, 0.5f)
            .SetEase(Ease.OutBack)
            .OnUpdate(() =>
            {
                // 스킵 스위치 켜졌을때
                if (isSkipped)
                    // 즉시 완료
                    mergedSlot.transform.DOComplete();
            })
            .SetUpdate(true);

            // 스킵 스위치 꺼져있을때
            if (!isSkipped)
                yield return new WaitForSecondsRealtime(0.5f);

            // 합성 완료 이펙트 켜기
            mergeAfterEffect.transform.localScale = Vector3.zero;
            mergeAfterEffect.GetComponent<Animator>().speed = 0.1f;
            mergeAfterEffect.gameObject.SetActive(true);
            mergeAfterEffect.transform.DOScale(Vector3.one, 0.2f)
            .OnUpdate(() =>
            {
                // 스킵 스위치 켜졌을때
                if (isSkipped)
                    // 즉시 완료
                    mergeAfterEffect.transform.DOComplete();
            })
            .SetUpdate(true);

            // 클릭, 확인 누르면 
            yield return new WaitUntil(() => Phone_Input.UI.Click.IsPressed() || Phone_Input.UI.Accept.IsPressed());
        }

        // 양쪽 슬롯 위치 초기화
        L_MergeSlotRect.anchoredPosition = L_originPos;
        R_MergeSlotRect.anchoredPosition = R_originPos;

        // 샤드 뽑기가 아닌 합성일때
        if (grade == 0)
        {
            // 인벤토리 빈칸에 합성된 마법 넣기
            GetMagic(mergedMagic);

            // 합성 완료 이펙트 끄기
            mergeAfterEffect.transform.DOScale(Vector3.zero, 0.2f)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                mergeAfterEffect.gameObject.SetActive(false);
            });

            // 합성 슬롯 작아지기
            mergedSlot.transform.DOScale(Vector3.zero, 0.5f)
            .SetEase(Ease.InBack)
            .SetUpdate(true);

            yield return new WaitForSecondsRealtime(0.5f);
        }

        // 합성 가능 여부 체크
        MergeNumCheck();

        // 좌측 슬롯 커지기
        L_MergeSlotRect.DOScale(Vector3.one, 0.2f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true);
        // 우측 슬롯 커지기
        R_MergeSlotRect.DOScale(Vector3.one, 0.2f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true);
        // 플러스 아이콘 커지기
        plusIcon.transform.DOScale(Vector3.one, 0.2f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true);

        // 상호작용 활성화
        InteractBtnsToggle(true);
    }

    public IEnumerator MergeFail(InventorySlot L_MergeSlot, InventorySlot R_MergeSlot)
    {
        // 상호작용 비활성화
        InteractBtnsToggle(false);

        // 머지슬롯 Rect 찾기
        RectTransform L_MergeSlotRect = L_MergeSlot.GetComponent<RectTransform>();
        RectTransform R_MergeSlotRect = R_MergeSlot.GetComponent<RectTransform>();

        // 좌측 슬롯 원래 위치 저장
        Vector3 L_originPos = L_MergeSlotRect.anchoredPosition;
        // 우측 슬롯 원래 위치 저장
        Vector3 R_originPos = R_MergeSlotRect.anchoredPosition;
        // 가운데 합성된 슬롯 위치
        Vector3 centerPos = mergedSlot.GetComponent<RectTransform>().anchoredPosition;

        // 합성 준비 이펙트 재생
        mergeBeforeEffect.gameObject.SetActive(true);

        // 플러스 아이콘 줄이기
        plusIcon.transform.localScale = Vector3.zero;

        // 좌측 슬롯 가운데로 절반만 이동
        L_MergeSlotRect.DOAnchorPos((centerPos + L_originPos) / 2f, 0.5f)
        .SetEase(Ease.InBack)
        .SetUpdate(true);
        // 우측 슬롯 가운데로 절반만 이동
        R_MergeSlotRect.DOAnchorPos((centerPos + R_originPos) / 2f, 0.5f)
        .SetEase(Ease.InBack)
        .SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.5f);

        // 합성 준비 이펙트 끄기
        mergeBeforeEffect.SmoothDisable();

        // 실패 이펙트 재생
        mergeFailEffect.Play();

        // 좌측 슬롯 위치 복귀
        L_MergeSlotRect.DOAnchorPos(L_originPos, 0.5f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true);
        // 우측 슬롯 위치 복귀
        R_MergeSlotRect.DOAnchorPos(R_originPos, 0.5f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true);

        // 플러스 아이콘 커지기
        plusIcon.transform.DOScale(Vector3.one, 0.5f)
        .SetUpdate(true);

        // 양쪽 슬롯 깜빡이기
        L_MergeSlot.BlinkSlot(4);
        R_MergeSlot.BlinkSlot(4);

        // 양쪽 슬롯 아이콘 떨기
        L_MergeSlot.ShakeIcon();
        R_MergeSlot.ShakeIcon();

        yield return new WaitForSecondsRealtime(0.5f);

        // 상호작용 활성화
        InteractBtnsToggle(true);
    }

    public IEnumerator GetGradeMagic(int grade)
    {
        // 메뉴, 백 버튼 상호작용 및 키입력 막기
        InteractBtnsToggle(false);

        // 뽑기 스크롤 그룹 투명하게
        CanvasGroup randomScrollGroup = randomScroll.GetComponent<CanvasGroup>();
        randomScrollGroup.alpha = 0;
        // 뽑기 스크롤 비활성화
        randomScroll.gameObject.SetActive(false);
        // 뽑기 스크롤 컴포넌트 비활성화
        randomScroll.enabled = false;

        // 모든 자식 비우기
        SystemManager.Instance.DestroyAllChild(randomScroll.Content);

        // 애니메이션용 슬롯 컴포넌트 찾기
        Image animIcon = animSlot.Find("Icon").GetComponent<Image>();
        Image animFrame = animSlot.Find("Frame").GetComponent<Image>();
        Mask shinyMask = animSlot.Find("ShinyMask").GetComponent<Mask>();
        Image shinyMaskImg = animSlot.Find("ShinyMask").GetComponent<Image>();
        Color maskColor = shinyMaskImg.color;

        int randomGrade = grade;

        // 뽑기 화면 전체 투명하게
        randomScreen.alpha = 0;
        // 뽑기 배경 활성화, 가려서 핸드폰 입력 막기
        randomScreen.gameObject.SetActive(true);

        // 애니메이션용 아이콘 색 넣기
        animIcon.color = MagicDB.Instance.GradeColor[randomGrade];
        // 애니메이션용 프레임 색 넣기
        animFrame.color = MagicDB.Instance.GradeColor[randomGrade];

        // 마스크 이미지 켜기
        shinyMask.showMaskGraphic = true;
        // 마스크 이미지 알파값 초기화
        maskColor.a = 1f / 255f;
        shinyMaskImg.color = maskColor;
        maskColor.a = 1f;
        // 마스크 이미지 알파값 올리기
        shinyMaskImg.color = maskColor;
        // shinyMaskImg.DOColor(maskColor, 0.5f)
        // .SetEase(Ease.InCirc)
        // .SetUpdate(true);

        // 스킵 스위치 꺼져있을때
        if (!isSkipped)
            // 등급 업 파티클 이펙트 켜기
            rankUpEffect.Play();

        // 애니메이션용 슬롯 켜기
        animSlot.gameObject.SetActive(true);

        // 애니메이션 슬롯 사이즈 키워서 나타내기
        animSlot.transform.localScale = Vector2.zero;
        animSlot.DOScale(Vector3.one, isSkipped ? 0f : 0.5f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true);

        // 등급 업 파티클 이펙트 끄기
        rankUpEffect.Stop();

        // 마스크 이미지 알파값 낮추기
        maskColor.a = 1f / 255f;
        shinyMaskImg.DOColor(maskColor, isSkipped ? 0f : 0.5f)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            // 마스크 이미지 끄기
            shinyMask.showMaskGraphic = false;
        });

        // 애니메이션용 아이콘 스크린 가운데로 올라가기
        animSlot.DOMove(randomScroll.transform.position, isSkipped ? 0f : 0.5f)
        .SetEase(Ease.OutSine)
        .SetUpdate(true);

        // 뽑기 화면 전체 나타내기
        DOTween.To(() => randomScreen.alpha, x => randomScreen.alpha = x, 1f, isSkipped ? 0f : 0.5f)
        .SetUpdate(true);

        // 스킵 스위치 꺼져있을때
        if (!isSkipped)
            yield return new WaitForSecondsRealtime(0.5f);

        // 등급 올리기 (실패할때까지)
        while (true)
        {
            // 확률로 등급 상승 - 1등급 50%, 2등급 40%, 3등급 30%, 4등급 20%, 5등급 10%, 6등급은 계산상 무조건 실패
            bool rankUp = Random.Range(0f, 1f) < 0.1f * (6 - randomGrade);

            // 6등급 미만일때만
            if (randomGrade < 6)
                // 언락된 윗등급 마법이 없으면 등급 상승 실패
                for (int i = 0; i < MagicDB.Instance.unlockMagics.Count; i++)
                {
                    //언락된 마법의 id
                    int magicId = MagicDB.Instance.unlockMagics[i];

                    // 바로 윗등급 마법이라면
                    if (MagicDB.Instance.GetMagicByID(magicId).grade == randomGrade + 1)
                        // 반복문 탈출
                        break;

                    // 마지막까지 반복문 탈출하지 못했으면
                    if (i == MagicDB.Instance.unlockMagics.Count - 1)
                        // 등급 상승 불가
                        rankUp = false;

                    yield return null;
                }

            // 등급 업 파티클 이펙트 켜기
            rankUpEffect.Play();

            // 마스크 이미지 켜기
            shinyMask.showMaskGraphic = true;
            // 마스크 이미지 알파값 초기화
            maskColor.a = 1f / 255f;
            shinyMaskImg.color = maskColor;
            maskColor.a = 1f;
            // 마스크 이미지 알파값 올리기
            shinyMaskImg.DOColor(maskColor, 0.2f)
            .SetEase(Ease.InCirc)
            .SetUpdate(true);

            // 슬롯 각도를 흔들기
            animSlot.DOShakeRotation(0.5f, Vector3.forward * 10f, 50, 90, true)
            .SetEase(Ease.InOutCirc)
            .SetUpdate(true);

            // 슬롯 흔드는 시간 대기
            yield return new WaitForSecondsRealtime(0.35f);
            // 등급 업 파티클 이펙트 끄기
            rankUpEffect.Stop();
            yield return new WaitForSecondsRealtime(0.35f);

            // 등급 상승 성공시
            if (rankUp)
            {
                // 등급 업 성공 사운드 재생
                SoundManager.Instance.PlaySound("RankUpSuccess");

                // 등급 상승 성공 이펙트, 폭죽 이펙트
                rankUpSuccessEffect.Play();

                // 성공시 등급 올리고 한번 더 반복
                randomGrade++;

                // 상승한 등급으로 애니메이션용 아이콘 색 갱신
                animIcon.color = MagicDB.Instance.GradeColor[randomGrade];
                // 상승한 등급으로 애니메이션용 프레임 색 갱신
                animFrame.color = MagicDB.Instance.GradeColor[randomGrade];
            }
            else
            {
                // 등급 업 실패 사운드 재생
                SoundManager.Instance.PlaySound("RankUpFail");

                // 등급 상승 실패 이펙트, 먼지 이펙트
                rankUpFailEffect.Play();
            }

            // 마스크 이미지 알파값 낮추기
            maskColor.a = 1f / 255f;
            shinyMaskImg.DOColor(maskColor, 0.5f)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // 마스크 이미지 끄기
                shinyMask.showMaskGraphic = false;
            });

            yield return new WaitForSecondsRealtime(0.5f);

            // 등급 상승 실패시
            if (!rankUp)
                // 실패시 반복문 탈출
                break;
        }

        // 랜덤 마법 리스트
        List<MagicInfo> randomList = new List<MagicInfo>();

        // 해당 등급의 언락된 마법 리스트 불러오기
        for (int i = 0; i < MagicDB.Instance.unlockMagics.Count; i++)
        {
            //언락된 마법의 id
            int magicId = MagicDB.Instance.unlockMagics[i];

            // 전체 마법중에 
            if (MagicDB.Instance.magicDB.TryGetValue(magicId, out MagicInfo magic))
            {
                // 선택된 마법과 등급이 같은 마법이면 
                if (magic.grade == randomGrade)
                {
                    // 랜덤 풀에 넣기
                    randomList.Add(magic);

                    // print(magic.id + " : " + magic.name);
                }
            }
        }

        // 랜덤 풀이 하나라도 있을때
        if (randomList.Count > 0)
            // 랜덤풀 스크롤에 필요한 최소 개수보다 부족하면 모든 마법 한번 더 넣기
            while (randomList.Count < 5)
            {
                // 풀의 마법 개수
                int poolNum = randomList.Count;

                for (int i = 0; i < poolNum; i++)
                {
                    MagicInfo magic = randomList[i];

                    randomList.Add(magic);

                    yield return null;
                }

                yield return null;
            }

        // 랜덤으로 순서 섞기
        System.Random random = new System.Random();
        randomList = randomList.OrderBy(x => random.Next()).ToList();

        // 랜덤 마법 풀 개수만큼 슬롯 생성
        for (int i = 0; i < randomList.Count; i++)
        {
            // 랜덤 스크롤 컨텐츠 자식으로 슬롯 넣기
            Transform magicSlot = LeanPool.Spawn(magicSlotPrefab, randomScroll.Content);

            Sprite icon = MagicDB.Instance.GetIcon(randomList[i].id);

            // 마법 아이콘 넣기
            magicSlot.Find("Icon").GetComponent<Image>().sprite = icon == null ? SystemManager.Instance.questionMark : icon;
            // 프레임 색 넣기
            magicSlot.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.GradeColor[randomList[i].grade];

            yield return null;
        }

        // 스킵 여부 초기화
        isSkipped = false;

        // 스냅 스크롤 컴포넌트 활성화
        randomScroll.enabled = true;
        // 뽑기 스크롤 활성화
        randomScroll.gameObject.SetActive(true);

        // 애니메이션용 슬롯 줄어들어 사라지기
        animSlot.DOScale(Vector3.zero, isSkipped ? 0f : 0.5f)
        .SetEase(Ease.InBack)
        .SetUpdate(true);

        // 스킵 스위치 꺼져있을때
        if (!isSkipped)
            yield return new WaitForSecondsRealtime(0.5f);

        // 애니메이션용 슬롯 초기화
        animSlot.gameObject.SetActive(false);
        animSlot.position = mergePanel.transform.position;
        animSlot.localScale = Vector3.one;

        // 뽑기 스크롤 그룹 알파값 초기화
        DOTween.To(() => randomScrollGroup.alpha, x => randomScrollGroup.alpha = x, 1f, isSkipped ? 0f : 0.5f)
        .SetUpdate(true);

        // 랜덤 스크롤 돌리기
        float spinSpeed = Random.Range(1000f, 2000f);
        randomScroll.Velocity = Vector2.down * spinSpeed;

        // 스핀 하는동안 사운드 재생
        StartCoroutine(SpinSound());

        // 스크롤 일정 속도 이하거나 스킵할때까지 대기
        yield return new WaitUntil(() => randomScroll.Velocity.magnitude <= 100f
        || isSkipped);

        // 스크롤이 일정 속도 이상이면 반복
        while (randomScroll.Velocity.magnitude > 100f)
        {
            // 속도 부드럽게 낮추기
            randomScroll.Velocity = Vector2.Lerp(randomScroll.Velocity, Vector2.zero, 0.01f);

            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
        }

        // 속도 멈추기
        randomScroll.Velocity = Vector2.zero;

        // 마지막 스핀 사운드 재생
        SoundManager.Instance.PlaySound("SlotSpin_Once");

        // 멈춘 후 딜레이
        yield return new WaitForSecondsRealtime(0.5f);

        // 랜덤풀에서 멈춘 시점 선택된 인덱스에 해당하는 마법 뽑기
        MagicInfo getMagic = randomList[randomScroll.CenteredPanel];
        // print(getMagic.name);

        // 팡파레 이펙트 켜기
        slotRayEffect.gameObject.SetActive(true);
        // 애니메이터 속도 느리게
        slotRayEffect.speed = 0.1f;
        // 팡파레 이펙트 등급색으로 변경
        Image raySprite = slotRayEffect.GetComponent<Image>();
        raySprite.color = MagicDB.Instance.GradeColor[getMagic.grade];
        // 사이즈 키우기
        slotRayEffect.transform.localScale = Vector2.zero;
        slotRayEffect.transform.DOScale(Vector3.one, 0.2f)
        .SetUpdate(true);

        // 획득 파티클 색 변경
        ParticleSystem.MainModule particleMain = getMagicEffect.main;
        particleMain.startColor = MagicDB.Instance.GradeColor[getMagic.grade];

        // 합성 완료 사운드 재생
        SoundManager.Instance.PlaySound("MergeComplete");

        // 확인, 클릭할때까지 대기
        yield return new WaitUntil(() => Phone_Input.UI.Click.IsPressed() || Phone_Input.UI.Accept.IsPressed());

        // 팡파레 이펙트 끄기
        slotRayEffect.gameObject.SetActive(false);

        // 뽑힌 슬롯 끄기
        GameObject getSlot = randomScroll.Content.GetChild(randomScroll.CenteredPanel).gameObject;
        getSlot.SetActive(false);

        // 빈칸 위치에 Attractor 오브젝트 옮기기
        particleAttractor.transform.position = invenSlots[GetEmptySlot()].transform.position;

        // 마법 획득 이펙트 켜기
        getMagicEffect.gameObject.SetActive(true);

        // 파티클 생성 사운드 재생
        SoundManager.Instance.PlaySound("MergeParticleGet", 0, 0.005f, 20, false);

        // 마법 획득 이펙트 시간 대기
        yield return new WaitForSecondsRealtime(0.2f);

        // 뽑기 스크린 전체 투명하게
        DOTween.To(() => randomScreen.alpha, x => randomScreen.alpha = x, 0f, 0.5f)
        .SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.2f);
        // 획득한 마법 인벤에 넣기
        GetMagic(getMagic);
        yield return new WaitForSecondsRealtime(0.3f);

        // 마법 획득 이펙트 끄기
        getMagicEffect.gameObject.SetActive(false);

        // 뽑기 스크린 비활성화
        randomScreen.gameObject.SetActive(false);

        // 뽑힌 슬롯 다시 켜기
        getSlot.SetActive(true);

        // 메뉴, 백 버튼 상호작용 및 키입력 막기 해제
        InteractBtnsToggle(true);
    }

    public void ParticleSound()
    {
        // 파티클이 슬롯에 충돌후 사라질때 사운드 재생
        SoundManager.Instance.PlaySound("MergeParticleGet");
    }

    IEnumerator SpinSound()
    {
        // 처음 위치 초기화
        float nowHeight = randomScroll.Content.anchoredPosition.y;

        while (randomScroll.Velocity.magnitude > 10f)
        {
            // 한칸 이상 이동했을때
            if (randomScroll.Content.anchoredPosition.y < nowHeight - 90f)
            {
                // 현재 위치 갱신
                nowHeight = randomScroll.Content.anchoredPosition.y - 90f;

                // 랜덤 스핀 1회 할때마다 사운드 재생
                SoundManager.Instance.PlaySound("SlotSpin_Once");
            }

            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
        }
    }

    public int GetEmptySlot()
    {
        int emptyIndex = -1;

        // 빈칸 찾기
        for (int i = 0; i < invenSlots.Count; i++)
        {
            // 빈칸 찾으면
            if (invenSlots[i].slotInfo == null)
            {
                // 빈칸 인덱스 기록
                emptyIndex = i;

                break;
            }
        }

        // 빈칸 인덱스 리턴
        return emptyIndex;
    }

    public void GetProduct(SlotInfo slotInfo, int getIndex = -1)
    {
        // 아이템일때
        if (slotInfo as ItemInfo != null)
            PhoneMenu.Instance.GetItem(slotInfo as ItemInfo, getIndex);

        // 마법
        if (slotInfo as MagicInfo != null)
            PhoneMenu.Instance.GetMagic(slotInfo as MagicInfo, false, getIndex);
    }

    public void GetItem(ItemInfo getItem, int getIndex = -1)
    {
        // print(getItem.itemType + " : " + getItem.itemName);

        // 획득 인덱스 지정 안했을때
        if (getIndex == -1)
            // 인벤토리 빈칸 찾기
            getIndex = GetEmptySlot();

        // 빈칸 있을때
        if (getIndex != -1)
        {
            // 빈 슬롯에 해당 아이템 넣기
            invenSlots[getIndex].slotInfo = getItem;

            // New 표시 켜기
            invenSlots[getIndex].newSign.SetActive(true);

            // 핸드폰 열려있으면
            if (gameObject.activeSelf)
                // 해당 칸 UI 갱신
                invenSlots[getIndex].Set_Slot(true);

            // 핸드폰 알림 개수 추가
            UIManager.Instance.PhoneNotice();
        }
    }

    public void GetMagic(MagicInfo getMagic, bool castCheck = false, int getIndex = -1)
    {
        // MagicInfo 인스턴스 생성
        MagicInfo magic = new MagicInfo(getMagic);

        //마법의 레벨 초기화
        magic.magicLevel = 1;

        // touchedMagics에 해당 마법 id가 존재하지 않으면
        if (!MagicDB.Instance.savedMagics.Exists(x => x == magic.id))
        {
            // 보유했던 마법 리스트에 추가
            MagicDB.Instance.savedMagics.Add(magic.id);
        }

        // 획득 인덱스 지정 안했을때
        if (getIndex == -1)
            // 인벤토리 빈칸 찾기
            getIndex = GetEmptySlot();

        // 빈칸 있을때
        if (getIndex != -1)
        {
            // 해당 빈 슬롯에 마법 넣기
            invenSlots[getIndex].slotInfo = getMagic;

            // New 표시 켜기
            invenSlots[getIndex].newSign.SetActive(true);

            // 핸드폰 열려있으면
            if (gameObject.activeSelf)
                // 해당 칸 UI 갱신
                invenSlots[getIndex].Set_Slot(true);

            // 핸드폰 알림 개수 추가
            UIManager.Instance.PhoneNotice();
        }

        //플레이어 총 전투력 업데이트
        PlayerManager.Instance.PlayerStat_Now.playerPower = PlayerManager.Instance.GetPlayerPower();

        // 마법 획득 시 바로 해당 마법 자동 시전일때
        if (castCheck)
            // 인벤토리에서 마법 찾아 자동 시전하기
            CastMagic.Instance.CastCheck();
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

        // 클릭,확인 누르면 스킵 켜기
        if (Phone_Input.UI.Click.IsPressed() || Phone_Input.UI.Accept.IsPressed())
            isSkipped = true;

        // 채팅패널 Lerp로 사이즈 반영해서 lerp로 늘어나기
        chatContentRect.sizeDelta = new Vector2(chatContentRect.sizeDelta.x, Mathf.Lerp(chatContentRect.sizeDelta.y, sumChatHeights, Time.unscaledDeltaTime * 5f));
    }

    public IEnumerator ChatAdd(string message)
    {
        // 메시지 생성
        GameObject chat = LeanPool.Spawn(chatPrefab,
        chatScroll.content.rect.position - new Vector2(0, -10f),
        Quaternion.identity, chatScroll.content);

        // 캔버스 그룹 찾고 알파값 0으로 낮춰서 숨기기
        CanvasGroup canvasGroup = chat.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;

        // 채팅 컬러 및 메시지 적용
        chat.GetComponent<Image>().color = new Color(1, 50f / 255f, 50f / 255f, 1);
        chat.GetComponentInChildren<TextMeshProUGUI>().text = message;

        // 1프레임 대기
        yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);

        // 컴포넌트 껐다켜서 레이아웃 정렬하기
        VerticalLayoutGroup layoutGroup = chatScroll.content.GetComponent<VerticalLayoutGroup>();
        layoutGroup.enabled = false;
        layoutGroup.enabled = true;

        float sumHeights = 0;

        // 메시지들의 총합 높이 갱신
        for (int i = 0; i < chatScroll.content.childCount; i++)
        {
            RectTransform rect = chatScroll.content.GetChild(i).GetComponent<RectTransform>();

            // 여백 합산
            sumHeights += 10;
            // 높이 합산
            sumHeights += rect.sizeDelta.y;

            // print(i + " : " + rect.sizeDelta.y);
        }

        sumChatHeights = sumHeights;

        // 알파값 높여서 표시
        canvasGroup.alpha = 1;
    }

    public void CancelSelectSlot()
    {
        // //마우스 꺼져있으면 리턴
        // if (Cursor.lockState == CursorLockMode.Locked)
        //     return;

        // 마우스의 아이콘 끄기
        UIManager.Instance.nowSelectIcon.enabled = false;

        // 선택된 슬롯에 슬롯 정보 넣기
        nowSelectSlot.slotInfo = nowSelectSlotInfo;
        // 선택된 슬롯 UI 갱신
        nowSelectSlot.Set_Slot();

        // 선택된 슬롯 shiny 이펙트 켜기
        nowSelectSlot.shinyEffect.gameObject.SetActive(false);
        nowSelectSlot.shinyEffect.gameObject.SetActive(true);

        // 현재 선택된 마법 인덱스 초기화
        // nowSelectIndex = -1;
        nowSelectSlot = null;

        // 폰 하단 버튼 상호작용 허용
        // InteractBtnsToggle(true);

        //선택된 마법 아이콘 마우스 위치로 이동
        MousePos();
    }

    public void ShakeMouseIcon()
    {
        // 현재 트윈 멈추기
        UIManager.Instance.nowSelectIcon.transform.DOPause();

        // 원래 위치 저장
        Vector2 originPos = UIManager.Instance.nowSelectIcon.transform.localPosition;

        // 마우스 아이콘 흔들기
        UIManager.Instance.nowSelectIcon.transform.DOPunchPosition(Vector2.right * 30f, 1f, 10, 1)
        .SetEase(Ease.Linear)
        .OnPause(() =>
        {
            UIManager.Instance.nowSelectIcon.transform.localPosition = originPos;
        })
        .SetUpdate(true);
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
            MagicInfo magic = MagicDB.Instance.magicDB[i];

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
            main_Icon.sprite = MagicDB.Instance.GetIcon(magic.id);
            // 재료 아이콘 해금됬으면 표시, 아니면 물음표
            elementA_Icon.sprite = unlocked && elementA != null ? MagicDB.Instance.GetIcon(elementA.id) : SystemManager.Instance.questionMark;
            elementB_Icon.sprite = unlocked && elementB != null ? MagicDB.Instance.GetIcon(elementB.id) : SystemManager.Instance.questionMark;

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

                main_tooltip._slotInfo = magic;
                main_tooltip.enabled = true;

                if (elementA != null && elementB != null)
                {
                    elementA_tooltip._slotInfo = elementA;
                    elementA_tooltip.enabled = true;
                    elementB_tooltip._slotInfo = elementB;
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

    public void InteractBtnsToggle(bool toggle)
    {
        // 키 입력 막기 변수 토글
        btnsInteractable = toggle;

        // 머지슬롯 상호작용 토글
        L_MergeSlot.slotButton.interactable = toggle;
        R_MergeSlot.slotButton.interactable = toggle;

        // 메뉴 버튼 상호작용 토글
        recipeBtn.interactable = toggle;
        // 백 버튼 상호작용 토글
        backBtn.interactable = toggle;
        //홈 버튼 상호작용 켜기
        homeBtn.interactable = toggle;
    }

    public void ScreenScrollStart()
    {
        // 키 입력 막혔으면 리턴
        if (!btnsInteractable)
            return;

        //마우스로 아이콘 들고 있으면 복귀시키기
        if (UIManager.Instance.nowSelectIcon.enabled)
            CancelSelectSlot();

        // 선택된게 인벤 스크린일때
        if (screenScroll.CenteredPanel == 0)
        {
        }
        // 선택된게 레시피 스크린일때
        else
        {
            // 레시피 스크롤 위치 초기화
            recipeScroll.Content.localPosition = Vector2.zero;
            recipeScroll.GoToPanel(0);
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

        // 키 입력 막혀있으면 리턴
        if (!btnsInteractable)
            yield break;

        //마우스로 아이콘 들고 있으면 복귀시키기
        if (UIManager.Instance.nowSelectIcon.enabled)
            CancelSelectSlot();

        // 툴팁 끄기
        ProductToolTip.Instance.QuitTooltip();

        // 백버튼 안누른 상태일때
        if (backBtnCount <= 0)
        {
            //버튼 시간 카운트 시작
            backBtnCount = 1f;

            // 한번 누르면 시간 재면서 버튼 절반 색 채우기
            DOTween.To(() => backBtnFill.fillAmount, x => backBtnFill.fillAmount = x, 0.5f, 0.2f)
            .SetUpdate(true);

            // Merge 슬롯의 아이템 전부 인벤에 넣기
            if (L_MergeSlot.slotInfo != null)
            {
                BackToInven(L_MergeSlot);
            }
            if (R_MergeSlot.slotInfo != null)
            {
                BackToInven(R_MergeSlot);
            }
            // 매직 머신 재화 슬롯의 아이템 인벤에 넣기
            if (MagicMachineUI.Instance.paySlot.slotInfo != null)
            {
                BackToInven(MagicMachineUI.Instance.paySlot);

                // spin 버튼 불 끄기
                MagicMachineUI.Instance.ClickPaySlot();
            }
        }
        // 백버튼 한번 더 누르면
        else
        {
            // 매직머신 팝업 켜져있으면
            if (MagicMachineUI.Instance.gameObject.activeSelf)
                // 매지머신 팝업과 함께 핸드폰 끄기
                MagicMachineUI.Instance.ExitPopup();
            else
                // 핸드폰 끄기
                ClosePhone();
        }
    }

    public void BackToInven(InventorySlot slot)
    {
        // 인벤 빈칸 찾기
        int emptyIndex = GetEmptySlot();

        // 빈칸에 마법 넣기
        invenSlots[emptyIndex].slotInfo = slot.slotInfo;
        // Merge 슬롯의 정보 삭제
        slot.slotInfo = null;

        // 인벤 슬롯 정보 갱신
        invenSlots[emptyIndex].Set_Slot(true);
        // Merge 슬롯 정보 갱신
        slot.Set_Slot(true);
    }

    public void ClosePhone()
    {
        StartCoroutine(PhoneExit());
    }

    public IEnumerator PhoneExit()
    {
        // 인벤 슬롯 상호작용 모두 끄기
        foreach (InventorySlot invenSlot in invenSlots)
        {
            invenSlot.slotButton.interactable = false;
        }

        // 버튼 상호작용 끄기
        InteractBtnsToggle(false);

        // 액티브 슬롯 3개 상호작용 막기
        UIManager.Instance.activeSlot_A.slotButton.interactable = false;
        UIManager.Instance.activeSlot_B.slotButton.interactable = false;
        UIManager.Instance.activeSlot_C.slotButton.interactable = false;

        //마우스로 아이콘 들고 있으면 복귀시키기
        if (UIManager.Instance.nowSelectIcon.enabled)
            CancelSelectSlot();

        // UI 커서 미리 끄기
        UICursor.Instance.UICursorToggle(false);

        // 로딩 패널 켜기
        loadingPanel.SetActive(true);

        // 백버튼 색 채우기
        DOTween.To(() => backBtnFill.fillAmount, x => backBtnFill.fillAmount = x, 1f, 0.2f)
        .SetUpdate(true);

        // 화면 검은색으로
        blackScreen.DOColor(new Color(70f / 255f, 70f / 255f, 70f / 255f, 1), 0.2f)
        .SetUpdate(true);

        // 스크린 토글 사운드 재생
        SoundManager.Instance.PlaySound("ScreenToggle");

        // 화면 꺼지는 동안 대기
        yield return new WaitForSecondsRealtime(0.2f);

        // 핸드폰 토글 사운드 재생
        SoundManager.Instance.PlaySound("PhoneToggle");

        // 핸드폰 화면 패널 끄기
        phonePanel.SetActive(false);

        // 핸드폰 알림 개수 초기화
        UIManager.Instance.PhoneNotice(0);

        // 인벤토리 new 표시 모두 끄기
        for (int i = 0; i < invenSlots.Count; i++)
        {
            invenSlots[i].newSign.SetActive(false);
        }

        float moveTime = 0.8f;

        // 매직폰 상태일때 위치로 변경
        CastMagic.Instance.phone.DOMove(phonePosition, moveTime)
        .SetUpdate(true);
        // 매직폰 상태일때 크기로 변경
        CastMagic.Instance.phone.DOScale(phoneScale, moveTime)
        .SetUpdate(true);
        // 매직폰 상태일때 회전값으로 변경
        CastMagic.Instance.phone.DORotate(new Vector3(0, 360f, 0), moveTime, RotateMode.WorldAxisAdd)
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

        //팝업 세팅
        UIManager.Instance.PopupSet(UIManager.Instance.phonePanel);

        // 인벤토리에서 마법 찾아 자동 시전하기
        CastMagic.Instance.CastCheck();
    }
}
