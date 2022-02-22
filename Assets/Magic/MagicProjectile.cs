using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class MagicProjectile : MonoBehaviour
{
    public int magicID = -1;
    Rigidbody2D rigid;
    float pierceNum = 0; //관통 횟수
    Vector3 lastPos; //오브젝트 마지막 위치

    private void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        pierceNum = MagicDB.Instance.GetMagicByID(magicID).pierceNum;
    }

    private void Update()
    {
        LookDirAngle();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //적에게 충돌
        if (other.CompareTag("Enemy"))
        {
            //남은 관통횟수 0일때 디스폰
            if (pierceNum == 0)
            {
                LeanPool.Despawn(transform);
            }
            else
            {
                pierceNum--;
            }
        }
    }

    void LookDirAngle()
    {
        // float angle = Mathf.Atan2(rigid.velocity.y, rigid.velocity.x);
        // transform.localEulerAngles = new Vector3(0, 0, (angle * 180) / Mathf.PI);

        // 날아가는 방향 바라보기
        if (transform.position != lastPos)
        {
            Vector3 returnDir = (transform.position - lastPos).normalized;
            float rotation = Mathf.Atan2(returnDir.y, returnDir.x) * Mathf.Rad2Deg;

            rigid.rotation = rotation - 90;
            lastPos = transform.position;
        }
    }


}
