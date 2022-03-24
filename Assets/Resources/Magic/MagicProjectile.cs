using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public MagicInfo magic;
    // public MagicHolder magicHolder;
    public Vector2 targetPos = Vector2.one * 10f; //목표 위치

    Rigidbody2D rigid;
    Collider2D col;
    public SpriteRenderer sprite;
    public Vector2 originColScale;
    float pierceNum = 0; //관통 횟수
    Vector3 lastPos; //오브젝트 마지막 위치
    // public bool isAutoDespawn = true;
    // public float magicDuration = 3f;
    // public float magicSpeed = 1f;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sprite = GetComponent<SpriteRenderer>();

        // 콜라이더 기본 사이즈 저장
        if (TryGetComponent(out BoxCollider2D boxCol))
        {
            originColScale = boxCol.size;
        }
        else if (TryGetComponent(out CapsuleCollider2D capCol))
        {
            originColScale = capCol.size;
        }
        else if (TryGetComponent(out CircleCollider2D circleCol))
        {
            originColScale = Vector2.one * circleCol.radius;
        }

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

        // 벡터값이 입력되지 않으면 랜덤으로 날리기
        if (targetPos == Vector2.one * 10f)
        {
            targetPos = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        }

        //속도 0 이상일때
        if (magic.speed != 0)
            //마법 속도만큼 날리기 (벡터 기본값 5 * 스피드 스탯 * 10 / 100)
            rigid.velocity = targetPos.normalized * MagicDB.Instance.MagicSpeed(magic, true);
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
        //마법 지속시간
        yield return new WaitForSeconds(delay);

        // 오브젝트 디스폰하기
        if (gameObject.activeSelf)
            LeanPool.Despawn(transform);
    }

    IEnumerator Initial()
    {
        //콜라이더 켜기
        col.enabled = true;

        //타겟 위치 초기화
        targetPos = Vector2.one * 10f;

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => TryGetComponent(out MagicHolder holder));
        magic = GetComponent<MagicHolder>().magic;

        //관통 횟수 초기화 
        pierceNum = MagicDB.Instance.MagicPierce(magic);

        // 마법 오브젝트 속도
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

            // if(magic.id == 1)
            // print(rigid.rotation + " : " + rotation);

            if (rotation > 90 || rotation < -90)
                sprite.flipY = true;
            else
                sprite.flipY = false;

            rigid.rotation = rotation;
            lastPos = transform.position;
        }
    }
}
