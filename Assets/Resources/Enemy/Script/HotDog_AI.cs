using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class HotDog_AI : MonoBehaviour
{
    [Header("Refer")]
    public EnemyManager enemyManager;
    EnemyInfo enemy;
    public ParticleSystem breathEffect; //숨쉴때 입에서 나오는 불꽃
    public ParticleSystem smokeEffect; // 안개 생성시 입에서 나오는 연기

    [Header("Meteor")]
    public float meteorCoolTime;
    public float meteorRange;
    public int meteorNum;
    MagicInfo meteorMagic;
    GameObject meteorPrefab;

    private void Awake()
    {
        enemyManager = enemyManager == null ? GetComponentInChildren<EnemyManager>() : enemyManager;
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        enemyManager.rigid.velocity = Vector2.zero; //속도 초기화

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        // 메테오 마법 데이터 찾기
        if (meteorMagic == null)
        {
            //찾은 마법 데이터로 MagicInfo 새 인스턴스 생성
            meteorMagic = new MagicInfo(MagicDB.Instance.GetMagicByName("Meteor"));

            // 강력한 데미지로 고정
            meteorMagic.power = 20f;

            // 메테오 떨어지는 속도 초기화
            meteorMagic.speed = 1f;

            // 메테오 프리팹 찾기
            meteorPrefab = MagicDB.Instance.GetMagicPrefab(meteorMagic.id);
        }
    }

    // meteor 애니메이션 끝날때쯤 meteor 소환 함수
    public void Meteor()
    {
        StartCoroutine(SummonMeteor());
    }

    IEnumerator SummonMeteor()
    {
        //메테오 개수만큼 반복
        for (int i = 0; i < meteorNum; i++)
        {
            // 메테오 떨어질 위치
            Vector2 targetPos = (Vector2)PlayerManager.Instance.transform.position + Random.insideUnitCircle * 20f;

            // 메테오 생성
            GameObject magicObj = LeanPool.Spawn(meteorPrefab, targetPos, Quaternion.identity, SystemManager.Instance.magicPool);

            // 메테오 스프라이트 빨갛게
            // magicObj.GetComponent<SpriteRenderer>().color = Color.red;

            MagicHolder magicHolder = magicObj.GetComponent<MagicHolder>();
            // magic 데이터 넣기
            magicHolder.magic = meteorMagic;

            // 타겟을 플레이어로 전환
            magicHolder.SetTarget(MagicHolder.Target.Player);

            // 메테오 목표지점 targetPos 넣기
            magicHolder.targetPos = targetPos;

            yield return new WaitForSeconds(0.1f);
        }
    }

    //TODO idle 상태일때 서서 숨쉬는 애니메이션 진행, 입에서 불꽃 및 연기 파티클 on/off
    //TODO meteor 패턴 코루틴
    //TODO stealthAtk 패턴 코루틴
    //TODO 플레이어 근접하면 해당 방향으로 bite 패턴 코루틴
}
