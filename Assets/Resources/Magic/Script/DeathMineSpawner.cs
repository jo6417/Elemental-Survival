using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class DeathMineSpawner : MonoBehaviour
{
    MagicHolder magicHolder;
    public GameObject minePrefab; //지뢰 프리팹
    [SerializeField] ParticleSystem showRange; // 폭발 범위 오브젝트
    float range;

    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);

        // 스탯 초기화
        range = MagicDB.Instance.MagicRange(magicHolder.magic);

        // 적이 죽을때 함수를 호출하도록 델리게이트에 넣기
        SystemManager.Instance.globalEnemyDeadCallback += DropMine;

        // 액티브 사용시 함수를 콜백에 넣기
        magicHolder.magicCastCallback += ExplodeMine;

        //플레이어 자식으로 들어가기
        transform.SetParent(PlayerManager.Instance.transform);
        transform.localPosition = Vector3.zero;

        // 폭발 트리거 범위 업데이트
        showRange.transform.localScale = new Vector2(range, range * 2f);
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    private void OnDisable()
    {
        // 해당 마법 장착 해제되면 델리게이트에서 함수 빼기
        SystemManager.Instance.globalEnemyDeadCallback -= DropMine;
    }

    // 지뢰 폭파
    void ExplodeMine()
    {
        // 폭발 트리거 범위 표시
        showRange.Play();

        // 폭발 코루틴 실행
        StartCoroutine(ExplodeCoroutine());
    }

    IEnumerator ExplodeCoroutine()
    {
        //범위 안의 모든 지뢰 콜라이더 리스트에 담기
        List<Collider2D> mineCollList = new List<Collider2D>();
        mineCollList.Clear();
        mineCollList = Physics2D.OverlapCircleAll(transform.position, range, 1 << SystemManager.Instance.layerList.EnemyPhysics_Layer).ToList();

        // 지뢰간의 딜레이
        WaitForSeconds wait = new WaitForSeconds(Time.deltaTime);

        // 수동 시전
        if (magicHolder.isManualCast)
        {
            // 범위내 모든 지뢰 찾기
            foreach (Collider2D mine in mineCollList)
            {
                if (mine.TryGetComponent(out DeathMine deathMine))
                {
                    // 지뢰와의 거리 산출
                    float distance = Vector2.Distance(transform.position, deathMine.transform.position);

                    // 범위보다 가까우면 폭파
                    if (distance <= range)
                        // 폭파 실행
                        deathMine.Explode();

                    yield return wait;
                }
            }
        }
        // 자동 시전
        else
        {
            // 필드의 모든 지뢰 터트리기
            foreach (Collider2D mine in mineCollList)
            {
                if (mine.TryGetComponent(out DeathMine deathMine))
                    // 폭파 실행
                    deathMine.Explode();

                yield return wait;
            }
        }
    }

    // 지뢰 드랍하기
    public void DropMine(Character enemyManager)
    {
        // print(MagicDB.Instance.MagicCritical(magic));

        // 크리티컬 확률 = 드랍 확률
        bool isDrop = MagicDB.Instance.MagicCritical(magicHolder.magic);

        //크리티컬 데미지 = 회복량
        int healAmount = Mathf.RoundToInt(MagicDB.Instance.MagicCriticalPower(magicHolder.magic));
        healAmount = (int)Mathf.Clamp(healAmount, 1f, healAmount); //최소 회복량 1f 보장

        // 마법 크리티컬 확률에 따라 지뢰 생성
        if (isDrop)
        {
            // 지뢰 오브젝트 생성
            GameObject deathMine = LeanPool.Spawn(minePrefab, enemyManager.transform.position + Vector3.up * 2f, Quaternion.identity, SystemManager.Instance.magicPool);

            // 매직홀더 찾기
            MagicHolder mineMagicHolder = deathMine.GetComponentInChildren<MagicHolder>();

            // 마법 타겟 넣기
            mineMagicHolder.SetTarget(MagicHolder.Target.Enemy);

            // 마법 타겟 위치 넣기
            mineMagicHolder.targetPos = enemyManager.transform.position;
        }
    }
}
