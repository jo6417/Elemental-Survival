using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Pool;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class MagicMixMenu : MonoBehaviour
{
    [Header("Refer")]
    public GameObject classPrefab; //클래스 전체 프리팹
    public GameObject magicIconPrefab; //마법 아이콘 프리팹
    public GameObject noMagicObj; //합성 가능한 마법 없을때 팝업

    public Scrollbar leftScrollbar; //왼쪽 스크롤바
    public Scrollbar rightScrollbar; //오른쪽 스크롤바

    public Transform leftContainer; //왼쪽 마법 리스트
    public Transform rightContainer; //오른쪽 마법 리스트
    List<MagicInfo> notHasMagic = new List<MagicInfo>(); //플레이어가 보유하지 않은 마법 리스트

    private MagicInfo leftMagic; //왼쪽에서 선택된 마법
    private MagicInfo rightMagic; //오른쪽에서 선택된 마법
    private MagicInfo mixedMagic = null;

    public Transform leftIcon; //왼쪽에서 선택된 마법 오브젝트
    public Transform rightIcon; //오른쪽에서 선택된 마법 오브젝트

    public Transform leftInfoPanel; //왼쪽에서 선택된 마법 정보창
    public Transform rightInfoPanel; //오른쪽에서 선택된 마법 정보창
    public Transform mixInfoPanel; //합성된 마법 정보창

    private void OnEnable()
    {
        // 레벨업 메뉴에 마법 정보 넣기
        if (MagicDB.Instance.loadDone)
            SetMenu();
    }

    void SetMenu()
    {
        DOTween.Clear();

        //시간 멈추기
        Time.timeScale = 0;

        // 보유하지 않은 마법만 DB에서 파싱
        notHasMagic.Clear();
        notHasMagic = MagicDB.Instance.magicDB.FindAll(x => x.magicLevel == 0);

        // 플레이어 보유중인 마법 참조
        List<MagicInfo> playerMagics = PlayerManager.Instance.hasMagics;

        //왼쪽 페이지 채우기
        SetPage(playerMagics, true);
        //오른쪽 페이지 채우기
        SetPage(playerMagics, false);
    }

    void SetPage(List<MagicInfo> playerMagics, bool isLeft)
    {
        //해당 페이지의 선택된 마법 아이콘
        Transform selectIcon = isLeft ? leftIcon : rightIcon;
        //마법 아이콘 비활성화
        selectIcon.gameObject.SetActive(false);

        //해당 페이지의 선택된 마법 정보창
        Transform selectInfoPanel = isLeft ? leftInfoPanel : rightInfoPanel;
        //좌,우 정보창 비활성화
        selectInfoPanel.gameObject.SetActive(false);

        //해당 페이지의 스크롤바
        Scrollbar scrollbar = isLeft ? leftScrollbar : rightScrollbar;

        //마법 리스트
        Transform container = isLeft ? leftContainer : rightContainer;
        // 리스트의 자식 모두 제거
        UIManager.Instance.DestroyChildren(container);
        // 리스트 활성화
        container.gameObject.SetActive(true);

        // 모든 마법 정보 비우기
        leftMagic = null;
        rightMagic = null;
        mixedMagic = null;

        // 해당 등급 마법 있으면 class 숫자 수정하고 마법 아이콘 추가
        for (int i = 6; i >= 1; i--) // 6 ~ 1 등급순
        {
            if (playerMagics.Exists(x => x.grade == i))
            {
                GameObject classMenu = LeanPool.Spawn(classPrefab, container);

                //등급 타이틀
                Transform classTitle = classMenu.transform.Find("ClassTitle");
                TextMeshProUGUI classTxt = classTitle.GetComponentInChildren<TextMeshProUGUI>();

                //등급 색깔 넣기
                classTitle.GetComponent<Image>().color = MagicDB.Instance.gradeColor[i];
                //등급 텍스트 넣기
                classTxt.text = "Class " + i;

                //해당 등급 마법 아이콘 넣을 그리드 오브젝트 찾기
                Transform magicList = classMenu.transform.Find("Magics");

                //해당 등급 마법 모두 찾기
                List<MagicInfo> magics = new List<MagicInfo>();
                magics = playerMagics.FindAll(x => x.grade == i);

                //해당 등급 모든 마법 아이콘 넣기
                foreach (var magic in magics)
                {
                    //해당 등급 리스트에 마법 아이콘 스폰
                    GameObject magicIcon = LeanPool.Spawn(magicIconPrefab, magicList);

                    // 마법 등급 넣기
                    magicIcon.transform.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[i];

                    // 마법 아이콘 이미지 넣기
                    magicIcon.transform.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.magicIcon.Find(
                    x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

                    // 아이콘의 마법 정보 넣기
                    magicIcon.GetComponent<MagicHolder>().magic = magic;

                    // 툴팁에 상품 정보 넣기
                    ToolTipTrigger tooltip = magicIcon.GetComponent<ToolTipTrigger>();
                    tooltip.toolTipType = ToolTipTrigger.ToolTipType.ProductTip;
                    tooltip.magic = magic;

                    // 아이콘 눌렀을때 일어날 이벤트 넣기
                    Button btn = magicIcon.GetComponent<Button>();
                    btn.onClick.AddListener(delegate
                    {
                        // print(selectMagic.position);

                        // 선택된 마법 가져오기
                        MagicInfo selectMagic = isLeft ? leftMagic : rightMagic;

                        // 마법 아이콘에 등급 넣기
                        selectIcon.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[magic.grade];

                        // 마법 아이콘 이미지 넣기
                        selectIcon.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.magicIcon.Find(
                        x => x.name == magic.magicName.Replace(" ", "") + "_Icon");

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

                        // 해당 아이콘 자리로 selectIcon가 이동
                        selectIcon.position = magicIcon.transform.position;
                        // 아이콘 사이즈 줄이기
                        selectIcon.localScale = Vector3.one;

                        // 아이콘 사이즈 키우기
                        selectIcon.DOScale(Vector3.one * 2f, 0.5f)
                        .SetUpdate(true);

                        // 선택한 아이콘이 페이지 가운데로 domove
                        selectIcon
                        .DOMove(infoIcon.position, 0.5f)
                        .SetUpdate(true)
                        .SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            // 정보창 활성화
                            selectInfoPanel.gameObject.SetActive(true);

                            //좌,우 각각 마법 정보 넣기
                            if (isLeft)
                                leftMagic = magic;
                            else
                                rightMagic = magic;

                            // 양쪽 마법 둘다 선택되면
                            if (leftMagic != null && rightMagic != null && mixedMagic == null)
                                //마법 합성 시작
                                MixMagic();
                        });
                    });
                }
                // UI 레이아웃 리빌드하기
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)classMenu.transform);
            }
        }

        //해당 페이지의 스크롤바 초기화
        scrollbar.value = 1f;
    }

    public void BackPage(bool isLeft)
    {
        //해당 페이지의 선택된 마법 아이콘
        Transform selectIcon = isLeft ? leftIcon : rightIcon;
        //해당 페이지의 선택된 마법 정보창
        Transform selectInfoPanel = isLeft ? leftInfoPanel : rightInfoPanel;
        //마법 리스트
        Transform parent = isLeft ? leftContainer : rightContainer;

        //좌,우 각각 마법 정보 없에기
        if (isLeft)
            leftMagic = null;
        else
            rightMagic = null;

        // 마법 아이콘 비활성화
        selectIcon.gameObject.SetActive(false);

        // 정보창 비활성화
        selectInfoPanel.gameObject.SetActive(false);

        // 마법 리스트 활성화
        parent.gameObject.SetActive(true);
    }

    void MixMagic()
    {
        Sequence mixSeq = DOTween.Sequence();

        //두 재료 모든 갖고 있는 마법 찾기
        mixedMagic = MagicDB.Instance.magicDB.Find(y => y.element_A == leftMagic.magicName && y.element_B == rightMagic.magicName);
        if (mixedMagic == null)
            mixedMagic = MagicDB.Instance.magicDB.Find(y => y.element_A == rightMagic.magicName && y.element_B == leftMagic.magicName);

        if (mixedMagic == null)
        {
            print("합성 가능한 마법이 없습니다");
            //TODO 합성 불가 메시지 띄우기

            // 양쪽 아이콘 떨리기
            leftIcon.DOPunchPosition(Vector2.right * 10f, 1f)
                .SetUpdate(true);
            rightIcon.DOPunchPosition(Vector2.right * 10f, 1f)
                .SetUpdate(true);

            return;
        }

        print("합성 성공 : " + mixedMagic.magicName);

        //좌,우 정보창 끄기
        leftInfoPanel.gameObject.SetActive(false);
        rightInfoPanel.gameObject.SetActive(false);

        //합성 마법 정보창 찾기
        Transform mixInfo = mixInfoPanel.Find("Mix_MagicInfo");

        // 이름 테두리에 등급 색깔 넣기
        Transform title = mixInfo.Find("TitleFrame");
        title.GetComponent<Image>().color = MagicDB.Instance.gradeColor[mixedMagic.grade];
        //이름 넣기
        title.Find("Title").GetComponent<TextMeshProUGUI>().text = mixedMagic.magicName;

        //아이콘 및 등급색 넣기
        Transform mixedIcon = mixInfo.Find("MagicIcon");
        mixedIcon.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.magicIcon.Find(
                    x => x.name == mixedMagic.magicName.Replace(" ", "") + "_Icon");
        mixedIcon.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[mixedMagic.grade];
        mixedIcon.localScale = Vector2.zero;

        //마법 설명 넣기
        mixInfo.Find("Descript").GetComponent<TextMeshProUGUI>().text = "마법정보 : " + mixedMagic.description;

        //좌,우 아이콘 사이즈 줄이면서 가운데로 모으기
        float seqDuration = 0.5f;
        mixSeq.Append(
            leftIcon.DOMove(mixInfo.position, seqDuration)
            .SetEase(Ease.InBack)
        )
        .Join(
            leftIcon.DOScale(Vector3.one, seqDuration)
        )
        .Join(
            rightIcon.DOMove(mixInfo.position, seqDuration)
            .SetEase(Ease.InBack)
        )
        .Join(
            rightIcon.DOScale(Vector3.one, seqDuration)
        )
        .OnComplete(() =>
        {
            //합성된 마법 정보창 활성화
            mixInfoPanel.gameObject.SetActive(true);

            //아이콘 커지면서 등장
            mixedIcon.DOScale(Vector3.one * 2f, 0.5f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
            //TODO 가운데에서 빛 스프라이트 커짐

        })
        .SetUpdate(true);
    }

    public void ClosePopup()
    {
        //합성 마법 페이지 닫기
        mixInfoPanel.gameObject.SetActive(false);

        // 마법 합성 팝업 닫기
        UIManager.Instance.PopupUI(UIManager.Instance.magicMixUI);

        // 마법 획득하기
        PlayerManager.Instance.GetMagic(mixedMagic);
    }
}
