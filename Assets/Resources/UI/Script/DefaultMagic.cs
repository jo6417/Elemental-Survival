using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DefaultMagic : MonoBehaviour
{
    [SerializeField] Image panel;
    [SerializeField] CanvasGroup screen;
    [SerializeField] CanvasGroup slots;
    [SerializeField] Image blockScreen; // 화면 가림막
    [SerializeField] ParticleSystem slotParticle;
    [SerializeField] Transform attractor;
    [SerializeField] Button firstSelect; // 초기 선택된 버튼

    private void OnEnable()
    {
        // 아이콘 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => SystemManager.Instance != null);
        // 시간 멈추기
        SystemManager.Instance.TimeScaleChange(0f);

        // 기본 마법 스크린 보이기
        screen.alpha = 1f;
        // 버튼 상호작용 풀기
        screen.interactable = true;
        // 레이캐스트 막기
        screen.blocksRaycasts = true;

        // 화면 가림막 켜기
        blockScreen.enabled = true;
        blockScreen.color = Color.black;

        // 파티클 끄기
        slotParticle.gameObject.SetActive(false);

        slots.alpha = 1f;
        panel.color = new Color32(0, 0, 0, 100);

        yield return new WaitUntil(() => MagicDB.Instance != null && MagicDB.Instance.loadDone);

        // 해당 패널로 팝업 초기화
        UIManager.Instance.PopupSet(gameObject);

        // 1등급 6개 마법 불러오기
        for (int i = 0; i < 6; i++)
        {
            // 마법 정보 찾기
            MagicInfo magic = MagicDB.Instance.GetMagicByID(i);
            // 아이콘 찾기
            Sprite sprite = MagicDB.Instance.GetIcon(magic.id);

            // 아이콘 넣기
            slots.transform.GetChild(i).Find("Icon").GetComponent<Image>().sprite = sprite;

            // 툴팁 정보 넣기
            ToolTipTrigger toolTip = slots.transform.GetChild(i).GetComponent<ToolTipTrigger>();
            toolTip._slotInfo = magic;
            toolTip.enabled = true;

            // 버튼 이벤트 넣기
            int index = i;
            slots.transform.GetChild(i).GetComponent<Button>().onClick.AddListener(() =>
            {
                ClickSlot(index);
            });
        }

        // 화면 가림막 투명해지며 제거
        blockScreen.DOColor(Color.clear, 1f)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            blockScreen.enabled = false;
        });

        // 초기 버튼 선택
        firstSelect.Select();
    }

    void ClickSlot(int index)
    {
        StartCoroutine(ChooseMagic(index));
    }

    public IEnumerator ChooseMagic(int index)
    {
        // 버튼 상호작용 막기 (중복 선택 방지)
        screen.interactable = false;
        // 레이캐스트 풀기
        screen.blocksRaycasts = false;

        Transform slot = slots.transform.GetChild(index);

        // 핸드폰 키입력 막기
        PhoneMenu.Instance.InteractBtnsToggle(false);
        PhoneMenu.Instance.Phone_Input.Disable();
        //플레이어 입력 끄기
        PlayerManager.Instance.player_Input.Disable();
        // 핸드폰 열기
        StartCoroutine(PhoneMenu.Instance.OpenPhone());

        //현재 열려있는 팝업 갱신
        UIManager.Instance.nowOpenPopup = UIManager.Instance.phonePanel;

        // 슬롯들 그룹 알파값 0으로 사라지기
        DOTween.To(() => slots.alpha, x => slots.alpha = x, 0f, 0.8f)
        .SetUpdate(true);
        // 패널 배경 투명하게
        panel.DOColor(Color.clear, 0.8f)
        .SetUpdate(true);

        // 선택된 슬롯 뒤에 슬롯모양 파티클 생성
        slotParticle.transform.position = slot.position;
        slotParticle.gameObject.SetActive(true);

        // 핸드폰 다 열릴때까지 대기
        yield return new WaitUntil(() => CastMagic.Instance.phone.localScale == Vector3.one);

        // 핸드폰 키입력 막기
        PhoneMenu.Instance.InteractBtnsToggle(false);

        // 빈칸으로 파티클 attractor 옮기기
        attractor.position = PhoneMenu.Instance.invenSlots[PhoneMenu.Instance.GetEmptySlot()].transform.position;

        yield return new WaitForSecondsRealtime(1f);

        // 빈칸에 해당 마법 획득
        PhoneMenu.Instance.GetMagic(MagicDB.Instance.GetMagicByID(index));

        yield return new WaitForSecondsRealtime(1f);

        // 핸드폰 닫기 및 게임시작
        PhoneMenu.Instance.ClosePhone();

        // 패널 닫기
        gameObject.SetActive(false);
    }
}
