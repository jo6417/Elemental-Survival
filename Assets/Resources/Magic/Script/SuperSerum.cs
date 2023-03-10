using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class SuperSerum : MonoBehaviour
{
    [Header("Refer")]
    MagicHolder magicHolder;
    public ParticleSystem getEffect; // 플레이어가 획득했을때 이펙트
    Rigidbody2D rigid;

    [Header("Status")]
    bool initialFinish = false;
    float hpAddAmount = 1;
    bool isGet; //획득 여부


    private void Awake()
    {
        magicHolder = magicHolder == null ? GetComponent<MagicHolder>() : magicHolder;

        rigid = rigid == null ? GetComponent<Rigidbody2D>() : rigid;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 초기화 시작
        initialFinish = false;

        // 획득 여부 초기화
        isGet = false;

        // 사이즈 초기화
        transform.localScale = Vector2.zero;

        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder.initDone);

        //크리티컬 데미지 = 최대체력 증가량
        hpAddAmount = Mathf.RoundToInt(magicHolder.criticalPower);
        //최소 증가량 1f 보장
        hpAddAmount = (int)Mathf.Clamp(hpAddAmount, 1f, hpAddAmount);

        // 초기화 완료
        initialFinish = true;

        // 플레이어 쪽으로 날아가기
        StartCoroutine(GetMove(PlayerManager.Instance.transform));
    }

    IEnumerator GetMove(Transform Getter)
    {
        // 오브 사이즈 키우기
        transform.DOScale(Vector2.one, 0.5f);

        // 아이템 위치부터 플레이어 쪽으로 방향 벡터
        Vector2 dir = Getter.position - transform.position;

        // 플레이어 반대 방향으로 날아가기
        rigid.DOMove((Vector2)transform.position - dir.normalized * 5f, 0.3f);

        yield return new WaitForSeconds(0.3f);

        //플레이어 이동 속도 계수
        float accelSpeed = 0.8f;

        // 플레이어 방향으로 날아가기, 아이템 사라질때까지 방향 갱신하며 반복
        while (gameObject.activeSelf)
        {
            accelSpeed += 0.05f;

            //방향 벡터 갱신
            dir = Getter.position - transform.position;

            //플레이어 속도 반영
            dir = dir.normalized * PlayerManager.Instance.characterStat.moveSpeed * PlayerManager.Instance.dashSpeed * accelSpeed;

            //해당 방향으로 날아가기
            rigid.velocity = dir;

            yield return new WaitForSeconds(0.05f);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 초기화 완료 후 플레이어에 충돌하면
        if (!isGet && initialFinish && other.CompareTag(TagNameList.Player.ToString()))
        {
            // 획득함, 중복 획득 방지
            isGet = true;

            // 이동 멈추기
            rigid.velocity = Vector3.zero;

            // 플레이어 세럼 획득
            GetSuperSerum();
        }
    }

    void GetSuperSerum()
    {
        // 플레이어 자식으로 획득 이펙트 스폰
        LeanPool.Spawn(getEffect.gameObject, PlayerManager.Instance.transform.position, Quaternion.identity, PlayerManager.Instance.transform);

        // 플레이어 최대체력 상승
        PlayerManager.Instance.characterStat.hpMax += hpAddAmount;

        // 체력바 UI 갱신
        UIManager.Instance.UpdateHp();

        // 세럼 오브젝트 디스폰
        LeanPool.Despawn(gameObject);
    }
}
