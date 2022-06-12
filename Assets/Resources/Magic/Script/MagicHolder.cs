using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicHolder : MonoBehaviour
{
    public MagicInfo magic; //보유한 마법 데이터
    public string magicName; //마법 이름 확인
    public Vector3 targetPos = default(Vector3); //목표 위치
    public enum Target { None, Enemy, Player };
    public Target target; //마법의 목표 타겟
    public float addDuration = 0f; // 마법 추가 유지 시간

    public float knockbackForce = 0; //넉백 파워
    public bool isStop; //정지 여부
    public float slowTime = 0; //슬로우 지속시간
    public float burnTime = 0; //화상 지속시간
    public float wetTime = 0; //젖음 지속시간
    public float bleedTime = 0; //출혈 지속시간
    public float electricTime = 0; //감전 지속시간
    public float freezeTime = 0; //빙결 지속시간

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //프리팹 이름으로 마법 정보 찾아 넣기
        if (magic == null)
            magic = MagicDB.Instance.GetMagicByName(transform.name.Split('_')[0]);
        magicName = magic.magicName;
    }

    public Target GetTarget()
    {
        return target;
    }

    public void SetTarget(Target changeTarget)
    {
        //입력된 타겟에 따라 오브젝트 태그 및 레이어 변경
        switch (changeTarget)
        {
            case Target.Enemy:
                transform.tag = "Magic";
                gameObject.layer = LayerMask.NameToLayer("Magic");
                break;

            case Target.Player:
                transform.tag = "EnemyAttack";
                gameObject.layer = LayerMask.NameToLayer("EnemyAttack");
                break;
        }

        //해당 마법의 타겟 변경
        target = changeTarget;
    }
}
