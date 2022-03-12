using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using DG.Tweening;
using UnityEngine.EventSystems;

public class MagicUpgradeMenu : MonoBehaviour
{
    [Header("Refer")]
    // public CanvasRenderer canvasRenderer; //스탯 다각형 채우기
    // public UILineRenderer uiLineRenderer; //스탯 다각형 테두리
    public UIPolygon statPolygon;
    public Material material;
    public Texture2D texture2D;
    public GameObject statBtnParent;
    public Text magicName;
    public Image magicIcon;
    public Image magicFrame;
    public GameObject spinBtn;
    public Sprite statBtnIdle; //스탯 버튼 기본 스프라이트
    public Sprite statBtnSelected; //스탯 버튼 선택됬을때 스프라이트

    [Header("Stat")]
    public MagicInfo magic;
    List<int> stats = new List<int>();
    List<int> statsBackup = new List<int>();
    public enum StatIndex { Power, Speed, Range, Critical, Pierce, projectile }; //각 스탯의 인덱스 번호 enum

    Vector2 originPopupScale;
    public Ease ease;
    public bool statCustom = false;
    public Button[] statBtns; //모든 스탯 버튼들
    public Image[] statBtnImages; //모든 스탯 버튼 스프라이트
    public int endSpinNum = 12; //최소 회전 횟수

    private void Awake()
    {
        // 팝업창 기본 사이즈 저장
        originPopupScale = transform.localScale;

        //스탯 버튼 오브젝트 모두 찾기
        statBtns = statBtnParent.GetComponentsInChildren<Button>();
        statBtnImages = statBtnParent.GetComponentsInChildren<Image>();

        // 아이템 없으면 스탯 커스텀 비활성화
        if (!statCustom)
            foreach (var btn in statBtns)
            {
                btn.GetComponent<EventTrigger>().enabled = false;
            }
    }

    private void OnEnable()
    {
        //스탯 버튼 이미지 초기화
        foreach (var img in statBtnImages)
        {
            img.sprite = statBtnIdle;
        }

        // 마법 모든 스탯 가져오기
        GetMagicStats();

        //초기값 백업하기
        statsBackup = stats.ConvertAll(x => x);
        //다각형 모양 갱신
        UpdateStats();

        // 팝업창 0,0에서 점점 커지면서 나타내기
        transform.localScale = Vector2.zero;
        transform.DOScale(originPopupScale, 0.5f)
        .SetUpdate(true)
        .SetEase(ease)
        .OnComplete(() =>
        {
            statPolygon.OnRebuildRequested();
        });

        // 룰렛 시작 버튼 상호작용 활성화
        spinBtn.GetComponent<Button>().interactable = true;
    }

    //수동 스탯 올리기
    public void StatUp(GameObject Btn)
    {
        string statName = Btn.name;

        //백업값으로 초기화
        ResetStats();

        //해당 스탯 올리기
        stats[(int)Enum.Parse(typeof(StatIndex), statName)]++;

        //스탯 다각형 모양 갱신
        UpdateStats();

        //TODO 올라간 마법 스탯 magic에 반영
        //TODO 팝업창 끄기
        //TODO 스크롤,자판기 등 이전 팝업창 끄기
    }

    public void StartSpin()
    {
        // 룰렛 시작 버튼 상호작용 비활성화
        spinBtn.GetComponent<Button>().interactable = false;

        // 랜덤 스탯 룰렛 시작
        StartCoroutine(SpinStat());
    }

    IEnumerator SpinStat()
    {
        int spinNum = 0;
        float rotateTime = 0.01f;
        int randomIndex = UnityEngine.Random.Range(0, 6);
        // print(randomIndex);
        int index = 0; //랜덤 선택되는 스탯 인덱스

        //랜덤 종료 시간값을 넘어서면 멈추기
        while (true)
        {
            //백업값으로 초기화
            stats = statsBackup.ConvertAll(x => x);

            //해당 스탯 올리기
            stats[index]++;

            //스탯 다각형 모양 갱신
            UpdateStats();

            //버튼 스프라이트 바꾸기
            statBtnImages[index].sprite = statBtnSelected;

            //제한시간 넘고 랜덤 인덱스가 됬을때 반복문 탈출
            if (spinNum > endSpinNum && index == randomIndex)
                break;

            rotateTime += 0.01f;
            yield return new WaitForSecondsRealtime(rotateTime);

            //버튼 스프라이트 바꾸기
            statBtnImages[index].sprite = statBtnIdle;

            //인덱스값 증가, 범위 넘어서면 0으로 초기화
            index++;
            index = index > 5 ? 0 : index;
            //회전 횟수 증가
            spinNum++;
        }

        // 선택된 스탯 버튼 깜빡이기
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            statBtnImages[index].sprite = statBtnIdle;
            yield return new WaitForSecondsRealtime(0.2f);
            statBtnImages[index].sprite = statBtnSelected;
        }
        yield return new WaitForSecondsRealtime(0.5f);

        // 마법의 선택된 스탯 올리기
        ApplyMagicStats();
    }

    public void ResetStats()
    {
        //백업값으로 초기화
        stats = statsBackup.ConvertAll(x => x);

        // 스탯값을 레이더 그래프 값으로 변형
        convertValue();

        //스탯 다각형 모양 갱신
        UpdateStats();
    }

    void UpdateStats()
    {
        statPolygon.VerticesDistances = convertValue().ToArray();
        statPolygon.OnRebuildRequested();
    }

    List<float> convertValue()
    {
        // 스탯값을 레이더 그래프 값으로 변형
        List<float> radarValue = new List<float>();
        foreach (var stat in stats)
        {
            radarValue.Add(stat / 7f + 1f / 7f);
        }

        return radarValue;
    }

    void GetMagicStats()
    {
        // 마법에서 6가지 스탯 int값 받아오기
        if (magic != null)
            stats.Clear();
        stats.Add(magic.power);
        stats.Add(magic.speed);
        stats.Add(magic.range);
        stats.Add(magic.critical);
        stats.Add(magic.pierce);
        stats.Add(magic.projectile);
        stats.Add(magic.power); //마지막값은 첫값과 같게

        // print(magic.power
        //     + " : " + magic.speed
        //     + " : " + magic.range
        //     + " : " + magic.critical
        //     + " : " + magic.pierce
        //     + " : " + magic.projectile
        // );
    }

    void ApplyMagicStats()
    {
        //마법 스탯 반영하기
        if (magic != null)
            magic.power = stats[0];
        magic.speed = stats[1];
        magic.range = stats[2];
        magic.critical = stats[3];
        magic.pierce = stats[4];
        magic.projectile = stats[5];

        // MagicInfo m = MagicDB.Instance.GetMagicByID(magic.id);
        // print(m.power
        //     + " : " + m.speed
        //     + " : " + m.range
        //     + " : " + m.critical
        //     + " : " + m.pierce
        //     + " : " + m.projectile
        // );

        // 마법 획득 및 언락
        PlayerManager.Instance.GetMagic(magic);

        //지불 원소젬 이름을 인덱스로 반환
        int gemTypeIndex = System.Array.FindIndex(MagicDB.Instance.elementNames, x => x == magic.priceType);
        // 가격 지불하기
        PlayerManager.Instance.PayGem(gemTypeIndex, magic.price);

        // 팝업 닫기
        UIManager.Instance.scrollMenu.SetActive(false);
        UIManager.Instance.vendMachineUI.SetActive(false);
        UIManager.Instance.PopupUI(UIManager.Instance.magicUpgradeUI);
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
