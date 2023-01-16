using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lean.Pool;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameoverMenu : MonoBehaviour
{
    [SerializeField] Transform gameoverScreen;
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] Image background;
    [SerializeField] Transform hasMagics;
    [SerializeField] Transform gameLog;
    [SerializeField] Transform gameoverSlot;
    [SerializeField] GameObject layEffect; // 팡파레 이펙트
    [SerializeField] Button retryBtn;
    AudioSource bzzSound;

    public void GameOver(bool isClear = false)
    {
        // 게임오버 패널 켜기
        gameObject.SetActive(true);

        // 모든 사운드 끄기
        SoundManager.Instance.SoundTimeScale(0, 0);

        // 클리어 여부에 따라 타이틀,배경색 바꾸기
        if (isClear)
        {
            title.text = "CLEAR";
            background.color = SystemManager.Instance.HexToRGBA("00903E");

            // 팡파레 이펙트 켜기
            layEffect.SetActive(true);
            layEffect.GetComponent<Animator>().speed = 0.1f;
        }
        else
        {
            title.text = "GAME OVER";
            background.color = SystemManager.Instance.HexToRGBA("006090");

            // 팡파레 이펙트 끄기
            layEffect.SetActive(false);
        }

        //TODO 캐릭터 넣기
        gameLog.Find("Character/Amount").GetComponent<TextMeshProUGUI>().text = "Chracter Test";
        //TODO 맵 넣기
        gameLog.Find("Map/Amount").GetComponent<TextMeshProUGUI>().text = SystemManager.Instance.nowMapElement.ToString();
        // 현재 시간 넣기
        gameLog.Find("Time/Amount").GetComponent<TextMeshProUGUI>().text = UIManager.Instance.UpdateTimer();
        // 재화 넣기
        gameLog.Find("Money/Amount").GetComponent<TextMeshProUGUI>().text = "Gem Test";
        // 킬 수 넣기
        gameLog.Find("KillCount/Amount").GetComponent<TextMeshProUGUI>().text = SystemManager.Instance.killCount.ToString();
        //TODO 사망원인 넣기
        gameLog.Find("KilledBy/Amount").GetComponent<TextMeshProUGUI>().text = "Mob Test";

        // 모든 자식 오브젝트를 제거
        SystemManager.Instance.DestroyAllChild(hasMagics);

        // 보유한 모든 마법을 리스트로 수집
        List<MagicInfo> haveMagics = CastMagic.Instance.hasAllMagic();
        // 마법을 id 순으로(등급순) 정렬하기
        haveMagics = haveMagics.OrderBy(magic => magic.id).ToList();

        // 이번 게임에서 보유 했었던 마법 전부 표시
        for (int i = 0; i < haveMagics.Count; i++)
        {
            //마법 슬롯 생성
            Transform slot = LeanPool.Spawn(gameoverSlot, hasMagics.position, Quaternion.identity, hasMagics).transform;

            //마법 찾기
            MagicInfo magic = haveMagics[i];

            //프레임 색 넣기
            slot.Find("Frame").GetComponent<Image>().color = MagicDB.Instance.GradeColor[magic.grade];
            //아이콘 넣기
            slot.Find("Icon").GetComponent<Image>().sprite = MagicDB.Instance.GetIcon(magic.id);
            //레벨 넣기
            slot.Find("Level").GetComponentInChildren<TextMeshProUGUI>().text = "Lv. " + magic.magicLevel.ToString();
        }

        // retry 버튼 선택
        retryBtn.Select();
    }

    public void RetryGame()
    {
        // 맵 속성 초기화
        SystemManager.Instance.nowMapElement = 0;

        // 게임 다시 시작
        SystemManager.Instance.StartGame();
    }

    public void QuitMainMenu()
    {
        // 게임 종료시 초기화
        SystemManager.Instance.QuitMainMenu();
    }

    void LandingSoundPlay(string soundName)
    {
        SoundManager.Instance.PlaySound(soundName);
    }

    void BzzSoundPlay()
    {
        bzzSound = SoundManager.Instance.PlaySound("Ascii_Screen_Bzz", 0, 0, -1);
    }

    void BzzSoundStop()
    {
        SoundManager.Instance.StopSound(bzzSound, 0.5f);
    }
}
