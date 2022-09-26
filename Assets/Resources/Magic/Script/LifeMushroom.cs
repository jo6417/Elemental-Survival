using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class LifeMushroom : MonoBehaviour
{
    [Header("Refer")]
    public ParticleSystem poisonSmoke; //독 연기 이펙트
    public Rigidbody2D rigid;
    public CircleCollider2D triggerColl; // 해당 콜라이더 충돌시 이동 시작
    MagicHolder magicHolder;
    MagicInfo magic;
    GameObject getterObj = null; // 버섯 획득한 캐릭터 변수
    SpriteRenderer sprite;
    bool getAble = false; //획득 가능 상태


    float power; //버섯 회복량 및 데미지
    float duration; //버섯 지속시간
    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
        sprite = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 획득 불가능
        getAble = false;

        // 획득 캐릭터 초기화
        getterObj = null;

        //콜라이더 끄기
        triggerColl.enabled = false;

        // 시작할땐 타겟 없음
        magicHolder.SetTarget(MagicHolder.Target.None);

        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        // 회복량, 데미지 계산
        power = MagicDB.Instance.MagicPower(magic);
        // 지속시간 계산
        duration = MagicDB.Instance.MagicDuration(magic);

        // 색깔 초록색으로 초기화
        sprite.color = Color.green;

        // 콜라이더 켜기
        triggerColl.enabled = true;

        // 효과 변경 코루틴 시작
        StartCoroutine(ChangeBuff());
    }

    private void Update()
    {
        // 획득한 캐릭터 없으면 속도 늦추기
        if (getterObj == null)
        {
            rigid.velocity = Vector2.Lerp(rigid.velocity, Vector2.zero, Time.deltaTime);
        }
    }

    IEnumerator ChangeBuff()
    {
        // 타겟 변경
        magicHolder.SetTarget(MagicHolder.Target.Player);

        // 시간 절반 지나면 보라색으로 색깔 변경
        sprite.DOColor(SystemManager.Instance.HexToRGBA("EA16EA"), duration / 2f)
        .SetEase(Ease.InExpo)
        .OnComplete(() =>
        {
            // 타겟 변경
            magicHolder.SetTarget(MagicHolder.Target.Enemy);

            // 독연기 이펙트 터지기
            poisonSmoke.Play();
        });

        // 보라색 될때까지 대기
        yield return new WaitUntil(() => sprite.color == SystemManager.Instance.HexToRGBA("EA16EA"));

        // 남은 시간동안 투명하게 색깔 변경
        sprite.DOColor(Color.clear, duration / 2f)
        .SetEase(Ease.InExpo)
        .OnComplete(() =>
        {
            // 디스폰
            LeanPool.Despawn(transform);
        });
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 획득 대상이 없을때, 획득 불가능 상태일때
        if (getterObj == null && !getAble)
        {
            // 플레이어 충돌했을때, 힐링 상태일때, getterObj 없을때
            if (other.CompareTag(SystemManager.TagNameList.Player.ToString())
            && magicHolder.targetType == MagicHolder.Target.Player)
            {
                // 획득한 오브젝트 기억하기
                getterObj = other.gameObject;

                // 색 변화 멈추기
                sprite.DOPause();
                // 색깔 리셋
                sprite.color = Color.green;

                // 플레이어에게 날아감
                StartCoroutine(GetMove(getterObj.transform, magicHolder.targetType));
            }

            // 몬스터 충돌했을때, 공격 상태일때, getterObj 없을때 획득상태로 변경 후 날아감
            if (other.CompareTag(SystemManager.TagNameList.Enemy.ToString())
            && magicHolder.targetType == MagicHolder.Target.Enemy)
            {
                // 획득한 오브젝트 기억하기
                getterObj = other.gameObject;

                // 색 변화 멈추기
                sprite.DOPause();
                // 색깔 리셋
                sprite.color = SystemManager.Instance.HexToRGBA("EA16EA");

                // 몬스터에게 날아감
                StartCoroutine(GetMove(getterObj.transform, magicHolder.targetType));
            }

            return;
        }

        // 획득 대상에 충돌했을때, 획득 가능할때
        if (getterObj == other.gameObject && getAble)
        {
            // 콜라이더 끄기
            triggerColl.enabled = false;

            // 플레이어가 획득하면
            if (other.transform.CompareTag(SystemManager.TagNameList.Player.ToString())
            && magicHolder.targetType == MagicHolder.Target.Player)
            {
                // 데미지 계산, 고정 데미지 setPower가 없으면 마법 파워로 계산
                float damage = magicHolder.fixedPower == 0 ? power : magicHolder.fixedPower;
                // 고정 데미지에 확률 계산
                damage = Random.Range(damage * 0.8f, damage * 1.2f);

                // 크리티컬 확률 및 데미지 계산
                bool critical = MagicDB.Instance.MagicCritical(magic);
                float criticalPower = MagicDB.Instance.MagicCriticalPower(magic);

                // 크리티컬 일때
                if (critical)
                {
                    // 크리티컬 데미지 계산해서 반영
                    damage *= criticalPower;
                }

                // print($"{damage / criticalPower} * {criticalPower} = {damage}");

                // 플레이어 체력 회복
                PlayerManager.Instance.hitBox.Damage(-damage, false);
            }

            // 몬스터가 획득하면
            if (other.transform.CompareTag(SystemManager.TagNameList.Enemy.ToString())
            && magicHolder.targetType == MagicHolder.Target.Enemy)
            {
                // 해당 몬스터 데미지
                if (other.transform.TryGetComponent(out HitBox enemyHitBox))
                {
                    StartCoroutine(enemyHitBox.Hit(magicHolder));
                }
            }

            // 변수 초기화
            getterObj = null;

            //버섯 디스폰
            LeanPool.Despawn(transform);

            return;
        }
    }

    IEnumerator GetMove(Transform Getter, MagicHolder.Target target)
    {
        // 타겟 정보 저장
        MagicHolder.Target _target = target;

        // 아이템 위치부터 타겟 쪽으로 방향 벡터
        Vector2 dir = Getter.position - transform.position;

        // 타겟 반대 방향으로 날아가기, 플레이어가 먹을때만
        if (magicHolder.targetType == MagicHolder.Target.Player)
        {
            rigid.DOMove((Vector2)transform.position - dir.normalized * 5f, 0.3f);

            yield return new WaitForSeconds(0.3f);
        }

        // 이제 획득 가능
        getAble = true;

        // 이동 속도 계수
        float accelSpeed = 0.8f;

        // 타겟 방향으로 날아가기, 아이템 사라질때까지 방향 갱신하며 반복
        while (getterObj != null || gameObject.activeSelf)
        {
            // 타겟 사라지면 이동 멈춤
            if (!getterObj.activeInHierarchy)
            {
                // 변수 초기화
                getterObj = null;

                break;
            }

            accelSpeed += 0.05f;

            //방향 벡터 갱신
            dir = Getter.position - transform.position;

            //타겟 속도 반영
            dir = dir.normalized * PlayerManager.Instance.PlayerStat_Now.moveSpeed * PlayerManager.Instance.dashSpeed * accelSpeed;

            //해당 방향으로 날아가기
            rigid.velocity = dir;

            // x방향으로 회전 시키기
            rigid.angularVelocity = dir.x * 10f;

            yield return new WaitForSeconds(0.05f);
        }
    }
}
