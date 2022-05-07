using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{
    public Animator animator;
    public Collider2D col;

    private void OnEnable()
    {
        //재활성화 됬을때 각 컴포넌트 초기화
        if (animator && col)
        {
            animator.SetBool("Open", false);
            col.enabled = true;
        }
    }

    private void Start()
    {
        // animator = GetComponent<Animator>();
        // col = GetComponent<Collider2D>();
        // animator.SetBool("Open", false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //플레이어가 상자에 닿았을때
        if (other.CompareTag("Player"))
        {
            col.enabled = false;

            //TODO 열린 상자 스프라이트로 바꾸기
            animator.SetBool("Open", true);

            //TODO 상자 팝업 UI 띄우기
            UIManager.Instance.PopupUI(UIManager.Instance.chestPanel);
        }
    }
}