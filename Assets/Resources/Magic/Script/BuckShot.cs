using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;

public class BuckShot : MonoBehaviour
{
    MagicHolder magicHolder;
    MagicInfo magic;
    Vector2 targetPos;
    ParticleSystem particle;
    public float particleSpeed;

    private void Awake()
    {
        particle = GetComponent<ParticleSystem>();
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());

        //시간 멈춤 확인
        StartCoroutine(StopCheck());
    }

    IEnumerator Initial()
    {
        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => TryGetComponent(out MagicHolder holder));
        magicHolder = GetComponent<MagicHolder>();
        magic = magicHolder.magic;
        targetPos = magicHolder.targetPos;

        //TODO 플레이어 바라보는 방향바라보기
        //방향 각도 구하기
        float rotation = Mathf.Atan2(PlayerManager.Instance.lastDir.y, PlayerManager.Instance.lastDir.x) * Mathf.Rad2Deg;

        //목표 위치로 회전
        transform.rotation = Quaternion.Euler(Vector3.forward * rotation);

        //파티클 발사
        particle.Play();

        //TODO 파티클 시스템에 속도 적용하기
        ParticleSystem.MainModule particleMain = particle.main;
        particleMain.simulationSpeed = MagicDB.Instance.MagicSpeed(magic, true);

        // 멈추면 디스폰
        yield return new WaitUntil(() => particle.isStopped);
        LeanPool.Despawn(transform);
    }

    IEnumerator StopCheck()
    {
        while (gameObject.activeSelf)
        {
            if (VarManager.Instance.playerTimeScale == 0)
                particle.Pause();

            yield return null;
        }
    }
}