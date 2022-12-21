using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Lean.Pool;

[System.Serializable]
public class Prop
{
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

public class GroundScroll : MonoBehaviour
{
    public Transform[] background = null;
    public Transform player;
    [SerializeField] List<Vector3> initPos = new List<Vector3>(); // 초기화 된 맵위치 리스트
    [SerializeField] List<Prop> propList = new List<Prop>(); // 사물 리스트

    [Header("State")]
    [SerializeField] int maxPropAttempt = 10; // 사물 생성 시도 최대 횟수
    float sizeX;
    float sizeY;
    float rightX;
    float leftX;
    float upY;
    float downY;

    private void Start()
    {
        sizeX = background[0].GetComponent<SpriteRenderer>().sprite.bounds.size.x;
        sizeY = background[0].GetComponent<SpriteRenderer>().sprite.bounds.size.y;

        rightX = sizeX / 2;
        leftX = -sizeX / 2;
        upY = sizeY / 2;
        downY = -sizeY / 2;
        // print(rightX + " : " + leftX + " : " + upY + " : " + downY);

        // 바닥 위치 초기화
        MoveGround();
    }

    void Update()
    {
        // 플레이어가 경계 끝에 왔을때 배경 9개 전부 위치 변경
        if (player.position.x >= rightX)
        {
            // 바닥 움직이기
            MoveGround(new Vector3(sizeX, 0));
            rightX += sizeX;
            leftX += sizeX;
        }

        if (player.position.x <= leftX)
        {
            // 바닥 움직이기
            MoveGround(new Vector3(-sizeX, 0));

            rightX -= sizeX;
            leftX -= sizeX;
        }

        if (player.position.y >= upY)
        {
            // 바닥 움직이기
            MoveGround(new Vector3(0, sizeY));

            upY += sizeY;
            downY += sizeY;
        }

        if (player.position.y <= downY)
        {
            // 바닥 움직이기
            MoveGround(new Vector3(0, -sizeY));

            // Y 위치 갱신
            upY -= sizeY;
            downY -= sizeY;
        }
    }

    void MoveGround(Vector3 movePos = default)
    {
        for (int i = 0; i < background.Length; i++)
        {
            // 현재 바닥 위치 
            Vector3 groundPos = background[i].position;
            // pos.y -= sizeY;

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
                    Random.Range(groundPos.x - sizeX / 2f, groundPos.x + sizeX / 2f),
                    Random.Range(groundPos.y - sizeY / 2f, groundPos.y + sizeY / 2f));

                // 시도 횟수 남았으면 계속 반복
                while (spawnAttemptCount > 0)
                {
                    // 현재 맵 이내의 모든 사물들 범위 겹치는지 확인, 이동해야할 벡터 리턴
                    Vector2 moveDir = CheckOverlap(spawnPos, fieldPropList, nowProp);

                    // 주변에 사물 없을때
                    if (moveDir == new Vector2(sizeX, sizeY))
                    {
                        // 사물 스폰
                        GameObject propObj = LeanPool.Spawn(nowProp.propPrefab, spawnPos, Quaternion.identity, SystemManager.Instance.objectPool);

                        // 장애물 속성 생성 및 초기화
                        Prop spawnProp = new Prop(nowProp);
                        spawnProp.propObj = propObj; // 현재 오브젝트 넣기

                        // 사물 리스트에 넣기
                        fieldPropList.Add(spawnProp);

                        //todo 확률따라 좌우반전

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
        return new Vector2(sizeX, sizeY);
    }
}
