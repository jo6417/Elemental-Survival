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
    float pulseCount;
    float reduceCoolCount;
    bool healTrigger = false;
    Vector2 originScale;
    MagicHolder magicHolder;
    MagicInfo magic;
    Collider2D coll;
    public List<GameObject> effectObjs = new List<GameObject>(); //거품, 물결 효과

    [Header("Refer")]
    public GameObject bubblePrefab;
    public GameObject pulsePrefab;
    public ParticleSystem smokeParticle;

    [Header("Magic Stat")]
    float range;
    float duration;
    int healPower;
    float coolTime;
    float speed;

    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
        coll = GetComponent<Collider2D>();
        originScale = transform.localScale;
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Initial());

        //딜레이마다 거품 생성
        StartCoroutine(BubbleCycle());

        //플레이어 들어오면 체력 회복
        StartCoroutine(HealPlayer());
    }

    private void Update()
    {
        // 오브젝트 크기에 맞춰 파티클 범위 갱신
        ParticleSystem.ShapeModule shape = smokeParticle.shape;
        shape.radius = 10 * transform.localScale.x / originScale.x;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 적이 닿으면 연못 크기 늘리기, 쿨타임 지났을때, 살아있는 적일때
        if (other.CompareTag("Enemy")
        && Time.time - reduceCoolCount >= 0.2f
        && !other.GetComponentInChildren<EnemyManager>().isDead)
        {
            // print(transform.localScale);
            transform.localScale += new Vector3(0.01f, 0.01f, 0);

            reduceCoolCount = Time.time;
        }

        if (Time.time - pulseCount >= 0.5f)
        {
            Vector2 pulsePos = (Vector2)other.transform.position + Vector2.down * other.GetComponentInChildren<SpriteRenderer>().bounds.size.y / 2f;

            //물결 소환 몇초후 삭제
            GameObject pulse = LeanPool.Spawn(pulsePrefab, pulsePos, Quaternion.identity);

            //리스트에 물결 추가
            effectObjs.Add(pulse);

            //점점 투명하게
            SpriteRenderer sprite = pulse.GetComponent<SpriteRenderer>();
            sprite.color = Color.white;
            sprite.DOColor(new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0), 1f)
            .SetEase(Ease.InCubic);

            //사이즈 제로부터 점점 키우기
            pulse.transform.localScale = Vector2.zero;
            pulse.transform.DOScale(Vector2.one * 2f, 1f)
            .OnComplete(() =>
            {
                //디스폰
                LeanPool.Despawn(pulse);

                //리스트에서 물결 오브젝트 제거
                effectObjs.Remove(pulse);
            });

            pulseCount = Time.time;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 들어오면 HealOn
        if (other.CompareTag("Player"))
            healTrigger = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 플레이어 나가면 HealOff
        if (other.CompareTag("Player"))
            healTrigger = false;

        //이펙트가 콜라이더 벗어나면 디스폰 시키기
        if(effectObjs.Find(x => x == other.gameObject))
        {
            print("이펙트 삭제");
            LeanPool.Despawn(other.gameObject);
            effectObjs.Remove(other.gameObject);
        }
    }

    IEnumerator Initial()
    {
        //마법 정보 불러올때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;

        range = MagicDB.Instance.MagicRange(magic);
        duration = MagicDB.Instance.MagicDuration(magic);
        healPower = Mathf.RoundToInt(MagicDB.Instance.MagicPower(magic)); //회복할 양, int로 반올림해서 사용
        coolTime = MagicDB.Instance.MagicCoolTime(magic);
        speed = MagicDB.Instance.MagicSpeed(magic, false);

        //딜레이 초기화
        reduceDelay = duration;

        //제로 사이즈에서 크기 키우기
        transform.localScale = Vector2.zero;
        transform.DOScale(Vector2.one * range, 0.5f)
        .SetEase(Ease.OutBack);

        //시간마다 사이즈 줄이기
        while (gameObject.activeSelf)
        {
            //딜레이만큼 대기
            yield return new WaitForSeconds(reduceDelay);

            // 연못 크기 줄이기, 점점 빠르게
            if (transform.localScale.x > 0)
                transform.localScale -= new Vector3(0.01f, 0.01f, 0);

            //딜레이는 점점 줄어듬
            reduceDelay -= 0.005f;

            //최소 딜레이 제한
            if (reduceDelay <= 0.01f)
                reduceDelay = 0.01f;

            //제로 사이즈까지 완전히 줄어들면 디스폰
            if (transform.localScale.x <= 0)
            {
                //체력 회복 끄기
                healTrigger = false;

                //모든 이펙트 제거
                foreach (var effect in effectObjs)
                {
                    LeanPool.Despawn(effect);
                }
                effectObjs.RemoveAll(x => x);

                LeanPool.Despawn(gameObject, 5f);
            }
        }

        // 최종 제한시간 지난 후 줄어들어 사라짐
        transform.DOScale(Vector2.zero, 1f)
        .SetEase(Ease.InBack)
        .SetDelay(MaxTime)
        .OnComplete(() => {
            LeanPool.Despawn(gameObject, 5f);
        });
    }

    IEnumerator HealPlayer()
    {
        //마법 정보 불러올때까지 대기
        yield return new WaitUntil(() => magic != null);

        while (healTrigger)
        {
            // print("heal : " + healPower);

            //체력 회복
            PlayerManager.Instance.Damage(-healPower);

            // 연못 크기 줄이기
            if (transform.localScale.x >= 0)
                transform.localScale -= new Vector3(0.01f, 0.01f, 0);

            //speed 마다 회복
            yield return new WaitForSeconds(speed);
        }
    }

    IEnumerator BubbleCycle()
    {
        //마법 정보 불러올때까지 대기
        yield return new WaitUntil(() => magic != null);

        range = MagicDB.Instance.MagicRange(magic);

        while (gameObject.activeSelf)
        {
            //쿨타임마다 반복
            yield return new WaitForSeconds(bubbleDelay + 1f);

            //거품 생성 위치
            Vector2 bubblePos = (Vector2)transform.position + Random.insideUnitCircle * transform.localScale.x;
            //거품 이동할 방향
            Vector2 bubbleDir = bubblePos + Random.insideUnitCircle.normalized;

            //거품 소환 몇초후 삭제
            GameObject bubble = LeanPool.Spawn(bubblePrefab, bubblePos, Quaternion.identity);
            //리스트에 거품 추가
            effectObjs.Add(bubble);

            bubble.transform.DOMove(bubbleDir, bubbleDelay)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                LeanPool.Despawn(bubble);

                //리스트에 거품 제거
                effectObjs.Remove(bubble);
            });
        }
    }
}
