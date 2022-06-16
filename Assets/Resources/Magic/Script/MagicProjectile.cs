using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public MagicInfo magic;
    MagicHolder magicHolder;

    Rigidbody2D rigid;
    Collider2D coll;
    SpriteRenderer sprite;
    public ParticleSystem particle;
    Vector3 lastPos; //오브젝트 마지막 위치
    public bool lookDir = true; //날아가는 방향 바라볼지 여부
    public float spreadForce = 10f; // 파편 날아가는 강도

    [Header("Refer")]
    public GameObject[] shatters; //파편들
    public GameObject hitEffect; //타겟에 적중했을때 이펙트

    [Header("Status")]
    float speed = 0;
    float duration = 0;
    float pierceNum = 0; //관통 횟수
    Vector2 velocity;

    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
        rigid = GetComponent<Rigidbody2D>();
        velocity = rigid.velocity;
        coll = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();
        particle = particle == null ? GetComponent<ParticleSystem>() : particle;
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        //콜라이더 켜기
        coll.enabled = true;

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        //관통 횟수 초기화 
        pierceNum = MagicDB.Instance.MagicPierce(magic);

        // 마법 스피드 계산 + 추가 스피드 곱하기
        speed = MagicDB.Instance.MagicSpeed(magic, true) * magicHolder.MultipleSpeed;

        // 마법 지속시간 계산 + 추가 지속시간
        duration = MagicDB.Instance.MagicDuration(magic) + magicHolder.AddDuration;

        // 스프라이트 활성화
        if (sprite != null)
            sprite.enabled = true;

        // 파티클 활성화
        if (particle != null)
            particle.Play();

        //콜라이더 활성화
        coll.enabled = true;

        //마법 날리기
        StartCoroutine(FlyingMagic());
    }

    IEnumerator FlyingMagic()
    {
        yield return new WaitUntil(() => magic != null);

        Vector2 targetPos = magicHolder.targetPos;

        // 벡터값이 입력되지 않았으면 랜덤 방향 설정
        if (targetPos == Vector2.zero)
        {
            targetPos = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        }

        //투사체 날릴 방향
        Vector2 dir = targetPos - (Vector2)transform.position;

        // 해당 방향으로 날리기
        rigid.velocity = dir.normalized * speed;

        //타겟 위치 초기화
        targetPos = Vector2.zero;

        //마법 자동 디스폰
        StartCoroutine(DespawnMagic(duration));
    }

    private void Update()
    {
        if (lookDir)
        {
            if (magic != null && magic.speed != 0)
                LookDirAngle();
        }
    }

    void OffCollider()
    {
        coll.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //적에게 충돌
        if (other.CompareTag("Enemy") && magicHolder.target == MagicHolder.Target.Enemy)
        {
            //남은 관통횟수 0 일때 디스폰
            // print(gameObject.name + " : " + pierceNum);
            if (pierceNum <= 0)
            {
                if (gameObject.activeSelf)
                    StartCoroutine(DespawnMagic());
            }
            else
            {
                //관통 횟수 차감
                pierceNum--;
            }
        }

        // 플레이어에게 충돌, 대쉬중이면 무시
        if (other.CompareTag("Player") && magicHolder.target == MagicHolder.Target.Player && !PlayerManager.Instance.isDash)
        {
            //남은 관통횟수 0 일때 디스폰
            print(gameObject.name + " : " + pierceNum);
            if (pierceNum <= 0)
            {
                if (gameObject.activeSelf)
                    StartCoroutine(DespawnMagic());
            }
            else
            {
                //관통 횟수 차감
                pierceNum--;
            }
        }
    }

    void LookDirAngle()
    {
        // 날아가는 방향 바라보기
        if (transform.position != lastPos && sprite != null)
        {
            Vector3 returnDir = (transform.position - lastPos).normalized;
            float rotation = Mathf.Atan2(returnDir.y, returnDir.x) * Mathf.Rad2Deg;

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

        //파괴 이펙트 있으면 남기기
        if (hitEffect)
        {
            GameObject effect = LeanPool.Spawn(hitEffect, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            //마법 정보 넘겨주기
            effect.GetComponent<MagicHolder>().magic = magic;
        }

        //파편 있으면 비산 시키기
        if (shatters.Length > 0)
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

            //스프라이트 비활성화
            if (sprite != null)
                sprite.enabled = false;

            // 파티클 비활성화
            if (particle != null)
                particle.Stop();

            //콜라이더 비활성화
            coll.enabled = false;

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
        magicHolder.AddDuration = 0f;
        magicHolder.MultipleSpeed = 1f;

        // 오브젝트 디스폰하기
        if (gameObject.activeSelf)
            LeanPool.Despawn(transform);
    }
}
