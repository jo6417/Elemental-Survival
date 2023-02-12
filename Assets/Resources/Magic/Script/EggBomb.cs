using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class EggBomb : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] MagicHolder magicHolder;
    [SerializeField] List<Sprite> eggSpriteList = new List<Sprite>(); // 달걀 이미지 목록
    [SerializeField] List<Egg> eggList = new List<Egg>(); // 달걀 오브젝트 목록
    [SerializeField] Egg eggPrefab; // 달걀 프리팹

    [Header("State")]
    float range;
    float speed;
    float coolTime;
    int atkNum;
    float respawnTime;
    float respawnRecord;
    IEnumerator cooldownCoroutine;

    private void Awake()
    {
        // 액티브 사용시 함수를 콜백에 넣기
        magicHolder.magicCastCallback = EggShot;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 마법 스탯 초기화
        yield return new WaitUntil(() => magicHolder.magic != null);

        range = MagicDB.Instance.MagicRange(magicHolder.magic);
        speed = MagicDB.Instance.MagicSpeed(magicHolder.magic, false);
        coolTime = MagicDB.Instance.MagicCoolTime(magicHolder.magic);
        atkNum = MagicDB.Instance.MagicAtkNum(magicHolder.magic);

        // 리스폰 쿨타임 초기화
        respawnTime = coolTime / atkNum;

        // 리스폰 쿨타임 카운트 갱신
        respawnRecord = Time.time + respawnTime;

        //플레이어 자식으로 들어가기
        transform.SetParent(PlayerManager.Instance.transform);
        transform.localPosition = Vector3.zero;

        // 공격 횟수만큼 달걀 생성
        for (int i = 0; i < atkNum; i++)
        {
            // 달걀 생성
            SpawnEgg();
        }

        // 달걀 거리 정렬
        SortEggs();
    }

    private void Update()
    {
        // 시간마다 현재 개수 검사
        if (respawnRecord <= Time.time)
        {
            // 쿨타임 카운트 갱신
            respawnRecord = Time.time + respawnTime;

            // 알 개수 부족하면 생성
            if (eggList.Count < atkNum)
            {
                // 달걀 생성
                SpawnEgg();

                // 달걀 거리 재정렬
                SortEggs();
            }

            // 자동 시전일때, 알 개수 꽉 찼을때
            if (!magicHolder.isManualCast && eggList.Count >= atkNum)
            {
                // 타겟 위치 산출
                magicHolder.targetPos = CastMagic.Instance.MarkEnemyPos(magicHolder.magic)[0];

                // 모든 달걀 던지기
                StartCoroutine(AllShot());

                // 글로벌 쿨다운 시작
                CastMagic.Instance.Cooldown(MagicDB.Instance.GetMagicByID(magicHolder.magic.id), coolTime);

                // 쿨타임 카운트 갱신
                respawnRecord = Time.time + respawnTime;
            }
        }
    }

    void EggShot()
    {
        // 수동 시전일때
        if (magicHolder.isManualCast)
        {
            // 타겟 위치에 마우스 위치 넣기
            magicHolder.targetPos = PlayerManager.Instance.GetMousePos();

            // 달걀 0개면 리턴
            if (eggList.Count <= 0)
                return;

            // 꼬리 마지막 알의 인덱스 찾기
            int eggIndex = eggList.Count - 1;

            // 타겟 위치 전달
            eggList[eggIndex].magicHolder.targetPos = magicHolder.targetPos;

            StartCoroutine(eggList[eggIndex].SingleShot(eggIndex));

            // 해당 달걀을 목록에서 삭제
            eggList.Remove(eggList[eggIndex]);

            // 달걀 거리 재정렬
            SortEggs();

            // 글로벌로 표시할 쿨타임 = 알 하나당 리스폰 타임 * (공격횟수 - 현재 남은 알 개수)
            float globalCooltime = respawnTime * (atkNum - eggList.Count);

            // 글로벌 쿨다운 시작
            CastMagic.Instance.Cooldown(MagicDB.Instance.GetMagicByID(magicHolder.magic.id), globalCooltime);

            // 쿨타임 카운트 갱신
            respawnRecord = Time.time + respawnTime;
        }
    }

    IEnumerator AllShot()
    {
        // 달걀 개수 모두 채워질때까지 대기
        yield return new WaitUntil(() => eggList.Count == atkNum);

        // 달걀 개수만큼 반복
        for (int i = 0; i < eggList.Count; i++)
        {
            // 자동,수동 여부 전달
            eggList[i].magicHolder.isManualCast = magicHolder.isManualCast;

            // 타겟 위치 전달
            eggList[i].magicHolder.targetPos = magicHolder.targetPos;

            // 달걀 순서대로 투척
            StartCoroutine(eggList[i].SingleShot(i));
        }

        // 리스트 비우기
        eggList.Clear();
    }

    void SpawnEgg()
    {
        // 달걀 생성 위치
        Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * 2f;

        // 달걀 생성
        Egg egg = LeanPool.Spawn(eggPrefab, spawnPos, Quaternion.identity, ObjectPool.Instance.magicPool);

        // 사이즈 키우기
        egg.transform.localScale = Vector3.zero;
        egg.transform.DOScale(Vector3.one, 0.4f);

        // 플레이어 따라가게 설정
        KeepDistanceMove eggFollow = egg.keepDistanceMove;
        eggFollow.followTarget = PlayerManager.Instance.transform;
        eggFollow.enabled = true;

        // 마법 정보 전달
        egg.magicHolder.magic = magicHolder.magic;
        // 달걀 스프라이트 중 하나 넣기
        egg.eggSprite.sprite = eggSpriteList[Random.Range(0, eggSpriteList.Count)];

        // 리스트에 달걀 포함
        eggList.Add(egg);
    }

    void SortEggs()
    {
        for (int i = 0; i < eggList.Count; i++)
        {
            KeepDistanceMove eggFollow = eggList[i].keepDistanceMove;
            // 최소, 최대 거리 설정
            eggFollow.minDistance = 0;
            eggFollow.maxDistance = (i + 1) * 2;
            // 점프 딜레이 설정
            eggFollow.jumpDelay = i * 0.05f;
        }
    }
}
