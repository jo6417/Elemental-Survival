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
    [SerializeField] CanvasGroup allGroup; // 전체 그룹
    [SerializeField] List<Button> spinBtns; // 스핀 버튼들
    [SerializeField] Transform slotParent; // 상품 슬롯들 부모
    [SerializeField] Button exitBtn; // 종료 버튼
    [SerializeField] Transform effectSlotParent; // 상품 이펙트 슬롯
    [SerializeField] Transform slotParticleParent; // 상품 슬롯 파티클
    [SerializeField] List<SimpleScrollSnap> slotScrolls = new List<SimpleScrollSnap>();
    public Transform itemDropper; // 아이템 드랍 시킬 오브젝트
    Color btnOffColor = new Color32(150, 0, 0, 255);
    [SerializeField] Image slotCover; // 슬롯 어둡게 가림막
    [SerializeField] List<Image> leds = new List<Image>(); // led 이미지
    [SerializeField] List<ParticleSystem> failEffect = new List<ParticleSystem>(); // 실패 이펙트
    [SerializeField] SlicedFilledImage feverGauge; // 피버 게이지

    [Header("State")]
    public List<SlotInfo> productList = null; // 판매 상품 리스트
    [SerializeField, ReadOnly] bool isSkipped;
    [SerializeField, ReadOnly] bool fieldDrop; // 아이템 필드드랍 할지 여부
    [SerializeField] float ledFlashSpeed = 0.05f; // led 깜빡이는 속도
    [SerializeField] float minSpinSpeed = 1000f; // 슬롯머신 스핀 속도 최소
    [SerializeField] float maxSpinSpeed = 1500f; // 슬롯머신 스핀 속도 최대
    [SerializeField] bool[] nowSlotSpin = new bool[3];
    [SerializeField] float getSoundSpeed = 0.03f;
    [SerializeField] int getParticleNum = 20;
    IEnumerator[] spinCoroutines = new IEnumerator[3]; // 회전 코루틴 리스트

    private void Awake()
    {
        // 지불 슬롯에 함수 넣기
        paySlot.setAction += SetPaySlot;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 전체 상호작용 끄기
        allGroup.interactable = false;

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
        .SetEase(Ease.OutBack)
        .OnComplete(() =>
        {
            // 전체 상호작용 켜기
            allGroup.interactable = true;

            // 등장 효과음 재생
            SoundManager.Instance.PlaySound("MagicMachine_Popup", 0, 0, 1, false);
        });

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

            // 슬롯 정보 있을때
            if (slotInfo != null)
                // 개수 0개일때 품절 처리
                if (slots[i].slotInfo.amount == 0)
                {
                    // 슬롯 어둡게 덮기
                    slots[i].indicator.color = new Color(0, 0, 0, 0.5f);
                    // 품절 이미지 켜기
                    slots[i].soldOut.SetActive(true);
                }
                else
                    // 품절 이미지 끄기
                    slots[i].soldOut.SetActive(false);
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

        // 지불 슬롯 상호작용 풀기
        paySlot.slotButton.interactable = true;
    }

    public void SetPaySlot()
    {
        // 슬롯에 아이템 넣을때
        if (paySlot.slotInfo != null)
        {
            // 지불 슬롯 깜빡임 끄기
            paySlot.indicator.DOKill();
            paySlot.indicator.color = Color.clear;

            // 모든 스핀 버튼 깜빡이기
            foreach (Button btn in spinBtns)
            {
                Image img = btn.GetComponent<Image>();
                // 어두운색으로 초기화
                img.color = new Color32(0, 100, 0, 255);
                // 초록색으로 깜빡임 반복
                img.DOKill();
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
        // 현재 도는 슬롯이 있으면 리턴
        for (int i = 0; i < spinCoroutines.Length; i++)
            if (spinCoroutines[i] != null)
                return;

        StartCoroutine(GoSpin(slotIndex));
    }

    IEnumerator GoSpin(int slotIndex)
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

        // 슬롯 회전 여부 전부 끄기
        for (int i = 0; i < 3; i++)
            nowSlotSpin[i] = false;

        //todo 슬롯 회전 이펙트 시작

        // 지불 슬롯 상호작용 막기
        paySlot.slotButton.interactable = false;

        // Exit 버튼 막기
        exitBtn.interactable = false;
        // 핸드폰 종료 막기
        PhoneMenu.Instance.InteractBtnsToggle(false);

        // 지불한 재화의 등급 저장
        float payGrade = paySlot.slotInfo.grade;

        // 재화 슬롯의 아이템 삭제
        paySlot.slotInfo = null;
        paySlot.Set_Slot();

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

        // 해당 슬롯 회전 코루틴 생성해서 저장
        spinCoroutines[slotIndex] = SpinScroll(slotIndex);
        // 해당 인덱스 슬롯 velocity로 돌리기
        StartCoroutine(spinCoroutines[slotIndex]);

        // 슬롯 회전 여부 켜기
        nowSlotSpin[slotIndex] = true;

        // 1차 피버 확률 추첨
        float feverRate = Random.value;
        yield return new WaitForSecondsRealtime(1f);

        // 1차 피버일때 (6등급 기준 50% 확률)
        if (feverRate < payGrade / 12f)
        {
            //todo 피버 게이지 반짝이기
            print("fever_1");

            // 멈춘 슬롯 하나 뽑아 돌리기
            NextSpin();

            // 2차 피버 확률 추첨 (6등급 기준 50% 확률)
            feverRate = Random.value;
            yield return new WaitForSecondsRealtime(1f);

            // 2차 피버일때 (6등급 기준 50% 확률)
            if (feverRate < payGrade / 12f)
            {
                //todo 피버 게이지 반짝이기
                print("fever_2");

                // 멈춘 슬롯 하나 뽑아 돌리기
                NextSpin();
            }
        }

        // 모든 슬롯이 멈출때까지 대기
        yield return new WaitUntil(() => spinCoroutines[0] == null && spinCoroutines[1] == null && spinCoroutines[2] == null);

        //todo 슬롯 회전 이펙트 끄기

        // 마우스에 아이콘 없을때 클릭이나 아무 키 누를때까지 대기
        yield return new WaitUntil(() => !UIManager.Instance.nowSelectIcon.enabled
        && (UIManager.Instance.UI_Input.UI.Click.IsPressed() || UIManager.Instance.UI_Input.UI.AnyKey.IsPressed()));

        // 피버 게이지 초기화
        DOTween.To(() => feverGauge.fillAmount, x => feverGauge.fillAmount = x, 0, 0.5f)
        .SetUpdate(true)
        .SetEase(Ease.OutExpo);

        // 상품 획득 슬롯 모두 끄기
        for (int i = 0; i < effectSlotParent.childCount; i++)
        {
            // 결과 슬롯 찾기
            InventorySlot effectSlot = effectSlotParent.GetChild(i).GetComponent<InventorySlot>();

            // 스크롤 참조
            SimpleScrollSnap scroll = slotScrolls[i];
            // 상품 정보 캐싱
            SlotInfo slotInfo = productList[i * 5 + scroll.CenteredPanel];

            // 회전했던 슬롯일때
            if (nowSlotSpin[i])
            {
                // 당첨된 슬롯 상품 개수 0개로 품절처리
                slotInfo.amount = 0;

                // 당첨된 슬롯 찾기
                InventorySlot getSlot = scroll.Content.GetChild(scroll.CenteredPanel).GetComponent<InventorySlot>();
                // 슬롯 어둡게 덮기
                getSlot.indicator.color = new Color(0, 0, 0, 0.5f);
                //todo 품절 이미지 켜기
                getSlot.soldOut.SetActive(true);

                // 인벤토리에 해당 아이템 넣기
                StartCoroutine(PutMagicInven(i));

                // 아이템 정보 초기화
                effectSlot.slotInfo = null;
                effectSlot.Set_Slot();
                // 결과 슬롯 끄기
                effectSlot.gameObject.SetActive(false);
            }
        }

        // 모든 버튼 상호작용 켜기
        for (int i = 0; i < 3; i++)
        {
            spinBtns[i].interactable = true;
        }

        // Exit 버튼 풀기
        exitBtn.interactable = true;
        // 지불 슬롯 상호작용 풀기
        paySlot.slotButton.interactable = true;
        // 핸드폰 종료 풀기
        PhoneMenu.Instance.InteractBtnsToggle(true);
    }

    void NextSpin()
    {
        // 현재 회전하지 않는 슬롯 뽑기
        List<int> indexes = new List<int>();
        for (int i = 0; i < 3; i++)
            // 현재 멈춰있는 슬롯일때
            if (!nowSlotSpin[i])
                // 해당 인덱스 수집
                indexes.Add(i);

        // 멈춰있는 슬롯 중에 하나 뽑기
        int feverIndex = indexes[Random.Range(0, indexes.Count)];
        // 슬롯 회전 여부 켜기
        nowSlotSpin[feverIndex] = true;

        // 회전 켜진 슬롯은 모두 회전
        for (int i = 0; i < 3; i++)
            // 회전 허용 슬롯 이면
            if (nowSlotSpin[i])
            {
                // 이미 회전 코루틴 중이면 종료
                if (spinCoroutines[i] != null)
                    StopCoroutine(spinCoroutines[i]);

                // 해당 슬롯 회전 코루틴 생성해서 저장
                spinCoroutines[i] = SpinScroll(i);

                // 해당 슬롯 velocity로 돌리기
                StartCoroutine(spinCoroutines[i]);
            }
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

                yield return new WaitForSecondsRealtime(0.2f);

                // 파티클 생성 사운드 재생
                SoundManager.Instance.PlaySound("MergeParticleGet", 0, getSoundSpeed, getParticleNum, false);

                yield return new WaitForSecondsRealtime(0.6f);

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

                // 파티클 생성 사운드 재생
                SoundManager.Instance.PlaySound("MergeParticleGet", 0, getSoundSpeed, getParticleNum, false);

                // 아이템 드롭
                StartCoroutine(ItemDB.Instance.ItemDrop(slotInfo, itemDropper.position));
            }
        }
    }

    IEnumerator SpinScroll(int slotIndex)
    {
        // 회전시킬 스크롤
        SimpleScrollSnap scroll = slotScrolls[slotIndex];

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

        // 해당 슬롯 코루틴 초기화
        spinCoroutines[slotIndex] = null;
    }

    public void SpinSound()
    {
        // 스핀 할때마다 사운드 재생
        SoundManager.Instance.PlaySound("SlotSpin_Once", 0, 0, 1, false);
    }

    public void GetSlot(int slotIndex)
    {
        // 회전시킬 스크롤
        SimpleScrollSnap scroll = slotScrolls[slotIndex];
        // 멈췄을때 아이템 반환
        int index = slotIndex * 5 + scroll.CenteredPanel;
        // 상품 정보 캐싱
        SlotInfo slotInfo = productList[index];
        // print(slotIndex + ":" + scroll.CenteredPanel + ":" + scroll.Content.GetChild(scroll.CenteredPanel).name + ":" + productList[index].name);

        // 품절일때
        if (slotInfo.amount == 0)
        {
            // 실패 이펙트 재생
            failEffect[slotIndex].gameObject.SetActive(true);
        }
        // 품절 아닐때
        if (slotInfo.amount > 0)
        {
            // 강조 슬롯 찾기
            InventorySlot effectSlot = effectSlotParent.GetChild(slotIndex).GetComponent<InventorySlot>();

            // 아이템 정보 전달, 갱신
            effectSlot.slotInfo = slotInfo;
            effectSlot.Set_Slot();

            // 획득 슬롯 켜기
            effectSlot.gameObject.SetActive(true);

            // 슬롯 후광 이펙트 색 변경 및 켜기
            effectSlot.slotBackEffect.GetComponent<Image>().color = MagicDB.Instance.GradeColor[slotInfo.grade];
            effectSlot.slotBackEffect.gameObject.SetActive(true);

            effectSlot.transform.localScale = Vector3.zero;
            effectSlot.transform.DOScale(Vector3.one, 0.5f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
        }
    }

    IEnumerator LEDFlash()
    {
        int lightIndex = 0;

        // 모든 슬롯 멈출때까지 진행
        while (spinCoroutines[0] != null
        || spinCoroutines[1] != null
        || spinCoroutines[2] != null)
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
            SetPaySlot();
        }

        // 매직머신 팝업 종료
        StartCoroutine(Exit());
    }

    IEnumerator Exit()
    {
        // 전체 상호작용 끄기
        allGroup.interactable = false;

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
            ParticleSystem particle = itemDropper.GetComponent<ParticleSystem>();
            if (particle != null) particle.Play();
        }

        // 드롭퍼 변수 초기화
        itemDropper = null;

        // 팝업 닫기
        UIManager.Instance.PopupUI(gameObject, false);
    }
}
