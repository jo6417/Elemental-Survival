using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TMPro;
using Lean.Pool;

public class Truck : MonoBehaviour
{
    [SerializeField] int duration = 10; // 트럭 판매 시간
    [SerializeField, ReadOnly] float timeCount; // 현재 남은 시간

    [Header("Refer")]
    [SerializeField] Collider2D stopColl; // 정차시 물리 콜라이더
    [SerializeField] Collider2D attackColl; // 몬스터 충격 콜라이더
    [SerializeField] List<Sprite> truckSpriteList = new List<Sprite>();
    [SerializeField] SpriteRenderer truckSprite;
    [SerializeField] SpriteRenderer shadow;
    [SerializeField] ParticleManager wheelDust; // 바퀴 파티클 이펙트
    [SerializeField] ParticleManager goEffect; // 배기구 매연 이펙트
    [SerializeField] ParticleManager stopEffect; // 도착 후 이펙트(배기구 매연, 착지 먼지 등)
    [SerializeField] GameObject shopGlass; // 양측 자판기 부모 오브젝트
    [SerializeField] Transform shield; // 몬스터 접근금지 쉴드
    [SerializeField] TextMeshProUGUI timerText; // 트럭 판매 시간 표시 UI
    [SerializeField] Rigidbody2D rigid;

    private void Awake()
    {
        // 쉴드 끄기
        shield.gameObject.SetActive(false);
        // 충돌 콜라이더 끄기
        stopColl.enabled = false;
        // 스프라이트 끄기
        truckSprite.gameObject.SetActive(false);
        // 정차 충돌용 콜라이더 충돌 끄기
        stopColl.isTrigger = true;
        // 그림자 끄기
        shadow.gameObject.SetActive(false);
        // 바퀴 파티클 대기 후 끄기
        wheelDust.gameObject.SetActive(false);
        // 주행 매연 이펙트 끄기
        goEffect.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 멈췄을때의 이펙트 끄기
        stopEffect.gameObject.SetActive(false);
        // 자판기 부모 오브젝트 끄기
        shopGlass.SetActive(false);

        // 타이머 끄기
        timerText.color = Color.clear;
        timerText.gameObject.SetActive(false);

        yield return new WaitUntil(() => WorldSpawner.Instance != null);

        // 플레이어가 오른쪽 보고있을때
        if (PlayerManager.Instance.lastDir.x > 0)
        {
            // 좌측에서 등장
            transform.position = new Vector2(WorldSpawner.Instance.spawnColl.bounds.min.x + 6f, PlayerManager.Instance.transform.position.y + 2f);

            // 진행 방향으로 트럭 방향 회전
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            // 우측에서 등장
            transform.position = new Vector2(WorldSpawner.Instance.spawnColl.bounds.max.x - 6f, PlayerManager.Instance.transform.position.y + 2f);

            // 진행 방향으로 트럭 방향 회전
            transform.rotation = Quaternion.Euler(0, 180f, 0);
        }

        // 충돌 콜라이더 켜기
        stopColl.enabled = true;
        // 스프라이트 켜기
        truckSprite.gameObject.SetActive(true);
        // 정차 충돌용 콜라이더 충돌 켜기
        stopColl.isTrigger = false;
        // 그림자 켜기
        shadow.gameObject.SetActive(true);
        // 바퀴 파티클 대기 후 켜기
        wheelDust.gameObject.SetActive(true);
        // 주행 매연 이펙트 켜기
        goEffect.gameObject.SetActive(true);

        // 타이머 각도 초기화
        timerText.transform.rotation = Quaternion.Euler(Vector3.zero);

        // 문 닫힌 스프라이트로 초기화
        truckSprite.sprite = truckSpriteList[0];
        // 공격 콜라이더 켜기
        attackColl.enabled = true;

        // 주행 매연 이펙트 켜기
        goEffect.gameObject.SetActive(true);

        // 화면 가운데 혹은 입력된 위치로 이동
        Vector2 targetPos = new Vector2(PlayerManager.Instance.transform.position.x, transform.position.y);
        transform.DOMove(targetPos, 1f)
        .OnComplete(() =>
        {
            // 정차 충돌용 콜라이더 충돌 켜기
            stopColl.isTrigger = false;
        });
        yield return new WaitForSeconds(0.5f);

        // 앞으로 기울어지는 애니메이션
        truckSprite.transform.DOLocalRotate(new Vector3(0f, 0f, -20f), 0.2f)
        .OnStart(() =>
        {
            // 기울어지기 전에 주행 매연 이펙트 끄기
            goEffect.SmoothDisable();
        })
        .SetEase(Ease.OutQuint);

        yield return new WaitForSeconds(0.5f);

        // 다시 기울기 복구하는 애니메이션
        truckSprite.transform.DOLocalRotate(new Vector3(0f, 0f, 0f), 0.2f)
        .SetEase(Ease.OutBounce)
        .OnComplete(() =>
        {
            // 도착 후 이펙트 켜기
            stopEffect.gameObject.SetActive(true);

            // 공격 콜라이더 끄기
            attackColl.enabled = false;
        });

        yield return new WaitForSeconds(0.5f);

        // 가판대 열린 스프라이트로 교체
        truckSprite.sprite = truckSpriteList[1];

        // 타이머 켜기
        timerText.DOColor(new Color32(255, 50, 50, 255), 0.5f);
        timerText.gameObject.SetActive(true);
        // 트럭 타이머 진행
        StartCoroutine(Timer());

        // 쉴드 전개
        shield.localScale = Vector3.zero;
        shield.gameObject.SetActive(true);
        ParticleSystem dust = shield.Find("DustTrails").GetComponent<ParticleSystem>();
        ParticleSystem.EmissionModule dustEm = dust.emission;
        ParticleSystem.MainModule dustMain = dust.main;

        Sequence shieldSeq = DOTween.Sequence();
        shieldSeq
        .Append(
            // 사이즈 확장
            shield.DOScale(Vector3.one * 40f, 1f)
        )
        .Append(
            // 사이즈 축소
            shield.DOScale(Vector3.zero, duration)
            .OnUpdate(() =>
            {
                // 쉴드 먼지 이펙트 같이 줄이기
                dustEm.rateOverTime = shield.localScale.x;
                dustMain.startSizeYMultiplier = shield.localScale.x / 40f;
                dustMain.startSizeZMultiplier = 3 * shield.localScale.x / 40f;
            })
        )
        .AppendCallback(() =>
        {
            // 쉴드 끄기
            shield.gameObject.SetActive(false);
        });

        // 랜덤 자판기 종류 뽑기 (중복 방지)
        List<int> randomShops = SystemManager.Instance.RandomIndexes(shopGlass.transform.GetChild(0).childCount, 2);

        // 자판기 개수만큼 반복
        for (int i = 0; i < shopGlass.transform.childCount; i++)
        {
            Transform shop = shopGlass.transform.GetChild(i);

            // 자판기 각도 초기화
            shop.rotation = Quaternion.Euler(Vector3.zero);

            // 정해진 자판기만 켜기
            for (int j = 0; j < shop.childCount; j++)
            {
                GameObject targetShop = shop.GetChild(j).gameObject;

                // 랜덤으로 뽑은 자판기 종류와 같다면
                if (randomShops[i] == j)
                    // 해당 오브젝트 켜기
                    targetShop.SetActive(true);
                else
                    // 해당 오브젝트 끄기
                    targetShop.SetActive(false);
            }
        }

        // 자판기 부모 켜기
        shopGlass.SetActive(true);
    }

    IEnumerator Timer()
    {
        timeCount = duration;
        while (timeCount > -1)
        {
            //시간을 60으로 나눈 몫을 60으로 나눈 나머지
            string minute = 0 < (int)(timeCount / 60f % 60f) ? string.Format("{0:00}", Mathf.FloorToInt(timeCount / 60f % 60f)) + ":" : "00:";
            //시간을 60으로 나눈 나머지
            string second = string.Format("{0:00}", timeCount % 60f);

            // 남은 시간 표시
            timerText.text = minute + second;

            // 1초 대기
            yield return new WaitForSeconds(1f);

            // 남은 시간 1초 차감
            timeCount--;
        }

        // 트럭 퇴장 시작
        StartCoroutine(ExitMove());
    }

    public IEnumerator ExitMove()
    {
        // 가판대 문 닫기
        truckSprite.sprite = truckSpriteList[0];
        // 자판기 모두 끄기
        for (int i = 0; i < shopGlass.transform.childCount; i++)
        {
            Transform shop = shopGlass.transform.GetChild(i);

            // 모든 자판기 끄기
            for (int j = 0; j < shop.childCount; j++)
                shop.GetChild(j).gameObject.SetActive(false);
        }

        // 잠시 대기
        yield return new WaitForSeconds(1f);

        // 정차 충돌용 콜라이더 충돌 끄기
        stopColl.isTrigger = true;

        // 뒤로 살짝 후퇴
        Vector3 backPos = transform.rotation.y == 0f ? -Vector2.right * 3f : Vector2.right * 3f;
        transform.DOMove(transform.position + backPos, 0.5f)
        .SetEase(Ease.OutCubic);
        yield return new WaitForSeconds(0.5f);

        // 정차 이펙트 끄기
        stopEffect.SmoothDisable();

        // 주행 매연 이펙트 켜기
        goEffect.gameObject.SetActive(true);

        // 공격 콜라이더 켜기
        attackColl.enabled = true;

        // 디스폰 트리거로 -2를 사용
        timeCount = -2;

        // 플레이어 이동 속도 계수
        float playerSpeed = PlayerManager.Instance.PlayerStat_Now.moveSpeed * PlayerManager.Instance.dashSpeed;
        // 가속도 기본값
        float accelSpeed = 1f;

        // 디스폰 할때까지 반복
        while (truckSprite.gameObject.activeSelf)
        {
            // 가속도 상승
            accelSpeed += 0.1f;

            // 플레이어 속도 및 가속도 반영
            backPos = backPos.normalized * playerSpeed * accelSpeed;

            // 해당 방향으로 주행
            rigid.velocity = Vector2.Lerp(rigid.velocity, -backPos, Time.deltaTime);

            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 스폰 콜라이더 밖으로 나갔을때, 제한시간 끝났을때
        if (other.CompareTag("Respawn") && timeCount == -2)
        {
            // 초기화 하고 디스폰
            StartCoroutine(OutDespawn());
        }
    }

    IEnumerator OutDespawn()
    {
        // 속도 초기화
        rigid.velocity = Vector3.zero;

        // 충돌 콜라이더 끄기
        stopColl.enabled = false;
        // 스프라이트 끄기
        truckSprite.gameObject.SetActive(false);
        // 그림자 끄기
        shadow.gameObject.SetActive(false);
        // 바퀴 파티클 대기 후 끄기
        wheelDust.SmoothDisable();
        // 주행 매연 이펙트 끄기
        goEffect.SmoothDisable();

        // 바퀴 파티클 및 주행 매연이 꺼질때까지 대기
        yield return new WaitUntil(() => !goEffect.gameObject.activeSelf && !wheelDust.gameObject.activeSelf);

        // 디스폰
        LeanPool.Despawn(transform);
    }
}
