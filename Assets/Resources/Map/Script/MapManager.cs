using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Lean.Pool;

[System.Serializable]
public class Prop
{
    public string name;
    public GameObject propPrefab; // 사물 프리팹
    public GameObject propObj; // 생성된 사물 인스턴스

    public float radius = 1f; // 자리 차지 반경
    public int amount = 1; // 소환 개수
    public float spawnRate = 0.8f; // 소환 될 확률

    public Prop(Prop prop)
    {
        this.radius = prop.radius;
        this.amount = prop.amount;
        this.spawnRate = prop.spawnRate;
    }
}

public class MapManager : MonoBehaviour
{
    #region Singleton
    private static MapManager instance;
    public static MapManager Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<MapManager>();
                if (obj != null)
                {
                    instance = obj;
                }
                // else
                // {
                //     var newObj = new GameObject().AddComponent<MapManager>();
                //     instance = newObj;
                // }
            }
            return instance;
        }
    }
    #endregion

    [Header("Refer")]
    public GameObject portalGate; //다음 맵 넘어가는 포탈게이트 프리팹
    public Transform[] background = null;
    [SerializeField] List<Vector3> initPos = new List<Vector3>(); // 초기화 된 맵위치 리스트
    [SerializeField] List<Prop> propList = new List<Prop>(); // 사물 리스트
    [SerializeField] List<TileMapGenerator> tileGenList = new List<TileMapGenerator>(); // 타일맵 생성기 리스트

    [Header("State")]
    public float portalRange = 100f; //포탈게이트 생성될 범위
    [SerializeField] float playerDistance = 5f; // 플레이어 이내 설치 금지 거리
    [SerializeField] int maxPropAttempt = 10; // 사물 생성 시도 최대 횟수
    [SerializeField] Vector3Int tilemapSize; // 설치할 사이즈
    [SerializeField] float boundRate = 0.5f; // 맵 생성 경계 비율
    float rightX;
    float leftX;
    float upY;
    float downY;
    [SerializeField] List<Vector2> genPosList = new List<Vector2>();

    private void OnEnable()
    {
        // tilemapSize.x = background[0].GetComponent<SpriteRenderer>().sprite.bounds.size.x;
        // tilemapSize.y = background[0].GetComponent<SpriteRenderer>().sprite.bounds.size.y;
        // rightX = tilemapSize.x / 2;
        // leftX = -tilemapSize.x / 2;
        // upY = tilemapSize.y / 2;
        // downY = -tilemapSize.y / 2;
        // print(rightX + " : " + leftX + " : " + upY + " : " + downY);

        // 설치할 타일맵 사이즈 절반만큼 경계 설정
        rightX = tilemapSize.x / 2f;
        leftX = -tilemapSize.x / 2f;
        upY = tilemapSize.y / 2f;
        downY = -tilemapSize.y / 2f;

        // print(rightX + " : " + leftX + " : " + upY + " : " + downY);

        // 바닥 위치 초기화
        GenerateMap();

        //다음맵으로 넘어가는 포탈게이트 생성하기
        SpawnPortalGate();
    }

    void Update()
    {
        // 플레이어가 경계 끝에 왔을때 배경 9개 전부 위치 변경
        if (PlayerManager.Instance.transform.position.x >= rightX)
        {

            // 경계 갱신
            rightX += tilemapSize.x;
            leftX += tilemapSize.x;
            // 새 위치에 타일 설치
            GenerateMap(new Vector3(tilemapSize.x, 0));
        }

        if (PlayerManager.Instance.transform.position.x <= leftX)
        {

            // 경계 갱신
            rightX -= tilemapSize.x;
            leftX -= tilemapSize.x;
            // 새 위치에 타일 설치
            GenerateMap(new Vector3(-tilemapSize.x, 0));
        }

        if (PlayerManager.Instance.transform.position.y >= upY)
        {

            // 경계 갱신
            upY += tilemapSize.y;
            downY += tilemapSize.y;
            // 새 위치에 타일 설치
            GenerateMap(new Vector3(0, tilemapSize.y));
        }

        if (PlayerManager.Instance.transform.position.y <= downY)
        {

            // 경계 갱신
            upY -= tilemapSize.y;
            downY -= tilemapSize.y;
            // 새 위치에 타일 설치
            GenerateMap(new Vector3(0, -tilemapSize.y));
        }
    }

    void GenerateMap(Vector3 movePos = default)
    {
        // 플레이어 서있는 타일셋 중심 위치 계산
        Vector2 setPos = new Vector2((leftX + rightX) / 2f, (upY + downY) / 2f);

        // 초기 설치 위치
        Vector2 defaultPos = setPos - new Vector2(tilemapSize.x, tilemapSize.y);

        // 총 9번 설치
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                // 타일셋 사이즈만큼 중심위치 이동
                Vector2 genPos = defaultPos + new Vector2(tilemapSize.x * x, tilemapSize.y * y);

                // 이미 소환된 위치일때 넘기기
                if (genPosList.Exists(x => x == genPos))
                    continue;
                // 리스트에 없으면
                else
                    // 리스트에 저장
                    genPosList.Add(genPos);

                // LeanPool.Spawn(SystemManager.Instance.targetPos, genPos, Quaternion.identity);
                // print(genPos);

                // 타일맵 위치로 전환
                Vector3Int nowMapPos = new Vector3Int((int)(genPos.x / 2f) - 1, (int)(genPos.y / 2f) - 1, 0);

                // 리스트의 모든 타일맵 설치
                foreach (TileMapGenerator gen in tileGenList)
                {
                    gen.GenTile(new Vector3Int(tilemapSize.x / 2, tilemapSize.y / 2, 0), nowMapPos);
                }

                // 장애물 설치하기
                SpawnProp(new Vector2(genPos.x, genPos.y));
            }
        }
    }

    void MoveGround(Vector3 movePos = default)
    {
        for (int i = 0; i < background.Length; i++)
        {
            // 현재 바닥 위치 
            Vector3 groundPos = background[i].position;
            // pos.y -= tilemapSize.y;

            // 옮겨질 바닥 위치
            groundPos += movePos;

            // 이미 초기화 된 위치가 아니면
            if (!initPos.Exists(x => x == groundPos))
            {
                // 해당 위치 기록
                initPos.Add(groundPos);

                // 해당 바닥 내에 리스트의 모든 오브젝트 설치
                SpawnProp(groundPos);
            }

            // 바닥 옮기기
            background[i].position = groundPos;
        }
    }

    void SpawnProp(Vector2 groundPos)
    {
        // 해당 맵의 사물 리스트
        List<Prop> fieldPropList = new List<Prop>();

        // 모든 사물 반복
        for (int i = 0; i < propList.Count; i++)
        {
            // 해당 사물 개수만큼 반복
            for (int j = 0; j < propList[i].amount; j++)
            {
                // 사물 생성 시도 카운트
                int spawnAttemptCount = maxPropAttempt;
                // 현재 생성하려는 사물 정보
                Prop nowProp = propList[i];

                // 바닥 범위내 랜덤 위치 정하기
                Vector2 spawnPos = new Vector2(
                    Random.Range(groundPos.x - tilemapSize.x / 2f, groundPos.x + tilemapSize.x / 2f),
                    Random.Range(groundPos.y - tilemapSize.y / 2f, groundPos.y + tilemapSize.y / 2f));

                // 시도 횟수 남았으면 계속 반복
                while (spawnAttemptCount > 0)
                {
                    // 현재 맵 이내의 모든 사물들 범위 겹치는지 확인, 이동해야할 벡터 리턴
                    Vector2 moveDir = CheckOverlap(spawnPos, fieldPropList, nowProp);

                    // 주변에 사물 없을때
                    if (moveDir == new Vector2(tilemapSize.x, tilemapSize.y))
                    {
                        // 사물 스폰
                        GameObject propObj = LeanPool.Spawn(nowProp.propPrefab, spawnPos, Quaternion.identity, ObjectPool.Instance.objectPool);

                        // 장애물 속성 생성 및 초기화
                        Prop spawnProp = new Prop(nowProp);
                        spawnProp.propObj = propObj; // 현재 오브젝트 넣기

                        // 사물 리스트에 넣기
                        fieldPropList.Add(spawnProp);

                        // 확률따라 좌우반전
                        propObj.transform.rotation = Random.value > 0.5f ? Quaternion.Euler(Vector3.zero) : Quaternion.Euler(Vector3.up * 180f);

                        break;
                    }
                    // 주변에 사물 있을때
                    else
                    {
                        // 소환 위치에 이동벡터 반영
                        spawnPos += moveDir;
                    }

                    // 시도횟수 차감
                    spawnAttemptCount--;
                }
            }
        }
    }

    Vector2 CheckOverlap(Vector2 spawnPos, List<Prop> fieldPropList, Prop nowProp)
    {
        for (int k = 0; k < fieldPropList.Count; k++)
        {
            // 벌려야하는 최소거리 = 설치하려는 사물과 다른 사물 prop의 range 합산
            float needDis = fieldPropList[k].radius + nowProp.radius;

            // 현재 계산중인 위치에서 거리
            Vector2 playerDir = spawnPos - (Vector2)PlayerManager.Instance.transform.position;
            // 플레이어 가까우면 거리 이격 시키기
            if (playerDir.magnitude <= playerDistance)
            {
                // 이동해야할 벡터를 리턴
                return playerDir;
            }

            // 현재 계산중인 위치에서 거리
            float nowDis = Vector2.Distance(fieldPropList[k].propObj.transform.position, spawnPos);
            // 거리 이내에 있다면 설치된 사물과 거리 이격 시키기
            if (nowDis <= needDis)
            {
                // 이동해야할 벡터를 리턴
                Vector2 moveDir = ((Vector2)fieldPropList[k].propObj.transform.position - spawnPos).normalized * (needDis - nowDis);
                return moveDir;
            }
        }

        // 아무도 안겹쳤을때
        return new Vector2(tilemapSize.x, tilemapSize.y);
    }

    void SpawnPortalGate()
    {
        //포탈이 생성될 위치
        Vector2 pos = (Vector2)PlayerManager.Instance.transform.position + UnityEngine.Random.insideUnitCircle.normalized * portalRange;

        //포탈 게이트 생성
        GameObject gate = LeanPool.Spawn(portalGate, pos, Quaternion.identity, ObjectPool.Instance.objectPool);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        // 경계 모서리 표시
        Gizmos.DrawLine(new Vector2(leftX, downY), new Vector2(leftX, upY));
        Gizmos.DrawLine(new Vector2(rightX, downY), new Vector2(rightX, upY));
        Gizmos.DrawLine(new Vector2(leftX, downY), new Vector2(rightX, downY));
        Gizmos.DrawLine(new Vector2(leftX, upY), new Vector2(rightX, upY));
    }
}
