using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Lean.Pool;

public class MagicFalling : MonoBehaviour
{
    [Header("Refer")]
    private MagicInfo magic;
    MagicHolder magicHolder;
    public Animator anim;
    float originAnimSpeed;    
    public string magicName;
    public Collider2D col;
    public Vector2 originScale; //원래 오브젝트 크기
    public Ease fallEase;
    // public float fallSpeed = 1f; //마법 떨어지는데 걸리는 시간 (시전 딜레이)
    public float fallDistance = 2f; //마법 오브젝트 떨어지는 거리(시전 시간)
    // public float coolTimeSet = 0.3f; //마법 쿨타임 임의 조정
    public bool isDespawn = false; //디스폰 여부
    public bool isExpand = false; //커지면서 등장 여부

    [Header("Effect")]
    public GameObject magicEffect;
    public Vector3 effectPos; //이펙트 생성 위치 보정

    private void Awake()
    {
        // 오브젝트 기본 사이즈 저장
        originScale = transform.localScale;

        //애니메이터 찾기
        anim = GetComponent<Animator>();
        // 애니메이션 기본 속도 저장
        originAnimSpeed = anim.speed;

        //초기화 하기
        StartCoroutine(Initial());
    }

    private void OnEnable()
    {
        StartCoroutine(FallingMagicObj());
    }

    IEnumerator Initial()
    {
        isDespawn = false;

        //시작할때 콜라이더 끄기
        ColliderTrigger(false);

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => TryGetComponent(out MagicHolder holder));
        magicHolder = GetComponent<MagicHolder>();
        magic = magicHolder.magic;

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (magic == null)
        {
            magic = MagicDB.Instance.GetMagicByName(transform.name.Split('_')[0]);
            magicName = magic.magicName;
        }

        //magic 못찾으면 코루틴 종료
        if (magic == null)
            yield break;
    }

    IEnumerator FallingMagicObj()
    {
        //초기화
        StartCoroutine(Initial());

        //magic이 null이 아닐때까지 대기
        yield return new WaitUntil(() => magic != null);

        //TODO 적 위치로 이동
        transform.position = magicHolder.targetPos;

        // 마법 오브젝트 속도, 숫자가 작을수록 빠름
        float magicSpeed = MagicDB.Instance.MagicSpeed(magic, false);

        // 애니메이션 속도 계산
        // anim.speed = originAnimSpeed * MagicDB.Instance.MagicSpeed(magic, true);

        // 팝업창 0,0에서 점점 커지면서 나타내기
        if (isExpand)
            transform.localScale = Vector2.zero;
        transform.DOScale(originScale, magicSpeed)
        .SetUpdate(true)
        .SetEase(Ease.OutBack);

        //시작 위치
        Vector2 startPos = (Vector2)transform.position + new Vector2(0, fallDistance);
        //끝나는 위치
        Vector2 endPos = transform.position;

        //시작 위치로 올려보내기
        transform.position = startPos;
        //목표 위치로 떨어뜨리기
        transform.DOMove(endPos, magicSpeed)
        .SetEase(fallEase)
        .OnComplete(() =>
        {
            //콜라이더 발동시키기
            ColliderTrigger(true);

            // 마법 오브젝트 속도
            float duration = MagicDB.Instance.MagicDuration(magic);

            // 오브젝트 자동 디스폰하기
            if (!isDespawn)
                isDespawn = true;
            StartCoroutine(AutoDespawn(duration));
        });
    }

    public void ColliderTrigger(bool magicTrigger = true)
    {
        // 마법 트리거 발동 됬을때 (적 데미지 입히기, 마법 효과 발동)
        if (magicTrigger)
        {
            col.enabled = true;

            // 이펙트 오브젝트 생성 (이펙트 있으면)
            if (magicEffect)
                LeanPool.Spawn(magicEffect, transform.position + effectPos, Quaternion.identity);
        }
        else
        {
            col.enabled = false;
        }
    }

    void OnCollider(){
        col.enabled = true;
    }

    //애니메이션 끝날때 이벤트 함수
    public void AnimEndDespawn()
    {
        //디스폰 중이면 리턴
        if (isDespawn)
            return;

        // 마법 오브젝트 속도
        float duration = MagicDB.Instance.MagicDuration(magic);

        StartCoroutine(AutoDespawn(duration));
    }

    IEnumerator AutoDespawn(float duration)
    {
        //range 속성만큼 지속시간 부여
        yield return new WaitForSeconds(duration);

        //콜라이더 끄고 종료
        ColliderTrigger(false);

        if (!isDespawn)
            isDespawn = true;
        // 오브젝트 디스폰하기
        LeanPool.Despawn(transform);
    }
}
