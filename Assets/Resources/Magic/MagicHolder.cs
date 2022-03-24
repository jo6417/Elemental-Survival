using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicHolder : MonoBehaviour
{
    public MagicInfo magic; //보유한 마법 데이터

    public float knockbackForce = 0; //넉백 파워
    public float slowTime = 0; //슬로우 지속시간
    public float burnTime = 0; //화상 지속시간
    public float wetTime = 0; //젖음 지속시간
    public float bleedTime = 0; //출혈 지속시간
    public float electricTime = 0; //감전 지속시간
    public float freezeTime = 0; //빙결 지속시간

    private void OnEnable() {
        //초기화
        StartCoroutine(Initial());
    }

    IEnumerator Initial()
    {
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);
        
        //프리팹 이름으로 마법 정보 찾아 넣기
        if (magic == null)
            magic = MagicDB.Instance.GetMagicByName(transform.name.Split('_')[0]);
    }
}
