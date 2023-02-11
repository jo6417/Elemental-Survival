using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class BuckShot : MonoBehaviour
{
    MagicHolder magicHolder;
    MagicInfo magic;
    ParticleSystem particle;
    public float particleSpeed;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => TryGetComponent(out MagicHolder holder));
        magicHolder = GetComponent<MagicHolder>();
        magic = magicHolder.magic;

        float rotation;
        // 수동 사격일때
        if (magicHolder.isManualCast)
        {
            // 마우스 방향
            Vector2 mouseDir = PlayerManager.Instance.GetMousePos() - PlayerManager.Instance.transform.position;

            // 마우스 각도
            rotation = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;
        }
        // 자동 사격일때
        else
        {
            //플레이어가 마지막 바라본 방향의 각도
            rotation = Mathf.Atan2(PlayerManager.Instance.lastDir.y, PlayerManager.Instance.lastDir.x) * Mathf.Rad2Deg;
        }

        //해당 각도로 회전
        transform.rotation = Quaternion.Euler(Vector3.forward * rotation);

        //파티클 발사
        particle.Play();

        // 파티클 시스템에 속도 적용하기
        ParticleSystem.MainModule particleMain = particle.main;
        particleMain.simulationSpeed = MagicDB.Instance.MagicSpeed(magic, true);

        // 멈추면 디스폰
        yield return new WaitUntil(() => particle.isStopped);
        LeanPool.Despawn(transform);
    }
}