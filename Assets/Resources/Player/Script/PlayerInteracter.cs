using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteracter : MonoBehaviour
{
    public List<Interacter> interacters = new List<Interacter>(); // 상호작용 가능한 오브젝트 리스트

    public Interacter nearInteracter = null; //현재 상호작용 가능한 오브젝트

    private void OnDisable()
    {
        // 비활성화시 상호작용 리스트 비우기
        interacters.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 상호작용 오브젝트 충돌시
        if (other.CompareTag(TagNameList.Object.ToString()) && other.TryGetComponent(out Interacter interacter))
        {
            //리스트에 넣기
            interacters.Add(interacter);

            // 가장 가까운 상호작용 개체 업데이트
            InteractCheck();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 상호작용 오브젝트 나가면
        if (other.CompareTag(TagNameList.Object.ToString()) && other.TryGetComponent(out Interacter interacter))
        {
            // 나간 오브젝트의 상호작용 트리거 취소 함수 콜백 실행하기
            if (interacter.interactTriggerCallback != null)
                interacter.interactTriggerCallback(false);

            //리스트에서 빼기
            interacters.Remove(interacter);

            // 가장 가까운 상호작용 개체 업데이트
            InteractCheck();
        }
    }

    void InteractCheck()
    {
        // 상호작용 가능한 개체 없을때
        if (interacters.Count == 0)
        {
            nearInteracter = null;

            return;
        }

        // 가장 가까운 상호작용 오브젝트와의 거리 초기화
        float nearDistance = float.PositiveInfinity;

        // 가장 가까운 상호작용 가능한 오브젝트 찾기
        for (int i = 0; i < interacters.Count; i++)
        {
            //오브젝트와의 거리 산출
            float distance = Vector2.Distance(interacters[i].transform.position, transform.position);

            // nearDistance 보다 더 가깝다면
            if (nearDistance > distance)
            {
                // 이전 오브젝트의 상호작용 트리거 취소 함수 콜백 실행하기
                if (interacters[i].interactTriggerCallback != null)
                    interacters[i].interactTriggerCallback(false);

                // 변수 갱신
                nearDistance = distance;

                // 가장 가까운 오브젝트도 갱신
                nearInteracter = interacters[i];
            }
        }

        // 상호작용 트리거 함수 콜백 실행하기
        if (nearInteracter.interactTriggerCallback != null)
            nearInteracter.interactTriggerCallback(true);
    }
}
