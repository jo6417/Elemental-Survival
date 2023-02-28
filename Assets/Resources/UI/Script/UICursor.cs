using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICursor : MonoBehaviour
{
    #region Singleton
    private static UICursor instance;
    public static UICursor Instance
    {
        get
        {
            if (instance == null)
            {
                return null;
                // var obj = FindObjectOfType<UICursor>();
                // if (obj != null)
                // {
                //     instance = obj;
                // }
                // else
                // {
                //     var newObj = new GameObject().AddComponent<UICursor>();
                //     instance = newObj;
                // }
            }
            return instance;
        }
    }
    #endregion

    [Header("State")]
    public NewInput UI_Input; // UI 인풋 받기
    public Selectable lastSelected; //마지막 선택된 오브젝트
    public Color targetOriginColor; //마지막 선택된 오브젝트 원래 selected 색깔
    public float UI_CursorPadding; //UI 커서 여백 
    [ReadOnly, SerializeField] bool isFlicking = false; //커서 깜빡임 여부
    [ReadOnly, SerializeField] bool isMove = false; //커서 이동중 여부
    Sequence cursorSeq; //깜빡임 시퀀스
    IEnumerator cursorAnimCoroutine;

    [Header("Refer")]
    public Transform mouseCursor; // 마우스 커서
    public GameObject aimCursor; // 전투 에임용 마우스 커서
    public GameObject arrowCursor; // UI 선택용 마우스 커서
    public Canvas UI_CursorCanvas; // UI커서 전용 캔버스
    public Transform UI_Cursor; // 선택된 UI 따라다니는 UI커서

    private void Awake()
    {
        // 다른 오브젝트가 이미 있을때
        if (instance != null)
        {
            // 파괴 후 리턴
            Destroy(gameObject);
            return;
        }
        // 최초 생성 됬을때
        else
        {
            instance = this;

            // 파괴되지 않게 설정
            DontDestroyOnLoad(gameObject);
        }

        UI_Input = new NewInput();
        // 방향키 입력시
        UI_Input.UI.NavControl.performed += val =>
        {
            // UI커서가 꺼져있고 lastSelected가 있으면 lastSelected 선택
            if (!UICursor.Instance.UI_Cursor.gameObject.activeInHierarchy && lastSelected != null)
            {
                // UI 커서 켜기
                UICursorToggle(true);

                lastSelected.Select();
            }
        };
        // 마우스 위치 입력
        UI_Input.UI.MousePosition.performed += val =>
        {
            MousePos(val.ReadValue<Vector2>());
        };
        // 마우스 클릭시
        UI_Input.UI.Click.performed += val =>
        {
            // 윈도우 마우스 커서 숨기기
            Cursor.visible = false;
        };

        // UI 입력 켜기
        UI_Input.Enable();
    }

    private void OnDestroy()
    {
        if (UI_Input != null)
        {
            UI_Input.Disable();
            UI_Input.Dispose();
        }
    }

    private void Update()
    {
        //선택된 UI 따라다니기
        FollowUICursor();
    }

    #region MouseCursor

    // 마우스 위치 입력되면 실행
    void MousePos(Vector2 mousePos)
    {
        // print(mousePos);

        // 마우스 따라서 에임 커서 이동
        mouseCursor.position = mousePos;
    }

    public void CursorChange(bool isUICursor)
    {
        // 화살표 커서 토글
        arrowCursor.SetActive(isUICursor);
        // 조준 커서 토글
        aimCursor.SetActive(!isUICursor);
    }

    #endregion

    #region UICursor

    void FollowUICursor()
    {
        // lastSelected와 현재 선택버튼이 같으면 버튼 깜빡임 코루틴 시작
        if (EventSystem.current.currentSelectedGameObject == null //현재 선택 버튼이 없을때
        || !EventSystem.current.currentSelectedGameObject.activeSelf //현재 선택 버튼 비활성화 체크 됬을때
        || !EventSystem.current.currentSelectedGameObject.activeInHierarchy //현재 선택 버튼 실제로 비활성화 됬을때
        || lastSelected != EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>() //다른 버튼 선택 됬을때
        || !lastSelected.interactable //버튼 상호작용 꺼졌을때
        || Cursor.lockState == CursorLockMode.None //마우스 켜져있을때
        )
        {
            // UI커서 애니메이션 켜져있으면
            if (isFlicking)
            {
                // 기억하고 있는 버튼 있으면
                if (lastSelected)
                {
                    // 기존 트윈 종료
                    lastSelected.targetGraphic.DOKill();
                }

                //커서 애니메이션 끝
                isFlicking = false;
            }

            // lastSelected 새로 갱신해서 기억하기
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                //마지막 버튼 기억 갱신
                lastSelected = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();

                //원본 컬러 기억하기
                targetOriginColor = lastSelected.targetGraphic.color;
            }
        }
        //선택된 버튼이 바뀌었을때
        else
        {
            if (!isFlicking)
            {
                //커서 애니메이션 시작
                isFlicking = true;
                isMove = true;

                // 기존 커서 애니메이션 끝내기
                if (cursorAnimCoroutine != null)
                    StopCoroutine(cursorAnimCoroutine);

                //커서 애니메이션 시작
                cursorAnimCoroutine = CursorAnim();
                StartCoroutine(cursorAnimCoroutine);
            }

            // domove 끝났으면 타겟 위치 따라가기
            if (!isMove)
                UI_Cursor.transform.position = lastSelected.transform.position;
        }

        //! 현재 선택된 UI 이름 표시
        if (UIManager.Instance != null)
            if (EventSystem.current.currentSelectedGameObject == null)
                SystemManager.Instance.nowSelectUI.text = "Last Select : null";
            else
                SystemManager.Instance.nowSelectUI.text = "Last Select : " + EventSystem.current.currentSelectedGameObject.name;
    }

    public void UpdateLastSelect(Selectable selectable)
    {
        // 버튼 선택 기본값
        lastSelected = selectable;

        //null 선택하기
        EventSystem.current.SetSelectedGameObject(null);

        // UI 커서 끄기
        UICursorToggle(false);
    }

    public void UICursorToggle(bool setToggle)
    {
        RectTransform cursorRect = UI_Cursor.GetComponent<RectTransform>();

        // 커서 켜져있을때 끄기
        if (!setToggle && UI_Cursor.gameObject.activeSelf)
        {
            // 기존 트윈 죽이기
            cursorRect.DOPause();
            cursorRect.DOKill();
            // lastSelected.targetGraphic.GetComponent<Image>().DOKill();

            // UI 커서 투명하게
            Image cursorImage = UI_Cursor.GetComponent<Image>();
            cursorImage.DOKill();
            cursorImage.DOColor(SystemManager.Instance.HexToRGBA("59AFFF", 0), 0.3f)
            .SetUpdate(true)
            .OnStart(() =>
            {
                cursorImage.color = SystemManager.Instance.HexToRGBA("59AFFF");
            })
            .OnKill(() =>
            {
                cursorImage.color = SystemManager.Instance.HexToRGBA("59AFFF", 0);
            });

            //UI커서 크기 및 위치 초기화
            RectTransform cursorCanvasRect = UI_CursorCanvas.GetComponent<RectTransform>();
            cursorRect.DOSizeDelta(cursorCanvasRect.sizeDelta, 0.3f)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                //UI커서 비활성화
                UI_Cursor.gameObject.SetActive(false);
            });

            UI_Cursor.DOMove(cursorCanvasRect.position, 0.3f)
            .SetUpdate(true);

            // 트리거 끄기
            // isFlicking = false;
            // isMove = false;
        }

        // 커서 꺼져있을때 켜기
        if (setToggle)
        {
            //UI커서 크기 및 위치 초기화
            // RectTransform cursorCanvasRect = UI_CursorCanvas.GetComponent<RectTransform>();
            // UI_Cursor.sizeDelta = cursorCanvasRect.sizeDelta;
            // print("cursorCanvasRect.sizeDelta : " + cursorCanvasRect.sizeDelta);

            // UI_Cursor.position = Vector2.zero;

            // cursorRect.DOPause();
            // cursorRect.DOKill();

            //UI커서 활성화
            UI_Cursor.gameObject.SetActive(true);

            //선택된 UI 따라다니기
            FollowUICursor();
        }
    }

    IEnumerator CursorAnim()
    {
        RectTransform cursorRect = UI_Cursor.GetComponent<RectTransform>();
        //원래 트윈 있으면 죽이기        
        cursorRect.DOKill();
        UI_Cursor.transform.DOComplete();
        UI_Cursor.DOComplete();

        // 선택된 타겟 이미지
        // Image image = lastSelected.targetGraphic.GetComponent<Image>();
        // 선택된 이미지 Rect
        RectTransform lastRect = lastSelected.GetComponent<RectTransform>();

        lastSelected.targetGraphic.DOKill();

        //깜빡일 시간
        float flickTime = 0.3f;
        //깜빡일 컬러 강조 비율
        float colorRate = 1.4f;
        //깜빡일 컬러
        Color flickColor = new Color(targetOriginColor.r * colorRate, targetOriginColor.g * colorRate, targetOriginColor.b * colorRate, 1f);
        //이동할 버튼 위치
        Vector3 btnPos = EventSystem.current.currentSelectedGameObject.transform.position;

        //커서 사이즈 + 여백 추가
        Vector2 scale = lastRect.sizeDelta * 1.1f;

        //마지막 선택된 버튼의 캔버스
        Canvas selectedCanvas = lastRect.GetComponentInParent<Canvas>();

        //todo 선택된 캔버스의 렌더 모드에 따라 UI커서의 스케일 계산하기
        // scale = selectedCanvas.renderMode == RenderMode.WorldSpace ? (Vector2)Camera.main.ScreenToWorldPoint(scale) : scale;

        // UI커서 부모 캔버스와 선택된 버튼 부모 캔버스의 렌더모드가 다를때
        if (UI_CursorCanvas.renderMode != selectedCanvas.renderMode)
        {
            //렌더 모드 일치 시키기
            UI_CursorCanvas.renderMode = selectedCanvas.renderMode;

            RectTransform cursorCanvasRect = UI_CursorCanvas.GetComponent<RectTransform>();
            RectTransform selectedCanvasRect = selectedCanvas.GetComponent<RectTransform>();

            // 스케일 일치
            cursorCanvasRect.localScale = selectedCanvasRect.localScale;

            // 바뀐 렌더모드에 따라 커서 스케일 정의
            if (UI_CursorCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                cursorCanvasRect.localScale = Vector2.one;
            }
            else
            {
                //캔버스 z축을 선택된 캔버스에 맞추기
                Vector3 canvasPos = cursorCanvasRect.position;
                canvasPos.z = selectedCanvas.transform.position.z;
                cursorCanvasRect.position = canvasPos;
            }
        }

        //UI커서 활성화
        UI_Cursor.gameObject.SetActive(true);

        // 타겟 위치로 이동
        UI_Cursor.transform.DOMove(btnPos, flickTime)
        .SetUpdate(true);

        // UI 커서 색깔 초기화
        Image cursorImage = UI_Cursor.GetComponent<Image>();
        cursorImage.DOKill();
        cursorImage.DOColor(SystemManager.Instance.HexToRGBA("59AFFF"), flickTime)
        .SetUpdate(true)
        .OnStart(() =>
        {
            cursorImage.color = SystemManager.Instance.HexToRGBA("59AFFF", 0);
        })
        .OnKill(() =>
        {
            cursorImage.color = SystemManager.Instance.HexToRGBA("59AFFF");
        });

        // 타겟과 사이즈 맞추기
        cursorRect.DOSizeDelta(scale, flickTime)
        .SetUpdate(true);

        // 이동 시간 대기
        yield return new WaitForSecondsRealtime(flickTime);

        // 사이즈 커졌다 작아졌다 무한 반복
        cursorRect.DOSizeDelta(scale * 1.1f, flickTime)
        .SetLoops(-1, LoopType.Yoyo)
        .SetUpdate(true)
        .OnStart(() =>
        {
            // 사이즈 초기화
            cursorRect.sizeDelta = scale;
        })
        .OnKill(() =>
        {
            //사이즈 초기화
            cursorRect.sizeDelta = scale;
        });

        // 컬러 초기화
        // image.color = targetOriginColor;
        Color originColor = lastSelected.targetGraphic.color;
        // 컬러 깜빡이기 무한 반복
        lastSelected.targetGraphic.DOColor(flickColor, flickTime)
        .SetLoops(-1, LoopType.Yoyo)
        .SetUpdate(true)
        .OnKill(() =>
        {
            // 컬러 초기화
            lastSelected.targetGraphic.color = originColor;
        });

        // 커서 코루틴 초기화
        cursorAnimCoroutine = null;
    }

    #endregion
}
