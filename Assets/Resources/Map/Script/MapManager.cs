using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Lean.Pool;
using System.Linq;

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

    [Header("State")]
    public MapElement nowMapElement = MapElement.Earth; // 현재 맵 원소 속성
    public enum MapElement { Earth, Fire, Life, Lightning, Water, Wind };
    public float portalRange = 100f; //포탈게이트 생성될 범위
    [SerializeField] float playerDistance = 5f; // 플레이어 이내 설치 금지 거리
    [SerializeField] int maxPropAttempt = 10; // 사물 생성 시도 최대 횟수
    [SerializeField] Vector3Int tilemapSize; // 설치할 사이즈
    [SerializeField] float boundRate = 0.5f; // 맵 생성 경계 비율
    float rightX, leftX, upY, downY; // 4방향 경계 좌표
    [SerializeField] List<Vector2> genPosList = new List<Vector2>();

    [Header("Tile")]
    [SerializeField] List<TileLayer> tileBundle_Bottom = new List<TileLayer>(); // 속성별 타일 번들 리스트, 가장 아래층
    [SerializeField] List<TileLayer> tileBundle_Middle = new List<TileLayer>(); // 속성별 타일 번들 리스트, 중간층
    [SerializeField] List<TileLayer> tileBundle_Deco = new List<TileLayer>(); // 속성별 타일 번들 리스트, 장식 타일
    [SerializeField] List<TileMapGenerator> tileGenList = new List<TileMapGenerator>(); // 타일맵 생성기 리스트

    [Header("Refer")]
    [SerializeField] List<PropBundle> propBundleList = new List<PropBundle>(); // 속성별 장애물 번들 리스트
    public GameObject portalGate; //다음 맵 넘어가는 포탈게이트 프리팹
    public Transform[] background = null;
    [SerializeField] List<Vector3> initPos = new List<Vector3>(); // 초기화 된 맵위치 리스트

    private void OnEnable()
    {
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
                tileGenList[0].GenTile(new Vector3Int(tilemapSize.x / 2, tilemapSize.y / 2, 0), nowMapPos, tileBundle_Bottom[(int)nowMapElement].tileBundle);
                List<Vector2> setList = tileGenList[1].GenTile(new Vector3Int(tilemapSize.x / 2, tilemapSize.y / 2, 0), nowMapPos, tileBundle_Middle[(int)nowMapElement].tileBundle);
                tileGenList[2].GenTile(new Vector3Int(tilemapSize.x / 2, tilemapSize.y / 2, 0), nowMapPos, tileBundle_Deco[(int)nowMapElement].tileBundle);

                // 장애물 설치하기

                StartCoroutine(SpawnProp(new Vector2(genPos.x, genPos.y), setList));
            }
        }
    }

    IEnumerator SpawnProp(Vector2 groundPos, List<Vector2> setList)
    {
        // 맵 속성에 따라 다른 장애물 번들 선택
        List<Prop> propBundle = propBundleList[(int)nowMapElement].props;

        // 타일 가로x세로 사이즈만큼 빈 타일의 좌표 리스트 만들기
        List<Vector2> emptyTileList = new List<Vector2>();
        for (int x = 0; x < tilemapSize.x; x++)
            for (int y = 0; y < tilemapSize.y; y++)
            {
                //todo middle 타일과 겹치지 않게 검사

                // 설치된 타일 리스트에 없으면
                if (!setList.Exists(a => a == new Vector2(x, y)))
                    // 빈타일 좌표 넣기
                    emptyTileList.Add(new Vector2(x, y));
                // else
                //     print(new Vector2(x, y));
            }

        // 모든 사물 반복
        for (int i = 0; i < propBundle.Count; i++)
        {
            // 해당 사물 개수만큼 반복
            for (int j = 0; j < propBundle[i].amount; j++)
            {
                // 생성 확률 통과 못하면 넘기기
                if (Random.value > propBundle[i].spawnRate)
                    continue;

                yield return null;

                // 사물 생성 시도 카운트
                int spawnAttemptCount = maxPropAttempt;
                // 현재 생성하려는 사물 정보
                Prop nowProp = propBundle[i];

                // 빈 타일 확인용으로 현재의 타일 좌표 리스트 복사
                List<Vector2> tileCheckList = emptyTileList.ToList();

                // 설치하려는 장애물의 사이즈 계산
                Vector2 propSize = nowProp.propPrefab.GetComponent<SpriteRenderer>().sprite.bounds.size;
                propSize.x = Mathf.CeilToInt(propSize.x);
                propSize.y = Mathf.CeilToInt(propSize.y);
                print(propSize);

                for (int k = 0; k < tileCheckList.Count; k++)
                {
                    // 비어있는 랜덤 좌표 뽑기
                    Vector2 randomPos = tileCheckList[Random.Range(0, tileCheckList.Count)];

                    // print(tileCheckList.Count + " : " + randomPos);

                    // 모든 타일 대조해보고 하나라도 빈타일 리스트에 없으면 넘기기
                    if (!TileCheck(tileCheckList, propSize, randomPos))
                        continue;

                    // 확인 리스트에서 해당 좌표 빼기
                    tileCheckList.Remove(randomPos);

                    // 해당하는 모든 타일 리스트에서 빼기
                    for (int x = 0; x < propSize.x; x++)
                        for (int y = 0; y < propSize.y; y++)
                            emptyTileList.Remove(randomPos + new Vector2(x, y));

                    // 설치 좌표 계산
                    Vector2 spawnPos = randomPos + new Vector2(groundPos.x - tilemapSize.x / 2f, groundPos.y - tilemapSize.y / 2f);

                    // 해당 타일에 장애물 설치하고 끝내기
                    GameObject propObj = LeanPool.Spawn(nowProp.propPrefab, spawnPos, Quaternion.identity, ObjectPool.Instance.objectPool);

                    // 해당 사물 확률에 따라 뒤집기
                    TransformControl.Shuffle shuffle = propObj.GetComponent<TransformControl>().ShuffleTransform();

                    // 뒤집었으면
                    if (shuffle == TransformControl.Shuffle.MirrorX)
                        // 오른쪽으로 X 좌표 절반만큼 이동
                        propObj.transform.position += Vector3.right * propSize.x / 2f;

                    break;
                }
            }
        }
    }

    bool TileCheck(List<Vector2> tileCheckList, Vector2 propSize, Vector2 randomPos)
    {
        // 해당 타일 차지공간만큼 반복
        for (int x = 0; x < propSize.x; x++)
            for (int y = 0; y < propSize.y; y++)
                // 랜덤 좌표에 사이즈 좌표 반영한 타일 중에서 하나라도 빈타일 리스트에 없으면
                if (!tileCheckList.Exists(a => a == randomPos + new Vector2(x, y)))
                {
                    // 해당 좌표는 이미 자리 있음
                    return false;
                }

        return true;
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

[System.Serializable]
public class PropBundle
{
    public string name;
    public List<Prop> props = new List<Prop>();
}

[System.Serializable]
public class Prop
{
    public string name;
    public GameObject propPrefab; // 사물 프리팹
    public GameObject propObj; // 생성된 사물 인스턴스

    public int amount = 1; // 소환 개수
    [Range(0f, 1f)]
    public float spawnRate = 1f; // 소환 될 확률
}

[System.Serializable]
public class TileLayer
{
    public string name;
    public List<TileBundle> tileBundle;
}

[System.Serializable]
public class TileBundle
{
    public string name;
    public RuleTile tile;
    public float rate; // 해당 타일 출현 확률
}