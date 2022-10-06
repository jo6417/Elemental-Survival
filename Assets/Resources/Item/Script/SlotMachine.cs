using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DanielLochner.Assets.SimpleScrollSnap;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Lean.Pool;
using System.Linq;

public class SlotMachine : MonoBehaviour
{
    [Header("Interact")]
    [SerializeField] Canvas uiCanvas; // 가격, 상호작용키 안내 UI 캔버스
    [SerializeField] Interacter interacter; //상호작용 콜백 함수 클래스
    [SerializeField] GameObject showKey; //상호작용 키 표시 UI
    [SerializeField] GameObject priceUI; // 가격 UI

    [Header("Refer")]
    [SerializeField] Animator leverAnim;
    [SerializeField] List<SimpleScrollSnap> slotScrolls = new List<SimpleScrollSnap>();
    [SerializeField] Transform gemLED;
    [SerializeField] Image blackScreen;
    [SerializeField] Transform itemDropper;

    [Header("State")]
    float spinCount;
    [SerializeField] float spinTime = 2f; // 슬롯머신 돌리는 시간
    [SerializeField] float randomTime;
    [SerializeField] float spinSpeed = 10f; // 슬롯머신 스핀 속도
    int slotStopNum;
    int randomType; // 아이템 종류 (마법,샤드,아티팩트)
    public ItemInfo itemInfo;
    public float price;
    public int priceType;
    [SerializeField, ReadOnly] string productName;

    private void Awake()
    {
        // 상호작용 컴포넌트 찾기
        interacter = interacter != null ? interacter : GetComponent<Interacter>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // blackScreen으로 슬롯 가리기
        blackScreen.color = Color.black;
        List<SpriteRenderer> gemLEDs = gemLED.GetComponentsInChildren<SpriteRenderer>().ToList();

        // led 전부 끄기        
        foreach (SpriteRenderer led in gemLEDs)
            // 해당 순서 led 켜기
            led.color = Color.clear;

        // 캔버스 끄기
        uiCanvas.gameObject.SetActive(false);

        // 상호작용 키 UI 끄기
        showKey.SetActive(false);

        //애니메이션 멈추기
        leverAnim.enabled = false;

        // 마법,아이템 DB 모두 로딩 될때까지 대기
        yield return new WaitUntil(() => ItemDB.Instance.loadDone);

        // 가격 타입 랜덤 초기화
        priceType = Random.Range(0, 6);
        // 상품 가격 랜덤 초기화
        price = Random.Range(10, 30);

        // 해당 아이템에 필요한 재화 종류, 가격 초기화
        priceUI.GetComponentInChildren<Image>().color = MagicDB.Instance.GetElementColor(priceType);
        priceUI.GetComponentInChildren<TextMeshProUGUI>().text = price.ToString();

        // 상호작용 트리거 함수 콜백에 연결 시키기
        interacter.interactTriggerCallback += InteractTrigger;
        // 상호작용 함수 콜백에 연결 시키기
        interacter.interactSubmitCallback += InteractSubmit;

        // led 전부 켜기        
        foreach (SpriteRenderer led in gemLEDs)
            // 해당 순서 led 켜기
            led.DOColor(Color.white, 1f);

        // blackScreen 투명하게
        blackScreen.DOColor(Color.clear, 1f);
        yield return new WaitForSeconds(1f);

        // 캔버스 켜기
        uiCanvas.gameObject.SetActive(true);
    }

    public void InteractTrigger(bool isClose)
    {
        // 상호작용 불가능하면 리턴
        if (!uiCanvas.gameObject.activeSelf)
            return;

        //todo 플레이어 상호작용 키가 어떤 키인지 표시
        // pressKey.text = 

        // 상호작용 가능 거리 접근했을때
        if (isClose)
            // 상호작용 키 UI 나타내기
            showKey.SetActive(true);
        else
            // 상호작용 키 UI 숨기기
            showKey.SetActive(false);
    }

    public void InteractSubmit()
    {
        // 상호작용 불가능하면 리턴
        if (!uiCanvas.gameObject.activeSelf)
            return;

        // 인디케이터 꺼져있으면 리턴
        if (!showKey.activeSelf)
            return;

        // 슬롯머신 스케일 바운스
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(new Vector3(0.2f, -0.2f, 1), 0.3f)
        .SetEase(Ease.InOutBack);

        // 재화가 가격보다 많을때
        if (PlayerManager.Instance.hasItems[priceType].amount > price)
        {
            // 플레이어 젬 소모 및 UI 갱신
            PlayerManager.Instance.PayGem(priceType, (int)price);
            UIManager.Instance.UpdateGem(priceType);

            // 아이템 뽑기
            StartCoroutine(DropSlot());

            // 캔버스 끄기
            uiCanvas.gameObject.SetActive(false);
        }
        // 재화가 가격보다 적을때
        else
        {
            // 중복 트윈 방지
            priceUI.transform.DOKill();
            // 가격 좌우로 흔들기
            Vector2 originPos = priceUI.transform.localPosition;
            priceUI.transform.DOPunchPosition(Vector2.right * 30f, 0.5f, 10, 1)
            .OnKill(() =>
            {
                // 원래 위치로 복귀
                priceUI.transform.localPosition = originPos;
            })
            .OnComplete(() =>
            {
                // 원래 위치로 복귀
                priceUI.transform.localPosition = originPos;
            });

            //해당 젬 UI 인디케이터
            UIManager.Instance.GemIndicator(priceType, Color.red);
        }
    }

    IEnumerator DropSlot()
    {
        // 레버 내리는 애니메이션 1회 재생
        leverAnim.enabled = true;

        // 완료 슬롯 개수 초기화
        slotStopNum = 0;

        // 드랍 아이템 결정 (샤드, 하트, 원소젬)
        randomType = Random.Range(0, 3);
        switch (randomType)
        {
            // 샤드일때
            case 0:
                itemInfo = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Shard);
                break;
            // 하트일때
            case 1:
                itemInfo = ItemDB.Instance.GetItemByName("Heart");
                break;
            // 원소젬일때
            case 2:
                itemInfo = ItemDB.Instance.GetRandomItem(ItemDB.ItemType.Gem);
                break;
        }

        randomTime = Random.Range(spinTime, spinTime * 1.5f);

        // led 순서대로 점멸 반복
        StartCoroutine(LEDFlash());

        // 슬롯 3개 시작 시간차 다르게 반복해서 내리기
        StartCoroutine(SpinMachine(0, randomType));
        StartCoroutine(SpinMachine(1, randomType));
        StartCoroutine(SpinMachine(2, randomType));

        // 모든 슬롯 멈출때까지 대기
        yield return new WaitUntil(() => slotStopNum == 3);
        // 슬롯이 멈출때까지 추가 대기
        yield return new WaitForSeconds(0.5f);

        // 아이템 생성 위치
        Vector3 dropPos;
        if (itemDropper != null)
            dropPos = itemDropper.position;
        else
            dropPos = transform.position + (transform.position - PlayerManager.Instance.transform.position).normalized * 3f;

        print(itemInfo.id + " : " + itemInfo.name);

        // 드랍할 아이템 오브젝트
        GameObject dropObj = LeanPool.Spawn(ItemDB.Instance.GetItemPrefab(itemInfo.id), dropPos, Quaternion.identity, SystemManager.Instance.itemPool);

        // 해당 상품 이름 확인
        productName = itemInfo.name;

        // 아이템 정보 넣기
        ItemManager itemManager = dropObj.GetComponent<ItemManager>();
        itemManager.itemInfo = itemInfo as ItemInfo;

        // 아이템 정보 삭제
        itemInfo = null;

        // 아이템 콜라이더 찾기
        Collider2D itemColl = dropObj.GetComponent<Collider2D>();
        // 아이템 rigid 찾기
        Rigidbody2D itemRigid = dropObj.GetComponent<Rigidbody2D>();

        // 콜라이더 끄기
        itemColl.enabled = false;

        // 플레이어 반대 방향, 랜덤 파워로 아이템 날리기
        if (itemDropper != null)
            itemRigid.velocity = (transform.rotation.eulerAngles).normalized * Random.Range(10f, 20f);
        else
            itemRigid.velocity = (dropObj.transform.position - PlayerManager.Instance.transform.position).normalized * Random.Range(10f, 20f);

        // 랜덤으로 방향 및 속도 결정
        float randomRotate = Random.Range(1f, 3f);
        // 아이템 랜덤 속도로 회전 시키기
        itemRigid.angularVelocity = randomRotate < 2f ? 90f * randomRotate : -90f * randomRotate;

        // 레버 내리는 애니메이션 끄기
        leverAnim.enabled = false;

        // 콜라이더 켜기
        itemColl.enabled = true;

        // 가격 타입 랜덤 초기화
        priceType = Random.Range(0, 6);
        // 가격 배수로 올리기
        price = price * 2;

        // 해당 아이템에 필요한 재화 종류, 가격 초기화
        priceUI.GetComponentInChildren<Image>().color = MagicDB.Instance.GetElementColor(priceType);
        priceUI.GetComponentInChildren<TextMeshProUGUI>().text = price.ToString();

        // 랜덤하게 슬롯머신 정지
        if (Random.value <= 0.3f)
        {
            // led 끄기
            List<SpriteRenderer> gemLEDs = gemLED.GetComponentsInChildren<SpriteRenderer>().ToList();
            foreach (SpriteRenderer led in gemLEDs)
                // 해당 순서 led 끄기
                led.DOColor(Color.clear, 1f);

            // blackScreen으로 가리기
            blackScreen.DOColor(Color.black, 1f);
        }
        // 정지 아닐때
        else
            // 캔버스 켜기, 다시 작동
            uiCanvas.gameObject.SetActive(true);
    }

    IEnumerator LEDFlash()
    {
        // led 스프라이트 모두 찾기
        List<SpriteRenderer> gemLEDs = gemLED.GetComponentsInChildren<SpriteRenderer>().ToList();

        // 슬롯머신 멈출때까지 진행
        while (slotStopNum < 3)
        {
            for (int i = 0; i < gemLEDs.Count; i++)
            {
                // 모든 led 초기화
                foreach (SpriteRenderer led in gemLEDs)
                {
                    led.color = Color.white;
                }

                // 해당 순서 led 밝히기
                gemLEDs[i].color = MagicDB.Instance.GetHDRElementColor(priceType);

                float delayTime = 0.1f * (randomTime - spinCount / randomTime);

                yield return new WaitForSeconds(delayTime);
            }
        }

        // 모든 led 초기화
        foreach (SpriteRenderer led in gemLEDs)
        {
            led.color = Color.white;
        }
    }

    IEnumerator SpinMachine(int slotIndex, int itemNum)
    {
        // 랜덤 시간 대기
        yield return new WaitForSeconds(Random.Range(0f, 0.3f));

        spinCount = randomTime;
        // 시간이 남았거나, 뽑힌 아이템의 슬롯이 아닐때 계속 스냅
        while (spinCount > 0f || slotScrolls[slotIndex].CenteredPanel != itemNum)
        {
            // 끝날때쯤 점점 느려짐
            if (spinCount <= randomTime * 0.1f)
            {
                // 스냅 스피드 계산
                float scrollSpeed = (spinCount - Time.deltaTime) * spinSpeed;
                scrollSpeed = Mathf.Clamp(scrollSpeed, 5f, spinSpeed);

                slotScrolls[slotIndex].SnapSpeed = scrollSpeed;
            }
            else
                slotScrolls[slotIndex].SnapSpeed = spinSpeed;

            slotScrolls[slotIndex].GoToNextPanel();

            // 슬롯 내리는 시간 대기
            spinCount -= 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        slotStopNum++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 스폰 콜라이더 밖으로 나갔을때, 기능 정지한 상태일때
        if (other.CompareTag("Respawn") && !uiCanvas.gameObject.activeSelf)
        {
            // 디스폰
            LeanPool.Despawn(transform);
        }
    }
}
