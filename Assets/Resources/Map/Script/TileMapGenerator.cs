using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

[System.Serializable]
public class TileLayerPack
{
    public RuleTile tile;
    public float rate;
}

public class TileMapGenerator : MonoBehaviour
{
    [Header("State")]
    [SerializeField] bool tileReset = false; // 타일 설치시 기존 타일 초기화 여부
    [SerializeField] RandomType randomType;
    enum RandomType { random, ground };

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
    public List<TileLayerPack> tileList = new List<TileLayerPack>();
    [SerializeField] GameObject grid;

    public void GenTile(Vector3Int genSize, Vector3Int genPos)
    {
        // 타일맵 생성할 사이즈 갱신
        tilemapSize = genSize;
        // 타일맵 생성할 위치 갱신
        tilemapPos = genPos;

        // // 육지 형태 랜덤일때 (이미 생성된 육지에 붙여서 생성)
        // if (randomType == RandomType.ground)
        //     ClearMap(false);
        // // 일반 랜덤일때 초기화
        // else
        ClearMap(true);

        // 확률에 따라 랜덤 위치에 타일 생성 예약
        if (tileSetPos == null)
        {
            // 입력된 가로,세로 사이즈만큼 생성
            tileSetPos = new int[tilemapSize.x, tilemapSize.y];

            // 입력된 모든 타일에 확률로 타일 생성 여부 산출
            InitPos();
        }

        // 육지 형태 랜덤일때 (이미 생성된 육지에 붙여서 생성)
        if (randomType == RandomType.ground)
            for (int i = 0; i < genNumRange; i++)
            {
                tileSetPos = GenTilePos(tileSetPos);
            }

        for (int x = 0; x < tilemapSize.x; x++)
        {
            for (int y = 0; y < tilemapSize.y; y++)
            {
                // 설치 예약된 위치라면
                if (tileSetPos[x, y] == 1)
                {
                    // 랜덤 타일 가중치 확인
                    List<float> tileRate = new List<float>();
                    foreach (TileLayerPack tile in tileList)
                        tileRate.Add(tile.rate);
                    // 타일 중에 하나 뽑기
                    int tileIndex = SystemManager.Instance.WeightRandom(tileRate);

                    // 설치할 타일
                    RuleTile setTile = tileList[tileIndex].tile;

                    // 타일 설치
                    tileMap.SetTile(tilemapPos + new Vector3Int(-x + tilemapSize.x / 2, -y + tilemapSize.y / 2, 0), setTile);
                }
            }
        }
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

    public int[,] GenTilePos(int[,] oldMap)
    {
        int[,] newMap = new int[tilemapSize.x, tilemapSize.y];
        // 이웃한 타일 개수
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

                    if (x + bound.x >= 0 && x + bound.x < tilemapSize.x && y + bound.y >= 0 && y + bound.y < tilemapSize.y)
                    {
                        nearNum += oldMap[x + bound.x, y + bound.y];
                    }
                    else
                    {
                        // 이웃한 타일 개수 증가
                        nearNum++;
                    }
                }

                if (oldMap[x, y] == 1)
                {
                    if (nearNum < deathLimit)
                        newMap[x, y] = 0;
                    else
                    {
                        newMap[x, y] = 1;
                    }
                }

                if (oldMap[x, y] == 0)
                {
                    if (nearNum > birthLimit)
                        newMap[x, y] = 1;
                    else
                    {
                        newMap[x, y] = 0;
                    }
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
