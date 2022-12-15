using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;
using DG.Tweening;
using System.Linq;

public class Seed_AI : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] GameObject dustPrefab;
    [SerializeField] string[] slimeBirthSounds = { };

    [Header("State")]
    public bool initStart = false; // 초기화 여부
    public bool turning = false; // 형태 변환 여부
    [SerializeField] float lifeTimeCount; // 남은 수명
    [SerializeField] float lifeTimeMax; // 최대 수명
    [SerializeField] AudioSource waterSound; // 재생중인 급수 사운드

    [Header("Slime")]
    [SerializeField] GameObject lifeSlimePrefab;
    public Character seedCharacter;
    [SerializeField] SpriteRenderer slimeSprite;

    [Header("Plant")]
    public LineRenderer waterLine;
    [SerializeField] GameObject plantPrefab;
    [SerializeField] SpriteRenderer fillGauge; // 현재 채워진 물 표시
    public float waterFillCount = 0f;
    public float waterFillMax = 3f;

    private void OnEnable()
    {
        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 초기화 안됨
        initStart = false;
        // 무적 켜기
        seedCharacter.invinsible = true;

        // 형태 변환 여부 초기화
        turning = false;

        // 물 보유량 초기화
        waterFillCount = 0f;

        // 슬라임 스프라이트 컬러 투명하게 초기화
        slimeSprite.color = new Color(1, 1, 1, 0);

        // 게이지 초기화
        fillGauge.material.SetFloat("_Arc2", 360f);

        // 물 끄기
        StopWater();

        // 초기화 시작할때까지 대기
        yield return new WaitUntil(() => initStart);

        // 무적 끄기
        seedCharacter.invinsible = false;

        // 먼지 이펙트 발생
        LeanPool.Spawn(dustPrefab, transform.position, Quaternion.identity, SystemManager.Instance.enemyPool);
    }

    private void Update()
    {
        //todo 시간 지나면 썩어서 사라짐
        // 스프라이트 점점 까맣게
        // 수명 다되면 색 초기화 후 죽음
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 초기화 안됬으면 리턴
        if (!initStart)
            return;
        // 형태 변환 시작했으면 리턴
        if (turning)
            return;
        // 죽었으면 리턴
        if (seedCharacter.hpNow <= 0)
            return;

        // 독구름 충돌 감지
        if (other.CompareTag(SystemManager.TagNameList.Enemy.ToString())
        && other.name == "BioGas")
        {
            // 형태 변환 시작
            turning = true;

            // 슬라임 소환
            SpawnSlime();
        }
    }

    void SpawnSlime()
    {
        // 슬라임 스프라이트 알파값 높이기
        slimeSprite.DOColor(Color.white, 1f)
        .OnComplete(() =>
        {
            // 먼지 이펙트 발생
            LeanPool.Spawn(dustPrefab, transform.position, Quaternion.identity, SystemManager.Instance.enemyPool);

            // 슬라임 소환
            GameObject slime = LeanPool.Spawn(lifeSlimePrefab, transform.position, Quaternion.identity, SystemManager.Instance.enemyPool);

            // 슬라임 생성 소리 재생
            SoundManager.Instance.PlaySoundPool(slimeBirthSounds.ToList(), transform.position);

            // 몬스터 스폰 리스트에 추가
            WorldSpawner.Instance.spawnEnemyList.Add(slime.GetComponent<Character>());

            // 슬라임 스프라이트 컬러 투명하게 초기화
            slimeSprite.color = new Color(1, 1, 1, 0);

            // 씨앗 본체를 디스폰
            SeedDespawn();
        });
    }

    public void FillWater()
    {
        // 초기화 안됬으면 리턴
        if (!initStart)
            return;
        // 형태 변환 시작했으면 리턴
        if (turning)
            return;
        // 죽었으면 리턴
        if (seedCharacter.hpNow <= 0)
            return;

        // 물 꺼져있으면
        if (!waterLine.enabled)
        {
            // 급수 소리 반복 재생
            waterSound = SoundManager.Instance.PlaySound("Farmer_Seed_Watering", transform, 0, 0, -1);
        }

        // 물 라인 켜기
        waterLine.enabled = true;

        // 한 프레임만큼 물 채우기
        waterFillCount += Time.deltaTime;

        // 채워진만큼 게이지 표시
        float fill = ((waterFillMax - waterFillCount) / waterFillMax) * 360f;
        fill = Mathf.Clamp(fill, 0, 360f);
        fillGauge.material.SetFloat("_Arc2", fill);

        // 맥스까지 채우면
        if (waterFillCount >= waterFillMax)
        {
            // 형태 변환 시작
            turning = true;

            // 물 라인 끄기
            StopWater();

            // 무적 켜기
            seedCharacter.invinsible = true;

            // 먼지 이펙트 발생
            LeanPool.Spawn(dustPrefab, transform.position, Quaternion.identity, SystemManager.Instance.enemyPool);

            // 식물 소환
            GameObject plantObj = LeanPool.Spawn(plantPrefab, transform.position, Quaternion.identity, SystemManager.Instance.enemyPool);

            // 몬스터 스폰 리스트에 추가
            WorldSpawner.Instance.spawnEnemyList.Add(plantObj.GetComponent<Character>());

            // 씨앗 본체를 디스폰
            SeedDespawn();
        }
    }

    public void StopWater()
    {
        // 물 끄기
        waterLine.enabled = false;

        // 급수 소리 끄기
        if (waterSound != null && waterSound.gameObject.activeInHierarchy)
            SoundManager.Instance.StopSound(waterSound, 0.5f);
    }

    void SeedDespawn()
    {
        // 초기화 해제
        initStart = false;

        // 게이지 초기화
        fillGauge.material.SetFloat("_Arc2", 360f);

        // 물 끄기
        StopWater();

        // 디스폰
        LeanPool.Despawn(gameObject);
    }
}
