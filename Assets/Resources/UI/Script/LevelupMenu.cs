using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelupMenu : MonoBehaviour
{
    [SerializeField] Selectable firstBtn;
    [SerializeField] Image panel;
    [SerializeField] CanvasGroup screen;
    [SerializeField] Transform slots;
    [SerializeField] ParticleSystem slotParticle;
    [SerializeField] Transform attractor;
    private enum GetSlotType { Magic, Shard, Gem };
    [SerializeField] List<float> typeRate = new List<float>();
    [SerializeField] List<float> gradeRate = new List<float>();

    private void OnEnable()
    {
        // 아이콘 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 패널 숨기기
        screen.alpha = 0f;

        //todo 카드 위치 및 사이즈 제로로 초기화

        // 시간 멈추기
        SystemManager.Instance.TimeScaleChange(0f);

        // 파티클 끄기
        slotParticle.gameObject.SetActive(false);

        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 해당 패널로 팝업 초기화
        UIManager.Instance.PopupSet(gameObject);

        // 아이템 3개 랜덤 뽑기
        for (int i = 0; i < 3; i++)
        {
            // 인덱스 캐싱
            int index = i;
            // 얻을 아이템
            SlotInfo getItem = null;

            // 아이콘 찾기
            Image icon = slots.transform.GetChild(i).Find("Slot/Icon").GetComponent<Image>();
            // 프레임 찾기
            Image frame = slots.transform.GetChild(i).Find("Slot/Frame").GetComponent<Image>();
            // 개수 찾기
            TextMeshProUGUI amount = slots.transform.GetChild(i).Find("Slot/Amount").GetComponent<TextMeshProUGUI>();
            // 이름 찾기
            TextMeshProUGUI name = slots.transform.GetChild(i).Find("Spec/Name").GetComponent<TextMeshProUGUI>();
            // 설명 찾기
            TextMeshProUGUI description = slots.transform.GetChild(i).Find("Spec/Description").GetComponent<TextMeshProUGUI>();
            // 툴팁 찾기
            ToolTipTrigger toolTip = slots.transform.GetChild(i).GetComponent<ToolTipTrigger>();
            // 버튼 찾기
            Button button = slots.transform.GetChild(i).GetComponent<Button>();

            // 얻을 아이템 종류 가중치로 뽑기
            int randomType = SystemManager.Instance.WeightRandom(typeRate);
            // 얻을 아이템 등급 가중치로 뽑기
            int randomGrade = SystemManager.Instance.WeightRandom(gradeRate) + 1;

            // 언락 마법, 샤드, 원소젬 중에서 결정
            switch (randomType)
            {
                case (int)GetSlotType.Magic:
                    while (getItem == null)
                    {
                        // 언락 마법 중 하나 뽑기
                        getItem = MagicDB.Instance.GetRandomMagic(randomGrade);

                        // 실패하면 등급 다시 뽑기
                        randomGrade = SystemManager.Instance.WeightRandom(gradeRate);
                    }

                    break;
                case (int)GetSlotType.Shard:

                    // 1~6등급 중에 샤드 하나 뽑기
                    getItem = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Shard, randomGrade);

                    break;
                case (int)GetSlotType.Gem:

                    // 원소젬 중에 하나 뽑기
                    getItem = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Gem);

                    // 원소젬 개수 랜덤
                    ItemInfo item = getItem as ItemInfo;
                    item.amount = Random.Range(1, 11) * 10;
                    getItem = item;
                    break;
            }

            // print(index + " : " + randomType + " : " + randomGrade + " : " + getItem.name);

            // 아이콘 찾기
            Sprite sprite = null;
            if (getItem as MagicInfo != null)
                sprite = MagicDB.Instance.GetIcon(getItem.id);
            if (getItem as ItemInfo != null)
                sprite = ItemDB.Instance.GetIcon(getItem.id);

            // 아이콘 넣기
            icon.sprite = sprite;

            // 마법, 샤드일때
            if (randomType == (int)GetSlotType.Magic
            || randomType == (int)GetSlotType.Shard)
            {
                // 프레임 색 변경
                frame.color = MagicDB.Instance.GradeColor[getItem.grade];

                // 아이템 개수 끄기
                amount.gameObject.SetActive(false);
            }
            // 원소젬일때
            else
            {
                // 흰색으로 초기화
                frame.color = Color.white;

                // 아이템 개수 끄기
                amount.gameObject.SetActive(true);
                // 아이템 개수 넣기
                ItemInfo item = getItem as ItemInfo;
                amount.text = item.amount.ToString();
            }

            // 툴팁 정보 넣기            
            toolTip._slotInfo = getItem;
            toolTip.enabled = true;

            // 아이템 이름 넣기
            name.text = getItem.name;
            // 아이템 설명 넣기
            description.text = getItem.description;

            // 버튼 이벤트 새로 넣기
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                ClickSlot(index, getItem);
            });
        }

        // 카드 왼쪽부터 순서대로 진행

        //todo 카드 사이즈 확장
        //todo 카드 이동
        //todo 카드 회전

        //todo 카드 inBack으로 살짝 커졌다가 원래 사이즈
        //todo 카드 inBack으로 살짝 올렸다가 원위치
        //todo 카드 진동
        //todo 카드 샤이닝 이펙트

        // 패널 나타내기
        DOTween.To(() => screen.alpha, x => screen.alpha = x, 1f, 0.5f)
        .SetUpdate(true);
        // 버튼 상호작용 풀기
        screen.interactable = true;
        // 레이캐스트 막기
        screen.blocksRaycasts = true;

        yield return new WaitForSecondsRealtime(0.5f);

        // 모든 버튼 상호작용 켜기
        Button[] btns = screen.GetComponentsInChildren<Button>();
        for (int i = 0; i < btns.Length; i++)
            btns[i].interactable = true;

        // 가운데 슬롯 선택하기
        UICursor.Instance.UpdateLastSelect(firstBtn);
    }

    void ClickSlot(int index, SlotInfo slotInfo)
    {
        // 아이템 선택
        StartCoroutine(ChooseSlot(index, slotInfo));
    }

    public IEnumerator ChooseSlot(int index, SlotInfo slotInfo)
    {
        // 모든 버튼 상호작용 끄기 (UI커서 선택 방지)
        Button[] btns = screen.GetComponentsInChildren<Button>();
        for (int i = 0; i < btns.Length; i++)
            btns[i].interactable = false;

        // 버튼 상호작용 막기 (중복 선택 방지)
        screen.interactable = false;
        // 레이캐스트 풀기
        screen.blocksRaycasts = false;

        Transform slot = slots.transform.GetChild(index);

        // 패널 투명해지며 숨기기
        DOTween.To(() => screen.alpha, x => screen.alpha = x, 0f, 0.2f)
        .SetUpdate(true);

        // UI 커서 끄기
        UICursor.Instance.UICursorToggle(false);

        // 드랍위치 계산
        Vector2 dropPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * 2f;

        // 아이템 드랍 위치로 어트랙터 옮기기
        attractor.position = dropPos;

        // 선택된 슬롯 뒤에 슬롯모양 파티클 생성
        slotParticle.transform.position = slot.position;
        slotParticle.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(0.5f);

        // 드랍 위치 해당 아이템 드랍
        // print("drop : " + slotInfo.name);
        StartCoroutine(ItemDB.Instance.ItemDrop(slotInfo, dropPos));

        // 패널 닫고 시간정지 해제        
        UIManager.Instance.PopupUI(UIManager.Instance.levelupPanel, false);
    }
}
