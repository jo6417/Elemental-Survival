using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Lean.Pool;

public class GatePortal : MonoBehaviour
{
    float maxGem; //필요 젬 개수
    float nowGem; //현재 젬 개수
    float delayCount; //상호작용 딜레이 카운트
    [SerializeField]
    float interactDelay = 1f; //상호작용 딜레이

    [Header("Refer")]
    [SerializeField]
    GameObject showKey; //상호작용 키 표시 UI
    [SerializeField]
    TextMeshProUGUI GemNum; //젬 개수 표시 UI
    [SerializeField]
    Animator anim; //포탈 이펙트 애니메이션
    [SerializeField]
    SpriteRenderer gaugeImg; //포탈 테두리 원형 게이지 이미지

    private void OnEnable()
    {
        Initial();
    }

    void Initial()
    {
        maxGem = Random.Range(10, 100);

        //! 테스트 
        maxGem = 10;

        //젬 개수 UI 갱신
        UpdateGemNum();

        //상호작용 표시 비활성화
        showKey.SetActive(false);

        //포탈 이펙트 오브젝트 비활성화
        anim.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //상호작용 키 표시 UI 활성화
        if (other.CompareTag("Player") && nowGem < maxGem)
        {
            showKey.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        //상호작용 키 표시 UI 비활성화
        if (other.CompareTag("Player"))
        {
            showKey.SetActive(false);
        }
    }

    private void Update()
    {
        // 상호작용 키 누르면
        if (showKey.activeSelf && Input.GetKey(KeyCode.E) && delayCount <= 0)
        {
            // 젬 하나씩 넣기
            nowGem++;

            //젬 개수 UI 갱신
            UpdateGemNum();

            if (nowGem == maxGem)
            {
                // 상호작용 끄기
                showKey.SetActive(false);

                //보스 소환
                StartCoroutine(SummonBoss());
            }

            //상호작용 딜레이 초기화
            delayCount = interactDelay;
        }
        else
        {
            //딜레이 카운트 차감
            if (delayCount > 0)
                delayCount -= Time.deltaTime;
        }

        //TODO 게이지 이미지 색깔 그라데이션으로 바꾸기
    }

    void UpdateGemNum()
    {
        //젬 개수 UI 갱신
        GemNum.text = nowGem.ToString() + " / " + maxGem.ToString();

        //젬 개수만큼 테두리 도넛 게이지 갱신
        float gaugeFill = ((maxGem - nowGem) / maxGem) * 360f;
        gaugeFill = Mathf.Clamp(gaugeFill, 0, 360f);

        gaugeImg.material.SetFloat("_Arc2", gaugeFill);
    }

    IEnumerator SummonBoss()
    {
        //보스 리스트
        List<EnemyInfo> bosses = new List<EnemyInfo>();

        //타입이 보스인 몬스터 찾기
        foreach (KeyValuePair<int, EnemyInfo> value in EnemyDB.Instance.enemyDB)
        {
            //타입이 보스면
            if (value.Value.enemyType == "boss")
            {
                //리스트에 포함
                bosses.Add(value.Value);
            }
        };

        //보스 프리팹 찾기
        GameObject bossPrefab = EnemyDB.Instance.GetPrefab(bosses[Random.Range(0, bosses.Count)].id);

        //보스 소환 위치
        Vector2 bossPos = (Vector2)transform.position + Random.insideUnitCircle * 10f;

        // 보스 소환 및 비활성화
        GameObject bossObj = LeanPool.Spawn(bossPrefab, bossPos, Quaternion.identity, EnemySpawn.Instance.enemyPool);
        bossObj.SetActive(false);

        // 보스 enemyManager 참조
        EnemyManager enemyManager = bossObj.GetComponent<EnemyManager>();

        //포탈에서 보스 소환
        StartCoroutine(EnemySpawn.Instance.PortalSpawn(bosses[Random.Range(0, bosses.Count)], false, bossPos, bossObj));

        // 보스 소환 후 포탈 이펙트 활성화
        anim.gameObject.SetActive(true);

        // 보스 죽을때까지 대기
        yield return new WaitUntil(() => enemyManager.isDead);

        print("boss dead");

        // 몬스터 스폰 멈추기
        EnemySpawn.Instance.spawnSwitch = false;

        // 남은 몬스터 화살표로 방향 표시해주기
        UIManager.Instance.enemyPointSwitch = true;

        //TODO 모든 몬스터 죽을때까지 대기
        yield return new WaitUntil(() => EnemySpawn.Instance.spawnEnemyList.Count == 0f);

        print("all dead");

        // PortalOpen 트리거 true / Open, Idle 애니메이션 순서대로 시작
        anim.SetTrigger("PortalOpen");
    }
}
