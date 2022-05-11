using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ScreenBossAI : MonoBehaviour
{
    public NowState nowState;
    public enum NowState { Idle, SystemStop, Dead, Hit, TimeStop, Walk, Attack }

    EnemyInfo enemy;
    EnemyManager enemyManager;
    SpriteRenderer sprite;
    Collider2D coll;
    Animator anim;
    Rigidbody2D rigid;

    [SerializeField]
    GameObject fallAtkObj;

    SpriteRenderer fallSprite;
    Collider2D fallColl;

    float speed;

    private void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        coll = GetComponent<Collider2D>();
        anim = GetComponentInChildren<Animator>();
        rigid = GetComponentInChildren<Rigidbody2D>();

        fallSprite = fallAtkObj.GetComponent<SpriteRenderer>();
        fallColl = fallAtkObj.GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        sprite.color = Color.white; //스프라이트 색깔 초기화
        rigid.velocity = Vector2.zero; //속도 초기화

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (enemy == null)
            enemy = enemyManager.enemy;

        transform.DOKill();

        //애니메이션 스피드 초기화
        if (anim != null)
            anim.speed = 1f;

        // 위치 고정 해제
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

        //스피드 초기화
        speed = enemy.speed;

        // 콜라이더 충돌 초기화
        coll.isTrigger = false;
    }

    private void Update()
    {
        //TODO 쿨타임 후 랜덤으로 레이저 공격 or 엎어지기 공격 시작
        if (nowState == NowState.Walk)
        {
            //TODO 걷기 애니메이션 시작
            anim.SetBool("isWalk", true);
        }

    }

    void Walk()
    {
        nowState = NowState.Walk;

        //애니메이터 켜기
        if (anim != null && !anim.enabled)
            anim.enabled = true;

        //움직일 방향
        Vector2 dir = PlayerManager.Instance.transform.position - transform.position;

        //해당 방향으로 가속
        rigid.velocity = dir.normalized * speed * SystemManager.Instance.timeScale;

        //움직일 방향에따라 회전
        if (dir.x > 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }

    void FalldownAttack()
    {
        nowState = NowState.Attack;

        //공격 범위 오브젝트 활성화
        fallAtkObj.gameObject.SetActive(true);

        //TODO 엎어질 범위 활성화 및 반짝거리기
        fallSprite.enabled = true;
        fallSprite.DOColor(Color.white, 1f)
        .SetEase(Ease.Flash, 5, 1)
        .OnComplete(() =>
        {
            //TODO 엎어지는 애니메이션 시작
            anim.SetBool("isFallDown", true);
        });
    }

    void FallCollider()
    {
        StartCoroutine(ColliderCoroutine());
    }

    IEnumerator ColliderCoroutine()
    {
        // 엎어지는 공격 콜라이더 켰다 끄기
        fallColl.enabled = true;

        yield return new WaitForSeconds(0.2f);

        fallColl.enabled = false;
        
        fallSprite.enabled = false;
        fallAtkObj.gameObject.SetActive(false);
        anim.SetBool("isFallDown", false);
    }
}
