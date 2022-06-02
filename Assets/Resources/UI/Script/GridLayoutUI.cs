using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class GridLayoutUI : MonoBehaviour
{
    int childNum = -1;
    private int PosY = 0;
    public bool isChanged = false; //자식 수정 여부

    Vector3[] corners = new Vector3[4];
    Vector3[] childCorners = new Vector3[4];
    Vector3[] obstacleCorners = new Vector3[4];

    RectTransform rect;
    public bool sizeFitter = false;
    public Vector2 cellSize = new Vector2(100f, 100f);
    public Vector2 spacing;
    public enum StartConer { UpLeft, UpRight, DownLeft, DownRight }
    public StartConer startConer;
    Vector2 startPos;
    public List<RectTransform> obstacles = new List<RectTransform>();

    private void OnGUI()
    {
        // 시작하면 자식 갯수 변수에 저장
        if (childNum == -1)
        {
            childNum = transform.childCount;
        }

        // 자식 개수 바뀌면 함수 실행
        if (childNum != transform.childCount && !isChanged)
        {
            //그리드 아이템 위치 업데이트
            if (transform.childCount > 0)
                GridUpdate();

            childNum = transform.childCount;
        }
    }

    private void Update()
    {
        //변경 명령 있으면 변경하기
        if (isChanged)
        {
            //그리드 아이템 위치 업데이트
            if (transform.childCount > 0)
                GridUpdate();

            isChanged = false;
        }
    }

    public void GridUpdate()
    {
        // print("grid update");

        PosY = 0;

        // 해당 오브젝트 바운드
        rect = transform.GetComponent<RectTransform>();
        //4방향 코너 구하기, 좌측아래부터 시계방향
        rect.GetWorldCorners(corners);

        // RectTransform 을 가진 자식 모두 찾기
        List<RectTransform> rects = new List<RectTransform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            rects.Add(transform.GetChild(i).GetComponent<RectTransform>());
        }

        for (int i = 0; i < rects.Count; i++)
        {
            //아이템 각각의 피벗 바꾸기
            if (startConer == StartConer.UpLeft)
            {
                rects[i].pivot = Vector2.up;
                rects[i].SetAnchor(AnchorPresets.TopLeft);
            }
            else if (startConer == StartConer.UpRight)
            {
                rects[i].pivot = Vector2.one;
                rects[i].SetAnchor(AnchorPresets.TopRight);
            }
            else if (startConer == StartConer.DownLeft)
            {
                rects[i].pivot = Vector2.zero;
                rects[i].SetAnchor(AnchorPresets.BottomLeft);
            }
            else if (startConer == StartConer.DownRight)
            {
                rects[i].pivot = Vector2.right;
                rects[i].SetAnchor(AnchorPresets.BottomRight);
            }

            //자식 크기 변경
            rects[i].sizeDelta = cellSize;

            //첫번째 자식일때
            if (i == 0)
            {
                // rects[i].position = startPos;
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

            Vector2 moveDir = Vector2.right;
            if (startConer == StartConer.UpLeft || startConer == StartConer.DownLeft)
                moveDir = Vector2.right;
            else if (startConer == StartConer.UpRight || startConer == StartConer.DownRight)
                moveDir = Vector2.left;

            //이전 아이템 위치부터 오른쪽으로 한칸 이동
            rects[i].anchoredPosition =
            prePos
            + moveDir * cellSize.x
            + moveDir * spacing.x;

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

        if (sizeFitter)
        {
            //그리드 전체 Y 사이즈 수정
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, cellSize.y * (PosY + 1));
        }

        //버튼 선택을 위해서 모든 자식 피벗 가운데로 바꾸기
        foreach (var rect in rects)
        {
            //피벗 바꾸기 전 위치 저장
            Vector2 savePos = rect.anchoredPosition;

            //피벗 수정
            rect.pivot = Vector2.one * 0.5f;

            //저장해둔 위치로부터 피벗 반대방향으로 절반만큼 옮기기
            if (startConer == StartConer.UpLeft)
                rect.anchoredPosition = savePos + new Vector2(cellSize.x / 2, -cellSize.y / 2);
            if (startConer == StartConer.UpRight)
                rect.anchoredPosition = savePos + new Vector2(-cellSize.x / 2, -cellSize.y / 2);
            if (startConer == StartConer.DownLeft)
                rect.anchoredPosition = savePos + new Vector2(cellSize.x / 2, cellSize.y / 2);
            if (startConer == StartConer.DownRight)
                rect.anchoredPosition = savePos + new Vector2(-cellSize.x / 2, cellSize.y / 2);
        }
    }

    void CheckOverFlowX(RectTransform child)
    {
        Vector2 nextDir = Vector2.down;
        // 시작지점이 위쪽이면 내리기
        if (startConer == StartConer.UpLeft || startConer == StartConer.UpRight)
        {
            nextDir = Vector2.down;
        }
        // 시작지점이 아래쪽이면 올리기
        else if (startConer == StartConer.DownLeft || startConer == StartConer.DownRight)
        {
            nextDir = Vector2.up;
        }

        // 오른쪽 바운드 벗어나면 y포지션 올리기, x 좌표 초기화
        if (startConer == StartConer.UpLeft || startConer == StartConer.DownLeft)
        {
            if (childCorners[2].x > corners[2].x)
            {
                PosY++;

                // 다음 위치로 이동
                child.anchoredPosition =
                Vector2.zero
                + nextDir * cellSize.y * PosY
                + nextDir * spacing.y * PosY;
            }
        }
        // 시작지점이 아래쪽이면 올리기
        else if (startConer == StartConer.UpRight || startConer == StartConer.DownRight)
        {
            if (childCorners[1].x < corners[1].x)
            {
                PosY++;

                // 다음 위치로 이동
                child.anchoredPosition =
                Vector2.zero
                + nextDir * cellSize.y * PosY
                + nextDir * spacing.y * PosY;
            }
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

            //해당 아이템이 무언가와 겹쳤을때
            if (isOverlap)
            {
                // print(child.name);

                Vector2 moveDir = Vector2.right;
                if (startConer == StartConer.UpLeft || startConer == StartConer.DownLeft)
                    moveDir = Vector2.right;
                else if (startConer == StartConer.UpRight || startConer == StartConer.DownRight)
                    moveDir = Vector2.left;

                //현재 위치로부터 오른쪽으로 한칸 이동
                child.anchoredPosition =
                child.anchoredPosition
                + moveDir * cellSize.x
                + moveDir * spacing.x;

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
