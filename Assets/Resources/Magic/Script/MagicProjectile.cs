using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    [Header("Modify")]
    [SerializeField] bool autoShot = true; // 자동 발사 여부
    [SerializeField] bool isFade = false; // 디스폰시 옅어지며 사라짐
    Vector3 lastPos; //오브젝트 마지막 위치
    public bool lookDir = true; //날아가는 방향 바라볼지 여부
    public bool isSpin; // 투사체 회전 여부
    public float spreadForce = 0f; // 파편 날아가는 강도 (0이면 비활성화)

    [Header("Refer")]
    [SerializeField] MagicHolder MagicHolder;
    public ParticleManager particleManager;
    public GameObject[] shatters; //파편들
    public GameObject hitEffect; //타겟에 적중했을때 이펙트
    public Rigidbody2D rigid;
    [SerializeField] Collider2D coll;
    [SerializeField] SpriteRenderer sprite;
    Color originColor;

    [Header("Status")]
    float speed = 0;
    float customDuration = 0;
    Vector2 velocity;

    private void Awake()
    {
        MagicHolder = MagicHolder == null ? GetComponent<MagicHolder>() : MagicHolder;
        rigid = rigid == null ? GetComponent<Rigidbody2D>() : rigid;
        coll = coll == null ? GetComponent<Collider2D>() : coll;
        sprite = sprite == null ? GetComponent<SpriteRenderer>() : sprite;

        velocity = rigid.velocity;

        // 원본 컬러 저장
        if (sprite != null)
            originColor = sprite.color;

        // particleManager = particleManager == null ? GetComponentInChildren<ParticleManager>() : particleManager;
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    private void OnDisable()
    {
        //타겟 위치 초기화
        MagicHolder.targetPos = Vector2.zero;
    }

    IEnumerator Init()
    {
        // 스프라이트 끄기
        if (sprite != null)
            sprite.enabled = false;

        //콜라이더 끄기
        coll.enabled = false;

        // magicHolder 초기화 대기
        yield return new WaitUntil(() => MagicHolder.initDone);

        // 마법 스피드 계산 + 추가 스피드 곱하기
        speed = MagicDB.Instance.MagicSpeed(MagicHolder.magic, true) * MagicHolder.MultipleSpeed;

        // 마법 지속시간 계산 + 추가 지속시간
        customDuration = MagicHolder.duration + MagicHolder.AddDuration;

        // 타겟 위치 바라보기
        Vector3 returnDir = (MagicHolder.targetPos - transform.position).normalized;
        float rotation = Mathf.Atan2(returnDir.y, returnDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(Vector3.forward * rotation);

        // 스프라이트 있을때
        if (sprite != null)
        {
            // 원본 컬러로 초기화
            sprite.color = originColor;

            // 스프라이트 켜기
            sprite.enabled = true;
        }

        // 파티클 있으면 켜기
        if (particleManager != null)
        {
            particleManager.gameObject.SetActive(true);
            particleManager.particle.Play();
        }

        //콜라이더 켜기
        coll.enabled = true;

        // 자동발사일때
        if (autoShot)
        {
            //마법 날리기
            StartCoroutine(ShotMagic());
        }
    }

    public IEnumerator ShotMagic()
    {
        // magicHolder 초기화 대기
        yield return new WaitUntil(() => MagicHolder.initDone);

        // 목표 위치 캐싱
        Vector2 targetPos = MagicHolder.targetPos;

        // 벡터값이 입력되지 않았으면 랜덤 방향 설정
        if (targetPos == Vector2.zero)
        {
            targetPos = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        }

        // 투사체 날릴 방향
        Vector2 dir = targetPos - (Vector2)transform.position;

        // 해당 방향으로 날리기
        rigid.velocity = dir.normalized * speed;

        // 날아가는 방향따라 회전 시키기
        if (isSpin)
            rigid.angularVelocity = dir.x > 0 ? -speed * 30f : speed * 30f;

        // duration 만큼 날아간후에
        yield return new WaitForSeconds(customDuration);

        // 자동 디스폰
        if (gameObject)
            StartCoroutine(DespawnMagic());

        // // 목표 위치까지 거리가 가까워지면 파괴
        // float lastDistance = -1;
        // while (gameObject.activeSelf)
        // {
        //     // 현재 목표 위치와의 거리 산출
        //     float nowDistance = (targetPos - (Vector2)transform.position).magnitude;

        //     // 목표위치와 거리가 이전 보다 멀어졌으면
        //     if (lastDistance != -1 && nowDistance > lastDistance)
        //     {
        //         //마법 자동 디스폰
        //         StartCoroutine(DespawnMagic());

        //         break;
        //     }
        //     // 이전보다 가까워졌으면
        //     else
        //         // 이전 거리를 현재 거리로 갱신
        //         lastDistance = nowDistance;

        //     yield return new WaitForSeconds(Time.deltaTime);
        // }
    }

    private void Update()
    {
        if (MagicHolder.initDone)
            if (lookDir && speed != 0)
                LookDirAngle();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //적에게 충돌
        if (MagicHolder.targetType == MagicHolder.TargetType.Enemy
        && other.CompareTag(TagNameList.Enemy.ToString()))
        {
            // 히트박스 없으면 리턴
            if (!other.TryGetComponent(out HitBox enemyHitBox))
                return;

            // 맞는 순간 콜라이더 끄기, 중복 충돌 방지
            // coll.enabled = false;

            // print(other.transform.parent.parent.name + " : " + magicHolder.pierceCount);

            //남은 관통횟수 0 일때 디스폰
            if (MagicHolder.pierce == 0)
            {
                if (gameObject.activeSelf)
                    StartCoroutine(DespawnMagic());
            }
            // else
            //     // 관통 횟수 남아있으면 다시 콜라이더 켜기
            //     coll.enabled = true;
        }

        // 플레이어에게 충돌, 대쉬중이면 무시
        if (MagicHolder.targetType == MagicHolder.TargetType.Player && other.CompareTag(TagNameList.Player.ToString()) && !PlayerManager.Instance.isDash)
        {
            // print(gameObject.name + " : " + magicHolder.pierceCount);

            // 히트 콜백 있으면 실행
            if (MagicHolder.hitAction != null)
                MagicHolder.hitAction.Invoke();

            //남은 관통횟수 0 일때 디스폰
            if (MagicHolder.pierce == 0)
            {
                if (gameObject.activeSelf)
                    StartCoroutine(DespawnMagic());
            }
        }
    }

    void LookDirAngle()
    {
        // 날아가는 방향 바라보기
        if (transform.position != lastPos)
        {
            Vector3 returnDir = (transform.position - lastPos).normalized;
            float rotation = Mathf.Atan2(returnDir.y, returnDir.x) * Mathf.Rad2Deg;

            if (sprite != null)
                if (rotation > 90 || rotation < -90)
                    sprite.flipY = true;
                else
                    sprite.flipY = false;

            rigid.rotation = rotation;
            lastPos = transform.position;
        }
    }

    IEnumerator DespawnMagic(float delay = 0)
    {
        //딜레이 만큼 대기
        yield return new WaitForSeconds(delay);

        //속도 초기화
        rigid.velocity = Vector3.zero;
        //각속도 초기화
        rigid.angularVelocity = 0f;

        // 콜라이더 끄기
        coll.enabled = false;
        // 스프라이트 끄기
        if (sprite != null)
            sprite.enabled = false;

        // 디스폰 콜백 함수 있으면 실행
        if (MagicHolder.despawnAction != null)
            MagicHolder.despawnAction.Invoke();

        //파괴 이펙트 있으면 남기기
        if (hitEffect)
        {
            GameObject effect = LeanPool.Spawn(hitEffect, transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

            //마법 정보 넘겨주기
            if (effect.TryGetComponent(out MagicHolder magicHolder))
            {
                magicHolder.magic = MagicHolder.magic;
            }
        }

        //파편 있으면 비산 시키기
        if (shatters.Length > 0 && spreadForce > 0)
        {
            foreach (GameObject shatter in shatters)
            {
                //파편 활성화
                shatter.SetActive(true);

                //날아갈 방향
                Vector2 dir = Random.insideUnitCircle;

                Rigidbody2D rigid = shatter.GetComponent<Rigidbody2D>();
                //속도 지정
                rigid.velocity = dir.normalized * spreadForce;
                //각속도 지정
                rigid.angularVelocity = dir.x * 20f * spreadForce;
            }

            //파편 비산되는동안 대기
            yield return new WaitForSeconds(1f);

            //파편 초기화
            foreach (GameObject shatter in shatters)
            {
                //파편 비활성화
                shatter.SetActive(false);

                Rigidbody2D rigid = shatter.GetComponent<Rigidbody2D>();
                //속도 초기화
                rigid.velocity = Vector3.zero;
                //각속도 초기화
                rigid.angularVelocity = 0f;

                //위치 초기화
                shatter.transform.localPosition = Vector3.zero;
                //각도 초기화
                shatter.transform.rotation = Quaternion.identity;
            }
        }

        // 마법 추가 스탯 초기화
        MagicHolder.AddDuration = 0f;
        MagicHolder.MultipleSpeed = 1f;

        if (sprite != null)
            // 오브젝트 투명하게
            if (isFade)
            {
                sprite.DOColor(Color.clear, 0.2f);
                yield return new WaitForSeconds(0.2f);
            }
            // 바로 끄기
            else
                sprite.enabled = false;

        // 파티클 매니저 있으면
        if (particleManager != null)
        {
            // 파티클 사라진후 디스폰
            particleManager.SmoothDisable();

            // 파티클 꺼질때까지 대기
            yield return new WaitUntil(() => !particleManager.gameObject.activeSelf);
        }

        // 오브젝트 디스폰하기
        if (gameObject.activeSelf)
            LeanPool.Despawn(transform);
    }
}
