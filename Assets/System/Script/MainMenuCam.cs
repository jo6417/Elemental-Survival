using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MainMenuCam : MonoBehaviour
{
    [SerializeField] Transform mapPos;
    [SerializeField] Transform mapList;
    [SerializeField] float moveTime = 5f; // 이동 시간
    int[] mapIndexs = { 0, 1, 2, 3, 5, 6, 7, 8 }; // 맵 인덱스들, 가운데 4를 제외

    private void OnEnable()
    {
        // 8개중에 맵 하나 뽑기
        int randomIndex = Random.Range(0, mapIndexs.Length);

        // 이동할 위치
        Vector3 movePos = mapList.GetChild(mapIndexs[randomIndex]).position;
        // z축은 그대로
        movePos.z = transform.position.z;

        // 해당 맵 위치로 이동
        transform.DOMove(movePos, moveTime)
        .SetLoops(-1, LoopType.Incremental)
        .SetEase(Ease.Linear)
        .OnStart(() =>
        {
            // 시작할때 바닥 위치 초기화
            mapPos.position = new Vector3(transform.position.x, transform.position.y, 0);
        })
        .OnStepComplete(() =>
        {
            // 이동 완료후 바닥을 옮기기
            mapPos.position = new Vector3(transform.position.x, transform.position.y, 0);
        });
    }
}
