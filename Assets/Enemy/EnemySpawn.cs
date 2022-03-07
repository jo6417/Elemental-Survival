using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class EnemySpawn : MonoBehaviour
{
    public GameObject[] mobList = null;
    Collider2D col;
    Transform mobParent;
    public int mobNumMax; //최대 몬스터 수
    public int mobNumNow; //현재 몬스터 수

    void Start()
    {
        col = GetComponent<BoxCollider2D>();
        mobParent = ObjectPool.Instance.transform.Find("MobPool");
    }

    void Update()
    {
        //TODO 플레이어 레벨에 따라 몬스터 등급 및 수량 바꾸기
        //몬스터 리스트에서 랜덤 넘버
        int mobNum = Random.Range(0, mobList.Length);

        mobNumNow = mobParent.childCount;

        // 몬스터 수 유지, 자동 스폰
        if (mobNumNow < mobNumMax && EnemyDB.Instance.loadDone)
        {
            // mobNumNow++; //현재 몬스터 수 증가
            var mob = LeanPool.Spawn(mobList[mobNum], SpawnPos(), Quaternion.identity, mobParent); //몬스터 생성
        }


        if (Input.GetKey(KeyCode.Space) && EnemyDB.Instance.loadDone)
        {
            var mob = LeanPool.Spawn(mobList[mobNum], SpawnPos(), Quaternion.identity, mobParent); //몬스터 생성
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 스폰 콜라이더 밖으로 나가면 콜라이더 내부 반대편으로 보내기
        if (other.CompareTag("Enemy") && other.gameObject.activeSelf)
        {
            Transform originParent = other.transform.parent; //원래 부모 기억

            other.transform.parent = transform; //몹 스포너로 부모 지정
            other.transform.localPosition = -other.transform.localPosition; // 내부 포지션 역전시키기

            other.transform.parent = originParent; //원래 부모로 복귀
        }
    }

    //콜라이더 테두리 스폰 위치
    Vector2 SpawnPos()
    {
        float spawnPosX = Random.Range(col.bounds.min.x, col.bounds.max.x);
        float spawnPosY = Random.Range(col.bounds.min.y, col.bounds.max.y);
        int spawnSide = Random.Range(0, 4);

        // 스폰될 모서리 방향
        switch (spawnSide)
        {
            case 0:
                spawnPosY = col.bounds.max.y;
                break;

            case 1:
                spawnPosX = col.bounds.max.x;
                break;

            case 2:
                spawnPosY = col.bounds.min.y;
                break;

            case 3:
                spawnPosX = col.bounds.min.x;
                break;
        }

        return new Vector2(spawnPosX, spawnPosY);
    }
}
