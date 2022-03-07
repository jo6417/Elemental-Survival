using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class MagicLevUpMenu : MonoBehaviour
{
    [Header("Refer")]
    // public CanvasRenderer canvasRenderer; //스탯 다각형 채우기
    // public UILineRenderer uiLineRenderer; //스탯 다각형 테두리
    public UIPolygon statPolygon;
    public Material material;
    public Texture2D texture2D;
    public GameObject statBtnParent;

    MagicInfo magic;
    //TODO 마법에서 6가지 스탯 int값 받아오기 (1~6)
    List<float> stats = new List<float>();
    List<float> statsBackup = new List<float>();
    public enum StatIndex { Power, Speed, Range, Critical, Pierce, Knockback }; //각 스탯의 인덱스 번호 enum
    public float power;
    public float speed;
    public float range;
    public float critical;
    
    public float pierce;
    public float knockback;
    Button[] statBtns; //모든 스탯 버튼들

    private void Awake()
    {
        //스탯 버튼 오브젝트 모두 찾기
        statBtns = statBtnParent.GetComponentsInChildren<Button>();

        stats.Add(power / 7f + 1f / 7f);
        stats.Add(speed / 7f + 1f / 7f);
        stats.Add(range / 7f + 1f / 7f);
        stats.Add(critical / 7f + 1f / 7f);
        stats.Add(pierce / 7f + 1f / 7f);
        stats.Add(knockback / 7f + 1f / 7f);
        stats.Add(power / 7f + 1f / 7f); //마지막값은 첫값과 같게

        statsBackup = stats.ConvertAll(x => x); //초기값 백업하기

        UpdateStats();
    }

    public void StatUp(GameObject Btn)
    {
        string statName = Btn.name;

        //백업값으로 초기화
        ResetStats();

        //해당 스탯 올리기
        stats[(int)Enum.Parse(typeof(StatIndex), statName)] += 1f / 7f;

        //스탯 다각형 모양 갱신
        UpdateStats();

        //TODO 올라간 마법 스탯 magic에 반영
        //TODO 팝업창 끄기
        //TODO 스크롤,자판기 등 이전 팝업창 끄기
    }

    public void ResetStats()
    {
        //백업값으로 초기화
        stats = statsBackup.ConvertAll(x => x);
        //스탯 다각형 모양 갱신
        UpdateStats();
    }

    void UpdateStats()
    {
        statPolygon.VerticesDistances = stats.ToArray();
        statPolygon.OnRebuildRequested();
    }

    void UpdateMesh()
    {
        Vector3 dirUp = Vector3.up * 233f;
        float angleAmount = 360f / 6; //회전 각도
        int verticeNum = 6; //꼭지점 개수

        // 메쉬 생성 6개 정점 찍고 연결
        Mesh mesh = new Mesh();

        Vector2[] linePoints = new Vector2[verticeNum + 1]; //테두리 꼭지점 벡터 배열
        Vector3[] vertices = new Vector3[verticeNum + 1];
        Vector2[] uv = new Vector2[verticeNum + 1];
        int[] triangles = new int[3 * verticeNum];

        vertices[0] = Vector3.zero;
        triangles[0] = 0;

        for (int i = 1; i < verticeNum + 1; i++)
        {
            //마법 능력치를 메쉬 포인트 벡터로 변환해서 넣기
            float multiple = (float)(stats[i - 1] + 1) / (float)(verticeNum + 1);
            vertices[i] = Quaternion.AngleAxis(-angleAmount * (i - 1), Vector3.forward) * dirUp * multiple;

            //라인렌더러 꼭지점도 똑같이 전달
            linePoints[i - 1] = (Vector2)vertices[i];
        }

        //마지막 꼭지점은 첫번째랑 동일
        linePoints[linePoints.Length - 1] = linePoints[0];

        uv[0] = Vector2.zero;
        uv[1] = Vector2.one;
        uv[2] = Vector2.one;
        uv[3] = Vector2.one;
        uv[4] = Vector2.one;
        uv[5] = Vector2.one;
        uv[6] = Vector2.one;

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        triangles[6] = 0;
        triangles[7] = 3;
        triangles[8] = 4;

        triangles[9] = 0;
        triangles[10] = 4;
        triangles[11] = 5;

        triangles[12] = 0;
        triangles[13] = 5;
        triangles[14] = 6;

        triangles[15] = 0;
        triangles[16] = 6;
        triangles[17] = 1;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        //라인렌더러에 테두리 포인트 전달
        // uiLineRenderer.Points =
        //  linePoints;

        // canvasRenderer.SetMesh(mesh);
        // canvasRenderer.SetMaterial(material, texture2D);

        //TODO 정점 위치 조정 0,0에서 위로 일정거리 이동한 벡터 / 6 * (1~6) * 각도60도 * (1~6)
    }

}
