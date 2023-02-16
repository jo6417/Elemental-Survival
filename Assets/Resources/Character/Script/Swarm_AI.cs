using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class Swarm_AI : MonoBehaviour
{
    [SerializeField] Rigidbody2D rigid;
    [SerializeField] Character character;
    public GameObject enemyPrefab; // 소환할 몬스터 프리팹
    public int amount = 10; // 군집 소환 개수
    public float duration = 60f; // 군집 유지 시간
    public float moveSpeed = 10f; // 타겟 이동 속도
    Vector2 swarmPos; // 소환될 위치 기본값
    Vector2 moveDir; // 이동 방향
    [SerializeField] List<GameObject> mobList = new List<GameObject>(); // 소환된 몬스터 리스트

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 몬스터 정보 들어올때까지 대기
        yield return new WaitUntil(() => enemyPrefab != null);

        // 모서리 스폰 위치로 이동
        transform.position = WorldSpawner.Instance.BorderRandPos();

        // 입력된 몬스터를 카메라 밖 구석에서 숫자만큼 소환하기
        for (int i = 0; i < amount; i++)
        {
            // 스웜 타겟 근처에서 소환
            Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * 3f;

            // 몬스터 생성
            GameObject enemyObj = LeanPool.Spawn(enemyPrefab, spawnPos, Quaternion.identity, ObjectPool.Instance.enemyPool);

            // 몬스터 정보 찾기
            EnemyInfo enemy = EnemyDB.Instance.GetEnemyByName(enemyObj.name.Split('_')[0]);

            // 현재 몬스터 총 전투력에 포함
            WorldSpawner.Instance.NowEnemyPower += enemy.grade;

            // 리스트에 몬스터 넣기
            mobList.Add(enemyObj);

            // 단일 몬스터 캐릭터
            Character mobCharacter = enemyObj.GetComponent<Character>();

            // 몬스터 타겟 정하기
            mobCharacter.TargetObj = gameObject;

            // 몬스터 스피드를 타겟 스피드와 동기화
            // character.speedNow = moveSpeed;

            // 몬스터 죽을때 리스트에서 제거를 이벤트로 넣기
            if (mobCharacter.deadCallback == null)
                mobCharacter.deadCallback += RemoveList;

            // 초기화 시작
            mobCharacter.initialStart = true;
        }

        // 초기화 끝날때까지 대기
        yield return new WaitUntil(() => character.initialFinish);

        // 플레이어 방향 계산
        moveDir = PlayerManager.Instance.transform.position - transform.position;

        // 플레이어 방향으로 타겟 계속 움직이기
        rigid.velocity = moveDir.normalized * moveSpeed / 2f;
    }

    void RemoveList(Character character)
    {
        // 리스트에서 몬스터 제거
        mobList.Remove(character.gameObject);

        // 리스트 비었으면 군집 해제
        if (mobList.Count == 0)
        {
            // 군집 끝내기
            EndSwarm();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 스포너 밖에 나갔을때
        if (other.CompareTag(TagNameList.Respawn.ToString()))
        {
            // 플레이어 위치로 이동
            transform.position = PlayerManager.Instance.transform.position;

            // 기존 방향의 반대로 이동
            rigid.velocity = -rigid.velocity;
        }
    }

    void EndSwarm()
    {
        // 다른 몬스터 죽을때 이미 디스폰 됬으면 리턴
        if (!gameObject.activeInHierarchy)
            return;

        // 이동 멈추기
        rigid.velocity = Vector3.zero;

        // 해당 군집 오브젝트 디스폰
        LeanPool.Despawn(gameObject);
    }

    private void OnDrawGizmos()
    {
        // 스웜 현재 위치
        Gizmos.color = Color.blue;
        Gizmos.DrawCube(transform.position, Vector3.one);

        // 추적 위치부터 이동 위치까지 직선
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)moveDir.normalized * 3f);
    }
}
