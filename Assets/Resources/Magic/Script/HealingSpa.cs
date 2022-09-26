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
    [SerializeField] Transform effectParent; // 펄스 이펙트 부모 오브젝트
    [SerializeField] SpriteRenderer pondSprite; // 연못 스프라이트
    public GameObject pulsePrefab; // 캐릭터 밑에 펄스 이펙트
    public GameObject dustEffect; // 디스폰 이펙트
    public List<GameObject> pulseCharacterList = new List<GameObject>(); // 연못 내부에 들어온 오브젝트

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
        if (other.TryGetComponent(out Character enemyManager))
        {
            // 해당 몬스터 히트 쿨타임 끝났을때
            if (enemyManager.hitDelayCount <= 0)
            {
                // 죽은 적이면 취소
                if (enemyManager.isDead)
                    return;

                // 닿은 적에게 데미지 주기
                StartCoroutine(enemyManager.hitBoxList[0].Hit(magicHolder));

                // 스케일 늘리기
                transform.localScale += new Vector3(0.01f, 0.01f, 0);
            }
        }

        // 플레이어가 닿으면 연못 크기 줄이기, 힐 수치 초기화 됬을때
        if (other.CompareTag(SystemManager.TagNameList.Player.ToString()) && healPower != -1)
        {
            // 쿨타임 끝났을때
            if (Time.time - healCoolCount >= 0.2f)
            {
                //체력 회복
                PlayerManager.Instance.hitBox.Damage(-healPower, false);

                // 연못 크기 줄이기
                if (transform.localScale.x >= 0)
                    transform.localScale -= new Vector3(0.01f, 0.01f, 0);

                // 플레이어 힐 쿨타임 초기화
                healCoolCount = Time.time;
            }
        }

        // 충돌한 캐릭터 발밑에 물결 일으키기
        WaterPulse(other.gameObject);
    }

    void WaterPulse(GameObject pulseCharacter)
    {
        // 이미 물결 일으키는 중인 캐릭터면 리턴
        if (pulseCharacterList.Exists(x => x == pulseCharacter))
            return;

        // 캐릭터 발밑에 물결 이펙트 소환
        GameObject pulse = LeanPool.Spawn(pulsePrefab, pulseCharacter.transform.position, Quaternion.identity, effectParent);

        // 리스트에 물결 일으킨 캐릭터 추가
        pulseCharacterList.Add(pulseCharacter);

        //점점 투명하게
        SpriteRenderer sprite = pulse.GetComponent<SpriteRenderer>();
        sprite.color = Color.white;
        sprite.DOColor(new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0), 1f)
        .SetEase(Ease.InCubic);

        //사이즈 제로부터 점점 키우기
        pulse.transform.localScale = Vector2.zero;
        pulse.transform.DOScale(Vector2.one, 1f)
        .OnComplete(() =>
        {
            // 리스트에서 물결 일으킨 캐릭터 제거
            pulseCharacterList.Remove(pulseCharacter);

            // 믈결 디스폰
            LeanPool.Despawn(pulse);
        });
    }
}
