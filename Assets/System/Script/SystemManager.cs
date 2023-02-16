using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Experimental.Rendering.Universal;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using Pixeye.Unity;
using System;
using UnityEditor;
using UnityEngine.SceneManagement;

public enum MapElement { Earth, Fire, Life, Lightning, Water, Wind };
public enum TagNameList { Player, Enemy, Magic, Item, Object, Respawn, Obstacle };
public enum DBType { Magic, Enemy, Item };

[Serializable]
public class PhysicsLayerList
{
    // 물리 충돌 레이어
    public LayerMask PlayerPhysics_Mask;
    public LayerMask EnemyPhysics_Mask;
    public LayerMask PlayerHit_Mask;
    public LayerMask EnemyHit_Mask;
    public LayerMask PlayerAttack_Mask;
    public LayerMask EnemyAttack_Mask;
    public LayerMask AllAttack_Mask;
    public LayerMask Item_Mask;
    public LayerMask Object_Mask;

    public int PlayerPhysics_Layer { get { return LayerMask.NameToLayer("PlayerPhysics"); } }
    public int EnemyPhysics_Layer { get { return LayerMask.NameToLayer("EnemyPhysics"); } }
    public int PlayerHit_Layer { get { return LayerMask.NameToLayer("PlayerHit"); } }
    public int EnemyHit_Layer { get { return LayerMask.NameToLayer("EnemyHit"); } }
    public int PlayerAttack_Layer { get { return LayerMask.NameToLayer("PlayerAttack"); } }
    public int EnemyAttack_Layer { get { return LayerMask.NameToLayer("EnemyAttack"); } }
    public int AllAttack_Layer { get { return LayerMask.NameToLayer("AllAttack"); } }
    public int Item_Layer { get { return LayerMask.NameToLayer("Item"); } }
    public int Object_Layer { get { return LayerMask.NameToLayer("Object"); } }
}

public class SystemManager : MonoBehaviour
{
    public delegate void EnemyDeadCallback(Character character);
    public EnemyDeadCallback globalEnemyDeadCallback;

    #region Singleton
    private static SystemManager instance;
    public static SystemManager Instance
    {
        get
        {
            // if (instance == null)
            // {
            //     return null;
            //     var obj = FindObjectOfType<SystemManager>();
            //     if (obj != null)
            //     {
            //         instance = obj;
            //     }
            //     else
            //     {
            //         var newObj = new GameObject().AddComponent<SystemManager>();
            //         instance = newObj;
            //     }
            // }

            return instance;
        }
    }
    #endregion

    [Header("State")]
    public bool sceneChanging = false; // 씬 변경중 여부
    public bool loadDone = false; // 초기 로딩 완료 여부
    public float playerTimeScale = 1f; //플레이어만 사용하는 타임스케일
    public float globalTimeScale = 1f; //전역으로 사용하는 타임스케일
    public float time_start; //시작 시간
    public float time_current; // 현재 스테이지 플레이 타임
    public float modifyTime; //! 디버깅 시간 추가
    public int killCount; //몬스터 킬 수
    public float globalLightDefault = 0.9f; //글로벌 라이트 기본값

    public MapElement nowMapElement = MapElement.Earth; // 현재 맵 원소 속성
    public float[] elementWeitght = new float[6]; // 인벤토리의 마법 원소 가중치
    public List<float> gradeRate = new List<float>(); // 랜덤 등급 가중치

    [Header("Debug")]
    public TextMeshProUGUI nowSelectUI; //! 선택된 UI 이름
    public Button timeBtn; //! 시간 속도 토글 버튼
    public Button godModBtn; //! 갓모드 토글 버튼
    [ReadOnly] public bool godMod = false; //! 플레이어 갓모드 여부
    //! DB 동기화 버튼들
    public Button magicDBSyncBtn;
    public Button enemyDBSyncBtn;
    public Button itemDBSyncBtn;

    [Header("Tag&Layer")]
    public PhysicsLayerList layerList;

    [Header("Refer")]
    public NewInput System_Input; // 인풋 받기
    public GameObject saveIcon; //저장 아이콘
    public Sprite gateIcon; //포탈게이트 아이콘
    public Sprite questionMark; //물음표 스프라이트
    public GameObject targetPos_Red; // 디버그용 타겟 위치 표시
    public GameObject targetPos_Blue; // 디버그용 타겟 위치 표시

    [Header("DataBase")]
    public DBType dBType;

    [Header("Prefab")]
    public GameObject slowDebuffUI; // 캐릭터 머리위에 붙는 슬로우 디버프 아이콘
    public GameObject bleedDebuffUI; // 캐릭터 머리위에 붙는 출혈 디버프 아이콘
    public GameObject stunDebuffEffect; // 캐릭터 머리위에 붙는 스턴 디버프 이펙트
    public GameObject burnDebuffEffect; // 캐릭터 몸에 붙는 화상 디버프 이펙트
    public GameObject poisonDebuffEffect; // 캐릭터 몸에 붙는 포이즌 디버프 이펙트
    public GameObject shockDebuffEffect; // 캐릭터 몸에 붙는 감전 디버프 이펙트

    [Header("Material")]
    public Material spriteLitMat; //일반 스프라이트 Lit 머터리얼
    public Material spriteUnLitMat; //일반 스프라이트 unLit 머터리얼
    public Material outLineMat; //아웃라인 머터리얼
    public Material characterMat; // 캐릭터 머터리얼 (아웃라인, 틴트 컬러 적용)
    public Material hitMat; //맞았을때 단색 머터리얼
    public Material HDR3_Mat; // HDR 3 머터리얼
    public Material HDR5_Mat; // HDR 5 머터리얼
    public Material HDR10_Mat; // HDR 10 머터리얼
    public Material ghostHDRMat; //고스팅 마법 HDR 머터리얼
    public Material verticalFillMat; // Vertical Fill Sprite 머터리얼

    [Header("Color")]
    public Color stopColor; //시간 멈췄을때 컬러
    public Color hitColor; // 맞았을때 flash 컬러
    public Color healColor; // 체력 회복시 컬러
    public Color poisonColor; //독 데미지 flash 컬러
    public Color DeadColor; //죽을때 서서히 변할 컬러

    private void Awake()
    {
        // 시스템 인풋 초기화
        System_Input = new NewInput();
        System_Input.Enable();

        // 다른 오브젝트가 이미 있을때
        if (instance != null)
        {
            // 파괴 후 리턴
            Destroy(gameObject);
            return;
        }
        // 최초 생성 됬을때
        else
        {
            instance = this;

            // 파괴되지 않게 설정
            DontDestroyOnLoad(gameObject);
        }

        //초기화
        StartCoroutine(AwakeInit());
    }

    IEnumerator AwakeInit()
    {
        yield return null;

        //TODO 로딩 UI 띄우기
        print("로딩 시작");

        // 로컬 세이브 불러오기
        yield return StartCoroutine(SaveManager.Instance.LoadData());

        // 마법, 몬스터, 아이템 로컬DB 모두 불러오기
        StartCoroutine(MagicDB.Instance.GetMagicDB());
        StartCoroutine(ItemDB.Instance.GetItemDB());
        StartCoroutine(EnemyDB.Instance.GetEnemyDB());

        // 모든 DB 동기화 여부 확인
        StartCoroutine(SaveManager.Instance.DBSyncCheck(DBType.Magic, magicDBSyncBtn, "https://script.googleusercontent.com/macros/echo?user_content_key=7V2ZVIq0mlz0OyEVM8ULXo0nlLHXKPuUIJxFTqfLhj4Jsbg3SVZjnSH4X9KTiksN02j7LG8xCj8EgELL1uGWpX0Tg3k2TlLvm5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnD_xj3pGHBsYNBHTy1qMO9_iBmRB6zvsbPv4uu5dqbk-3wD3VcpY-YvftUimQsCyzKs3JAsCIlkQoFkByun7M-8F5ap6m-tpCA&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp"));
        StartCoroutine(SaveManager.Instance.DBSyncCheck(DBType.Item, itemDBSyncBtn, "https://script.googleusercontent.com/macros/echo?user_content_key=SFxUnXenFob7Vylyu7Y_v1klMlQl8nsSqvMYR4EBlwac7E1YN3SXAnzmp-rU-50oixSn5ncWtdnTdVhtI4nUZ9icvz8bgj6om5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnDd5HMKPhPTDYFVpd6ZAI5lT6Z1PRDVSUH9zEgYKrhfZq5_-qo0tdzwRz-NvpaavXaVjRCMLKUCBqV1xma9LvJ-ti_cY4IfTKw&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp"));
        StartCoroutine(SaveManager.Instance.DBSyncCheck(DBType.Enemy, enemyDBSyncBtn, "https://script.googleusercontent.com/macros/echo?user_content_key=6ZQ8sYLio20mP1B6THEMPzU6c7Ph6YYf0LUfc38pFGruRhf2CiPrtPUMnp3RV9wjWS5LUI11HGSiZodVQG0wgrSV-9f0c_yJm5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnKa-POu7wcFnA3wlQMYgM526Nnu0gbFAmuRW8zSVEVAU9_HiX_KJ3qEm4imXtAtA2I-6ud_s58xOj3-tedHHV_AcI_N4bm379g&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp"));

        // 갓모드 false 초기화
        // GodModeToggle();

        // 마법 DB 로딩 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);
        // 아이템 DB 로딩 대기
        yield return new WaitUntil(() => ItemDB.Instance.loadDone);
        // 몬스터 DB 로딩 대기
        yield return new WaitUntil(() => EnemyDB.Instance.loadDone);
        // 사운드 매니저 초기화 대기
        yield return new WaitUntil(() => SoundManager.Instance.initFinish);
        // 플레이어 초기화 대기
        // yield return new WaitUntil(() => PlayerManager.Instance.initFinish);

        //TODO 로딩 UI 끄기
        print("로딩 완료");
        loadDone = true;
    }

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 모든 로딩 끝날때까지 대기
        yield return new WaitUntil(() => loadDone);

        // // 시간 속도 초기화
        // TimeScaleChange(1f);
    }

    public Color HexToRGBA(string hex, float alpha = 1)
    {
        Color color;
        ColorUtility.TryParseHtmlString("#" + hex, out color);

        if (alpha != 1)
        {
            color.a = alpha;
        }

        return color;
    }

    public float GetVector2Dir(Vector2 to, Vector2 from)
    {
        // 타겟 방향
        Vector2 targetDir = to - from;

        // 플레이어 방향 2D 각도
        float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

        // 각도를 리턴
        return angle;
    }

    public float GetVector2Dir(Vector2 targetDir)
    {
        // 플레이어 방향 2D 각도
        float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

        // 각도를 리턴
        return angle;
    }

    public void TimeScaleToggle()
    {
        if (Time.timeScale > 0)
            TimeScaleChange(0f);
        else
            TimeScaleChange(1f);
    }

    public void TimeScaleChange(float timeScale, float fadeTime)
    {
        StartCoroutine(TimeScaleFadeChange(timeScale, fadeTime));
    }

    IEnumerator TimeScaleFadeChange(float timeScale, float fadeTime)
    {
        // 타임스케일 변화 적용 시간
        float timeCount = fadeTime;
        // 프레임당 시간값 계산
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(Time.unscaledDeltaTime);
        while (timeCount > 0)
        {
            TimeScaleChange(Mathf.MoveTowards(Time.timeScale, timeScale, Time.unscaledDeltaTime));

            timeCount -= Time.unscaledDeltaTime;

            yield return wait;
        }
    }

    public void TimeScaleChange(float timeScale)
    {
        // 씬 타임스케일 변경
        Time.timeScale = timeScale;

        Image timeImg = timeBtn.GetComponent<Image>();
        TextMeshProUGUI timeTxt = timeBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>();

        // 모든 오디오 소스 피치에 반영
        SoundManager.Instance.SoundTimeScale(timeScale, 0);

        // 멈추면 빨강 아니면 초록색으로 버튼에 표시
        if (timeScale > 0f)
            timeImg.color = Color.green;
        else
            timeImg.color = Color.red;

        // 시간 진행도 디버그 버튼에 표시
        timeTxt.text = "TimeSpeed = " + timeScale;
    }

    public void GodModeToggle()
    {
        Image godModImg = godModBtn.GetComponent<Image>();
        TextMeshProUGUI godModTxt = godModBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>();

        godMod = !godMod;

        if (godMod)
        {
            godModImg.color = Color.green;
            godModTxt.text = "GodMod On";
        }
        else
        {
            godModImg.color = Color.red;
            godModTxt.text = "GodMod Off";
        }
    }

    //오브젝트의 모든 자식을 제거
    public void DestroyAllChild(Transform obj)
    {
        Transform[] children = obj.GetComponentsInChildren<Transform>(true);
        //모든 자식 오브젝트 제거
        if (children != null)
            for (int j = 1; j < children.Length; j++)
            {
                if (children[j] != transform)
                {
                    Destroy(children[j].gameObject);
                }
            }
    }

    public SlotInfo SortInfo(SlotInfo slotInfo)
    {
        // 각각 마법 및 아이템으로 형변환
        MagicInfo magic = slotInfo as MagicInfo;
        ItemInfo item = slotInfo as ItemInfo;

        // null 이 아닌 정보를 반환
        if (magic != null)
            return magic;
        else if (item != null)
            return item;
        else
            return null;
    }

    public bool IsMagic(SlotInfo slotInfo)
    {
        // 각각 마법 및 아이템으로 형변환
        MagicInfo magic = slotInfo as MagicInfo;
        ItemInfo item = slotInfo as ItemInfo;

        // null 이 아닌 정보를 반환
        if (magic != null)
            return true;
        else if (item != null)
            return false;
        else
            return false;
    }

    public int WeightRandom(List<float> rateList)
    {
        // 아이템들의 가중치 총량 계산
        float totalRate = 0;
        foreach (var rate in rateList)
        {
            totalRate += rate;
        }

        // 0~1 사이 숫자에 가중치 총량을 곱해서 랜덤 숫자
        float randomNum = UnityEngine.Random.value * totalRate;

        // 랜덤 목록 개수만큼 반복
        for (int i = 0; i < rateList.Count; i++)
        {
            // 가중치가 0이면 넘기기
            if (rateList[i] == 0)
                continue;

            // 랜덤 숫자가 i번 가중치보다 작다면
            if (randomNum <= rateList[i])
            {
                // 해당 인덱스 반환
                return i;
            }
            else
            {
                // 랜덤 숫자에서 가중치 빼기
                randomNum -= rateList[i];
            }
        }

        // 아무것도 리턴 못했다면 -1을 리턴
        return -1;
    }

    // 중복 없이 인덱스 뽑기
    public List<int> RandomIndexes(int listNum, int getNum)
    {
        List<int> indexes = new List<int>();
        List<int> returnIndexes = new List<int>();

        // 모든 인덱스 넣어주기
        for (int i = 0; i < listNum; i++)
        {
            indexes.Add(i);
        }

        // 필요한 인덱스 수만큼 반복
        for (int i = 0; i < getNum; i++)
        {
            // 랜덤 인덱스 하나 뽑기
            int randomIndex = UnityEngine.Random.Range(0, indexes.Count);

            // 해당 인덱스를 리턴 리스트에 넣기
            returnIndexes.Add(indexes[randomIndex]);

            // 해당 인덱스를 인덱스 풀에서 삭제해 중복방지
            indexes.RemoveAt(randomIndex);
        }

        return returnIndexes;
    }

    public class LerpToPosition : MonoBehaviour
    {
        public Vector3 positionToMoveTo;
        void Start()
        {
            StartCoroutine(LerpPosition(positionToMoveTo, 5));
        }
        IEnumerator LerpPosition(Vector3 targetPosition, float duration)
        {
            float time = 0;
            Vector3 startPosition = transform.position;
            while (time < duration)
            {
                transform.position = Vector3.Lerp(startPosition, targetPosition, time / duration);
                time += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPosition;
        }
    }

    public void ToggleInput(bool UI_enable)
    {
        // UI 인풋 켤때
        if (UI_enable)
        {
            UIManager.Instance.UI_Input.Enable();
            PlayerManager.Instance.player_Input.Disable();
        }
        // 플레이어 인풋 켤때
        else
        {
            PlayerManager.Instance.player_Input.Enable();
            UIManager.Instance.UI_Input.Disable();
        }

        // 마우스 커서 전환
        UICursor.Instance.CursorChange(UI_enable);
    }

    // 화면에 프레임 레이트를 표시해 주는 함수
    float fps;
    private void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
        float msec = Time.deltaTime * 1000.0f;
        fps = 1.0f / Time.deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(rect, text, style);
    }

    public void GameQuit()
    {
        // dontDestroy 오브젝트 모두 파괴
        if (ObjectPool.Instance != null)
            Destroy(ObjectPool.Instance.gameObject); // 오브젝트 풀 파괴
        if (UIManager.Instance != null)
            Destroy(UIManager.Instance.gameObject); // UI 매니저 파괴
        if (PlayerManager.Instance != null)
            Destroy(PlayerManager.Instance.gameObject); // 플레이어 파괴
        if (CastMagic.Instance != null)
            Destroy(CastMagic.Instance.gameObject); // 핸드폰 파괴

        if (SoundManager.Instance.BGMCoroutine != null)
        {
            // 배경음 코루틴 끄기
            StopCoroutine(SoundManager.Instance.BGMCoroutine);
            // 배경음 정지
            SoundManager.Instance.nowBGM.Pause();
        }
    }

    IEnumerator LoadScene(string sceneName)
    {
        // 씬 변경 시작
        sceneChanging = true;

        // 마우스 커서 전환
        UICursor.Instance.CursorChange(true);

        // 화면 마스크로 덮기
        yield return StartCoroutine(Loading.Instance.SceneMask(true));

        // 종료 전 초기화
        GameQuit();

        // 매개변수로 들어온 씬 로딩 시작
        StartCoroutine(Loading.Instance.LoadScene(sceneName));
    }

    public void StartGame()
    {
        // 인게임 씬 켜기
        StartCoroutine(LoadScene("InGameScene"));
    }

    public void QuitMainMenu()
    {
        StartCoroutine(LoadScene("MainMenuScene"));
    }

    public void GameOverPanelOpen(bool isClear)
    {
        UIManager.Instance.gameoverPanel.GetComponent<GameoverMenu>().GameOver(isClear);
    }

    public void SoundPlay(string soundName)
    {
        // 버튼 선택 사운드 재생
        SoundManager.Instance.PlaySound(soundName);
    }
}
