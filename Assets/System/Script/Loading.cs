using System.Collections;
using System.Collections.Generic;
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
        // // 최초 생성 됬을때
        // if (instance == null)
        //     // 파괴되지 않게 설정
        //     DontDestroyOnLoad(gameObject);
        // else
        //     // 해당 오브젝트 파괴
        //     Destroy(gameObject);

        // 로딩 인풋 활성화
        loading_Input = new NewInput();
        loading_Input.Enable();
    }

    public IEnumerator LoadScene(string sceneName)
    {
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
                loadingBar.value = Mathf.MoveTowards(loadingBar.value, 0.9f, Time.deltaTime);
            }
            // 씬 불러오기 완료했으면
            else if (operation.progress >= 0.9f)
            {
                // 로딩 바 끝까지 채우기
                loadingBar.value = Mathf.MoveTowards(loadingBar.value, 1f, Time.deltaTime);
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
                    // 씬 마저 넘기기
                    operation.allowSceneActivation = true;
                }

            yield return null;
        }
    }

    IEnumerator LoadText()
    {
        string text = "Loading";

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

            yield return new WaitForSeconds(1f);
        }
    }
}
