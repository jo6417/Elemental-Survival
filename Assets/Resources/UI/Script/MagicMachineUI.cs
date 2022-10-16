using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;
using DanielLochner.Assets.SimpleScrollSnap;
using Lean.Pool;

public class MagicMachineUI : MonoBehaviour
{
    #region Singleton
    private static MagicMachineUI instance;
    public static MagicMachineUI Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<MagicMachineUI>(true);
                if (obj != null)
                {
                    instance = obj;
                }
                // else
                // {
                //     var newObj = new GameObject().AddComponent<MagicMachineUI>();
                //     instance = newObj;
                // }
            }
            return instance;
        }
    }
    #endregion

    [Header("Refer")]
    public InventorySlot paySlot; // 마법or샤드를 지불할 슬롯
    [SerializeField] List<Button> spinBtns; // 스핀 버튼들
    [SerializeField] Transform slotParent; // 상품 슬롯들 부모
    [SerializeField] Button exitBtn; // 종료 버튼
    [SerializeField] Transform effectSlotParent; // 상품 이펙트 슬롯
    [SerializeField] Transform slotParticleParent; // 상품 슬롯 파티클
    [SerializeField] List<SimpleScrollSnap> slotScrolls = new List<SimpleScrollSnap>();
    public Transform itemDropper; // 아이템 드랍 시킬 오브젝트
    Color btnOffColor = new Color32(150, 0, 0, 255);
    [SerializeField] Image slotCover; // 슬롯 어둡게 가림막
    [SerializeField] List<Image> leds = new List<Image>(); // 매직 머신 이미지
    [SerializeField] SlicedFilledImage feverGauge; // 피버 게이지

    [Header("State")]
    public List<SlotInfo> productList = null; // 판매 상품 리스트
    [SerializeField, ReadOnly] bool isSkipped;
    [SerializeField, ReadOnly] bool fieldDrop; // 아이템 필드드랍 할지 여부
    [SerializeField] float ledFlashSpeed = 0.05f; // led 깜빡이는 속도
    [SerializeField] float minSpinSpeed = 1000f; // 슬롯머신 스핀 속도 최소
    [SerializeField] float maxSpinSpeed = 1500f; // 슬롯머신 스핀 속도 최대
    [SerializeField] bool[] nowSlotSpin = new bool[3];


    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 모든 불 끄기
        for (int i = 0; i < leds.Count; i++)
            leds[i].material = null;

        // 필드드랍 스위치 초기화
        fieldDrop = false;

        // 피버 게이지 초기화
        feverGauge.color = Color.white;
        feverGauge.fillAmount = 0;

        // 스케일 초기화
        transform.localScale = Vector3.zero;

        // 이펙트 슬롯 모두 끄기
        for (int i = 0; i < effectSlotParent.childCount; i++)
            effectSlotParent.GetChild(i).gameObject.SetActive(false);

        // 슬롯 파티클 모두 끄기
        for (int i = 0; i < slotParticleParent.childCount; i++)
            slotParticleParent.GetChild(i).gameObject.SetActive(false);

        // 핸드폰을 화면 옆에 띄우기
        UIManager.Instance.PhoneOpen(new Vector3(20, 0, 0));

        yield return new WaitForSecondsRealtime(0.2f);

        // 팝업 스케일 키우기
        transform.DOScale(Vector3.one, 0.5f)
        .SetUpdate(true)
        .SetEase(Ease.OutBack);

        // 슬롯 가림막 가리기
        slotCover.color = Color.black;

        yield return new WaitUntil(() => ItemDB.Instance.loadDone && MagicDB.Instance.loadDone);

        // 매직 머신에 있는 모든 슬롯 불러오기
        List<InventorySlot> slots = slotParent.GetComponentsInChildren<InventorySlot>().ToList();

        for (int i = 0; i < productList.Count; i++)
        {
            SlotInfo slotInfo = productList[i];

            // 슬롯에 정보 넣기
            slots[i].slotInfo = slotInfo;
            // 슬롯 아이콘, 프레임세팅
            slots[i].Set_Slot();
        }

        // 슬롯 가림막 천천히 제거
        slotCover.DOColor(Color.clear, 1f)
        .SetUpdate(true);

        // 모든 스핀 버튼 끄기
        foreach (Button btn in spinBtns)
        {
            // 상호작용 초기화
            btn.interactable = true;

            // 버튼 꺼서 초기화
            btn.GetComponent<Image>().color = btnOffColor;
        }

        // 지불 슬롯 반복해서 깜빡이기
        paySlot.BlinkSlot(-1, 0.5f, new Color(1, 1, 1, 0.2f));
    }

    public void ClickPaySlot()
    {
        // 슬롯에 아이템 넣을때
        if (paySlot.slotInfo != null)
        {
            // 슬롯 깜빡임 끄기
            paySlot.indicator.DOKill();
            paySlot.indicator.color = Color.clear;

            // 모든 스핀 버튼 깜빡이기
            foreach (Button btn in spinBtns)
            {
                Image img = btn.GetComponent<Image>();
                // 어두운색으로 초기화
                img.color = new Color32(0, 100, 0, 255);
                // 초록색으로 깜빡임 반복
                img.DOColor(Color.green, 0.5f)
                .SetUpdate(true)
                .SetLoops(-1, LoopType.Yoyo);
            }

            // 피버 게이지 색 변경
            feverGauge.DOColor(MagicDB.Instance.GradeColor[paySlot.slotInfo.grade], 0.5f)
            .SetUpdate(true);

            // 피버 게이지 표시
            DOTween.To(() => feverGauge.fillAmount, x => feverGauge.fillAmount = x, (float)paySlot.slotInfo.grade / 6f, 0.5f)
            .SetUpdate(true)
            .SetEase(Ease.OutExpo);
        }
        // 슬롯에서 아이템 뺄때
        else
        {
            // 지불 슬롯 반복해서 깜빡이기
            paySlot.BlinkSlot(-1, 0.5f, new Color(1, 1, 1, 0.2f));

            // 버튼 색 초기화
            foreach (Button btn in spinBtns)
            {
                Image img = btn.GetComponent<Image>();

                img.DOKill();

                // 버튼 꺼서 초기화
                img.DOColor(btnOffColor, 0.5f)
                .SetUpdate(true);
            }

            // 피버 게이지 색 변경
            feverGauge.DOColor(Color.white, 0.5f)
            .SetUpdate(true);

            // 피버 게이지 초기화
            DOTween.To(() => feverGauge.fillAmount, x => feverGauge.fillAmount = x, 0, 0.5f)
            .SetUpdate(true)
            .SetEase(Ease.OutExpo);
        }
    }

    public void SpinClick(int slotIndex)
    {
        StartCoroutine(TrySpin(slotIndex));
    }

    IEnumerator TrySpin(int slotIndex)
    {
        // 슬롯이 비었을때
        if (paySlot.slotInfo == null)
        {
            // 버튼 색 초기화
            foreach (Button btn in spinBtns)
            {
                Image img = btn.GetComponent<Image>();

                img.DOKill();

                // 버튼 꺼서 초기화
                img.DOColor(btnOffColor, 0.5f)
                .SetUpdate(true);
            }

            // 지불 슬롯 빨갛게 깜빡이기
            paySlot.DOKill();
            paySlot.BlinkSlot(4);
            yield return new WaitForSecondsRealtime(0.8f);

            // 지불 슬롯 반복해서 깜빡이기
            paySlot.BlinkSlot(-1, 0.5f, new Color(1, 1, 1, 0.2f));

            yield break;
        }

        // Exit 버튼 막기
        exitBtn.interactable = false;
        // 핸드폰 종료 막기
        PhoneMenu.Instance.InteractBtnsToggle(false);

        // 지불한 재화의 등급 저장
        float payGrade = paySlot.slotInfo.grade;

        // 재화 슬롯의 아이템 삭제
        paySlot.slotInfo = null;
        paySlot.Set_Slot();

        // 해당 슬롯 스핀 여부 true
        nowSlotSpin[slotIndex] = true;

        // LED 점멸 반복
        StartCoroutine(LEDFlash());

        // 모든 버튼 상호작용 끄기, 컬러 트윈 끄기
        foreach (Button btn in spinBtns)
        {
            // 스핀 되는동안 상호작용 끄기
            btn.interactable = false;

            Image img = btn.GetComponent<Image>();

            img.DOKill();

            // 버튼 꺼서 초기화
            img.DOColor(btnOffColor, 0.5f)
            .SetUpdate(true);
        }

        // 회전 코루틴 리스트
        IEnumerator[] spinCoroutines = new IEnumerator[3];
        // 해당 슬롯 회전 코루틴 생성해서 저장
        spinCoroutines[slotIndex] = SpinScroll(slotIndex);

        // 해당 인덱스 슬롯 velocity로 돌리기
        StartCoroutine(spinCoroutines[slotIndex]);

        // 1차 피버 확률 추첨
        float feverRate = Random.value;
        // print("1차 피버 : " + feverRate + "/" + payGrade / 12f);

        yield return new WaitForSecondsRealtime(1f);

        // 1차 피버일때 (6등급 기준 50% 확률)
        if (feverRate < payGrade / 12f)
        {
            //todo 피버 게이지 반짝이기

            // 현재 스핀중인 슬롯 아닌 슬롯 뽑기
            List<int> indexes = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                // 현재 멈춰있는 슬롯일때
                if (!nowSlotSpin[i])
                    // 해당 인덱스 모두 수집
                    indexes.Add(i);
            }
            // 멈춰있는 슬롯 중에 하나 뽑기
            int feverIndex = indexes[Random.Range(0, indexes.Count)];

            // 해당 슬롯 스핀 여부 true
            nowSlotSpin[feverIndex] = true;

            // 회전 켜진 슬롯은 모두 회전
            for (int i = 0; i < 3; i++)
            {
                // 스핀 중인 슬롯 인덱스라면
                if (nowSlotSpin[i])
                {
                    // 회전 코루틴 종료
                    if (spinCoroutines[i] != null)
                        StopCoroutine(spinCoroutines[i]);

                    // 해당 슬롯 회전 코루틴 생성해서 저장
                    spinCoroutines[i] = SpinScroll(i);

                    // 해당 슬롯 velocity로 돌리기
                    StartCoroutine(spinCoroutines[i]);
                }
            }

            // 2차 피버 확률 추첨 (6등급 기준 50% 확률)
            feverRate = Random.value;
            // print("2차 피버 : " + feverRate + "/" + payGrade / 12f);

            yield return new WaitForSecondsRealtime(1f);

            // 2차 피버일때 (6등급 기준 50% 확률)
            if (feverRate < payGrade / 12f)
            {
                //todo 피버 게이지 반짝이기

                // 마지막 멈춰있는 슬롯 찾기
                feverIndex = nowSlotSpin.ToList().FindIndex(x => x == false);

                // 해당 슬롯 스핀 여부 true
                nowSlotSpin[feverIndex] = true;

                // 회전 켜진 슬롯은 모두 회전
                for (int i = 0; i < 3; i++)
                {
                    // 스핀 중인 슬롯 인덱스라면
                    if (nowSlotSpin[i])
                    {
                        // 회전 코루틴 종료
                        if (spinCoroutines[i] != null)
                            StopCoroutine(spinCoroutines[i]);

                        // 해당 슬롯 회전 코루틴 생성해서 저장
                        spinCoroutines[i] = SpinScroll(i);

                        // 해당 슬롯 velocity로 돌리기
                        StartCoroutine(spinCoroutines[i]);
                    }
                }
            }
        }
        else
        {
            // 스크롤이 일정 속도 이상이면 반복
            while (slotScrolls[slotIndex].Velocity.magnitude > 100f)
            {
                // 속도 부드럽게 낮추기
                slotScrolls[slotIndex].Velocity = Vector2.Lerp(slotScrolls[slotIndex].Velocity, Vector2.zero, 0.01f);

                yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
            }
        }

        //todo 슬롯 회전 이펙트 시작

        // 모든 슬롯이 멈출때까지 대기
        yield return new WaitUntil(() => !nowSlotSpin[0] && !nowSlotSpin[1] && !nowSlotSpin[2]);

        //todo 슬롯 회전 이펙트 끄기

        // 확인, 클릭할때까지 대기
        yield return new WaitUntil(() => UIManager.Instance.UI_Input.UI.Click.IsPressed() || UIManager.Instance.UI_Input.UI.Accept.IsPressed());

        // 피버 게이지 초기화
        DOTween.To(() => feverGauge.fillAmount, x => feverGauge.fillAmount = x, 0, 0.5f)
        .SetUpdate(true)
        .SetEase(Ease.OutExpo);

        for (int i = 0; i < 3; i++)
        {
            // 인벤토리에 해당 아이템 넣기
            StartCoroutine(PutMagicInven(i));
        }

        // 상품 획득 슬롯 모두 끄기
        for (int i = 0; i < effectSlotParent.childCount; i++)
        {
            // 결과 슬롯 찾기
            InventorySlot effectSlot = effectSlotParent.GetChild(i).GetComponent<InventorySlot>();

            // 아이템 정보 초기화
            effectSlot.slotInfo = null;
            effectSlot.Set_Slot();

            // 결과 슬롯 끄기
            effectSlot.gameObject.SetActive(false);
        }

        // 모든 버튼 상호작용 켜기
        for (int i = 0; i < 3; i++)
        {
            spinBtns[i].interactable = true;
        }

        // Exit 버튼 풀기
        exitBtn.interactable = true;
        // 핸드폰 종료 풀기
        PhoneMenu.Instance.InteractBtnsToggle(true);
    }

    IEnumerator PutMagicInven(int index)
    {
        InventorySlot effectSlot = effectSlotParent.GetChild(index).GetComponent<InventorySlot>();

        // 상품 정보 획득
        SlotInfo slotInfo = effectSlot.slotInfo;

        // 해당 슬롯에서 상품 획득했을때
        if (slotInfo != null)
        {
            // 인벤토리 빈칸 찾기
            int emptyInvenIndex = PhoneMenu.Instance.GetEmptySlot();

            // 해당 상품 파티클
            ParticleSystem getMagicEffect = slotParticleParent.GetChild(index).GetComponent<ParticleSystem>();

            // 획득 파티클 색 변경
            ParticleSystem.MainModule particleMain = getMagicEffect.main;
            particleMain.startColor = MagicDB.Instance.GradeColor[slotInfo.grade];

            // 인벤토리 빈칸이 있을때
            if (emptyInvenIndex != -1)
            {
                // 해당 인벤토리에 상품 정보만 넣기
                PhoneMenu.Instance.invenSlots[emptyInvenIndex].slotInfo = slotInfo;

                // 빈칸 위치에 Attractor 오브젝트 옮기기
                getMagicEffect.transform.Find("ParticleAttractor").transform.position = PhoneMenu.Instance.invenSlots[emptyInvenIndex].transform.position;

                // 획득 상품 파티클 재생
                getMagicEffect.gameObject.SetActive(false);
                getMagicEffect.gameObject.SetActive(true);
                yield return new WaitForSecondsRealtime(1f);

                // 획득한 인벤토리 이미지 갱신
                PhoneMenu.Instance.invenSlots[emptyInvenIndex].Set_Slot(true);
            }
            // 인벤토리 빈칸 없을때
            else
            {
                // 필드드랍 1개 이상 있으면 켜기
                fieldDrop = true;

                // Exit 버튼 위치에 Attractor 오브젝트 옮기기
                getMagicEffect.transform.Find("ParticleAttractor").transform.position = exitBtn.transform.position;

                // 획득 상품 파티클 재생
                getMagicEffect.gameObject.SetActive(false);
                getMagicEffect.gameObject.SetActive(true);

                // 아이템 드롭
                StartCoroutine(ItemDB.Instance.ItemDrop(slotInfo, itemDropper.position));
            }
        }
    }

    IEnumerator SpinScroll(int scrollIndex)
    {
        // 회전시킬 스크롤
        SimpleScrollSnap scroll = slotScrolls[scrollIndex];

        // 각 스크롤 배경
        Image scrollBack = scroll.GetComponent<Image>();
        // 회전 하는동안 해당 슬롯 배경 밝히기
        scrollBack.DOColor(Color.white, 1f)
        .SetUpdate(true);

        // 랜덤 스크롤 돌리기
        scroll.Velocity = Vector2.down * Random.Range(minSpinSpeed, maxSpinSpeed);

        // 스크롤 일정 속도 이하거나 스킵할때까지 대기
        yield return new WaitUntil(() => scroll.Velocity.magnitude <= 100f
        || isSkipped);

        // 스크롤이 일정 속도 이상이면 반복
        while (scroll.Velocity.magnitude > 100f)
        {
            // 속도 부드럽게 낮추기
            scroll.Velocity = Vector2.Lerp(scroll.Velocity, Vector2.zero, 0.01f);

            yield return new WaitForSecondsRealtime(Time.unscaledDeltaTime);
        }

        // 속도 멈추기
        scroll.Velocity = Vector2.zero;

        // 해당 슬롯 배경 초기화
        scrollBack.DOColor(Color.black, 1f)
        .SetUpdate(true);

        // 해당 슬롯 현재 회전 여부 초기화
        nowSlotSpin[scrollIndex] = false;
    }

    public void GetSlot(int scrollIndex)
    {
        // 회전시킬 스크롤
        SimpleScrollSnap scroll = slotScrolls[scrollIndex];

        // 멈췄을때 아이템 반환
        int index = scrollIndex * 5 + scroll.CenteredPanel;
        // print(scrollIndex + ":" + scroll.CenteredPanel + ":" + scroll.Content.GetChild(scroll.CenteredPanel).name + ":" + productList[index].name);

        InventorySlot effectSlot = effectSlotParent.GetChild(scrollIndex).GetComponent<InventorySlot>();

        // 아이템 정보 전달, 갱신
        effectSlot.slotInfo = productList[index];
        effectSlot.Set_Slot();

        // 획득 슬롯 켜기
        effectSlot.gameObject.SetActive(true);

        // 슬롯 후광 이펙트 색 변경 및 켜기
        effectSlot.slotBackEffect.GetComponent<Image>().color = MagicDB.Instance.GradeColor[productList[index].grade];
        effectSlot.slotBackEffect.gameObject.SetActive(true);

        effectSlot.transform.localScale = Vector3.zero;
        effectSlot.transform.DOScale(Vector3.one, 0.5f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true);
    }

    IEnumerator LEDFlash()
    {
        int lightIndex = 0;

        // 모든 슬롯 멈출때까지 진행
        while (nowSlotSpin[0] || nowSlotSpin[1] || nowSlotSpin[2])
        {
            for (int i = 0; i < leds.Count; i++)
            {
                // 불켜기
                if (i == lightIndex)
                    leds[i].material = SystemManager.Instance.HDR3_Mat;
                // 불끄기
                else
                    leds[i].material = null;

                yield return null;
            }

            lightIndex++;

            // 최대 인덱스 넘어가면 초기화
            if (lightIndex >= leds.Count)
                lightIndex = 0;

            yield return new WaitForSecondsRealtime(ledFlashSpeed);
        }

        // 모든 불 끄기
        for (int i = 0; i < leds.Count; i++)
            leds[i].material = null;
    }

    public void ExitPopup()
    {
        // 매직 머신 재화 슬롯의 아이템 인벤에 넣기
        if (paySlot.slotInfo != null)
        {
            PhoneMenu.Instance.BackToInven(paySlot);

            // spin 버튼 불 끄기
            ClickPaySlot();
        }

        // 매직머신 팝업 종료
        StartCoroutine(Exit());
    }

    IEnumerator Exit()
    {
        // 팝업 줄어들기
        transform.DOScale(Vector3.zero, 0.5f)
        .SetUpdate(true)
        .SetEase(Ease.InBack);
        // yield return new WaitForSecondsRealtime(0.2f);

        // 핸드폰 끄기
        yield return StartCoroutine(PhoneMenu.Instance.PhoneExit());

        // 필드드랍 아이템 1개 이상 있을때
        if (fieldDrop)
        {
            // 트럭 전조등 이펙트 켜기
            itemDropper.GetComponent<ParticleSystem>().Play();
        }

        // 드롭퍼 변수 초기화
        itemDropper = null;

        // 팝업 닫기
        UIManager.Instance.PopupUI(gameObject, false);
    }
}
