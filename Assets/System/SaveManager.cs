using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

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
                //비활성화된 오브젝트도 포함
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

    public GameObject saveIcon; //저장 아이콘
    private bool isSaving = false; //저장 중 여부

    public IEnumerator Save()
    {
        isSaving = true; //저장 시작
        // StartCoroutine(Saving()); //저장 중일때 저장 아이콘

        //세이브 데이터 인스턴스 선언
        // using (SaveData data = new SaveData())
        // {
        //     // SaveData 형태의 데이터를 json 문자열로 변환
        //     string saveData = JsonConvert.SerializeObject(data);

        //     // 해당 파일 경로에 저장
        //     File.WriteAllText(Application.persistentDataPath + "/save.json", saveData);
        //     print("저장 완료 - " + Application.persistentDataPath + "/save.json");
        // }

        SaveData data = new SaveData();

        // SaveData 형태의 데이터를 json 문자열로 변환
        string saveData = JsonConvert.SerializeObject(data);

        // 해당 파일 경로에 저장
        File.WriteAllText(Application.persistentDataPath + "/save.json", saveData);
        print("저장 완료 - " + Application.persistentDataPath + "/save.json");


        isSaving = false; //저장 끝
        yield return null;
    }

    IEnumerator Saving()
    {
        // 저장 아이콘 켜기
        saveIcon.SetActive(true);

        // 최소 저장 시간 선 대기
        yield return new WaitForSecondsRealtime(1.5f);

        // 저장 아이콘 켜져 있으면 반복
        while (saveIcon.activeSelf)
        {
            // 저장 끝났으면
            if (!isSaving)
            {
                //저장 아이콘 끄기
                saveIcon.SetActive(false);
            }

            yield return null;
        }
    }

    public void Load()
    {
        // 세이브 파일이 없으면 리턴
        if (!File.Exists(Application.persistentDataPath + "/save.json"))
            return;

        string loadData = File.ReadAllText(Application.persistentDataPath + "/save.json");
        print(loadData);

        // 불러온 json 문자열을 SaveData 형태로 변환
        SaveData data = JsonConvert.DeserializeObject<SaveData>(loadData);

        //해금 마법 목록 불러오기
        MagicDB.Instance.unlockMagics = data.unlockMagics.ToList();
        //TODO 해금 캐릭터 목록
        //TODO 소지금 불러오기

    }
}
