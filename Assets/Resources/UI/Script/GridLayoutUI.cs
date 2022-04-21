using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class GridLayoutUI : MonoBehaviour
{
    public Vector3[] corners = new Vector3[4];
    Vector3[] childCorners = new Vector3[4];
    Vector3[] obstacleCorners = new Vector3[4];

    public int PosY = 0;
    public Vector2 cellSize = new Vector2(100f, 100f);
    public Vector2 spacing;
    public enum StartConer { UpLeft, UpRight, DownLeft, DownRight }
    public StartConer startConer;
    public List<RectTransform> obstacles = new List<RectTransform>();

    private void OnGUI()
    {
        PosY = 0;

        // 해당 오브젝트 바운드
        RectTransform rect = transform.GetComponent<RectTransform>();
        //4방향 코너 구하기, 좌측아래부터 시계방향
        rect.GetWorldCorners(corners);

        // RectTransform 자식에서 모두 찾기
        List<RectTransform> rects = new List<RectTransform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            rects.Add(transform.GetChild(i).GetComponent<RectTransform>());
        }

        for (int i = 0; i < rects.Count; i++)
        {
            //자식 크기 변경
            rects[i].sizeDelta = cellSize;

            //첫번째 자식일때
            if (i == 0)
            {
                rects[i].anchoredPosition = Vector2.zero;

                //겹치면 옆으로 한칸 옮기기
                foreach (var obstacle in obstacles)
                {
                    CheckOverlap(obstacle, rects[i]);
                }

                continue;
            }

            //이전 아이템 위치
            Vector2 prePos = (Vector2)rects[i - 1].anchoredPosition;

            //이전 아이템 위치부터 오른쪽으로 한칸 이동
            rects[i].anchoredPosition =
            prePos
            + Vector2.right * cellSize.x
            + Vector2.right * spacing.x;

            //자식의 코너 월드 좌표 구하기
            rects[i].GetWorldCorners(childCorners);

            //X 좌표 바깥으로 벗어나면 Y좌표 한칸 올리기
            CheckOverFlowX(rects[i]);

            //겹치면 옆으로 한칸 옮기기
            foreach (var obstacle in obstacles)
            {
                // print(rects[i]);
                CheckOverlap(obstacle, rects[i]);
            }
        }
    }

    void CheckOverFlowX(RectTransform child)
    {
        // 오른쪽 바운드 벗어나면 y포지션 올리기, x 좌표 초기화
        if (childCorners[2].x > corners[2].x)
        {
            PosY++;

            // 다음 위치로 이동
            child.anchoredPosition =
            Vector2.zero
            + Vector2.up * cellSize.y * PosY
            + Vector2.up * spacing.y * PosY;
        }
    }

    void CheckOverlap(RectTransform obstacle, RectTransform child)
    {
        //자식의 코너 월드 좌표 구하기
        child.GetWorldCorners(childCorners);
        //장애물의 코너 월드 좌표
        obstacle.GetWorldCorners(obstacleCorners);

        Vector2 childMin = childCorners[0];
        Vector2 childMax = childCorners[2];

        Vector2 obsMin = obstacleCorners[0];
        Vector2 obsMax = obstacleCorners[2];

        foreach (var corner in obstacleCorners)
        {
            bool isOverlap = false;

            //x좌표상 child min,max 사이에 장애물 좌표가 있을때
            if ((obsMin.x >= childMin.x && obsMin.x <= childMax.x) ||
                (obsMax.x >= childMin.x && obsMax.x <= childMax.x))
            {
                //장애물 min,max Y값이 child보다 둘다 크거나 작거나 하면 안겹침, 넘기기
                if ((obsMin.y >= childMax.y && obsMax.y >= childMax.y) ||
                    (obsMin.y <= childMin.y && obsMax.y <= childMin.y))
                {
                    continue;
                }
                else
                {
                    //겹침
                    isOverlap = true;
                }
            }

            //y좌표상 child min,max 사이에 장애물 좌표가 있을때
            if ((obsMin.y >= childMin.y && obsMin.y <= childMax.y) ||
                (obsMax.y >= childMin.y && obsMax.y <= childMax.y))
            {
                //장애물 min,max X값이 child보다 둘다 크거나 작거나 하면 안겹침, 넘기기
                if ((obsMin.x >= childMax.x && obsMax.x >= childMax.x) ||
                    (obsMin.x <= childMin.x && obsMax.x <= childMin.x))
                {
                    continue;
                }
                else
                {
                    //겹침
                    isOverlap = true;
                }
            }

            //코너 하나가 장애물 min, max 사이에 있으면 겹친것으로 판단
            if (corner.x >= childMin.x &&
                corner.x <= childMax.x &&
                corner.y >= childMin.y &&
                corner.y <= childMax.y)
            {
                isOverlap = true;
            }

            if (obsMin.x <= childMin.x &&
                obsMax.x >= childMax.x &&
                obsMin.y <= childMin.y &&
                obsMax.y >= childMax.y)
            {
                isOverlap = true;
            }

            if (isOverlap)
            {
                print(child.name);
                
                //현재 위치로부터 오른쪽으로 한칸 이동
                child.anchoredPosition =
                child.anchoredPosition
                + Vector2.right * cellSize.x
                + Vector2.right * spacing.x;

                //자식의 코너 월드 좌표 구하기
                child.GetWorldCorners(childCorners);

                //X 좌표 바깥으로 벗어나면 Y좌표 한칸 올리기
                CheckOverFlowX(child);

                //그래도 겹치는지 한번 더 검사
                CheckOverlap(obstacle, child);
                break;
            }
        }
    }
}
