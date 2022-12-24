using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LevelupMenu : MonoBehaviour
{
    [SerializeField] Image panel;
    [SerializeField] CanvasGroup screen;
    [SerializeField] Transform slots;
    [SerializeField] ParticleSystem slotParticle;
    [SerializeField] Transform attractor;
    private enum GetSlotType { Magic, Shard, Gem };

    private void OnEnable()
    {
        // 아이콘 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 패널 숨기기
        screen.alpha = 0f;

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
            // 얻을 아이템
            SlotInfo getItem = new SlotInfo();

            // 언락 마법, 샤드, 원소젬 중에서 결정
            switch (Random.Range(0, 3))
            {
                case (int)GetSlotType.Magic:

                    // 언락 마법 중 하나 뽑기
                    getItem = MagicDB.Instance.GetMagicByID(i);

                    break;
                case (int)GetSlotType.Shard:

                    // 1~6등급 중에 샤드 하나 뽑기
                    getItem = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Shard);

                    break;
                case (int)GetSlotType.Gem:

                    // 원소젬 중에 하나 뽑기
                    getItem = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Gem);

                    // 원소젬 개수 랜덤
                    ItemInfo item = getItem as ItemInfo;
                    item.amount = Random.Range(1, 11) * 10;

                    break;
            }

            // 아이콘 찾기
            Sprite sprite = null;
            if (getItem as MagicInfo != null)
                sprite = MagicDB.Instance.GetIcon(getItem.id);
            if (getItem as ItemInfo != null)
                sprite = ItemDB.Instance.GetIcon(getItem.id);

            // 아이콘 넣기
            slots.transform.GetChild(i).Find("Icon").GetComponent<Image>().sprite = sprite;

            // 툴팁 정보 넣기
            ToolTipTrigger toolTip = slots.transform.GetChild(i).GetComponent<ToolTipTrigger>();
            toolTip._slotInfo = getItem;
            toolTip.enabled = true;

            //todo 아래에 아이템 설명 넣기

            // 버튼 이벤트 넣기
            int index = i;
            slots.transform.GetChild(i).GetComponent<Button>().onClick.AddListener(() =>
            {
                ClickSlot(index, getItem);
            });
        }

        // 패널 나타내기
        DOTween.To(() => screen.alpha, x => screen.alpha = x, 1f, 0.5f)
        .SetUpdate(true);
        // 버튼 상호작용 풀기
        screen.interactable = true;
        // 레이캐스트 막기
        screen.blocksRaycasts = true;

        yield return new WaitForSeconds(0.5f);

        //todo 가운데 슬롯 선택하기
        // Button btn = slots.transform.GetChild(1).GetComponent<Button>();
        // UIManager.Instance.lastSelected = btn;
        // UIManager.Instance.SelectObject(slots.transform.GetChild(1).gameObject);
        // btn.Select();
    }

    void ClickSlot(int index, SlotInfo slotInfo)
    {
        // 아이템 선택
        StartCoroutine(ChooseSlot(index, slotInfo));
    }

    public IEnumerator ChooseSlot(int index, SlotInfo slotInfo)
    {
        // 버튼 상호작용 막기 (중복 선택 방지)
        screen.interactable = false;
        // 레이캐스트 풀기
        screen.blocksRaycasts = false;

        Transform slot = slots.transform.GetChild(index);

        // 패널 투명해지며 숨기기
        DOTween.To(() => screen.alpha, x => screen.alpha = x, 0f, 0.2f)
        .SetUpdate(true);

        // UI 커서 끄기
        UIManager.Instance.UICursorToggle(false);

        // 드랍위치 계산
        Vector2 dropPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle.normalized * 2f;

        //todo 아이템 드랍 위치로 어트랙터 옮기기
        attractor.position = Camera.main.WorldToScreenPoint(dropPos);

        // 선택된 슬롯 뒤에 슬롯모양 파티클 생성
        slotParticle.transform.position = slot.position;
        slotParticle.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(0.3f);

        // 드랍 위치 해당 아이템 드랍
        StartCoroutine(ItemDB.Instance.ItemDrop(slotInfo, dropPos));

        // 패널 닫고 시간정지 해제        
        UIManager.Instance.PopupUI(UIManager.Instance.levelupPanel, false);
    }
}
