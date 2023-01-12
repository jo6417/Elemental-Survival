using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
                var obj = FindObjectOfType<Loading>();
                if (obj != null)
                {
                    instance = obj;
                }
                // else
                // {
                //     var newObj = new GameObject().AddComponent<Loading>();
                //     instance = newObj;
                // }
            }
            return instance;
        }
    }
    #endregion

    [SerializeField] Slider loadingBar; // 로딩바
    [SerializeField] TextMeshProUGUI loadingText; // 로딩 텍스트 안내
    NewInput loading_Input; // 로딩 넘기기용 인풋

    private void Awake()
    {
        // 최초 생성 됬을때
        if (instance == null)
        {
            instance = this;
        }
        else
            // 해당 오브젝트 파괴
            Destroy(gameObject);

        // 로딩 인풋 활성화
        loading_Input = new NewInput();
        loading_Input.Enable();

        // 시간 멈추기
        SystemManager.Instance.TimeScaleChange(0f);
    }

    public IEnumerator LoadScene(string sceneName)
    {
        //! 시간 측정
        Stopwatch debugTime = new Stopwatch();
        debugTime.Start();

        // 이름으로 씬 불러오기
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        // 로딩 진행도 0.9에서 대기 시키기
        operation.allowSceneActivation = false;

        // 로딩 텍스트 초기화
        loadingText.text = "Loading...";
        StartCoroutine(LoadText());

        // 로딩바 초기화
        loadingBar.value = 0f;

        // 로딩 될때까지 반복
        while (!operation.isDone)
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

            // 로딩 완료시
            if (loadingBar.value >= 1f)
            {
                loadingText.text = "Press SpaceBar";
            }

            // 클릭,확인 누르면
            if (loading_Input.UI.Click.IsPressed()
            || loading_Input.UI.Accept.IsPressed())
                // 로딩 완료일때
                if (loadingBar.value >= 1f)
                {
                    // 화면 마스크로 덮고 끝날때까지 대기
                    yield return StartCoroutine(SystemManager.Instance.SceneMask(true));

                    // 씬 마저 넘기기
                    operation.allowSceneActivation = true;

                    // 마스크 커져서 화면 보이기
                    yield return StartCoroutine(SystemManager.Instance.SceneMask(false));

                    yield break;
                }

            yield return null;
        }

        // 걸린 시간 측정
        debugTime.Stop();
        print($"Loading Done : {debugTime.ElapsedMilliseconds / 1000f}s");
    }

    IEnumerator LoadText()
    {
        string text = "Now Laoding";

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

        // 재실행
        StartCoroutine(LoadText());
    }
}
