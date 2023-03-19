using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAtkTrigger : MonoBehaviour
{
    public System.Action attackAction;
    // public AttackCallback attackCallback; // 공격시 실행될 콜백
    // public delegate void AttackCallback();
    public GameObject explosionPrefab;
    [SerializeField] bool showIndicator = true;
    [SerializeField] SpriteRenderer indicatorSprite;
    public SpriteRenderer atkRangeBackground;
    public SpriteRenderer atkRangeFill;
    public Character character;

    public bool atkTrigger; //범위내 타겟 들어왔는지 여부

    private void Awake()
    {
        //공격 범위 인디케이터 스프라이트 찾기
        // atkRangeBackground = GetComponent<SpriteRenderer>();

        // 캐릭터 찾기
        character = character != null ? character : GetComponentInChildren<Character>();
    }

    private void OnEnable()
    {
        if (atkRangeBackground)
            atkRangeBackground.enabled = false;
        if (atkRangeFill)
            atkRangeFill.enabled = false;

        //폭발 이펙트 있을때
        if (explosionPrefab)
        {
            if (transform.TryGetComponent(out CircleCollider2D circleColl))
            {
                //폭발 콜라이더 및 트랜스폼 사이즈 동기화
                explosionPrefab.GetComponent<CircleCollider2D>().radius = circleColl.radius;
                explosionPrefab.transform.localScale = transform.localScale;
            }
        }

        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return new WaitUntil(() => character.enemy != null);

        // 고스트 여부따라 트리거 레이어 바꾸기

        // 고스트일때
        if (character.IsGhost)
        {
            // 플레이어가 공격하는 레이어
            gameObject.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
        }
        // 일반 몹일때
        else
        {
            // 몬스터가 공격하는 레이어
            gameObject.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
        }

        // 인디케이터 비활성화
        if (showIndicator && indicatorSprite != null)
            indicatorSprite.enabled = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 공격 쿨타임 중일때 리턴
        if (character.atkCoolCount > 0)
            return;

        // 플레이어가 충돌하면
        if (other.CompareTag(TagNameList.Player.ToString()))
        {
            // 트리거 활성화
            atkTrigger = true;

            // 공격 실행
            AttackAction();

            // 인디케이터 활성화
            if (showIndicator && indicatorSprite != null)
                indicatorSprite.enabled = true;
        }

        // 몬스터가 충돌하면
        if (other.CompareTag(TagNameList.Enemy.ToString()))
        {
            // 몬스터가 충돌했을때 히트박스 있을때
            if (other.TryGetComponent(out HitBox hitBox))
            {
                // 충돌 대상이 본인이면 리턴
                if (hitBox.character == character)
                    return;

                // 고스트 여부가 서로 같을때 리턴
                if (character.IsGhost == hitBox.character.IsGhost)
                    return;
            }
            // 콜라이더가 히트박스를 갖고 있지 않을때 리턴
            else
                return;

            // 트리거 활성화
            atkTrigger = true;

            // 공격 실행
            AttackAction();

            // 인디케이터 활성화
            if (showIndicator && indicatorSprite != null)
                indicatorSprite.enabled = true;
        }
    }

    void AttackAction()
    {
        // 액션 실행
        if (attackAction != null)
            attackAction.Invoke();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 공격 트리거 꺼진 상태면 리턴
        if (!atkTrigger)
            return;

        // 고스트 아닐때, 플레이어가 나가면
        // 고스트일때, 몬스터가 나가면
        if ((!character.IsGhost && other.CompareTag(TagNameList.Player.ToString()))
        || (character.IsGhost && other.CompareTag(TagNameList.Enemy.ToString())))
        {
            // 트리거 비활성화
            atkTrigger = false;

            // 인디케이터 비활성화
            if (showIndicator && indicatorSprite != null)
                indicatorSprite.enabled = false;
        }
    }
}
