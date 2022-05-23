using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UltimateList : MonoBehaviour
{
    List<MagicInfo> ultimateList = new List<MagicInfo>(); //궁극기 마법 리스트

    public GameObject slotsParent;
    List<GameObject> ultimateSlots = new List<GameObject>(); //각각 슬롯 오브젝트
    Vector2[] slotPos = new Vector2[5]; //각각 슬롯의 초기 위치

    private void Awake()
    {
        for (int i = 0; i < 5; i++)
        {
            // 모든 슬롯 오브젝트 넣기
            ultimateSlots.Add(slotsParent.transform.GetChild(i).gameObject);
            // 슬롯들의 초기 위치 넣기
            slotPos[i] = ultimateSlots[i].GetComponent<RectTransform>().anchoredPosition;
            // print(slotPos[i]);
        }
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    private void Update()
    {
        //TODO 좌,우 방향키로 궁극기 스크롤하기
        if (Input.GetKeyDown(KeyCode.A))
        {
            ScrollSlots(false);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            ScrollSlots(true);
        }
    }

    IEnumerator Initial()
    {
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //! 테스트, 마법 넣기
        // if(ultimateList.Count == 0)
        // for (int i = 0; i < 4; i++)
        // {
        //     PlayerManager.Instance.GetMagic(MagicDB.Instance.GetMagicByID(i + 46));
        // }

        // 보유한 마법 중 궁극기 마법 모두 불러오기
        ultimateList.Clear();
        ultimateList = PlayerManager.Instance.hasStackMagics.FindAll(x => x.castType == "ultimate");

        //궁극기 마법이 1개이상 있을때
        if (ultimateList.Count > 0 && PlayerManager.Instance.ultimateMagic != null)
        {
            // 현재 착용중인 마법이 0번에 올때까지 정렬 반복
            while (ultimateList[0] != PlayerManager.Instance.ultimateMagic)
            {
                MagicInfo targetMagic = ultimateList[0]; //첫번째 마법 얻기
                ultimateList.RemoveAt(0); //첫번째 마법 삭제
                ultimateList.Add(targetMagic); //마지막에 넣기
            }
        }

        //아이콘 세팅하기
        SetSlots();

        //! 테스트
        if (ultimateList.Count > 0 && PlayerManager.Instance.ultimateMagic == null)
            //사용할 궁극기 마법 교체하기
            PlayerManager.Instance.GetUltimateMagic(ultimateList[0]);
    }

    void SetSlots()
    {
        //마지막 이전 마법
        SetIcon(0, 3, ultimateList.Count - 2);
        //마지막 마법
        SetIcon(1, 2, ultimateList.Count - 1);
        //0번째 마법
        SetIcon(2, 1, 0);
        //1번째 마법
        SetIcon(3, 2, 1);
        //2번째 마법
        SetIcon(4, 3, 2);
    }

    void SetIcon(int objIndex, int num, int magicIndex)
    {
        // ultimateList의 보유 마법이 num 보다 많을때
        if (ultimateList.Count >= num)
        {
            ultimateSlots[objIndex].transform.Find("Icon").gameObject.SetActive(true);
            Sprite sprite = MagicDB.Instance.GetMagicIcon(ultimateList[magicIndex].id);
            ultimateSlots[objIndex].transform.Find("Icon").GetComponent<Image>().sprite = sprite == null ? SystemManager.Instance.questionMark : sprite;
            ultimateSlots[objIndex].transform.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.gradeColor[ultimateList[magicIndex].grade];
        }
        //넣을 마법 없으면 아이콘 및 프레임 숨기기
        else
        {
            ultimateSlots[objIndex].transform.Find("Icon").gameObject.SetActive(false);
            ultimateSlots[objIndex].transform.Find("Frame").GetComponent<Image>().color = Color.white;
        }
    }

    public void ScrollSlots(bool isLeft)
    {
        //모든 슬롯 domove 강제 즉시 완료
        foreach (var slot in ultimateSlots)
        {
            slot.transform.DOComplete();
        }

        //슬롯 오브젝트 리스트 인덱스 계산
        int startSlotIndex = isLeft ? ultimateSlots.Count - 1 : 0;
        int endSlotIndex = isLeft ? 0 : ultimateSlots.Count - 1;

        // 마지막 슬롯을 첫번째 인덱스 자리에 넣기
        GameObject targetSlot = ultimateSlots[startSlotIndex]; //타겟 오브젝트 얻기
        ultimateSlots.RemoveAt(startSlotIndex); //타겟 마법 삭제
        ultimateSlots.Insert(endSlotIndex, targetSlot); //타겟 마법 넣기

        // 마지막 슬롯은 slotPos[0] 으로 이동
        targetSlot.GetComponent<RectTransform>().anchoredPosition = slotPos[endSlotIndex];

        // 모든 슬롯 오브젝트들을 slotPos 초기위치에 맞게 domove
        for (int i = 0; i < ultimateSlots.Count; i++)
        {
            RectTransform rect = ultimateSlots[i].GetComponent<RectTransform>();

            //이미 domove 중이면 빠르게 움직이기
            // float moveTime = Vector2.Distance(rect.anchoredPosition, slotPos[i]) != 120f ? 0.1f : 0.5f;
            float moveTime = 0.2f;

            //한칸 옆으로 위치 이동
            if (i != endSlotIndex)
                rect.DOAnchorPos(slotPos[i], moveTime)
                .SetUpdate(true);

            //자리에 맞게 사이즈 바꾸기
            float scale = i == 2 ? 1f : 0.5f;
            rect.DOScale(scale, moveTime)
            .SetUpdate(true);

            //아이콘 알파값 바꾸기
            ultimateSlots[i].GetComponent<CanvasGroup>().alpha = i == 2 ? 1f : 0.5f;
        }

        //궁극기 하나도 없으면 리턴
        if (ultimateList.Count == 0)
            return;

        //마법 데이터 리스트 인덱스 계산
        int startIndex = isLeft ? ultimateList.Count - 1 : 0;
        int endIndex = isLeft ? 0 : ultimateList.Count - 1;

        // 실제 데이터도 마지막 슬롯을 첫번째 인덱스 자리에 넣기
        MagicInfo targetMagic = ultimateList[startIndex]; //타겟 마법 얻기
        ultimateList.RemoveAt(startIndex); //타겟 마법 삭제
        ultimateList.Insert(endIndex, targetMagic); //타겟 마법 넣기

        //hasMagic에서도 순서 바꾸기
        PlayerManager.Instance.hasStackMagics.Remove(targetMagic);
        PlayerManager.Instance.hasStackMagics.Insert(endIndex, targetMagic);

        // 모든 아이콘 다시 넣기
        SetSlots();

        //사용할 궁극기 마법 교체하기
        PlayerManager.Instance.GetUltimateMagic(ultimateList[0]);
    }
}
