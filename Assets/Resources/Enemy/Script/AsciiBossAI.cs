using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TMPro;

public class AsciiBossAI : MonoBehaviour
{
    public NowState nowState;
    public enum NowState { Idle, SystemStop, TimeStop, Dead, Hit, Walk, Attack, Rest }

    [Header("Refer")]
    
    public TextMeshProUGUI faceText;
    EnemyManager enemyManager;
    Collider2D coll;
    Animator anim;
    Rigidbody2D rigid;
    public GameObject fallRangeObj;
    SpriteRenderer fallSprite;
    Collider2D fallColl;

    EnemyInfo enemy;
    float speed;
    float coolCount;
    [SerializeField]
    float followDistance = 5f; //해당 거리보다 멀면 따라가기

    private void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        coll = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();

        fallSprite = fallRangeObj.GetComponent<SpriteRenderer>();
        fallColl = fallRangeObj.GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        rigid.velocity = Vector2.zero; //속도 초기화

        //EnemyDB 로드 될때까지 대기
        yield return new WaitUntil(() => enemyManager.enemy != null);

        //프리팹 이름으로 아이템 정보 찾아 넣기
        if (enemy == null)
            enemy = enemyManager.enemy;

        //fall어택 콜라이더에 enemy 정보 넣어주기
        fallRangeObj.GetComponent<EnemyManager>().enemy = enemy;

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

        //공격범위 오브젝트 초기화
        fallRangeObj.SetActive(false);
        fallSprite.enabled = false;
        fallColl.enabled = false;
    }

    private void Update()
    {
        //일정 거리 이상 멀면 플레이어 따라가기
        if (Vector2.Distance(transform.position, PlayerManager.Instance.transform.position) >= followDistance)
        {
            // 대기 상태면 걷기 시작
            if (nowState == NowState.Idle)
            {
                nowState = NowState.Walk;

                // 걷기 애니메이션 시작
                anim.SetBool("isWalk", true);
            }
        }
        else
        {
            if (nowState == NowState.Walk)
            {
                // 걷기 애니메이션 끝내기
                anim.SetBool("isWalk", false);

                //가까워지면 idle 상태로 전환
                nowState = NowState.Idle;

                // Idle 애니메이션 시작
                StateIdle();
            }
        }


        if (nowState == NowState.Walk)
        {
            //플레이어 따라가기
            Walk();
        }
        else
        {
            //이동 멈추기
            rigid.velocity = Vector2.zero;
        }

        if (nowState == NowState.Idle)
        {
            // 공격 쿨타임 돌리기
            if (coolCount <= 0 && nowState != NowState.Attack)
            {
                nowState = NowState.Attack;

                Attack();
            }
            else
            {
                coolCount -= Time.deltaTime;
            }
        }
    }

    void Walk()
    {
        //걸을때 표정
        faceText.text = "● ▽ ●";

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

    void Attack()
    {
        nowState = NowState.Attack;

        // 범위 랜덤 쿨타임 입력
        coolCount = Random.Range(1f, 3f);

        //TODO fall 콜라이더에 플레이어 있으면 엎어지기 공격 시작
        FalldownAttack();
    }

    void FalldownAttack()
    {
        //당황하는 표정
        faceText.text = "◉ Д ◉";

        // 엎어질 준비 애니메이션 시작
        anim.SetTrigger("FallReady");

        //공격 범위 오브젝트 활성화
        fallRangeObj.SetActive(true);

        // 엎어질 범위 활성화 및 반짝거리기
        fallSprite.enabled = true;
        Color originColor = new Color(1, 0, 0, 0.3f);
        fallSprite.color = originColor;

        fallSprite.DOColor(new Color(1, 1, 1, 0.5f), 2f)
        .SetEase(Ease.InOutFlash, 5, 0)
        .OnComplete(() =>
        {
            fallSprite.color = originColor;

            //넘어질때 표정
            faceText.text = "> ︿ <";

            //엎어지는 애니메이션
            anim.SetBool("isFallAtk", true);
        });
    }

    void FallAtkCollider()
    {
        //일어서는 애니메이션 시작
        StartCoroutine(FallAtkCoroutine());
    }

    IEnumerator FallAtkCoroutine()
    {
        //일어날때 표정
        faceText.text = "x  _  x";

        // 엎어진채로 1초 대기
        yield return new WaitForSeconds(1f);

        // 일어서는 애니메이션 시작
        anim.SetBool("isFallAtk", false);

        // print("콜라이더 끄기 : " + Time.time);
    }

    void StateIdle()
    {
        nowState = NowState.Idle;

        //로딩중 텍스트 애니메이션
        StartCoroutine(LoadingText());
    }

    IEnumerator LoadingText()
    {
        string targetText = "...";

        faceText.text = "...";

        while (nowState == NowState.Idle)
        {
            if (faceText.text.Length < targetText.Length)
                faceText.text = targetText.Substring(0, faceText.text.Length + 1);
            else
                faceText.text = targetText.Substring(0, 0);

            yield return new WaitForSeconds(0.2f);
        }
    }
}
