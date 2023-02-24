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
            if (instance == null)
            {
                var obj = FindObjectOfType<SystemManager>();
                if (obj != null)
                {
                    instance = obj;
                }
            }
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
    public bool isFullscreen = false;
    public Vector2 lastResolution = new Vector2(1920f, 1080f); // 해상도 저장

    public MapElement nowMapElement = MapElement.Earth; // 현재 맵 원소 속성
    public float[] elementWeitght = new float[6]; // 인벤토리의 마법 원소 가중치
    public List<float> gradeRate = new List<float>(); // 랜덤 등급 가중치

    [Header("Debug")]
    public TextMeshProUGUI nowSelectUI; // 선택된 UI 이름
    public Button timeBtn; // 시간 속도 토글 버튼
    public Button godModBtn; // 갓모드 토글 버튼
    [ReadOnly] public bool godMod = false; // 플레이어 갓모드 여부
    // DB 동기화 버튼들
    public Button magicDBSyncBtn;
    public Button enemyDBSyncBtn;
    public Button itemDBSyncBtn;
    float frameRateCount = 0;
    [SerializeField] Transform consoleMsgList; // 콘솔 메시지 담을 부모 오브젝트
    [SerializeField] Transform consoleMsgText; // 콘솔 메시지 프리팹
    private List<string> logMessages = new List<string>(); // 로그 메시지를 저장할 리스트
    private bool isLogChanged = false; // 로그 메시지 변경 여부를 나타내는 플래그
    [SerializeField] Button spawnBtn; // 몬스터 자동 스폰 버튼
    [SerializeField] Button showStateBtn; // 몬스터 상태 디버깅 토글 버튼
    public bool spawnSwitch; //몬스터 스폰 여부
    public bool showEnemyState = false; // 몬스터 상태 디버깅 여부

    [Header("Tag&Layer")]
    public PhysicsLayerList layerList;

    [Header("Refer")]
    public NewInput System_Input; // 인풋 받기
    public GameObject saveIcon; //저장 아이콘
    public Sprite gateIcon; //포탈게이트 아이콘
    public Sprite questionMark; //물음표 스프라이트
    public GameObject targetPos_Red; // 디버그용 타겟 위치 표시
    public GameObject targetPos_Blue; // 디버그용 타겟 위치 표시
    public FixResolution fixResolution;

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
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        //초기화
        StartCoroutine(AwakeInit());
    }

    IEnumerator AwakeInit()
    {
        // 각각 버튼 색깔 초기화
        ToggleSwitcher(ref spawnSwitch, spawnBtn, true);
        ToggleSwitcher(ref showEnemyState, showStateBtn, true);

        // 몬스터 자동 스폰 버튼 상태값 연결
        spawnBtn.onClick.AddListener(() => { ToggleSwitcher(ref spawnSwitch, spawnBtn); });
        // 몬스터 상태 보여주기 버튼 상태값 연결
        showStateBtn.onClick.AddListener(() => { ToggleSwitcher(ref showEnemyState, showStateBtn); });

        //TODO 로딩 UI 띄우기
        print("로딩 시작");

        // 로컬 세이브 불러오기
        yield return StartCoroutine(SaveManager.Instance.LoadData());

        // 모든 DB 웹에서 불러와 로컬에 넣기
        MagicDB.Instance.MagicDBSynchronize(false);
        ItemDB.Instance.ItemDBSynchronize(false);
        EnemyDB.Instance.EnemyDBSynchronize(false);

        // 마법 DB 로딩 대기
        yield return new WaitUntil(() => MagicDB.Instance.loadDone);
        // 아이템 DB 로딩 대기
        yield return new WaitUntil(() => ItemDB.Instance.loadDone);
        // 몬스터 DB 로딩 대기
        yield return new WaitUntil(() => EnemyDB.Instance.loadDone);

        // 수정된 로컬 세이브데이터를 저장, 완료시까지 대기
        yield return StartCoroutine(SaveManager.Instance.Save());

        // DB 전부 Enum으로 바꿔서 저장
        yield return StartCoroutine(SaveManager.Instance.DBtoEnum());

        // 동기화 여부 다시 검사
        StartCoroutine(SaveManager.Instance.DBSyncCheck(DBType.Magic, SystemManager.Instance.enemyDBSyncBtn));
        StartCoroutine(SaveManager.Instance.DBSyncCheck(DBType.Item, SystemManager.Instance.enemyDBSyncBtn));
        StartCoroutine(SaveManager.Instance.DBSyncCheck(DBType.Enemy, SystemManager.Instance.enemyDBSyncBtn));

        // 사운드 매니저 초기화 대기
        yield return new WaitUntil(() => SoundManager.Instance.initFinish);
        // 플레이어 초기화 대기
        // yield return new WaitUntil(() => PlayerManager.Instance.initFinish);

        //TODO 로딩 UI 끄기
        print("로딩 완료");
        loadDone = true;
    }

    private void Start()
    {
        // 로그 업데이트 코루틴 실행
        StartCoroutine(UpdateLogMessages());
    }

    private void OnEnable()
    {
        StartCoroutine(Init());

        // 메시지 함수 시작
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        // 메시지 함수 빼기
        Application.logMessageReceived -= HandleLog;
    }

    #region Debugging
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // 로그 메시지 추가
        logMessages.Add(logString);
        // 로그 플래그 변경됨
        isLogChanged = true;
    }

    private IEnumerator UpdateLogMessages()
    {
        while (true)
        {
            // 로그 메시지 변경 여부 확인
            if (isLogChanged)
            {
                // 새로운 UI 오브젝트 생성하여 메시지 출력
                foreach (string logMessage in logMessages)
                {
                    Transform consoleLog = LeanPool.Spawn(consoleMsgText, consoleMsgList);
                    TextMeshProUGUI messageText = consoleLog.GetComponentInChildren<TextMeshProUGUI>();
                    messageText.text = logMessage;
                }

                // 로그 메시지 초기화
                logMessages.Clear();
                isLogChanged = false;
            }

            // 잠시 대기
            yield return new WaitForEndOfFrame();
        }
    }

    public void ToggleObject()
    {
        // 버튼 오브젝트 찾기
        GameObject buttonObject = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;

        // 버튼 오브젝트의 자식 오브젝트를 찾아서 활성화/비활성화
        Transform childTransform = buttonObject.transform.GetChild(0);
        if (childTransform != null)
            childTransform.gameObject.SetActive(!childTransform.gameObject.activeSelf);
    }
    #endregion

    IEnumerator Init()
    {
        // 모든 로딩 끝날때까지 대기
        yield return new WaitUntil(() => loadDone);

        // 켜졌을때 초록, 꺼졌을때 빨강으로 버튼 컬러 바꾸기
        showStateBtn.image.color = showEnemyState ? Color.green : Color.red;
    }

    public void ToggleSwitcher(ref bool toggle, Selectable selectable, bool init = false)
    {
        // 초기화 아닐때
        if (!init)
            // 해당 bool값 전환
            toggle = !toggle;

        // 켜졌을때 초록, 꺼졌을때 빨강으로 버튼 컬러 바꾸기
        selectable.image.color = toggle ? Color.green : Color.red;
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

    public void TimeScaleChange(float timeScale, bool soundScaling = true)
    {
        // 씬 타임스케일 변경
        Time.timeScale = timeScale;

        Image timeImg = timeBtn.GetComponent<Image>();
        TextMeshProUGUI timeTxt = timeBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>();

        // 재생중인 사운드도 끌때
        if (soundScaling)
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

    //오브젝트의 모든 자식을 파괴
    public void DestroyAllChild(Transform obj)
    {
        Transform[] children = obj.GetComponentsInChildren<Transform>(true);
        //모든 자식 오브젝트 파괴
        if (children != null)
            for (int j = 1; j < children.Length; j++)
                if (children[j] != transform)
                    Destroy(children[j].gameObject);
    }

    //오브젝트의 모든 자식을 디스폰
    public void DespawnAllChild(Transform obj)
    {
        Transform[] children = obj.GetComponentsInChildren<Transform>(true);
        //모든 자식 오브젝트 디스폰
        if (children != null)
            for (int j = 1; j < children.Length; j++)
                if (children[j] != transform)
                    LeanPool.Despawn(children[j]);
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
        // 1초마다 실행
        if (frameRateCount - Time.time > 1f)
        {
            // 프레임 갱신 시간 업데이트
            frameRateCount = Time.time;

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

    public void ToggleFullScreen(bool _isFullscreen)
    {
        // 해상도 및 레터박스 갱신
        fixResolution.ChangeResolution(_isFullscreen, true);
    }

    public Vector2 AntualSpriteScale(SpriteRenderer spriteRenderer)
    {
        // SpriteRenderer에서 Sprite 가져오기
        Sprite sprite = spriteRenderer.sprite;

        // Sprite의 텍스처 크기 가져오기
        Texture2D texture = sprite.texture;
        int textureWidth = texture.width;
        int textureHeight = texture.height;

        // SpriteRenderer의 Sprite가 차지하는 영역 크기 가져오기
        Bounds spriteBounds = sprite.bounds;
        float spriteWidth = spriteBounds.size.x;
        float spriteHeight = spriteBounds.size.y;

        // SpriteRenderer의 Transform 스케일 값 가져오기
        Vector3 localScale = transform.localScale;
        float scaleX = localScale.x;
        float scaleY = localScale.y;

        // 실제 크기 계산하기
        float actualWidth = textureWidth * spriteWidth * scaleX;
        float actualHeight = textureHeight * spriteHeight * scaleY;

        // 계산된 스케일값 리턴
        return new Vector3(actualWidth / textureWidth, actualHeight / textureHeight, 1);
    }
}
