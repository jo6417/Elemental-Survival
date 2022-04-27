using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

public class MagicArea : MonoBehaviour
{
    private MagicInfo magic;
    public SpriteRenderer sprite;
    public Vector2 originScale; //원래 사이즈
    public float frontDistance = 2f; //오브젝트를 얼마나 앞에서 생성할지
    public Ease ease;
    public bool isThrow; //던지기 시퀀스 실행 여부

    private void Awake()
    {
        originScale = transform.localScale;
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Initial());

        //시간 멈춤 체크
        // StartCoroutine(StopCheck());
    }

    IEnumerator Initial()
    {
        //색깔 초기화
        sprite.color = Color.white;

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => TryGetComponent(out MagicHolder holder));
        magic = GetComponent<MagicHolder>().magic;

        // 마법 지속시간
        float duration = MagicDB.Instance.MagicDuration(magic);

        // 마법 날아가는 속도
        float speed = MagicDB.Instance.MagicSpeed(magic, true);

        // 마법 범위
        float range = MagicDB.Instance.MagicRange(magic);

        // 떨어지는 시간
        float fallingTime = 1;

        //플레이어 위치
        Vector3 playerPos = PlayerManager.Instance.transform.position;
        //플레이어 방향, 방향 없을때는 vector2.right 넣기
        Vector3 playerDir = PlayerManager.Instance.lastDir != Vector3.zero ? PlayerManager.Instance.lastDir : Vector3.right;

        //던지기 시퀀스
        if (isThrow)
        {
            //플레이어 앞에서 시작
            transform.position = playerPos + playerDir * frontDistance;

            // print(transform.position);

            //최소 사이즈로 시작
            transform.localScale = Vector2.zero;

            //range 적용된 크기까지 커지기
            transform.DOScale(originScale * range, fallingTime)
            .SetEase(Ease.OutBack);

            // 플레이어 방향으로 날리기
            transform.DOMove(transform.position + playerDir * speed, fallingTime)
            .SetEase(ease)
            .OnComplete(() =>
            {
                //점점 투명해지기
                sprite.DOColor(Color.clear, duration)
                    .SetEase(Ease.InExpo);

                //떨어진 시점부터 마법 자동 디스폰
                StartCoroutine(DespawnMagic(duration));
            });
        }
        else
        {
            //range 따라 사이즈 키우기
            transform.localScale = originScale * range;

            //점점 투명해지기
            sprite.DOColor(Color.clear, duration)
                .SetEase(Ease.InExpo);

            //떨어진 시점부터 마법 자동 디스폰
            StartCoroutine(DespawnMagic(duration));
        }

    }

    IEnumerator DespawnMagic(float delay = 0)
    {
        //마법 지속시간
        yield return new WaitForSeconds(delay);

        yield return new WaitUntil(() => VarManager.Instance.playerTimeScale > 0);

        // 오브젝트 디스폰하기
        if (gameObject.activeSelf)
            LeanPool.Despawn(transform);
    }
}
