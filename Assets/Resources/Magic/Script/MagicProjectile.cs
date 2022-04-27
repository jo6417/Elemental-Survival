using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public MagicInfo magic;
    public MagicHolder magicHolder;

    Rigidbody2D rigid;
    Collider2D col;
    public SpriteRenderer sprite;
    float pierceNum = 0; //관통 횟수
    Vector3 lastPos; //오브젝트 마지막 위치

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();

        //초기화
        StartCoroutine(Initial());
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Initial());

        //마법 날리기
        StartCoroutine(FlyingMagic());
    }

    IEnumerator FlyingMagic()
    {
        yield return new WaitUntil(() => magic != null);

        Vector2 targetPos = magicHolder.targetPos;

        // 벡터값이 입력되지 않으면 랜덤으로 날리기
        if (targetPos == Vector2.one * 10f)
        {
            targetPos = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        }

        //속도 0 이상일때
        if (magic.speed != 0)
            //마법 속도만큼 날리기 (벡터 기본값 5 * 스피드 스탯 * 10 / 100)
            rigid.velocity = targetPos.normalized * MagicDB.Instance.MagicSpeed(magic, true);

        //원래 속도에 플레이어 타임스케일 지속 반영
        Vector2 originVel = rigid.velocity;
        while (gameObject.activeSelf)
        {
            rigid.velocity = originVel * VarManager.Instance.playerTimeScale;
            yield return null;
        }

        //타겟 위치 초기화
        targetPos = Vector2.one * 10f;
    }

    private void Update()
    {
        if (magic != null && magic.speed != 0)
            LookDirAngle();
    }

    void OffCollider()
    {
        col.enabled = false;
    }

    IEnumerator DespawnMagic(float delay = 0)
    {
        while (delay > 0)
        {
            delay -= Time.deltaTime * VarManager.Instance.playerTimeScale;

            yield return null;
        }

        // 오브젝트 디스폰하기
        if (gameObject.activeSelf)
            LeanPool.Despawn(transform);
    }

    IEnumerator Initial()
    {
        //콜라이더 켜기
        col.enabled = true;

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => TryGetComponent(out MagicHolder holder));
        magicHolder = GetComponent<MagicHolder>();
        magic = magicHolder.magic;

        //관통 횟수 초기화 
        pierceNum = MagicDB.Instance.MagicPierce(magic);

        // 마법 지속시간
        float duration = MagicDB.Instance.MagicDuration(magic);
        //마법 자동 디스폰
        StartCoroutine(DespawnMagic(duration));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //적에게 충돌
        if (other.CompareTag("Enemy"))
        {
            //남은 관통횟수 0 일때 디스폰
            // print(gameObject.name + " : " + pierceNum);
            if (pierceNum == 0)
            {
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
        if (transform.position != lastPos)
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
}
