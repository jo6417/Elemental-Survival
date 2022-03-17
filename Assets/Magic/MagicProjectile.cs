using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public MagicInfo magic;
    Rigidbody2D rigid;
    Collider2D col;
    public Vector2 originColScale;
    float pierceNum = 0; //관통 횟수
    Vector3 lastPos; //오브젝트 마지막 위치
    public bool isAutoDespawn = true;
    public float magicDuration = 3f;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

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
    }

    private void OnEnable()
    {
        //마법 자동 디스폰
        if(isAutoDespawn)
        StartCoroutine(AutoDespawn());
    }

    private void Update()
    {
        LookDirAngle();
    }

    IEnumerator AutoDespawn()
    {
        //초기화
        StartCoroutine(Initial());

        // 속도 버프 계수
        float durationBuff = magicDuration * (PlayerManager.Instance.duration - 1f);
        // 마법 오브젝트 속도
        float duration = magicDuration - durationBuff;

        //마법 지속시간
        yield return new WaitForSeconds(duration);

        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform);
    }

    IEnumerator Initial()
    {
        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magic != null);

        //관통 횟수 초기화 (onlyOne 이면 projectileNum 만큼 추가)
        if (magic.onlyOne == 1)
        {
            pierceNum = magic.pierce + PlayerManager.Instance.projectileNum;
        }
        else
        {
            pierceNum = magic.pierce;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //적에게 충돌
        if (other.CompareTag("Enemy"))
        {
            //남은 관통횟수 0일때 디스폰
            if (pierceNum == 0)
            {
                LeanPool.Despawn(transform);
            }
            else
            {
                pierceNum--;
            }
        }
    }

    void LookDirAngle()
    {
        // float angle = Mathf.Atan2(rigid.velocity.y, rigid.velocity.x);
        // transform.localEulerAngles = new Vector3(0, 0, (angle * 180) / Mathf.PI);

        // 날아가는 방향 바라보기
        if (transform.position != lastPos)
        {
            Vector3 returnDir = (transform.position - lastPos).normalized;
            float rotation = Mathf.Atan2(returnDir.y, returnDir.x) * Mathf.Rad2Deg;

            rigid.rotation = rotation - 90;
            lastPos = transform.position;
        }
    }
}
