using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoverTest : MonoBehaviour
{

    // private MagicInfo magic;
    // public MagicHolder magicHolder;
    // public Animator anim;
    // public Collider2D magicCol; //마법 데미지용 콜라이더
    Vector3 slowFollowPlayer;
    public float followSpeed = 5f;
    Vector3 spinOffset;
    Vector3 spinPos;

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Initial());
    }

    private void Update()
    {
        SpinMagic();
    }

    // void CoolTimeCount()
    // {
    //     if (cooltimeCounter <= 0)
    //     {
    //         // Attack 애니메이션 켜기
    //         anim.SetTrigger("Attack");

    //         //쿨타임 입력
    //         float coolTime = MagicDB.Instance.MagicCoolTime(magic);
    //         // print("coolTime : " + coolTime);
    //         cooltimeCounter = coolTime;
    //     }
    //     else
    //     {
    //         //쿨타임 카운트다운
    //         cooltimeCounter -= Time.deltaTime;
    //     }
    // }

    Vector3 SpinMagic()
    {
        // 중심점 벡터 slowFollowPlayer 가 플레이어 천천히 따라가기
        slowFollowPlayer = Vector3.Lerp(slowFollowPlayer, PlayerManager.Instance.transform.position, Time.deltaTime * followSpeed);

        // 중심점 기준으로 마법 오브젝트 위치 보정
        spinPos = slowFollowPlayer + spinOffset;

        // 중심점 기준 공전위치로 이동
        float speed = 1;
        transform.RotateAround(slowFollowPlayer, Vector3.back, speed);

        // 중심점 벡터 기준으로 오프셋 재설정
        spinOffset = spinPos - slowFollowPlayer;

        // transform.rotation = Quaternion.Euler(Vector3.zero);

        return spinPos;
    }

    // 마법 레벨업 할때 새로 초기화 하기
    IEnumerator Initial()
    {
        // yield return new WaitUntil(() => magicHolder.magic != null);
        // magic = magicHolder.magic;

        //플레이어와의 거리 보정
        float range = 3;
        spinPos = slowFollowPlayer + Vector3.up * range;
        spinOffset = spinPos - slowFollowPlayer;

        yield return null;
    }

    // public void ColliderOn()
    // {
    //     //콜라이더 켜기
    //     magicCol.enabled = true;
    // }

    // public void ColliderOff()
    // {
    //     //콜라이더 끄기
    //     magicCol.enabled = false;

    //     // Idle 애니메이션 켜기
    //     anim.SetTrigger("Idle");
    // }
}
