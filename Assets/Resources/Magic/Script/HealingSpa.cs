using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

public class HealingSpa : MonoBehaviour
{
    public float MaxTime; // 최종 제한시간
    public float bubbleDelay;
    public float reduceDelay = 3f; //딜레이마다 연못 크기가 줄어듬
    float reduceCoolCount; // 연못 줄이기 쿨타임
    float healCoolCount; // 플레이어 힐 쿨타임
    [SerializeField] MagicHolder magicHolder;
    MagicInfo magic;

    [Header("Refer")]
    [SerializeField] Collider2D coll;
    [SerializeField] SpriteRenderer pondSprite; // 연못 스프라이트
    public GameObject pulsePrefab; // 캐릭터 밑에 펄스 이펙트
    public GameObject dustEffect; // 디스폰 이펙트
    public List<GameObject> nowPulseCharacter = new List<GameObject>(); // 연못 내부에 들어온 오브젝트

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

        // 딜레이 초기화
        reduceDelay = duration;

        // 타겟 위치로 이동
        transform.position = magicHolder.targetPos;

        // 제로 사이즈로 초기화
        transform.localScale = Vector2.zero;

        // 연못 이미지 나타내기
        pondSprite.enabled = true;

        //제로 사이즈에서 크기 키우기
        transform.DOScale(Vector2.one * range, 0.5f)
        .SetEase(Ease.OutBack);

        // 플레이어 및 몬스터 피직스와 충돌하게 충돌 레이어 초기화
        gameObject.layer = SystemManager.Instance.layerList.Object_Layer;

        // 콜라이더 켜기
        coll.enabled = true;

        nowPulseCharacter.Clear();
    }

    private void Update()
    {
        // 쿨타임 중일때
        if (Time.time - reduceCoolCount >= reduceDelay)
        {
            // 연못 스케일 줄이기
            ReduceScale();

            // 사이즈 감소 쿨타임 초기화
            reduceCoolCount = Time.time;
        }
    }

    void ReduceScale()
    {
        // 연못 크기 줄이기
        if (transform.localScale.x > 0)
            transform.localScale -= new Vector3(0.01f, 0.01f, 0);

        // 줄이기 딜레이는 점점 줄어듬
        reduceDelay -= 0.01f;

        // 최소 딜레이 제한
        if (reduceDelay <= 0.01f)
            reduceDelay = 0.01f;

        //제로 사이즈까지 완전히 줄어들면 디스폰
        if (transform.localScale.x <= 0)
        {
            // 먼지 퍼지는 이펙트 소환
            LeanPool.Spawn(dustEffect, transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

            // 연못 디스폰
            LeanPool.Despawn(gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 적이 닿으면
        if (other.TryGetComponent(out Character character))
        {
            // 해당 몬스터 히트 쿨타임 끝났을때
            if (character.hitDelayCount <= 0)
            {
                // 죽은 적이면 취소
                if (character.isDead)
                    return;

                // 닿은 적에게 데미지 주기
                StartCoroutine(character.hitBoxList[0].Hit(magicHolder));

                // 스케일 늘리기
                transform.localScale += new Vector3(0.01f, 0.01f, 0);
            }

            // 충돌한 캐릭터 발밑에 물결 일으키기
            StartCoroutine(WaterPulse(other.gameObject));
        }

        // 플레이어가 닿으면 연못 크기 줄이기, 힐 수치 초기화 됬을때
        if (other.TryGetComponent(out PlayerManager playerManager))
        {
            // 쿨타임 끝났을때
            if (Time.time - healCoolCount >= 0.2f)
            {
                // 플레이어 체력 회복
                PlayerManager.Instance.hitBox.Damage(-healPower, false);

                // 연못 크기 줄이기
                if (transform.localScale.x >= 0)
                    transform.localScale -= new Vector3(0.01f, 0.01f, 0);

                // 플레이어 힐 쿨타임 초기화
                healCoolCount = Time.time;
            }

            // 충돌한 캐릭터 발밑에 물결 일으키기
            StartCoroutine(WaterPulse(other.gameObject));
        }
    }

    IEnumerator WaterPulse(GameObject pulseCharacter)
    {
        // 이미 물결 일으키는 중인 캐릭터면 리턴
        if (nowPulseCharacter.Exists(x => x == pulseCharacter))
            yield break;

        // 캐릭터 발밑에 물결 이펙트 소환
        GameObject pulse = LeanPool.Spawn(pulsePrefab, pulseCharacter.transform.position, Quaternion.identity, SystemManager.Instance.effectPool);

        // 리스트에 물결 일으킨 캐릭터 추가
        nowPulseCharacter.Add(pulseCharacter);

        // 일정 시간 대기
        yield return new WaitForSeconds(0.5f);

        // 리스트에서 물결 일으킨 캐릭터 제거
        nowPulseCharacter.Remove(pulseCharacter);
    }
}
