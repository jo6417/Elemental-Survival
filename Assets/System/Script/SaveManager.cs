using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.Networking;
using SimpleJSON;
using TMPro;
using UnityEngine.UI;
using System.Text;

[System.Serializable]
public class SaveData : System.IDisposable
{
    #region DB        
    public int[] unlockMagicList; // 해금된 마법 리스트
    public int[] banMagicList; // 해금 마법 중에서 사용 마법 리스트
    public string magicDBJson; //마법 DB json
    public string enemyDBJson; //몬스터 DB json
    public string itemDBJson; //아이템 DB json
    #endregion

    #region Option
    // public float[] volumes = new float[4]; // 전체볼륨, 배경음, 효과음, UI
    // public FullScreenMode fullscreenMode = FullScreenMode.Windowed;
    // public float[] resolution = { 1920f, 1080f }; // 저장된 해상도
    // public bool showDamage = true; // 데미지 표시여부
    // public float optionBrightness = 0.9f; // 밝기 설정값
    #endregion

    public SaveData()
    {
        //해금 마법 목록
        // unlockMagics = MagicDB.Instance.unlockMagics.ToArray();
        //TODO 해금 캐릭터 목록
        //TODO 소지금(원소젬)
    }

    public void Dispose()
    {
        throw new System.NotImplementedException();
    }
}

public class SaveManager : MonoBehaviour
{
    #region Singleton
    private static SaveManager instance;
    public static SaveManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 비활성화된 오브젝트도 포함 찾기
                var obj = FindObjectOfType<SaveManager>(true);
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    // print("new obj");
                    var newObj = new GameObject().AddComponent<SaveManager>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    [Header("SaveKey")]
    public const string MASTER_VOLUME_KEY = "masterVolume";
    public const string MUSIC_VOLUME_KEY = "musicVolume";
    public const string SFX_VOLUME_KEY = "sfxVolume";
    public const string UI_VOLUME_KEY = "uiVolume";
    public const string SCREENMODE_KEY = "screenMode";
    public const string RESOLUTION_X_KEY = "resolutionX";
    public const string RESOLUTION_Y_KEY = "resolutionY";
    public const string BRIGHTNESS_KEY = "brightness";
    public const string SHOW_DAMAGE_KEY = "showDamage";

    [Header("State")]
    [ReadOnly] public bool nowSaving = false; //저장 중 여부

    public SaveData webSaveData; // 웹에서 불러온 세이브 데이터
    public SaveData localSaveData; // 로컬에서 불러온 세이브 데이터

    string magicURI = "https://script.googleusercontent.com/macros/echo?user_content_key=7V2ZVIq0mlz0OyEVM8ULXo0nlLHXKPuUIJxFTqfLhj4Jsbg3SVZjnSH4X9KTiksN02j7LG8xCj8EgELL1uGWpX0Tg3k2TlLvm5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnD_xj3pGHBsYNBHTy1qMO9_iBmRB6zvsbPv4uu5dqbk-3wD3VcpY-YvftUimQsCyzKs3JAsCIlkQoFkByun7M-8F5ap6m-tpCA&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp";
    string enemyURI = "https://script.googleusercontent.com/macros/echo?user_content_key=6ZQ8sYLio20mP1B6THEMPzU6c7Ph6YYf0LUfc38pFGruRhf2CiPrtPUMnp3RV9wjWS5LUI11HGSiZodVQG0wgrSV-9f0c_yJm5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnKa-POu7wcFnA3wlQMYgM526Nnu0gbFAmuRW8zSVEVAU9_HiX_KJ3qEm4imXtAtA2I-6ud_s58xOj3-tedHHV_AcI_N4bm379g&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp";
    string itemURI = "https://script.googleusercontent.com/macros/echo?user_content_key=SFxUnXenFob7Vylyu7Y_v1klMlQl8nsSqvMYR4EBlwac7E1YN3SXAnzmp-rU-50oixSn5ncWtdnTdVhtI4nUZ9icvz8bgj6om5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnDd5HMKPhPTDYFVpd6ZAI5lT6Z1PRDVSUH9zEgYKrhfZq5_-qo0tdzwRz-NvpaavXaVjRCMLKUCBqV1xma9LvJ-ti_cY4IfTKw&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp";

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

        // PlayerPref 설정값 모두 불러오기
        StartCoroutine(LoadPref());
    }

    IEnumerator Saving()
    {
        // 저장 아이콘 켜기
        SystemManager.Instance.saveIcon.SetActive(true);

        // 최소 저장 시간 대기
        yield return new WaitForSecondsRealtime(1f);
        // 저장 끝날때까지 대기
        yield return new WaitUntil(() => !nowSaving);

        //저장 아이콘 끄기
        SystemManager.Instance.saveIcon.SetActive(false);
    }

    public void SavePref()
    {
        // 볼륨 옵션값 모두 저장
        // PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, SoundManager.Instance.masterVolume);
        // PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, SoundManager.Instance.musicVolume);
        // PlayerPrefs.SetFloat(SFX_VOLUME_KEY, SoundManager.Instance.sfxVolume);
        // PlayerPrefs.SetFloat(UI_VOLUME_KEY, SoundManager.Instance.uiVolume);

        // 화면 모드 저장
        PlayerPrefs.SetInt(SCREENMODE_KEY, (int)SystemManager.Instance.screenMode);

        // 해상도 저장
        PlayerPrefs.SetFloat(RESOLUTION_X_KEY, SystemManager.Instance.lastResolution.x);
        PlayerPrefs.SetFloat(RESOLUTION_Y_KEY, SystemManager.Instance.lastResolution.y);

        // 데미지 표시 여부 저장
        PlayerPrefs.SetInt(SHOW_DAMAGE_KEY, SystemManager.Instance.showDamage ? 1 : 0);

        // 밝기 값 저장
        PlayerPrefs.SetFloat(BRIGHTNESS_KEY, SystemManager.Instance.OptionBrightness);
    }

    public IEnumerator LoadPref()
    {
        // 해상도 불러오기
        SystemManager.Instance.lastResolution = new Vector2(PlayerPrefs.GetFloat(RESOLUTION_X_KEY, 1920f), PlayerPrefs.GetFloat(RESOLUTION_Y_KEY, 1080f)); ;
        // 화면모드 불러와서 적용
        SystemManager.Instance.ChangeResolution((FullScreenMode)PlayerPrefs.GetInt(SCREENMODE_KEY, (int)FullScreenMode.ExclusiveFullScreen), true);
        // 데미지 표시 여부 로드
        SystemManager.Instance.showDamage = PlayerPrefs.GetInt(SHOW_DAMAGE_KEY, 1) == 1 ? true : false;

        // 밝기 값 로드
        SystemManager.Instance.OptionBrightness = PlayerPrefs.GetFloat(BRIGHTNESS_KEY, 0.9f);
        // 현재 글로벌 라이트에 적용
        SystemManager.Instance.SetBrightness();

        // 사운드 매니저 초기화 대기
        yield return new WaitUntil(() => SoundManager.Instance != null && SoundManager.Instance.initFinish);

        // 오디오 옵션값 불러오기
        SoundManager.Instance.Set_MasterVolume(PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f));
        SoundManager.Instance.Set_MusicVolume(PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f));
        SoundManager.Instance.Set_SFXVolume(PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f));
        SoundManager.Instance.Set_UIVolume(PlayerPrefs.GetFloat(UI_VOLUME_KEY, 1f));
    }

    public IEnumerator Save()
    {
        // PlayerPref 설정값 모두 저장
        SavePref();

        string SAVE_PATH = Application.persistentDataPath + "/save.json";
        string old_SaveData = "";

        // 세이브 파일이 없으면
        if (!File.Exists(SAVE_PATH))
            // 새 파일 생성
            File.Create(SAVE_PATH).Dispose();
        // 파일 있으면
        else
            // 기존의 로컬 세이브 데이터 불러와 json 문자열로 변환
            old_SaveData = File.ReadAllText(SAVE_PATH);

        // 해금 마법 목록 저장
        localSaveData.unlockMagicList = MagicDB.Instance.unlockMagicList.ToArray();
        // 사용 마법 목록 저장
        localSaveData.banMagicList = MagicDB.Instance.banMagicList.ToArray();

        #region Saving

        // SaveData 형태의 데이터를 json 문자열로 변환
        string new_SaveData = JsonConvert.SerializeObject(localSaveData);

        // 기존 세이브 데이터와 똑같으면 저장 취소
        if (old_SaveData == new_SaveData)
            yield break;

        //저장 시작
        nowSaving = true;
        //저장 중일때 저장 아이콘 띄우기
        StartCoroutine(Saving());

        // 해당 파일 경로에 저장
        File.WriteAllText(SAVE_PATH, new_SaveData);

        print("저장 완료 - " + SAVE_PATH);

        nowSaving = false; //저장 끝

        #endregion

        yield return null;
    }

    public IEnumerator LoadData()
    {
        // 세이브 파일이 없으면 리턴
        if (!File.Exists(Application.persistentDataPath + "/save.json"))
            yield break;

        string loadData = File.ReadAllText(Application.persistentDataPath + "/save.json");
        // print(loadData);

        // 불러온 json 문자열을 SaveData 형태로 변환해서 변수에 넣기
        localSaveData = JsonConvert.DeserializeObject<SaveData>(loadData);

        //TODO 소지금 불러오기

        // 해금 마법 목록 불러오기
        MagicDB.Instance.unlockMagicList = new List<int>(localSaveData.unlockMagicList.ToList());
        // 사용중 마법 목록 불러오기
        MagicDB.Instance.banMagicList = new List<int>(localSaveData.banMagicList.ToList());
    }

    public IEnumerator DBSyncCheck(DBType dbType, Button syncBtn)
    {
        // DB 종류에 따라 다른 uri 사용
        string uri = "";
        switch (dbType)
        {
            case DBType.Magic:
                uri = magicURI;
                break;
            case DBType.Enemy:
                uri = enemyURI;
                break;
            case DBType.Item:
                uri = itemURI;
                break;
        }

        // 동기화 버튼 노랑불
        if (syncBtn != null)
            syncBtn.targetGraphic.color = Color.yellow;

        // 로컬에 저장된 세이브 불러와 변수에 넣기
        yield return StartCoroutine(LoadData());
        // 웹에서 세이브 불러와 변수에 넣기
        yield return StartCoroutine(WebDataLoad(dbType));

        string localDBJson = "";
        string webDBJson = "";
        // DB 종류에 따라 다른 DB json 불러오기
        switch (dbType)
        {
            case DBType.Magic:
                localDBJson = localSaveData.magicDBJson;
                webDBJson = webSaveData.magicDBJson;
                break;
            case DBType.Enemy:
                localDBJson = localSaveData.enemyDBJson;
                webDBJson = webSaveData.enemyDBJson;
                break;
            case DBType.Item:
                localDBJson = localSaveData.itemDBJson;
                webDBJson = webSaveData.itemDBJson;
                break;
        }

        // print($"{string.Equals(localDBJson, webDBJson)}, {localDBJson.Length} : {webDBJson.Length}");

        if (syncBtn != null)
        {
            // 해당 DB 종류의 로컬 json 문자열과 웹 json 문자열 비교
            if (string.Equals(localDBJson, webDBJson))
            {
                // 동기화 버튼 초록불
                syncBtn.targetGraphic.color = Color.green;

                // 버튼 상호작용 끄기
                // syncBtn.interactable = false;
            }
            else
            {
                // 동기화 버튼 빨간불
                syncBtn.targetGraphic.color = Color.red;

                // 버튼 상호작용 켜기
                // syncBtn.interactable = true;
            }

            // 동기화 아이콘 애니메이션 멈추고 각도 초기화
            syncBtn.transform.Find("SyncIcon").GetComponent<Animator>().enabled = false;
            syncBtn.transform.Find("SyncIcon").rotation = Quaternion.Euler(Vector3.zero);
        }
    }

    public IEnumerator WebDataLoad(DBType dbType)
    {
        // DB 종류에 따라 다른 uri 사용
        string uri = "";
        switch (dbType)
        {
            case DBType.Magic:
                uri = magicURI;
                break;
            case DBType.Enemy:
                uri = enemyURI;
                break;
            case DBType.Item:
                uri = itemURI;
                break;
        }

        //Apps Script에서 가공된 json 데이터 문서 주소
        UnityWebRequest www = UnityWebRequest.Get(uri);
        // 해당 주소에 요청
        yield return www.SendWebRequest();

        //에러 뜰 경우 에러 표시
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log("Error : " + www.error);
        }
        else
        {
            // DB 종류에 따라 받아온 json string값 넣어주기
            switch (dbType)
            {
                case DBType.Magic:
                    webSaveData.magicDBJson = www.downloadHandler.text;
                    break;
                case DBType.Enemy:
                    webSaveData.enemyDBJson = www.downloadHandler.text;
                    break;
                case DBType.Item:
                    webSaveData.itemDBJson = www.downloadHandler.text;
                    break;
            }

            yield return null;
        }
    }

    public IEnumerator DBtoEnum()
    {
        string filePath = "Assets/System/Script/" + "DBEnums.cs";

        // enum 파일이 없으면 리턴
        if (!File.Exists(filePath))
            yield break;

        // 입력될 코드 스크립트
        StringBuilder DBEnumScript = new StringBuilder();

        DBEnumScript.Append("using System.Collections;\n");
        DBEnumScript.Append("using System.Collections.Generic;\n");
        DBEnumScript.Append("using UnityEngine;\n");
        DBEnumScript.Append("public class DBEnums : MonoBehaviour\n");
        DBEnumScript.Append("{\n");

        // 마법 enum 작성
        DBEnumScript.Append("public enum MagicDBEnum {");
        foreach (var value in MagicDB.Instance.magicDB)
        {
            // 공백 제거
            string magicName = value.Value.name;
            magicName = magicName.Replace(" ", "");

            // 마법 이름으로 enum에 추가
            DBEnumScript.Append(magicName);

            // 마지막 값이 아닐때만
            if (value.Value.id < MagicDB.Instance.magicDB.Count - 1)
                DBEnumScript.Append(", ");

            yield return null;
        }
        DBEnumScript.Append("};" + "\n\n");

        // 아이템 enum 작성
        DBEnumScript.Append("public enum ItemDBEnum {");
        foreach (var value in ItemDB.Instance.itemDB)
        {
            // 공백 제거
            string itemName = value.Value.name;
            itemName = itemName.Replace(" ", "");

            // 마법 이름으로 enum에 추가
            DBEnumScript.Append(itemName);

            // 마지막 값이 아닐때만
            if (value.Value.id < ItemDB.Instance.itemDB.Count - 1)
                DBEnumScript.Append(", ");

            yield return null;
        }
        DBEnumScript.Append("};" + "\n\n");

        // 몬스터 enum 작성
        DBEnumScript.Append("public enum EnemyDBEnum {");
        foreach (var value in EnemyDB.Instance.enemyDB)
        {
            // 공백 제거
            string enemyName = value.Value.name;
            enemyName = enemyName.Replace(" ", "");

            // 마법 이름으로 enum에 추가
            DBEnumScript.Append(enemyName);

            // 마지막 값이 아닐때만
            if (value.Value.id < EnemyDB.Instance.enemyDB.Count - 1)
                DBEnumScript.Append(", ");

            yield return null;
        }
        DBEnumScript.Append("};" + "\n");
        DBEnumScript.Append("}\n");

        // 해당 파일 경로에 저장
        File.WriteAllText(filePath, DBEnumScript.ToString());
    }

    // public void DBSynchronize(DBType dbType)
    // {
    //     // 데이터 동기화 코루틴 실행
    //     StartCoroutine(DBSync(dbType));
    // }

    // IEnumerator DBSync(DBType dbType)
    // {
    //     // SaveData 형태로 로컬 데이터 로드하기
    //     SaveData saveData = LoadData();

    //     //todo 로컬 json에 웹 json 넣고 저장, 완료시까지 대기
    //     yield return StartCoroutine(SaveManager.Instance.Save());

    //     // DB 타입에따라 데이터 넣어주기
    //     switch (dbType)
    //     {
    //         case DBType.Magic:
    //             StartCoroutine(MagicDB.Instance.GetMagicDB());
    //             break;
    //         case DBType.Enemy:
    //             break;
    //         case DBType.Item:
    //             break;
    //     }

    //     //todo 마법,몬스터,아이템 DB에 로컬 json 데이터로 파싱해서 넣기

    //     //todo 동기화 여부 다시 검사
    //     StartCoroutine(DBSyncCheck(webMagicDBJson, SystemManager.Instance.magicDBSyncBtn, "https://script.googleusercontent.com/macros/echo?user_content_key=7V2ZVIq0mlz0OyEVM8ULXo0nlLHXKPuUIJxFTqfLhj4Jsbg3SVZjnSH4X9KTiksN02j7LG8xCj8EgELL1uGWpX0Tg3k2TlLvm5_BxDlH2jW0nuo2oDemN9CCS2h10ox_1xSncGQajx_ryfhECjZEnD_xj3pGHBsYNBHTy1qMO9_iBmRB6zvsbPv4uu5dqbk-3wD3VcpY-YvftUimQsCyzKs3JAsCIlkQoFkByun7M-8F5ap6m-tpCA&lib=MlJXL_oXznex1TzTWlp6olnqzQVRJChSp"));
    // }
}
