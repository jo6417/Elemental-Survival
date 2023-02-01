using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelupMenu : MonoBehaviour
{
    [SerializeField] Selectable firstBtn;
    [SerializeField] CanvasGroup allGroup; // 전체 캔버스 그룹
    [SerializeField] CanvasGroup background; // 반투명 검은 배경
    [SerializeField] Transform[] cards = new Transform[3]; // 카드 위치를 위한 오브젝트
    [SerializeField] GameObject[] dustEffects = new GameObject[3]; // 카드 먼지 이펙트
    [SerializeField] Transform[] slots = new Transform[3]; // 카드에 들어갈 정보들
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
        // 배경 색 투명하게
        background.alpha = 0f;

        // 전체 상호작용 막기
        allGroup.interactable = false;

        // 시간 멈추기
        SystemManager.Instance.TimeScaleChange(0f);

        // 파티클 끄기
        slotParticle.gameObject.SetActive(false);

        // UI 중심 위치
        Vector3 panelPos = Camera.main.transform.position; panelPos.z = -10f;
        // 카드 초기화
        for (int i = 0; i < cards.Length; i++)
        {
            // 알파값 초기화
            cards[i].GetComponent<CanvasGroup>().alpha = 1;

            // 위치 초기화
            cards[i].position = panelPos;
            // 사이즈 초기화
            cards[i].localScale = Vector3.zero;
            // 먼지 파티클 끄기
            dustEffects[i].SetActive(false);
        }

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
            Image icon = slots[index].Find("Slot/Icon").GetComponent<Image>();
            // 프레임 찾기
            Image frame = slots[index].Find("Slot/Frame").GetComponent<Image>();
            // 개수 찾기
            TextMeshProUGUI amount = slots[index].Find("Slot/Amount").GetComponent<TextMeshProUGUI>();
            // 이름 찾기
            TextMeshProUGUI name = slots[index].Find("Spec/Name").GetComponent<TextMeshProUGUI>();
            // 설명 찾기
            TextMeshProUGUI description = slots[index].Find("Spec/Description").GetComponent<TextMeshProUGUI>();
            // 툴팁 찾기
            ToolTipTrigger toolTip = slots[index].GetComponent<ToolTipTrigger>();
            // 버튼 찾기
            Button button = slots[index].GetComponent<Button>();

            //todo 뒷면 배경 찾기
            //todo 뒷면 프레임 찾기
            //todo 뒷면 아이콘 찾기

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

        // 카드 각각 이동할 위치
        Vector3[] movePos = {
            panelPos + Vector3.left * 17f,
            panelPos,
            panelPos + Vector3.right * 17f
        };

        // 카드 왼쪽부터 순서대로 각각 코루틴으로 진행
        for (int i = 0; i < cards.Length; i++)
        {
            // 각 카드 트랜지션 시작
            StartCoroutine(CardTransition(i, 0.5f, movePos[i]));

            yield return new WaitForSecondsRealtime(0.2f);
        }

        // 배경 보이게
        DOTween.To(() => background.alpha, x => background.alpha = x, 1f, 0.5f)
        .SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.5f);

        // 전체 상호작용 풀기
        allGroup.interactable = true;

        // 가운데 슬롯 선택하기
        UICursor.Instance.UpdateLastSelect(firstBtn);
    }

    IEnumerator CardTransition(int index, float moveTime, Vector3 movePos)
    {
        Transform card = cards[index];

        // 카드 사이즈 확장
        card.DOScale(Vector3.one, moveTime - 0.2f)
        .SetEase(Ease.OutSine)
        .SetUpdate(true);

        // 카드 회전
        card.DORotate(Vector3.zero + Vector3.up * 360f, moveTime, RotateMode.WorldAxisAdd)
        .SetUpdate(true);

        // 카드 이동
        card.DOMove(movePos, moveTime).SetUpdate(true)
        .SetEase(Ease.OutExpo);

        // 트랜지션 대기
        yield return new WaitForSecondsRealtime(moveTime - 0.2f);

        // 카드 사이즈 다시 확장
        card.DOScale(Vector3.one * 1.05f, 0.4f)
        .SetUpdate(true);

        // 확장 시간 대기
        yield return new WaitForSecondsRealtime(0.4f);

        // 카드 원본 사이즈로 초기화
        card.DOScale(Vector3.one, 0.2f)
        .SetEase(Ease.InBack)
        .SetUpdate(true);
        // 축소 시간 대기
        yield return new WaitForSecondsRealtime(0.2f);

        // 카드 진동
        card.DOShakePosition(0.2f, 0.5f, 30, 90, false, false)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            card.position = movePos;
        });

        // 카드 테두리 모양 먼지 파티클 재생
        dustEffects[index].SetActive(true);
    }

    void ClickSlot(int index, SlotInfo slotInfo)
    {
        // 아이템 선택
        StartCoroutine(ChooseSlot(index, slotInfo));
    }

    public IEnumerator ChooseSlot(int index, SlotInfo slotInfo)
    {
        Transform slot = slots[index];

        // 전체 상호작용 막기
        allGroup.interactable = false;

        // 배경 투명해지며 숨기기
        DOTween.To(() => background.alpha, x => background.alpha = x, 0f, 0.2f)
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

        for (int i = 0; i < cards.Length; i++)
        {
            // 선택된 인덱스는 투명하게
            if (index == i)
            {
                CanvasGroup selectedCard = cards[index].GetComponent<CanvasGroup>();
                DOTween.To(() => selectedCard.alpha, x => selectedCard.alpha = x, 0f, 0.5f)
                .SetUpdate(true);
            }
            // 선택되지 않은 카드는 아래로 내리기
            else
            {
                cards[i].DOMove(cards[i].position + Vector3.down * 30f, 0.5f)
                .SetEase(Ease.InBack)
                .SetUpdate(true);
            }
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // 드랍 위치 해당 아이템 드랍
        // print("drop : " + slotInfo.name);
        StartCoroutine(ItemDB.Instance.ItemDrop(slotInfo, dropPos));

        // 패널 닫고 시간정지 해제
        UIManager.Instance.PopupUI(UIManager.Instance.levelupPanel, false);
    }
}
