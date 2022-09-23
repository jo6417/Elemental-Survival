using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : Character
{
    // public Animator animator;
    public Collider2D coll;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        //todo 체력 초기화
        hpMax = 30f;
        hpNow = hpMax;

        yield return null;
    }

    private void Start()
    {
        // animator = GetComponent<Animator>();
        // col = GetComponent<Collider2D>();
        // animator.SetBool("Open", false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // //플레이어가 상자에 닿았을때
        // if (other.CompareTag(SystemManager.TagNameList.Player.ToString()))
        // {
        //     col.enabled = false;

        //     //TODO 열린 상자 스프라이트로 바꾸기
        //     animator.SetBool("Open", true);

        //     //TODO 상자 팝업 UI 띄우기
        //     UIManager.Instance.PopupUI(UIManager.Instance.chestPanel);
        // }
    }
}