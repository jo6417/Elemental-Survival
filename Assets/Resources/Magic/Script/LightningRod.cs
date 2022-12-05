using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;
using DigitalRuby.LightningBolt;

public class LightningRod : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] EdgeCollider2D coll;
    [SerializeField] Transform lightningRodObj; // 피뢰침 오브젝트
    [SerializeField] ParticleManager electroBallPrefab; // 전기 구체 오브젝트
    [SerializeField] LineRenderer electroLinePrefab; // 각 포인트 사이 전기 라인 오브젝트
    [SerializeField] ParticleManager subLightPrefab; // 전기 라인 주변 전기 이펙트
    [SerializeField] ParticleManager spikeSpark;
    [SerializeField] MagicHolder magicHolder;
    List<ParticleManager> ballList = new List<ParticleManager>(); // 적의 위치에 생성될 전기 구체 리스트
    List<LineRenderer> lineList = new List<LineRenderer>(); // 전기 구체 사이마다 들어갈 전기 라인
    List<ParticleManager> lineSubEffectList = new List<ParticleManager>(); // 전기 라인 주변의 파티클

    [Header("Spec")]
    float range;
    float duration;
    int pierceNum;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 피뢰침 오브젝트 활성화
        lightningRodObj.gameObject.SetActive(false);

        //magic 불러올때까지 대기
        yield return new WaitUntil(() => magicHolder.magic != null);
        range = MagicDB.Instance.MagicRange(magicHolder.magic);
        duration = MagicDB.Instance.MagicDuration(magicHolder.magic);
        pierceNum = MagicDB.Instance.MagicPierce(magicHolder.magic);

        // 타겟 위치 위쪽으로 이동
        transform.position = magicHolder.targetPos + Vector3.up * 3f;

        // 구체 및 라인 리스트 초기화
        ballList.Clear();
        lineList.Clear();
        lineSubEffectList.Clear();

        // 콜라이더 끄기
        coll.enabled = false;

        // 해당 위치에 못박기
        StartCoroutine(SpikeRod());
    }

    IEnumerator SpikeRod()
    {
        // 피뢰침 오브젝트 활성화
        lightningRodObj.gameObject.SetActive(true);

        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.2f);

        // 못 회전하기
        lightningRodObj.rotation = Quaternion.Euler(Vector3.zero);
        lightningRodObj.DORotate(Vector3.up * 360f, 0.5f, RotateMode.LocalAxisAdd);

        yield return new WaitForSeconds(0.2f);

        // 못 domove inback으로 박기
        transform.DOMove(magicHolder.targetPos, 0.3f)
        .SetEase(Ease.InBack)
        .OnComplete(() =>
        {
            // 박힐때 흙 튀기기
            // 바닥에 꽂힐때 전기 파티클 재생
            spikeSpark.particle.Play();
        });

        yield return new WaitForSeconds(0.5f);

        // 플레이어 현재 위치
        Vector2 playerPos = PlayerManager.Instance.transform.position;

        // 범위 내 적 모두 찾아서 리스트업
        List<Vector2> atkPosList = MarkEnemyPos(magicHolder.magic);

        if (atkPosList.Count > 0)
            // 플레이어와 가까운 순으로 정렬
            atkPosList = atkPosList.OrderBy(x => Vector2.Distance(x, playerPos)).ToList();

        // 리스트 개수만큼 반복
        for (int i = 0; i < atkPosList.Count; i++)
        {
            // 리스트의 모든 적 위치마다 전기 구체 소환
            ParticleManager electroBall = LeanPool.Spawn(electroBallPrefab, atkPosList[i], Quaternion.identity, SystemManager.Instance.magicPool);
            ballList.Add(electroBall);

            // 마지막 인덱스 아닐때
            if (i < atkPosList.Count - 1)
            {
                // 각 포인트 사이마다 전기 라인 프리팹 소환하고 전기라인 리스트에 추가
                LineRenderer electroLine = LeanPool.Spawn(electroLinePrefab, atkPosList[i], Quaternion.identity, SystemManager.Instance.magicPool);
                lineList.Add(electroLine);

                // 전기 라인 컴포넌트 찾기
                LightningBoltScript lightningLine = electroLine.GetComponent<LightningBoltScript>();
                // 전기 라인 렌더러의 시작,끝 지점 옮기기
                lightningLine.StartObject.transform.position = atkPosList[i];
                lightningLine.EndObject.transform.position = atkPosList[i + 1];

                // 서브 라이팅 위치 - 라인의 시작,끝 지점 중간 지점
                Vector2 subPos = (lightningLine.StartObject.transform.position + lightningLine.EndObject.transform.position) / 2f;
                // 서브 라이팅의 각도 - 라인의 시작 부분을 바라보게
                Vector2 subRotation = (Vector2)lightningLine.StartObject.transform.position - subPos;
                float angle = Mathf.Atan2(subRotation.y, subRotation.x) * Mathf.Rad2Deg;
                // 서브 라이팅의 길이 - 시작,끝 부분 사이 길이
                float subDistance = Vector2.Distance(lightningLine.StartObject.transform.position, lightningLine.EndObject.transform.position);

                // 전기 라인 주변부 서브 전기 파티클 추가하기
                ParticleManager subLight = LeanPool.Spawn(subLightPrefab, subPos, Quaternion.identity, SystemManager.Instance.magicPool);
                lineSubEffectList.Add(subLight);

                // 시작,끝 부분 사이 거리만큼 Y 스케일 늘리기
                subLight.transform.localScale = new Vector3(subDistance, 1, 1);

                // 길이에 비례해서 파티클 개수 갱신
                ParticleSystem.EmissionModule subEmission = subLight.particle.emission;
                subEmission.rateOverTime = subDistance * 40f;

                // 각도 수정
                subLight.transform.rotation = Quaternion.Euler(Vector3.forward * angle);
            }
        }

        // 현재 피뢰침 위치값을 빼서 월드 좌표를 상대 좌표로 전환
        for (int i = 0; i < atkPosList.Count; i++)
        {
            atkPosList[i] -= (Vector2)transform.position;
        }

        // 엣지 콜라이더 포인트 초기화
        coll.SetPoints(atkPosList);
        coll.enabled = true;

        // duration 동안 콜라이더 껐다 켰다 반복
        StartCoroutine(FlickerColl());

        // duration 만큼 대기
        yield return new WaitForSeconds(duration);

        // 피뢰침 끄기
        lightningRodObj.gameObject.SetActive(false);

        // 콜라이더 끄기
        coll.enabled = false;

        // 모든 전기 구체 디스폰
        for (int i = 0; i < ballList.Count; i++)
        {
            ballList[i].SmoothDespawn();
        }
        // 모든 전기 라인 서브 이펙트 디스폰
        for (int i = 0; i < lineSubEffectList.Count; i++)
        {
            lineSubEffectList[i].SmoothDespawn();
        }
        // 모든 전기 라인 디스폰
        for (int i = 0; i < lineList.Count; i++)
        {
            // 라인 렌더러 먼저 끄기
            lineList[i].positionCount = 0;

            // 전기 라인 디스폰
            LeanPool.Despawn(lineList[i]);
        }

        // 셀프 디스폰
        LeanPool.Despawn(transform);
    }

    IEnumerator FlickerColl()
    {
        // 깜빡일 시간 받기
        float flickCount = duration;
        while (flickCount > 0)
        {
            // 콜라이더 토글
            coll.enabled = !coll.enabled;

            // 잠깐 대기
            flickCount -= Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    public List<Vector2> MarkEnemyPos(MagicInfo magic)
    {
        // 공격 위치 리스트
        List<Vector2> atkPosList = new List<Vector2>();

        //리턴할 적 오브젝트 리스트
        List<Character> enemyObjs = new List<Character>();

        //범위 안의 모든 적 콜라이더 리스트에 담기
        List<Collider2D> enemyCollList = new List<Collider2D>();
        enemyCollList.Clear();
        enemyCollList = Physics2D.OverlapCircleAll(transform.position, range, 1 << SystemManager.Instance.layerList.EnemyHit_Layer).ToList();

        // 찾은 적과 관통 개수 중 많은 쪽만큼 반복
        int findNum = Mathf.Max(enemyCollList.Count, pierceNum);
        for (int i = 0; i < findNum; i++)
        {
            // 관통 개수만큼 채워지면 반복문 끝내기
            if (enemyObjs.Count >= pierceNum)
                break;

            Character character = null;
            Collider2D targetColl = null;

            if (enemyCollList.Count > 0)
            {
                // 리스트 내에서 랜덤으로 선택
                targetColl = enemyCollList[Random.Range(0, enemyCollList.Count)];
                // 적 히트박스 찾기
                HitBox targetHitBox = targetColl.GetComponent<HitBox>();
                if (targetHitBox != null)
                    // 적 매니저 찾기
                    character = targetHitBox.character;

                // 이미 들어있는 오브젝트일때
                if (enemyObjs.Exists(x => x == character)
                // 해당 몬스터가 유령일때
                || (character && character.IsGhost))
                {
                    // 넣을 몬스터 정보 다시 초기화
                    character = null;
                }
            }

            // 적 오브젝트 변수에 담기
            enemyObjs.Add(character);

            // 임시 리스트에서 지우기
            if (targetColl != null)
                enemyCollList.Remove(targetColl);
        }

        for (int i = 0; i < enemyObjs.Count; i++)
        {
            Vector2 targetPos = default;

            // 몬스터가 있으면 위치 넣기
            if (enemyObjs[i] != null)
                targetPos = enemyObjs[i].transform.position;
            else
                // 오브젝트 없으면 범위내 랜덤 위치 넣기
                targetPos = (Vector2)transform.position + Random.insideUnitCircle * MagicDB.Instance.MagicRange(magicHolder.magic);

            atkPosList.Add(targetPos);
        }

        //적의 위치 리스트 리턴
        return atkPosList;
    }
}
