using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using Lean.Pool;

public class TileMapGenerator : MonoBehaviour
{
    [Header("State")]
    [SerializeField] bool tileReset = false; // 타일 설치시 기존 타일 초기화 여부
    [SerializeField] RandomType randomType;
    enum RandomType { Random, Ground, Prop };
    [SerializeField] bool border = false; // 테두리 타일 설치 여부

    [Range(0, 100)]
    public int iniChance;
    [Range(1, 8)]
    public int birthLimit;
    [Range(1, 8)]
    public int deathLimit;

    [Range(1, 10)]
    public int genNumRange;
    private int count = 0;

    private int[,] tileSetPos;
    public Vector3Int tilemapSize; // 설치할 사이즈
    public Vector3Int tilemapPos; // 설치할 위치

    [Header("Refer")]
    public Tilemap tileMap;
    // public List<TileLayer> tileList = new List<TileLayer>();
    [SerializeField] GameObject grid;

    public List<Vector2> GenTile(Vector3Int genSize, Vector3Int genPos, List<TileBundle> tileList, List<Vector2> exeptList = null)
    {
        // 입력된 타일이 없으면 리턴
        if (tileList.Count <= 0)
            return null;

        // 타일맵 생성할 사이즈 갱신
        tilemapSize = genSize;
        // 타일맵 생성할 위치 갱신
        tilemapPos = genPos;

        // 타일 비우기
        ClearMap(true);

        // 확률에 따라 랜덤 위치에 타일 생성 예약
        if (tileSetPos == null)
        {
            // 입력된 가로,세로 사이즈만큼 생성
            tileSetPos = new int[tilemapSize.x, tilemapSize.y];

            // 입력된 모든 타일에 확률로 타일 생성 여부 산출
            InitPos();
        }

        // 육지 타입 랜덤일때 (이미 생성된 육지에 붙여서 생성)
        if (randomType == RandomType.Ground)
            for (int i = 0; i < genNumRange; i++)
            {
                tileSetPos = GenGroundTile(tileSetPos);
            }

        // 타일 설치된 리스트
        List<Vector2> setList = new List<Vector2>();

        for (int x = 0; x < tilemapSize.x; x++)
        {
            for (int y = 0; y < tilemapSize.y; y++)
            {
                // 설치 예약된 위치라면
                if (tileSetPos[x, y] == 1)
                {
                    // 랜덤 타일 가중치 확인
                    List<float> tileRate = new List<float>();
                    for (int i = 0; i < tileList.Count; i++)
                    {
                        // 타일 풀에서 가중치 수집
                        float weight = tileList[i].weightRate;

                        // 확률 못넘기면 가중치 0으로 설정
                        if (Random.value > tileList[i].rate)
                            weight = 0;

                        // 가중치 입력
                        tileRate.Add(weight);
                    }
                    // 타일 중에 하나 뽑기
                    int tileIndex = SystemManager.Instance.WeightRandom(tileRate);

                    // 설치할 타일
                    RuleTile setTile = tileList[tileIndex].tile;

                    // 타일 설치 위치
                    Vector3Int tilePos = new Vector3Int(-x - 1 + (int)(tilemapSize.x / 2f), -y - 1 + (int)(tilemapSize.y / 2f), 0);

                    // 설치된 위치 월드 포지션으로 전환
                    Vector2 setPos = new Vector2(-x - 1 + (int)(tilemapSize.x), -y - 1 + (int)(tilemapSize.y)) * 2;

                    // 설치 타일 위치 리스트에 저장
                    setList.Add(setPos);

                    // 중심위치 더하기
                    tilePos = tilePos + tilemapPos;

                    // 타일 설치
                    tileMap.SetTile(tilePos, setTile);
                }
            }
        }

        // 설치된 타일 리스트 리턴
        return setList;
    }

    public void InitPos()
    {
        for (int x = 0; x < tilemapSize.x; x++)
        {
            for (int y = 0; y < tilemapSize.y; y++)
            {
                // 랜덤 확률 따라서 생성
                tileSetPos[x, y] = Random.Range(1, 101) <= iniChance ? 1 : 0;
            }
        }
    }

    public int[,] GenGroundTile(int[,] oldMap)
    {
        int[,] newMap = new int[tilemapSize.x, tilemapSize.y];
        // 현재 검사하는 타일 주변의 설치 예약된 타일 개수
        int nearNum;
        // 각 타일의 주변 경계
        BoundsInt boundary = new BoundsInt(-1, -1, 0, 3, 3, 1);

        for (int x = 0; x < tilemapSize.x; x++)
        {
            for (int y = 0; y < tilemapSize.y; y++)
            {
                nearNum = 0;

                // 주변의 타일 전부 검사
                foreach (Vector3Int bound in boundary.allPositionsWithin)
                {
                    //
                    if (bound.x == 0 && bound.y == 0)
                        continue;

                    // 주변 타일이 타일사이즈 범위 내에 있을때
                    if (x + bound.x >= 0 && x + bound.x < tilemapSize.x && y + bound.y >= 0 && y + bound.y < tilemapSize.y)
                    {
                        // 해당 주변 타일이 이미 예약된 상태면 nearNum 변수 증가
                        nearNum += oldMap[x + bound.x, y + bound.y];
                    }
                    // 타일 사이즈 범위 밖일때
                    else
                    {
                        // 테두리도 검사할때
                        if (border)
                            // nearNum 변수 증가
                            nearNum++;
                    }
                }

                // 설치 예약 되어있을때
                if (oldMap[x, y] == 1)
                {
                    if (nearNum < deathLimit)
                        newMap[x, y] = 0;
                    // else
                    // {
                    //     newMap[x, y] = 1;
                    // }
                }

                // 설치 예약 되어있지 않을때
                if (oldMap[x, y] == 0)
                {
                    // int borderAdd = 0;
                    // // 테두리 타일일때
                    // if (x == 0 || y == 0)
                    //     borderAdd = -3;

                    if (nearNum > birthLimit)
                        newMap[x, y] = 1;
                    // else
                    // {
                    //     newMap[x, y] = 0;
                    // }
                }
            }
        }

        return newMap;
    }

    void Update()
    {
        // if (Input.GetMouseButtonDown(0))
        // {
        //     doSim(numR);
        // }

        // if (Input.GetMouseButtonDown(1))
        // {
        //     clearMap(true);
        // }

        // if (Input.GetMouseButton(2))
        // {
        //     SaveAssetMap();
        // }
    }

    public void SaveMapPrefab()
    {
        string saveName = "tmapXY_" + count;

        if (grid)
        {
            var savePath = "Assets/" + saveName + ".prefab";
            if (PrefabUtility.CreatePrefab(savePath, grid))
            {
                EditorUtility.DisplayDialog("Tilemap saved", "Your Tilemap was saved under" + savePath, "Continue");
            }
            else
            {
                EditorUtility.DisplayDialog("Tilemap NOT saved", "An ERROR occured while trying to saveTilemap under" + savePath, "Continue");
            }
        }

        count++;
    }

    public void ClearMap(bool complete = true)
    {
        if (tileReset)
            // 모든 타일 삭제
            tileMap.ClearAllTiles();

        // 기존 예약된 설치 위치 초기화
        if (complete)
        {
            tileSetPos = null;
        }
    }
}
