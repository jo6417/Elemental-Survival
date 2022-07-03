using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;
using Lean.Pool;

public class Ghosting : MonoBehaviour
{
    public MagicHolder magicHolder;
    public MagicInfo magic;
    public RectTransform fogCircle; //화면 덮을 원 오브젝트
    public Canvas fogCanvas;
    List<GameObject> enemyObjs;

    [Header("Ghost")]
    float ghostCount = 0;
    public float ghostFrequency = 0.005f; //잔상 생성 주기
    public float ghostDuration = 0.5f; //잔상 유지 시간
    bool ghostSwitch = true;
    public Color ghostStartColor;
    Color ghostEndColor;

    [Header("Stat")]
    float duration;
    float speed;

    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
        fogCanvas = GetComponentInChildren<Canvas>();

        // ghostEndColor 초기화
        ghostEndColor = ghostStartColor;
        ghostEndColor.a = 0f;
    }

    private void OnEnable()
    {
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        yield return new WaitUntil(() => magicHolder.magic != null);
        magic = magicHolder.magic;
        duration = MagicDB.Instance.MagicDuration(magic); //시간정지 지속시간
        speed = MagicDB.Instance.MagicSpeed(magic, false); //스피드만큼 플레이어 속도감소 효과 반감됨

        //플레이어 위치로 이동
        transform.position = PlayerManager.Instance.transform.position;

        //잔상 생성 시작
        ghostSwitch = true;

        //시간 멈추기
        SystemManager.Instance.globalTimeScale = 0f;

        // 안개 사이즈 줄이기
        fogCircle.sizeDelta = Vector2.zero;
        // 안개 사이즈 키우기
        fogCircle.DOSizeDelta(Vector2.one * 1000f, 2f);

        //존재하는 모든 적 찾기
        enemyObjs = GameObject.FindGameObjectsWithTag("Enemy").ToList();

        //모든 오브젝트 멈추기
        ToggleGhosting(true);

        //플레이어 주변 라이트 켜기
        PlayerManager.Instance.playerLight.enabled = true;

        // 플레이어 머터리얼 바꾸기
        PlayerManager.Instance.sprite.material = SystemManager.Instance.ghostHDRMat;

        // 머터리얼 색깔 초기화
        PlayerManager.Instance.sprite.material.color = Color.clear;
        // 머터리얼 색깔 변경
        PlayerManager.Instance.sprite.material.DOColor(new Color(1, 0, 1, 0) * 5f, 2f);

        //적 스폰 멈추기
        EnemySpawn.Instance.spawnSwitch = false;

        // 플레이어 이동속도 버프
        SystemManager.Instance.playerTimeScale = 2f;
        //플레이어 이동속도 갱신
        PlayerManager.Instance.Move();

        //duration 만큼 대기
        yield return new WaitForSecondsRealtime(duration);

        //잔상 생성 멈추기
        ghostSwitch = false;

        // 머터리얼 색깔 천천히 돌아오기
        PlayerManager.Instance.sprite.material.DOColor(Color.clear, 2f)
        .OnComplete(() =>
        {
            //플레이어 주변 라이트 끄기
            PlayerManager.Instance.playerLight.enabled = false;

            // 플레이어 색깔 및 머터리얼 초기화
            PlayerManager.Instance.sprite.material = SystemManager.Instance.spriteLitMat;

            //플레이어 및 전역 시간 속도 초기화
            SystemManager.Instance.globalTimeScale = 1f;
        });

        fogCircle.DOSizeDelta(Vector2.zero, 2f)
        .OnComplete(() =>
        {
            //모든 오브젝트 속도 재개
            ToggleGhosting(false);

            //적 스폰 재개
            EnemySpawn.Instance.spawnSwitch = true;

            // 플레이어 이동속도 버프 해제
            SystemManager.Instance.playerTimeScale = 1f;
            //플레이어 이동속도 갱신
            PlayerManager.Instance.Move();

            //고스팅 오브젝트 디스폰
            LeanPool.Despawn(transform);
        })
        .Play();
    }

    void ToggleGhosting(bool isStop)
    {
        if (enemyObjs.Count == 0)
            return;

        //플레이어와 몬스터 레이어 충돌 무시
        if (isStop)
            Physics2D.IgnoreLayerCollision(PlayerManager.Instance.gameObject.layer, enemyObjs[0].layer, true);
        else
            Physics2D.IgnoreLayerCollision(PlayerManager.Instance.gameObject.layer, enemyObjs[0].layer, false);

        foreach (var enemy in enemyObjs)
        {
            //모든 오브젝트의 트윈 멈추기
            if (isStop)
                enemy.transform.DOPause();
            else
                enemy.transform.DOPlay();

            //애니메이터 멈추기 토글
            if (enemy.TryGetComponent(out Animator anim))
            {
                anim.speed = SystemManager.Instance.globalTimeScale;
            }

            //파티클 멈추기 토글
            if (enemy.TryGetComponent(out ParticleSystem particle))
            {
                if (isStop)
                    particle.Pause();
                else
                    particle.Play();
            }
        }
    }

    private void Update()
    {
        //잔상 남기기
        GhostTrail();
    }

    void GhostTrail()
    {
        if (ghostCount <= 0 && ghostSwitch)
        {
            //쿨타임 갱신
            ghostCount = ghostFrequency * PlayerManager.Instance.PlayerStat_Now.moveSpeed;

            StartCoroutine(GhostTransition());
        }
        else
        {
            ghostCount -= Time.deltaTime;
        }
    }

    IEnumerator GhostTransition()
    {
        //고스트 오브젝트 소환
        GameObject ghostObj = LeanPool.Spawn(SystemManager.Instance.ghostPrefab, PlayerManager.Instance.transform.position, PlayerManager.Instance.transform.rotation, SystemManager.Instance.effectPool);

        //스프라이트 렌더러 찾기
        SpriteRenderer ghostSprite = ghostObj.GetComponent<SpriteRenderer>();

        //플레이어 현재 스프라이트 넣기
        ghostSprite.sprite = PlayerManager.Instance.sprite.sprite;

        // 플레이어 레이어 넣기
        ghostSprite.sortingLayerID = PlayerManager.Instance.sprite.sortingLayerID;
        // 플레이어보다 한단계 낮게
        ghostSprite.sortingOrder = PlayerManager.Instance.sprite.sortingOrder - 1;

        //고스트 색 초기화
        ghostSprite.color = new Color(1, 1, 1, 150f / 255f);

        yield return new WaitForSeconds(ghostDuration / 3f);

        //고스트 색깔로 변경, 알파값 유지
        ghostSprite.DOColor(ghostStartColor, ghostDuration / 3f);

        yield return new WaitForSeconds(ghostDuration / 3f);

        //알파값 최저로 낮춰 없에기
        ghostSprite.DOColor(ghostEndColor, ghostDuration / 3f);

        yield return new WaitForSeconds(ghostDuration / 3f);

        LeanPool.Despawn(ghostObj);
    }
}
