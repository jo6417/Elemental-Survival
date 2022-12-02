using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

public class Vacuum_AI : MonoBehaviour
{
    [Header("Refer")]
    [SerializeField] Character character;
    [SerializeField] EnemyAtkTrigger enemyAtkTrigger;
    [SerializeField] Collider2D atkColl;
    [SerializeField] GameObject dashEffect;
    // [SerializeField] Collider2D absorbColl;

    [Header("State")]
    public float absorbSpeed = 1f; //흡수 속도
    public float getRange; //아이템 획득 범위
    [SerializeField] float nowGem; // 현재 젬 획득량
    [SerializeField] float maxGem; // 젬 획득량 최대치
    [SerializeField] Color fillColor; // 젬 획득량 표시 컬러
    [SerializeField] SpriteRenderer fillAmount; // 젬 획득량 표시
    [SerializeField] SpriteRenderer pulseLight;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 사운드 매니저 초기화 대기
        yield return new WaitUntil(() => SoundManager.Instance.init);

        //todo 청소기 사운드 무한 반복 재생
        // SoundManager.Instance.PlaySound("Vacuum_Suck", chracter.transform, 0.1f, 0, -1, true);

        // 젬 보유량 표시 컬러 갱신
        fillAmount.material.SetColor("Tint", fillColor);
        // 젬 보유량 초기화
        fillAmount.material.SetFloat("_Arc2", 360f);

        // 펄스 라이트 밝기 초기화
        Color lightColor = new Color(1, 0, 0, 20f / 255f);
        pulseLight.color = lightColor;

        // 펄스 라이트 반짝이기
        lightColor.a = 100f / 255f;
        pulseLight.DOColor(lightColor, 1f)
        .SetLoops(-1, LoopType.Yoyo);

        //todo 공격 함수 초기화
        if (enemyAtkTrigger.attackAction == null)
            enemyAtkTrigger.attackAction += Dash;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 고스트면 아이템 흡수 하지않음
        if (character.IsGhost)
            return;

        // 최대치까지 획득했으면 리턴
        if (nowGem >= maxGem)
            return;

        //아이템과 충돌 했을때
        if (other.CompareTag(SystemManager.TagNameList.Item.ToString()))
        {
            ItemManager itemManager = other.GetComponent<ItemManager>();
            ItemInfo item = itemManager.itemInfo;

            // 아이템이 젬타입 아니면 리턴
            if (item == null || item.itemType != ItemDB.ItemType.Gem.ToString())
            {
                print(other.name + " : " + other.transform.position);
                return;
            }

            Rigidbody2D rigid = other.GetComponent<Rigidbody2D>();

            //해당 아이템 획득 여부 갱신, 중복 획득 방지
            // itemManager.coll.enabled = false;

            // 자동 디스폰 중지, 색깔 초기화
            itemManager.sprite.DOKill();

            // 흡수 범위내에 들어오면 획득해서 소지 아이템에 포함
            if (Vector2.Distance(transform.position, other.transform.position) <= getRange)
            {
                // 젬 보유량 올리기
                if (nowGem < maxGem)
                    nowGem++;

                // 현재 젬 보유량 표시
                float fill = ((maxGem - nowGem) / maxGem) * 360f;

                // 머터리얼 fill 값 갱신
                fillAmount.material.SetFloat("_Arc2", fill);

                //보유 아이템중 해당 아이템 있는지 찾기
                ItemInfo findItem = character.nowHasItem.Find(x => x.id == item.id);
                // 해당 아이템 보유하지 않았을때
                if (findItem == null)
                {
                    //개수 1개로 초기화
                    item.amount = 1;

                    //해당 아이템 획득
                    character.nowHasItem.Add(item);
                }
                // 해당 아이템 이미 보유했을때
                else
                {
                    // 보유한 아이템에 개수 증가
                    findItem.amount++;
                }

                //아이템 속도 초기화
                rigid.velocity = Vector2.zero;
                LeanPool.Despawn(other);

                // 획득 사운드 재생
                SoundManager.Instance.PlaySound("Vacuum_Absorb", transform.position);

                return;
            }

            //아이템 움직일 방향
            Vector2 dir = transform.position - other.transform.position;

            // 가까이 끌어들이기
            rigid.AddForce(dir.normalized * Time.deltaTime * absorbSpeed);
        }
    }

    void Dash()
    {
        // 현재 공격중 아니면
        if (character.nowState != Character.State.Attack)
            StartCoroutine(DashAttack());
    }

    public IEnumerator DashAttack()
    {
        // 공격 액션으로 전환
        character.nowState = Character.State.Attack;

        // 밀리지 않게 kinematic으로 전환
        // chracter.rigid.bodyType = RigidbodyType2D.Kinematic;

        //플레이어 방향 다시 계산
        Vector2 targetDir = character.TargetObj.transform.position - transform.position;

        //움직일 방향에따라 회전
        float leftAngle = character.lookLeft ? 180f : 0f;
        float rightAngle = character.lookLeft ? 0f : 180f;
        if (targetDir.x > 0)
            character.transform.rotation = Quaternion.Euler(0, leftAngle, 0);
        else
            character.transform.rotation = Quaternion.Euler(0, rightAngle, 0);

        // 돌진 시작 인디케이터 켜기
        dashEffect.SetActive(true);

        WaitForSeconds delta = new WaitForSeconds(Time.deltaTime);

        // 해당 시간동안 속도 유지
        float atkCount = 1f;
        while (atkCount > 0)
        {
            // 타겟 방향 반대로 살짝 이동
            character.rigid.velocity = -targetDir.normalized * 3f;

            // 시간 차감하고 대기
            atkCount -= Time.deltaTime;
            yield return delta;
        }

        // character.transform.DOMove((Vector2)character.transform.position - targetDir.normalized, 1f);
        // yield return new WaitForSeconds(1f);

        // rigid 타입 전환
        character.rigid.bodyType = RigidbodyType2D.Dynamic;

        //공격 콜라이더 켜기
        atkColl.enabled = true;

        // 해당 시간동안 속도 유지
        atkCount = 0.5f;
        while (atkCount > 0)
        {
            // 타겟 방향으로 돌진
            character.rigid.velocity = targetDir.normalized * 20f;

            // 시간 차감하고 대기
            atkCount -= Time.deltaTime;
            yield return delta;
        }

        // chracter.transform.DOMove(transform.position + targetDir.normalized * 5f, 0.5f);        
        // yield return new WaitForSeconds(0.5f);

        // 속도 멈추기
        character.rigid.velocity = Vector3.zero;

        //공격 콜라이더 끄기
        atkColl.enabled = false;

        // 타겟 위치 추적 시간 초기화
        character.targetResetCount = 0f;

        // 쿨타임만큼 대기후 초기화
        yield return new WaitForSeconds(character.cooltimeNow / character.enemy.cooltime);
        // Idle로 전환
        character.nowState = Character.State.Idle;
    }
}
