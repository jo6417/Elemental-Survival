using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

public class HealingSpa : MonoBehaviour
{
    [Header("State")]
    [SerializeField] MagicHolder magicHolder;
    MagicInfo magic;
    [SerializeField, ReadOnly] bool inPlayer = false; // 연못안에 플레이어 들어있는지 여부

    [Header("Refer")]
    [SerializeField] Collider2D coll;
    [SerializeField] SpriteRenderer pondSprite; // 연못 스프라이트
    public GameObject pulsePrefab; // 캐릭터 밑에 펄스 이펙트
    public GameObject dustEffect; // 디스폰 이펙트

    [Header("Magic Stat")]
    float range;
    float duration;
    int healPower = -1;
    float coolTime;
    float speed;

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 콜라이더 끄기
        coll.enabled = false;

        // 연못 이미지 숨기기
        pondSprite.enabled = false;

        //마법 정보 불러올때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        range = MagicDB.Instance.MagicRange(magic);
        duration = MagicDB.Instance.MagicDuration(magic);
        healPower = Mathf.RoundToInt(MagicDB.Instance.MagicPower(magic)); //회복할 양, int로 반올림해서 사용
        coolTime = MagicDB.Instance.MagicCoolTime(magic);
        speed = MagicDB.Instance.MagicSpeed(magic, false);

        if (magicHolder.isManualCast)
            // 타겟 위치로 이동
            transform.position = magicHolder.targetPos;
        else
            // 플레이어 위치로 이동
            transform.position = PlayerManager.Instance.transform.position;

        // 제로 사이즈로 초기화
        transform.localScale = Vector2.zero;

        // 연못 이미지 나타내기
        pondSprite.enabled = true;

        //제로 사이즈에서 크기 키우기
        transform.DOScale(range, 0.5f)
        .SetEase(Ease.OutBack);

        // 플레이어 및 몬스터 피직스와 충돌하게 충돌 레이어 초기화
        gameObject.layer = SystemManager.Instance.layerList.EnemyPhysics_Layer;

        // 콜라이더 켜기
        coll.enabled = true;

        // 지속시간 동안 대기
        yield return new WaitForSeconds(duration);

        // 제로 사이즈로 줄이기
        transform.DOScale(0, 0.5f)
        .SetEase(Ease.InBack)
        .OnComplete(() =>
        {
            // 먼지 퍼지는 이펙트 소환
            LeanPool.Spawn(dustEffect, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            // 연못 디스폰
            LeanPool.Despawn(gameObject);
        });
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 들어옴
        if (other.TryGetComponent(out PlayerManager playerManager))
            inPlayer = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 플레이어 나감
        if (other.TryGetComponent(out PlayerManager playerManager))
            inPlayer = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 적이 닿으면
        if (other.CompareTag(SystemManager.TagNameList.Enemy.ToString()) && other.TryGetComponent(out Character character))
        {
            // 해당 몬스터 히트 쿨타임 끝났을때
            if (character.hitDelayCount <= 0)
            {
                // 죽은 적이면 취소
                if (character.isDead)
                    return;

                // 닿은 적에게 데미지 주기
                StartCoroutine(character.hitBoxList[0].Hit(magicHolder));

                // 충돌한 캐릭터 발밑에 물결 일으키기
                LeanPool.Spawn(pulsePrefab, other.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

                // 연못 안에 플레이어 있으면
                if (inPlayer)
                {
                    // 플레이어 체력 회복
                    PlayerManager.Instance.hitBox.Damage(-healPower, false);

                    // 플레이어 발밑에 물결 일으키기
                    LeanPool.Spawn(pulsePrefab, PlayerManager.Instance.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);
                }
            }
        }
    }
}
