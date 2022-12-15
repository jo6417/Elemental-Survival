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
    [SerializeField] float atkRange = 30f;

    private void OnEnable()
    {
        // 쿨타임 카운트 초기화
        coolCount = 1f;

        // 스케일 초기화
        transform.localScale = Vector3.one;

        // 식물 생성 소리 재생
        SoundManager.Instance.PlaySound("Farmer_Plant_Birth", transform.position);
    }

    private void Update()
    {
        // 쿨타임 다됬으면
        if (coolCount <= 0)
        {
            // 잎날 생성 위치
            Vector3 leafPos = transform.position + Vector3.up * 1.5f;
            // 잎날 날아갈 방향,속도 벡터
            Vector2 targetDir = (PlayerManager.Instance.transform.position - leafPos).normalized * shotSpeed;

            // 범위 내에 있을때
            if ((PlayerManager.Instance.transform.position - leafPos).magnitude <= atkRange)
            {
                // 잎날 생성
                Transform leaf = LeanPool.Spawn(leafPrefab, leafPos, Quaternion.identity, SystemManager.Instance.enemyAtkPool);

                // 타겟을 향해 잎날 발사
                leaf.GetComponentInChildren<Rigidbody2D>().velocity = targetDir;

                // 식물 바운스하는 트윈
                transform.GetChild(0).DOPunchScale(Vector2.up * 0.5f, 0.5f);

                // 잎날 발사 소리 재생
                SoundManager.Instance.PlaySound("Farmer_Plant_LeafShot", transform.position);

                // 쿨타임 초기화
                coolCount = cooltime;
            }
        }
        else
            // 쿨타임 차감
            coolCount -= Time.deltaTime;
    }
}
