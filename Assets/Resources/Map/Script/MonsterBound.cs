using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterBound : MonoBehaviour
{
    public bool dragSwitch = true; //몬스터 반대편 이동 ON/OFF

    private void OnTriggerExit2D(Collider2D other)
    {
        // 스폰 콜라이더 밖으로 나가면 콜라이더 내부 반대편으로 보내기, 콜라이더 꺼진 경우 아닐때만
        if (other.CompareTag(TagNameList.Enemy.ToString())
        && other.gameObject.activeSelf && dragSwitch && other.enabled)
        {
            // print(other.gameObject.name);

            Character character = other.GetComponent<Character>();
            EnemyAI enemyAI = other.GetComponent<EnemyAI>();

            // 매니저가 없으면 몬스터 본체가 아니므로 리턴
            if (character == null)
                return;

            //죽은 몬스터는 미적용
            if (character.isDead)
                return;

            //이동 대기 카운트 초기화
            character.oppositeCount = 0.5f;

            //원래 부모 기억
            Transform originParent = other.transform.parent;

            //몹 스포너로 부모 지정
            other.transform.parent = transform;
            // 내부 포지션 역전 및 거리 추가
            other.transform.localPosition *= -0.8f;
            //원래 부모로 복귀
            other.transform.parent = originParent;
        }
    }
}
