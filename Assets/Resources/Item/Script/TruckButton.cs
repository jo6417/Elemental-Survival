using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class TruckButton : MonoBehaviour
{
    [Header("Interact")]
    [SerializeField] Interacter interacter; //상호작용 콜백 함수 클래스
    [SerializeField] Canvas uiCanvas; // 가격, 상호작용키 안내 UI 캔버스
    [SerializeField] GameObject showKey; //상호작용 키 표시 UI

    [Header("Refer")]
    [SerializeField] Collider2D coll;
    [SerializeField] SpriteRenderer btnSprite;
    [SerializeField] Sprite[] btnSpriteList = new Sprite[2];
    [SerializeField] ParticleManager backLightEffect;
    [SerializeField] GameObject truckPrefab; // 소환할 트럭 프리팹

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
        // 충돌 콜라이더 켜기
        coll.enabled = true;

        // 캔버스 끄기
        uiCanvas.gameObject.SetActive(false);

        // 상호작용 키 UI 끄기
        showKey.SetActive(false);

        // 버튼 스프라이트 초기화
        btnSprite.sprite = btnSpriteList[0];

        // 후면 파티클 켜기
        backLightEffect.gameObject.SetActive(true);

        // 상호작용 트리거 함수 콜백에 연결 시키기
        if (interacter.interactTriggerCallback == null)
            interacter.interactTriggerCallback += InteractTrigger;
        // 상호작용 함수 콜백에 연결 시키기
        if (interacter.interactSubmitCallback == null)
            interacter.interactSubmitCallback += InteractSubmit;

        // 캔버스 켜기
        uiCanvas.gameObject.SetActive(true);

        yield return null;
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

    public void InteractSubmit(bool isPress = false)
    {
        // 상호작용 불가능하면 리턴
        if (!uiCanvas.gameObject.activeSelf)
            return;

        // 인디케이터 꺼져있으면 리턴
        if (!showKey.activeSelf)
            return;

        // 상호작용 버튼 뗐을때
        if (!isPress)
            return;

        // 버튼 누르고 트럭 소환
        StartCoroutine(SummonTruck());
    }

    IEnumerator SummonTruck()
    {
        // 캔버스 끄기
        uiCanvas.gameObject.SetActive(false);
        // 상호작용 키 UI 끄기
        showKey.SetActive(false);
        // 충돌용 콜라이더 끄기
        coll.enabled = false;

        // 후면 파티클 끄기
        backLightEffect.SmoothDisable();

        // 버튼 누른 스프라이트로 변경
        btnSprite.sprite = btnSpriteList[1];

        // 트럭 소환
        LeanPool.Spawn(truckPrefab, transform.position, Quaternion.identity, ObjectPool.Instance.itemPool);

        // 버튼 사라지기
        btnSprite.DOColor(Color.clear, 1f);

        yield return new WaitForSeconds(1f);

        // 버튼 증발
        LeanPool.Despawn(transform);
    }
}
