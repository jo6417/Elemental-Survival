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
    float respawnTime;
    float respawnRecord;

    private void Awake()
    {
        // 액티브 사용시 함수를 콜백에 넣기
        magicHolder.magicCastCallback = QuickEggShot;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // magicHolder 초기화 대기
        yield return new WaitUntil(() => magicHolder && magicHolder.initDone);

        print(magicHolder.coolTime + ":" + magicHolder.atkNum);

        // 리스폰 쿨타임 초기화
        respawnTime = magicHolder.coolTime / magicHolder.atkNum;

        // 리스폰 쿨타임 카운트 갱신
        respawnRecord = Time.time + respawnTime;

        //플레이어 자식으로 들어가기
        transform.SetParent(PlayerManager.Instance.transform);
        transform.localPosition = Vector3.zero;
    }

    private void OnDisable()
    {
        // 달걀 개수만큼 반복
        for (int i = 0; i < eggList.Count; i++)
        {
            // 모든 달걀 디스폰
            LeanPool.Despawn(eggList[i]);
        }

        // 달걀 리스트 비우기
        eggList.Clear();
    }

    private void Update()
    {
        // 시간마다 현재 개수 검사
        if (respawnRecord <= Time.time)
        {
            // 쿨타임 카운트 갱신
            respawnRecord = Time.time + respawnTime;

            // 알 개수 부족하면 생성
            if (eggList.Count < magicHolder.atkNum)
            {
                // 달걀 생성
                SpawnEgg();
                // 달걀 거리 재정렬
                SortEggs();
            }

            // 자동 시전일때, 알 개수 꽉 찼을때
            if (!magicHolder.isQuickCast && eggList.Count >= magicHolder.atkNum)
            {
                // 모든 달걀 던지기
                AutoEggShot();
            }
        }
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

        // 달걀 생성 사운드 재생
        SoundManager.Instance.PlaySound("EggBomb_Spawn", spawnPos);
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

    void AutoEggShot()
    {
        // 플레이어 위치 넣기
        magicHolder.targetPos = PlayerManager.Instance.transform.position;

        EggShot();

        // 글로벌 쿨다운 시작
        CastMagic.Instance.Cooldown(magicHolder.magic, false);
    }

    void QuickEggShot()
    {
        // 마우스 위치 넣기
        magicHolder.targetPos = PlayerManager.Instance.GetMousePos();

        EggShot();

        // 퀵슬롯 쿨다운 시작
        CastMagic.Instance.Cooldown(magicHolder.magic, true);
    }

    void EggShot()
    {
        // 현재 생성된 달걀 개수만큼 반복
        for (int i = 0; i < eggList.Count; i++)
        {
            // range 만큼 오차 추가
            Vector2 atkPos = (Vector2)magicHolder.targetPos + Random.insideUnitCircle.normalized * magicHolder.range;

            Egg egg = eggList[i];

            if (egg)
            {
                // 타겟 위치 전달
                egg.magicHolder.targetPos = atkPos;

                StartCoroutine(egg.SingleShot(i, atkPos));
            }
        }

        // 달걀 리스트 비우기
        eggList.Clear();

        // 글로벌로 표시할 쿨타임 = 알 하나당 리스폰 타임 * (공격횟수 - 현재 남은 알 개수)
        float globalCooltime = respawnTime * (magicHolder.atkNum - eggList.Count);

        // 쿨타임 카운트 갱신
        respawnRecord = Time.time + respawnTime;
    }
}
