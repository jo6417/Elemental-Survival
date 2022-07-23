using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeMaker : MonoBehaviour
{
    public int linkNum = 5;
    public float linkDistance = 1f;
    public float linkGravityScale = 10f;
    public List<Transform> links = new List<Transform>();
    public LineRenderer lineRenderer;
    public GameObject ropeLinkPrefab;

    public bool drawLine = false;

    private void OnEnable()
    {
        // 링크 전부 찾아 추가하기
        links.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.Contains("Link"))
                links.Add(child);
        }
    }

    private void Update()
    {
        if (drawLine)
        {
            for (int i = 0; i < links.Count; i++)
            {
                //각 링크의 포지션을 포인트로 라인 그리기
                lineRenderer.SetPosition(i, links[i].localPosition);
            }
        }
    }

    [ContextMenu("CreateJoints")]
    public void CreateJoints()
    {
        Rigidbody2D preRigid = null;

        for (int i = 0; i < transform.childCount; i++)
        {
            if (preRigid != null)
                transform.GetChild(i).GetComponent<HingeJoint2D>().connectedBody = preRigid;

            preRigid = transform.GetChild(i).GetComponent<Rigidbody2D>();
        }
    }

    [ContextMenu("CreateRope")]
    public void CreateRope()
    {
        // //자식 비우기
        // for (int i = 0; i < transform.childCount; i++)
        // {
        //     DestroyImmediate(transform.GetChild(i).gameObject);
        // }

        // 이전 rigidbody 기억하기
        Rigidbody2D preRigid = null;

        //라인 렌더러 없으면 컴포넌트 추가
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        // 라인렌더러 포인트 개수 초기화
        lineRenderer.positionCount = linkNum;

        // 링크 수+2 만큼 오브젝트 생성
        for (int i = 0; i < linkNum + 2; i++)
        {
            GameObject linkObj = null;

            // 첫번째거나, 마지막일때
            if (i == 0 || i == linkNum + 1)
                linkObj = Instantiate(ropeLinkPrefab, transform.position, Quaternion.identity);
            else
                // 자식으로 넣기
                linkObj = Instantiate(ropeLinkPrefab, transform.position, Quaternion.identity, transform);


            // rigidbody2d, hingejoint2d 붙여주기
            Rigidbody2D rigid = linkObj.GetComponent<Rigidbody2D>();
            HingeJoint2D hinge = linkObj.GetComponent<HingeJoint2D>();

            // 첫번째일때
            if (i == 0)
            {
                linkObj.name = "StartPoint";

                rigid.bodyType = RigidbodyType2D.Kinematic;

                //링크 위치로 이동
                Vector3 pos = new Vector3(0, 0, 0);
                linkObj.transform.localPosition = pos;
            }
            // 마지막일때
            else if (i == linkNum + 1)
            {
                linkObj.name = "LastPoint";

                rigid.bodyType = RigidbodyType2D.Kinematic;

                //링크 위치로 이동
                Vector3 pos = new Vector3(0, (linkNum - 1) * linkDistance, 0);
                linkObj.transform.localPosition = pos;
            }
            // 그외 나머지일때
            else
            {
                linkObj.name = "Link_" + i;

                //링크 위치로 이동
                Vector3 pos = new Vector3(0, (i - 1) * linkDistance, 0);
                linkObj.transform.localPosition = pos;

                //라인 렌더러 포인트 넣기
                lineRenderer.SetPosition(i - 1, pos);

                // 오브젝트 리스트에 넣기
                links.Add(linkObj.transform);

                rigid.gravityScale = linkGravityScale;
            }

            // 모든 링크 조인트 붙이기
            if (preRigid != null)
                hinge.connectedBody = preRigid;

            // 마지막 포인트는 자동 위치 기입 풀기
            if (i == linkNum + 1)
                hinge.autoConfigureConnectedAnchor = false;

            preRigid = rigid;
        }
    }
}
