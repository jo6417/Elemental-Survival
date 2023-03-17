using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Lean.Pool;
using DG.Tweening;
using System.Linq;

public class MainMenuBtn : MonoBehaviour
{
    public GameObject buttonParent;
    public GameObject mainMenuPanel;
    public GameObject characterSelectUI;
    public GameObject shopUI;
    public CanvasGroup collectionUI;
    public GameObject optionUI;

    [Header("Collection")]
    [SerializeField] Transform collectionContent;
    [SerializeField] Transform collectionSlotPrefab;
    [SerializeField] Button clickModeButton;
    enum ClickMode { BanMode, LockMode }; // 마법 밴 토글모드 및 해금 토글모드
    [SerializeField, ReadOnly] ClickMode clickMode;
    [SerializeField, ReadOnly] List<int> temp_banMagicList = new List<int>();
    [SerializeField] GameObject modeTooltip; // 모드 전환 툴팁

    [Header("Buttons")]
    [SerializeField] Button play_btn;
    [SerializeField] Button shop_btn;
    [SerializeField] Button collection_btn;
    [SerializeField] Button option_btn;
    [SerializeField] Button quit_btn;

    private Button preSelectedObj;

    private void Awake()
    {
        play_btn.onClick.AddListener(() => { Play(); });
        shop_btn.onClick.AddListener(() => { Shop(); });
        collection_btn.onClick.AddListener(() => { Collection(); });
        option_btn.onClick.AddListener(() => { Option(); });
        quit_btn.onClick.AddListener(() => { Quit(); });
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => SystemManager.Instance != null);

        // 시간 속도 초기화
        // SystemManager.Instance.TimeScaleChange(1f);

        // ui 커서 초기화까지 대기
        yield return new WaitUntil(() => UICursor.Instance != null);

        // 메인메뉴 패널 켜기
        BackToMenu();

        // 마우스 커서 전환
        UICursor.Instance.CursorChange(true);

        // 씬이동 끝내기
        SystemManager.Instance.sceneChanging = false;

        // 시스템 초기화까지 대기
        yield return new WaitUntil(() => SystemManager.Instance.initDone);

        // 첫번째 버튼 선택
        UICursor.Instance.UpdateLastSelect(play_btn);
    }

    public void CharacterSelect()
    {
        // 캐릭터 선택 UI 토글
        characterSelectUI.SetActive(!characterSelectUI.activeSelf);
    }

    public void Play()
    {
        // 버튼 Select 해제
        UICursor.Instance.UpdateLastSelect(null);

        // 스테이지 변수 첫맵으로 초기화
        SystemManager.Instance.NowMapElement = MapElement.Earth;

        // 로딩하고 인게임 씬 띄우기
        SystemManager.Instance.StartGame();
    }

    public void Shop()
    {
        // 상점 UI 띄우기
        shopUI.SetActive(!shopUI.activeSelf);
    }

    public void Collection()
    {
        // 도감 패널 켜기
        collectionUI.gameObject.SetActive(!collectionUI.gameObject.activeSelf);

        // 도감 패널 초기화
        InitCollectionPanel();
    }

    public void Option()
    {
        // 메인메뉴 끄기
        mainMenuPanel.SetActive(false);

        // 옵션 UI 켜기
        optionUI.SetActive(true);
    }

    public void Quit()
    {
        // 게임 종료
        print("QuitGame");
        Application.Quit();
    }

    public void BackToMenu()
    {
        // 메인메뉴 켜기
        mainMenuPanel.SetActive(true);

        // 첫번째 버튼 선택
        UICursor.Instance.UpdateLastSelect(play_btn);

        // 다른 패널 끄기
        characterSelectUI.SetActive(false);
        shopUI.SetActive(false);
        collectionUI.gameObject.SetActive(false);
        optionUI.SetActive(false);
    }

    public void QuitCollection()
    {
        // 이전 밴 리스트와 다르면 저장
        if (!temp_banMagicList.SequenceEqual(MagicDB.Instance.banMagicList))
        {
            // 밴 리스트 갱신
            MagicDB.Instance.banMagicList = new List<int>(temp_banMagicList);

            // 저장
            StartCoroutine(SaveManager.Instance.Save());
        }

        // 메뉴로 돌아가기
        BackToMenu();
    }

    public void InitCollectionPanel()
    {
        // 도감 불러오기 코루틴 실행
        StartCoroutine(LoadCollection());
    }

    private IEnumerator LoadCollection()
    {
        // 도감 패널 투명하게
        collectionUI.alpha = 0;

        // 밴 리스트 복사
        temp_banMagicList = new List<int>(MagicDB.Instance.banMagicList);

        // 마법 DB 초기화 대기
        yield return new WaitUntil(() => MagicDB.Instance.initDone);

        // 마법 DB 캐싱
        Dictionary<int, MagicInfo> _magicDB = MagicDB.Instance.magicDB;

        // 마법 DB 개수만큼 반복
        for (int i = 0; i < _magicDB.Count; i++)
        {
            // 각 슬롯 UI
            Transform magicSlot = null;

            // 남는 슬롯이 있으면
            if (collectionContent.childCount > i && collectionContent.GetChild(i) != null)
                // 해당 슬롯 참조
                magicSlot = collectionContent.GetChild(i);
            // 남는 슬롯이 없으면
            else
                // 마법 슬롯 UI 생성
                magicSlot = LeanPool.Spawn(collectionSlotPrefab, collectionContent);

            // 마법 정보 찾기
            MagicInfo magic = MagicDB.Instance.GetMagicByID(_magicDB[i].id);
            // 재료들 정보 찾기
            MagicInfo elementA = MagicDB.Instance.GetMagicByName(magic.element_A);
            MagicInfo elementB = MagicDB.Instance.GetMagicByName(magic.element_B);
            // 해당 마법 해금 여부 판단
            bool isUnlock = MagicDB.Instance.unlockMagicList.Exists(x => x == magic.id);
            // 해당 마법 밴 여부 판단
            bool isDisable = temp_banMagicList.Exists(x => x == magic.id);

            // 아이콘 및 프레임 찾기
            Image iconImage = magicSlot.transform.Find("Icon").GetComponent<Image>();
            Image frameImage = magicSlot.transform.Find("Frame").GetComponent<Image>();
            GameObject shinyMask = magicSlot.transform.Find("ShinyMask").gameObject;
            // 툴팁 트리거 찾기
            ToolTipTrigger tooltip = magicSlot.GetComponent<ToolTipTrigger>();
            // 커버 찾기
            GameObject blackCover = magicSlot.transform.Find("Cover").gameObject;
            // 잠금 커버 찾기
            GameObject banCover = magicSlot.transform.Find("Lock").gameObject;
            // 잠금 표시 찾기
            GameObject lockIcon = magicSlot.transform.Find("Lock/LockIcon").gameObject;

            // 아이콘 스프라이트 찾기
            Sprite iconSprite = MagicDB.Instance.GetIcon(magic.id);
            // 프레임 등급 색깔 지정
            Color frameColor = MagicDB.Instance.GradeColor[magic.grade];

            // 프리팹이 없으면
            if (MagicDB.Instance.GetMagicPrefab(magic.id) == null)
            {
                // 미해금으로 처리
                isUnlock = false;

                // 아이콘은 물음표로 대체
                iconSprite = SystemManager.Instance.questionMark;
                // 색깔은 흰색으로 대체
                frameColor = Color.white;
            }

            // 아이콘 컬러 초기화
            iconImage.color = isUnlock ? Color.white : Color.black;

            // 미해금이면 커버 씌우기
            blackCover.SetActive(!isUnlock);

            // 해금은 됬지만 밴 마법일때 자물쇠로 덮기
            if (isUnlock && isDisable)
                banCover.SetActive(true);
            else
                banCover.SetActive(false);

            // 아이콘 표시
            iconImage.sprite = iconSprite;
            // 아이콘 프레임 색 넣기
            frameImage.color = frameColor;

            // 툴팁에 정보 넣고 켜기
            tooltip._slotInfo = magic;
            tooltip.enabled = true;

            // 마우스 올렸을때 이벤트
            tooltip.onMouseEnter = () =>
            {
                // 사운드 재생
                SoundManager.Instance.PlaySound("SelectButton");
            };

            // 마우스 클릭했을때 이벤트
            tooltip.onMouseClick = () =>
            {
                // 잠금 사운드 재생
                SoundManager.Instance.PlaySound("SlotLock");

                // 밴 여부 다시 받아오기
                isDisable = temp_banMagicList.Exists(x => x == magic.id);
                // 프리팹이 있으면
                if (MagicDB.Instance.GetMagicPrefab(magic.id) != null)
                    // 해금 여부 다시 가져오기
                    isUnlock = MagicDB.Instance.unlockMagicList.Exists(x => x == magic.id);

                // 현재 밴모드일때
                if (clickMode == ClickMode.BanMode)
                {
                    // 해금된 마법만 가능
                    if (isUnlock)
                    {
                        // // 밴 전환
                        // isDisable = !isDisable;

                        // 현재 밴이면
                        if (isDisable)
                            // 입력된 마법 ID를 밴 리스트에서 제거
                            temp_banMagicList.Remove(magic.id);
                        // 현재 잠겼으면
                        else
                        {
                            // 밴 리스트에 없으면
                            if (!temp_banMagicList.Contains(magic.id))
                                // 밴 리스트에 저장
                                temp_banMagicList.Add(magic.id);
                        }

                        // 밴 마법이면 자물쇠 표시
                        banCover.SetActive(!isDisable);

                        // 슬롯 빛나는 효과 재생
                        shinyMask.SetActive(false);
                        shinyMask.SetActive(true);
                    }
                }
                // // 미해금 마법 클릭 이벤트
                // 현재 해금 모드일때
                else
                {
                    // 해금 전환
                    isUnlock = !isUnlock;

                    // 마법 잠그기
                    if (!isUnlock)
                    {
                        // 입력된 마법 ID를 해금 리스트에서 제거
                        MagicDB.Instance.unlockMagicList.Remove(magic.id);
                        // 입력된 마법 ID를 밴 리스트에서 제거
                        temp_banMagicList.Remove(magic.id);

                        // 밴 표시 없에기
                        banCover.SetActive(false);
                    }
                    // 해금 하기
                    else
                    {
                        // 해금 리스트에 없으면
                        if (!MagicDB.Instance.unlockMagicList.Contains(magic.id))
                            // 해금 리스트에 저장
                            MagicDB.Instance.unlockMagicList.Add(magic.id);
                    }

                    // 아이콘 컬러 초기화
                    iconImage.color = isUnlock ? Color.white : Color.black;
                    // 검은색 커버 활성화 전환
                    blackCover.SetActive(!isUnlock);
                    // 아이콘 프레임 색 넣기
                    frameImage.color = isUnlock ? frameColor : Color.white;

                    // 슬롯 빛나는 효과 재생
                    shinyMask.SetActive(false);
                    shinyMask.SetActive(true);
                }
            };
        }

        // 남는 슬롯 개수
        int restSlotNum = collectionContent.childCount - _magicDB.Count;
        // 남는 슬롯 있다면 모두 삭제
        for (int i = collectionContent.childCount; i < restSlotNum; i++)
            LeanPool.Despawn(collectionContent.GetChild(i));


        // 도감 패널 알파값 올려서 보이기        
        DOTween.To(() => collectionUI.alpha, x => collectionUI.alpha = x, 1f, 0.5f).SetUpdate(true);
    }

    public void ResetBan()
    {
        for (int i = 0; i < collectionContent.childCount; i++)
        {
            // 밴 표시 UI 찾기
            GameObject lockCover = collectionContent.GetChild(i).transform.Find("Lock").gameObject;
            // 모든 밴 표시 끄기
            lockCover.SetActive(false);
        }

        // 밴 리스트 모두 제거
        temp_banMagicList.Clear();
    }

    public void ModeOnClick()
    {
        // 슬롯 클릭할때 액션을 해금,밴 중에 선택하도록 토글
        clickMode = clickMode == ClickMode.BanMode ? ClickMode.LockMode : ClickMode.BanMode;

        // 버튼 색 바꾸기
        clickModeButton.targetGraphic.color = clickMode == ClickMode.BanMode ? Color.red : Color.green;
        // 버튼 텍스트 바꾸기
        clickModeButton.transform.Find("Mode").GetComponent<TextMeshProUGUI>().text = clickMode == ClickMode.BanMode ? "Ban Mode" : "Lock Mode";
        // 툴팁 텍스트 바꾸기
        clickModeButton.transform.Find("Tooltip/Text").GetComponent<TextMeshProUGUI>().text = clickMode == ClickMode.BanMode
        ? "슬롯 클릭시\n<color=red><size=35>BAN</color></size> 전환"
        : "슬롯 클릭시\n<color=green><size=35>LOCK</color></size> 전환";
    }
}
