using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class TileMapGenerator : MonoBehaviour
{
    [Range(0, 100)]
    public int iniChance;
    [Range(1, 8)]
    public int birthLimit;
    [Range(1, 8)]
    public int deathLimit;

    [Range(1, 10)]
    public int genNumRange;
    private int count = 0;

    private int[,] terrainMap;
    public Vector3Int tmpSize;
    public Tilemap topMap;
    public Tilemap botMap;
    public RuleTile topTile;
    public AnimatedTile botTile;
    [SerializeField] GameObject grid;

    int width;
    int height;

    public void doSim()
    {
        clearMap(false);
        width = tmpSize.x;
        height = tmpSize.y;

        if (terrainMap == null)
        {
            // 입력된 가로,세로 사이즈만큼 생성
            terrainMap = new int[width, height];
            // 입력된 모든 타일에 확률로 타일 생성 여부 산출
            initPos();
        }

        for (int i = 0; i < genNumRange; i++)
        {
            terrainMap = genTilePos(terrainMap);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 설치 예약된 위치라면
                if (terrainMap[x, y] == 1)
                    // 지상 타일 설치
                    topMap.SetTile(new Vector3Int(-x + width / 2, -y + height / 2, 0), topTile);

                // 아래 배경 타일 설치
                botMap.SetTile(new Vector3Int(-x + width / 2, -y + height / 2, 0), botTile);
            }
        }
    }

    public void initPos()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                terrainMap[x, y] = Random.Range(1, 101) < iniChance ? 1 : 0;
            }
        }
    }

    public int[,] genTilePos(int[,] oldMap)
    {
        int[,] newMap = new int[width, height];
        // 이웃한 타일 개수
        int nearNum;
        // 
        BoundsInt myB = new BoundsInt(-1, -1, 0, 3, 3, 1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                nearNum = 0;

                // 주변의 타일 전부 검사
                foreach (Vector3Int bound in myB.allPositionsWithin)
                {
                    //
                    if (bound.x == 0 && bound.y == 0)
                        continue;

                    if (x + bound.x >= 0 && x + bound.x < width && y + bound.y >= 0 && y + bound.y < height)
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

    public void SaveAssetMap()
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

    public void clearMap(bool complete = true)
    {
        topMap.ClearAllTiles();
        botMap.ClearAllTiles();
        if (complete)
        {
            terrainMap = null;
        }
    }
}
