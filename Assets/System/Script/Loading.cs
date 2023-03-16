using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using DG.Tweening;
using Lean.Pool;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{
    #region Singleton
    private static Loading instance;
    public static Loading Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<Loading>();
                if (instance == null)
                {
                    GameObject obj = new GameObject();
                    obj.name = "Loading";
                    instance = obj.AddComponent<Loading>();
                }
            }
            return instance;
        }
    }
    #endregion

    [SerializeField] Canvas loadingCanvas; // 로딩 캔버스
    [SerializeField] Animator cutoutMask;
    [SerializeField] GameObject cutoutCover;
    [SerializeField] CanvasGroup loadingGroup; // 로딩 그룹
    [SerializeField] Transform gemCircle; // 젬서클
    [SerializeField] Transform loadingBarGroup; // 로딩바
    [SerializeField] Slider loadingBar; // 로딩바
    [SerializeField] TextMeshProUGUI loadingText; // 로딩 텍스트 안내
    IEnumerator loadingTextCoroutine;
    NewInput loading_Input; // 로딩 넘기기용 인풋

    private void Awake()
    {
        // 다른 오브젝트가 이미 있을 때
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // 로딩 인풋 활성화
        loading_Input = new NewInput();
        loading_Input.Enable();

        // 로딩 그룹 숨기기
        loadingGroup.alpha = 0f;
    }

    private void OnEnable()
    {
        // 로딩 오브젝트들 초기화
        cutoutMask.gameObject.SetActive(true);
        cutoutCover.gameObject.SetActive(false);
        loadingBar.gameObject.SetActive(true);

        if (loadingCanvas == null) loadingCanvas = transform.parent.GetComponent<Canvas>();
        // 캔버스에 카메라 입력
        if (loadingCanvas.worldCamera == null) loadingCanvas.worldCamera = Camera.main;
    }

    public IEnumerator LoadScene(string sceneName)
    {
        //! 시간 측정
        Stopwatch debugTime = new Stopwatch();
        debugTime.Start();

        // 씬 종료 전에 초기화
        QuitScene(sceneName);

        // 이전 씬 언로드
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        // 이전 씬 언로드가 끝날 때까지 기다림
        yield return unloadOperation;

        // 이름으로 씬 불러오기
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        // // 로딩 진행도 0.9에서 대기 시키기
        // operation.allowSceneActivation = false;

        // 로딩 텍스트 초기화
        loadingText.text = "Loading...";
        StartCoroutine(LoadText());

        // 로딩바 초기화
        loadingBar.value = 0f;

        // 다음씬 초기화 끝날때까지 
        while (SystemManager.Instance.sceneChanging
        || !operation.isDone
        || loadingBar.value < 1f)
        {
            // 로딩 바 0.9 미만일때
            if (loadingBar.value < 0.9f)
            {
                // 로딩 바 0.9까지 채우기
                loadingBar.value = Mathf.MoveTowards(loadingBar.value, 0.9f, Time.unscaledDeltaTime);
            }
            // 씬 불러오기 완료했으면
            else if (operation.progress >= 0.9f)
            {
                // 로딩 바 끝까지 채우기
                loadingBar.value = Mathf.MoveTowards(loadingBar.value, 1f, Time.unscaledDeltaTime);
            }

            yield return null;
        }

        // 게이지 모두 찼을때, 다음 씬 초기화 완료 했을때
        if (loadingBar.value >= 1f
        && !SystemManager.Instance.sceneChanging)
        {
            loadingText.text = "Press AnyKey";

            // 텍스트 깜빡이기
            loadingText.color = Color.white;
            loadingText.DOColor(Color.gray, 0.5f)
            .SetUpdate(true)
            .SetLoops(-1, LoopType.Yoyo)
            .OnUpdate(() =>
            {
                // 캔버스에 카메라 입력
                if (loadingCanvas.worldCamera == null) loadingCanvas.worldCamera = Camera.main;
            })
            .OnKill(() =>
            {
                loadingText.color = Color.white;
            });
        }

        // 인게임 진입시
        if (sceneName == SceneName.InGameScene.ToString())
        {
            // 기본 마법 패널 켜기
            yield return new WaitUntil(() => UIManager.Instance != null);

            // 인게임 모든 캔버스 패널 초기화
            UIManager.Instance.InitPanel();

            // 첫 맵일때
            if (SystemManager.Instance.NowMapElement == 0)
            {
#if !UNITY_EDITOR
                // 기본 마법 패널 켜기
                UIManager.Instance.PopupUI(UIManager.Instance.defaultPanel, true);
#endif
            }

            // 플레이어 빔에서 스폰
            StartCoroutine(PlayerManager.Instance.SpawnPlayer());
        }

        // 해상도 변경 및 빈공간에 레터박스 넣기
        SystemManager.Instance.ChangeResolution(SystemManager.Instance.screenMode);

        // 클릭 혹은 아무키나 누를때까지, 로딩 완료, 다음씬 초기화 완료까지 대기
        yield return new WaitUntil(() => (loading_Input.UI.Click.IsPressed() || loading_Input.UI.AnyKey.IsPressed())
        && loadingBar.value >= 1f && !SystemManager.Instance.sceneChanging);

        // 텍스트 색깔 초기화
        loadingText.DOKill();

        // 씬 마스크 제거하기
        yield return StartCoroutine(SceneMask(false));

        // 걸린 시간 측정
        debugTime.Stop();
        print($"Loading Duration : {debugTime.ElapsedMilliseconds / 1000f}s");
    }

    public IEnumerator SceneMask(bool isFadeout)
    {
        // 트윈 시간
        float tweenTime = 0.5f;

        // 마스크 끝내기
        if (!isFadeout)
        {
            // 마스크 끝날때 초기화
            InitScene(false);

            // 로딩 그룹 전체 알파값 낮추기
            loadingGroup.alpha = 1f;
            DOTween.To(() => loadingGroup.alpha, x => loadingGroup.alpha = x, 0, tweenTime * 2)
            .SetUpdate(true)
            .SetEase(Ease.OutCirc);

            // 젬 서클 사이즈 줄이기
            gemCircle.transform.localScale = Vector2.one;
            gemCircle.transform.DOScale(Vector2.zero, tweenTime)
            .SetUpdate(true)
            .SetEase(Ease.InBack);

            // 로딩바 위치 내리기
            loadingBarGroup.transform.localPosition = Vector2.down * 535f;
            loadingBarGroup.transform.DOLocalMove(Vector2.down * 740f, tweenTime)
            .SetUpdate(true)
            .SetEase(Ease.OutCubic);

            yield return new WaitForSecondsRealtime(tweenTime * 2);
        }
        // 마스크 시작
        else
        {
            // 마스크 시작할때 초기화
            InitScene(true);

            // 시간 멈추기
            SystemManager.Instance.TimeScaleChange(0f);
        }

        // 마스크 켜기
        cutoutCover.SetActive(true);
        // 애니메이터 켜기
        cutoutMask.enabled = true;

        // 컷아웃 마스크 애니메이션 재생
        cutoutMask.SetBool("isFadeout", isFadeout);
        yield return new WaitForSecondsRealtime(1.5f);

        // 현재 씬 가려졌는지 여부 갱신
        SystemManager.Instance.screenMasked = isFadeout;

        // 애니메이터 끄기
        cutoutMask.enabled = false;

        // 마스크 시작
        if (isFadeout)
        {
            // 로딩 텍스트 초기화
            loadingText.text = "Loading...";
            // 로딩바 초기화
            loadingBar.value = 0f;

            // 로딩 그룹 전체 알파값 올리기
            loadingGroup.alpha = 0f;
            DOTween.To(() => loadingGroup.alpha, x => loadingGroup.alpha = x, 1, tweenTime * 2)
            .SetUpdate(true)
            .SetEase(Ease.OutCirc);

            // 젬 서클 사이즈 키우기
            gemCircle.transform.localScale = Vector2.zero;
            gemCircle.transform.DOScale(Vector2.one, tweenTime)
            .SetUpdate(true)
            .SetEase(Ease.OutBack);

            // 로딩바 위치 올리기
            loadingBarGroup.transform.localPosition = Vector2.down * 740f;
            loadingBarGroup.transform.DOLocalMove(Vector2.down * 535f, tweenTime)
            .SetUpdate(true)
            .SetEase(Ease.OutCubic);

            yield return new WaitForSecondsRealtime(tweenTime * 2);
        }
        // 마스크 끝내기
        else
        {
            // 마스크 끄기
            cutoutCover.SetActive(false);

            // UI매니저 있을때, 기본 마법 켜져있을때
            if (UIManager.Instance != null && UIManager.Instance.defaultPanel.activeInHierarchy)
                yield break;

            // 시간 속도 초기화
            SystemManager.Instance.TimeScaleChange(1f);
        }
    }

    void InitScene(bool isOpenScene)
    {
        // 마스크로 가릴때
        if (isOpenScene)
        {
            // 배경음 정지
            SoundManager.Instance.PauseBGM();
            // 모든 효과음 중지 및 삭제
            SoundManager.Instance.DestoryAllSound();

            // 플레이어 있으면
            if (PlayerManager.Instance)
            {
                // 플레이어 비활성화
                PlayerManager.Instance.gameObject.SetActive(false);
                // 플레이어 활성화
                PlayerManager.Instance.gameObject.SetActive(true);
            }
        }
        // 마스크 끝날때
        else
        {
            // UI 커서 끄기
            UICursor.Instance.UICursorToggle(false);

            // 인게임 씬일때
            if (SystemManager.Instance.GetSceneName() == SceneName.InGameScene.ToString())
            {
                // 저장된 SFX 볼륨으로 초기화
                SoundManager.Instance.Set_SFXVolume(PlayerPrefs.GetFloat(SaveManager.SFX_VOLUME_KEY));

                // 플레이어 활성화
                PlayerManager.Instance.gameObject.SetActive(true);
            }

            // 배경음 초기화
            SoundManager.Instance.PlayBGM();
        }
    }

    void QuitScene(string sceneName)
    {
        // 메인메뉴로 씬 이동일때
        if (sceneName == SceneName.MainMenuScene.ToString())
        {
            // dontDestroy 오브젝트 모두 파괴
            if (ObjectPool.Instance != null)
                Destroy(ObjectPool.Instance.gameObject); // 오브젝트 풀 파괴

            LeanPool.DespawnAll();

            if (UIManager.Instance != null)
                Destroy(UIManager.Instance.gameObject); // UI 매니저 파괴
            if (PlayerManager.Instance != null)
                Destroy(PlayerManager.Instance.gameObject); // 플레이어 파괴
            if (CastMagic.Instance != null)
                Destroy(CastMagic.Instance.gameObject); // 핸드폰 파괴
        }
    }

    IEnumerator LoadText()
    {
        string text = "Now Laoding";

        // 씬 로딩중일때
        while (SystemManager.Instance.sceneChanging)
        {
            for (int i = 0; i < 3; i++)
            {
                // 로딩바 꽉차면 리턴
                if (loadingBar.value >= 1f)
                    yield break;
                else
                {
                    // 로딩 텍스트 갱신
                    text += ".";
                    loadingText.text = text;
                }

                yield return new WaitForSecondsRealtime(0.2f);
            }
        }
    }
}
