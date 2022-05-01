using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;

public class Ghosting : MonoBehaviour
{
    public MagicHolder magicHolder;
    public MagicInfo magic;
    public RectTransform fogCircle; //화면 덮을 원 오브젝트
    public Canvas fogCanvas;
    List<GameObject> enemyObjs;

    [Header("Stat")]
    float duration;
    float speed;

    private void Awake()
    {
        magicHolder = GetComponent<MagicHolder>();
        fogCanvas = GetComponentInChildren<Canvas>();
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

        VarManager.Instance.timeScale = 0f;

        //원 사이즈 줄이기
        fogCircle.localScale = Vector2.zero;

        //화면 대각선 반지름 길이 구하기
        float radius = Mathf.Sqrt(Mathf.Pow(fogCanvas.pixelRect.width, 2) + Mathf.Pow(fogCanvas.pixelRect.height, 2)) * 1.1f;
        // print(fogCanvas.pixelRect.width + ":" + fogCanvas.pixelRect.height + " = " + radius);

        //가로,세로 길이 적용
        fogCircle.sizeDelta = Vector2.one * radius;

        //존재하는 모든 적 찾기
        enemyObjs = GameObject.FindGameObjectsWithTag("Enemy").ToList();

        //모든 오브젝트 멈추기
        ToggleGhosting(true);

        //원 화면 크기만큼 키우기
        fogCircle.DOScale(Vector2.one, 1f);

        // 플레이어 하얗게 빛나고 투명하게
        PlayerManager.Instance.sprite.material = VarManager.Instance.outLineMat;
        PlayerManager.Instance.sprite.material.color = Color.cyan;
        PlayerManager.Instance.playerLight.enabled = true;

        // 플레이어 이동속도 버프
        VarManager.Instance.playerTimeScale = 2f;

        //적 스폰 멈추기
        EnemySpawn.Instance.spawnSwitch = false;

        //duration 만큼 대기
        yield return new WaitForSecondsRealtime(duration);

        fogCircle.DOScale(Vector2.zero, 1f)
        .OnComplete(() =>
        {
            // 플레이어 색깔 초기화
            PlayerManager.Instance.sprite.material = VarManager.Instance.spriteMat;
            PlayerManager.Instance.sprite.material.color = Color.white;
            PlayerManager.Instance.playerLight.enabled = false;

            //플레이어 및 전역 시간 속도 초기화
            VarManager.Instance.AllTimeScale(1f);

            //모든 오브젝트 속도 재개
            ToggleGhosting(false);

            //적 스폰 재개
            EnemySpawn.Instance.spawnSwitch = true;
        })
        .Play();
    }

    void ToggleGhosting(bool isStop)
    {
        if(enemyObjs.Count == 0)
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
                anim.speed = VarManager.Instance.timeScale;
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
}
