using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class Plant_AI : MonoBehaviour
{
    [SerializeField] Transform leafPrefab;
    [SerializeField] float cooltime;
    [SerializeField, ReadOnly] float coolCount;
    [SerializeField] float shotSpeed = 30f;

    // private void OnEnable()
    // {
    //     StartCoroutine(Init());
    // }

    // IEnumerator Init()
    // {
    //     // 초기화 대기
    //     yield return new WaitUntil(() => initialFinish);
    // }

    private void Update()
    {
        // 쿨타임 차감
        coolCount -= Time.deltaTime;

        // 쿨타임 다됬으면
        if (coolCount <= 0)
        {
            // 잎날 생성
            Transform leaf = LeanPool.Spawn(leafPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity, SystemManager.Instance.enemyAtkPool);

            // 잎날 날아갈 방향,속도 벡터
            Vector2 targetDir = (PlayerManager.Instance.transform.position - leaf.position).normalized * shotSpeed;

            // 타겟을 향해 잎날 발사
            leaf.GetComponentInChildren<Rigidbody2D>().velocity = targetDir;

            // 식물 바운스하는 트윈
            transform.GetChild(0).DOPunchScale(Vector2.up * 0.5f, 0.5f);

            // 쿨타임 초기화
            coolCount = cooltime;
        }
    }
}
