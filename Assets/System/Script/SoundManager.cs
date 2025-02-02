using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[System.Serializable]
public class Sound
{
    public string name;

    public AudioClip clip;

    [Range(0f, 3f)]
    public float volume = 0.5f;
    [Range(0f, 3f)]
    public float pitch = 1f;

    public AudioMixerGroup mixerGroup; // 오디오 아웃풋 믹서그룹
    public AudioSource source;
}

[System.Serializable]
public class SoundBundle
{
    public string name;

    public Sound[] sounds;
}

public class SoundManager : MonoBehaviour
{
    #region Singleton
    private static SoundManager instance;
    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                return null;
                // var obj = FindObjectOfType<SoundManager>();
                // if (obj != null)
                // {
                //     instance = obj;
                // }
                // else
                // {
                //     var newObj = new GameObject().AddComponent<SoundManager>();
                //     instance = newObj;
                // }
            }
            return instance;
        }
    }
    #endregion

    [Header("State")]
    // [ReadOnly] public float masterVolume = 1f; // 전체 음량
    // [ReadOnly] public float musicVolume = 1f; // 배경음 음량
    // [ReadOnly] public float sfxVolume = 1f; // 효과음 음량
    // [ReadOnly] public float uiVolume = 1f; // UI 음량
    [ReadOnly] public float globalPitch = 1f; // 전체 사운드 속도 계수
    [ReadOnly] public bool initFinish = false;
    [SerializeField] float bgmFadeTime = 1f; // 배경음 페이드인, 페이드아웃 시간
    public Sound nowBGM_Sound; // 현재 재생중인 배경음 정보
    public AudioSource nowBGM; // 현재 재생중인 배경음
    public bool isPauseBGM = false; // 배경음 일시정지
    public IEnumerator BGMCoroutine; // 배경음 코루틴

    [Header("Refer")]
    [SerializeField] AudioMixer audiomixer;
    [SerializeField] AudioMixerGroup masterMixerGroup;
    [SerializeField] AudioMixerGroup musicMixerGroup;
    [SerializeField] AudioMixerGroup sfxMixerGroup;
    [SerializeField] AudioMixerGroup uiMixerGroup;
    // [SerializeField] string startBGM;
    [SerializeField] GameObject emptyAudio;
    // public Transform soundPool_Global;
    public Transform soundPool_Music;
    public Transform soundPool_SFX;
    public Transform soundPool_UI;
    [SerializeField] AnimationCurve curve_3D; // 3D 볼륨 커브

    [Header("Sounds")]
    private List<Sound> Origin_SoundList = new List<Sound>(); // 미리 준비된 사운드 소스 (같은 사운드 동시 재생 불가)
    [SerializeField, ReadOnly] public List<AudioSource> Playing_SFXList = new List<AudioSource>(); // 재생중인 효과음 리스트
    [SerializeField, ReadOnly] public List<AudioSource> Playing_UIList = new List<AudioSource>(); // 재생중인 UI 효과음 리스트
    [SerializeField] List<SoundBundle> soundBundleList = new List<SoundBundle>();
    [SerializeField] SoundBundleList soundBundleDB; // 사운드 리스트 DB

    private void Awake()
    {
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

        masterMixerGroup = audiomixer.FindMatchingGroups("Master")[0];
        musicMixerGroup = audiomixer.FindMatchingGroups("Master/Music")[0];
        sfxMixerGroup = audiomixer.FindMatchingGroups("Master/SFX")[0];
        uiMixerGroup = audiomixer.FindMatchingGroups("Master/UI")[0];

        // SO에서 사운드 리스트 불러오기
        soundBundleList = soundBundleDB.soundBundles;

        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        int soundsNum = 0;

        foreach (SoundBundle bundle in soundBundleList)
        {
            // 번들마다 오디오소스 만들기
            AudioMake(bundle);

            // 총 개수 더하기
            soundsNum += bundle.sounds.Length;

            yield return null;
        }

        // 모든 사운드 추가 될때까지 대기
        yield return new WaitUntil(() => Origin_SoundList.Count == soundsNum);

        // 초기화 완료
        print("Sound Loaded!");
        initFinish = true;

        // 시스템 매니저 초기화 대기
        yield return new WaitUntil(() => SystemManager.Instance.initDone);

        // BGM에 아웃풋 연결
        nowBGM.outputAudioMixerGroup = musicMixerGroup;

        // // 시작 bgm 있으면 재생
        // if (startBGM != "")
        // {
        //     // 해당 이름으로 배경음 찾기
        //     Sound sound = all_Sounds.Find(x => x.name == startBGM);

        //     // 배경음 사운드 정보 갱신
        //     nowBGM_Sound = sound;

        //     // 오디오 초기화
        //     nowBGM.clip = sound.clip;
        //     nowBGM.volume = sound.volume;
        //     nowBGM.pitch = sound.pitch * globalPitch;
        //     nowBGM.Play();
        // }
        // BGM 재생
        PlayBGM();
    }

    void AudioMake(SoundBundle soundBundle)
    {
        // 빈 오브젝트 만들어 자식으로 넣기
        GameObject bundleObj = new GameObject(soundBundle.name);
        bundleObj.transform.SetParent(transform);

        // 입력된 오디오 클립을 오디오 소스로 생성
        foreach (Sound sound in soundBundle.sounds)
        {
            // 사운드 리스트에 종합
            Origin_SoundList.Add(sound);

            // 사운드 안넣었으면 넘기기
            if (sound.clip == null)
            {
                Debug.Log("Sound is Null");
                continue;
            }

            // 오디오 소스 컴포넌트 생성
            sound.source = bundleObj.AddComponent<AudioSource>();
            // 오디오 클립 넣기
            sound.source.clip = sound.clip;

            // 볼륨 및 피치 동기화
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch * globalPitch;
        }
    }

    string GetSoundType(Sound findSound)
    {
        string bundleType = "";

        // 모든 번들 조회
        foreach (SoundBundle soundBundle in soundBundleList)
        {
            // 번들의 모든 사운드 조회
            foreach (Sound sound in soundBundle.sounds)
            {
                // 찾는 사운드와 같으면
                if (sound == findSound)
                {
                    // 번들 이름 리턴
                    bundleType = soundBundle.name;
                    return bundleType;
                }
            }
        }
        return bundleType;
    }

    Transform GetSoundPool(Sound findSound)
    {
        // 리턴될 타입의 사운드풀
        Transform soundPool = null;

        // UI, 음악, 효과음 중에 볼륨 계수 구분하기
        if (GetSoundType(findSound) == "UI_Sounds")
        {
            soundPool = soundPool_UI;
            // Sound에 믹서그룹 전달
            findSound.mixerGroup = uiMixerGroup;
        }
        else if (GetSoundType(findSound) == "Music_Sounds")
        {
            soundPool = soundPool_Music;
            // Sound에 믹서그룹 전달
            findSound.mixerGroup = musicMixerGroup;
        }
        else
        {
            soundPool = soundPool_SFX;
            // Sound에 믹서그룹 전달
            findSound.mixerGroup = sfxMixerGroup;
        }

        // 해당 타입의 사운드풀 리턴
        return soundPool;
    }

    void SaveSoundList(Transform soundPool, AudioSource audio)
    {
        // 재생중인 오디오를 UI 리스트에 저장
        if (soundPool == soundPool_UI)
            Playing_UIList.Add(audio);
        // 재생중인 오디오를 SFX 리스트에 저장
        else if (soundPool == soundPool_SFX)
            Playing_SFXList.Add(audio);
    }

    private IEnumerator BGMPlayer()
    {
        // 시스템 매니저 초기화 대기
        yield return new WaitUntil(() => SystemManager.Instance.initDone);

        while (gameObject)
        {
            // 랜덤 배경음 이름 뽑기
            string soundName = "";
            // 메인메뉴일때
            if (SceneManager.GetActiveScene().name == SceneName.MainMenuScene.ToString())
                soundName = "MainMenuBGM";
            // 인게임일때
            if (SceneManager.GetActiveScene().name == SceneName.InGameScene.ToString())
                soundName = "InGameBGM_" + UnityEngine.Random.Range(1, 4);

            // 사운드 찾기
            Sound sound = Origin_SoundList.Find(x => x.name == soundName);

            // 배경음 사운드 정보 갱신
            nowBGM_Sound = sound;

            // 오디오 클립 넣기
            nowBGM.clip = sound.clip;
            // 볼륨 및 피치 초기화
            nowBGM.volume = sound.volume;
            nowBGM.pitch = sound.pitch * globalPitch;

            // 루프 없음
            nowBGM.loop = false;
            // 처음부터 재생
            nowBGM.time = 0;
            nowBGM.Play();

            // 볼륨 0으로 초기화
            nowBGM.volume = 0;
            // 서서히 원래 볼륨까지 올리기
            DOTween.To(() => nowBGM.volume, x => nowBGM.volume = x, sound.volume, bgmFadeTime)
            .SetUpdate(true);

            // bgm 켜짐 상태로 전환
            isPauseBGM = false;

            // 음악 끝날때까지 대기, 임의로 일시정지 하지 않았을때
            yield return new WaitUntil(() => !isPauseBGM && !nowBGM.isPlaying);

            // 오디오 정지
            nowBGM.Stop();
        }
    }

    public void PlayBGM()
    {
        if (BGMCoroutine != null)
        {
            // 배경음 코루틴 끄기
            StopCoroutine(BGMCoroutine);
            // 배경음 정지
            nowBGM.Pause();
        }

        // BGM 코루틴 재생
        BGMCoroutine = BGMPlayer();
        StartCoroutine(BGMCoroutine);
    }

    public void PauseBGM()
    {
        if (BGMCoroutine != null)
            // 배경음 코루틴 끄기
            StopCoroutine(BGMCoroutine);

        // 서서히 볼륨 낮추기
        DOTween.To(() => nowBGM.volume, x => nowBGM.volume = x, 0, bgmFadeTime)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            // 배경음 정지
            nowBGM.Pause();
        });
    }

    // 원하는 특성별로 오디오 소스 초기화
    AudioSource InitAudio(GameObject audioObj, Transform soundPool, Sound sound, float spatialBlend, float fadeIn = 0, float delay = 0, int loopNum = 1, bool scaledTime = true)
    {
        // 오브젝트 이름을 사운드 이름으로 동기화
        audioObj.name = sound.name;

        // 받은 Sound 데이터를 스폰된 오브젝트에 복사
        AudioSource audio = audioObj.GetComponent<AudioSource>();

        // 오디오 아웃풋에 믹서그룹 연결
        audio.outputAudioMixerGroup = sound.mixerGroup;

        // 오디오 클립 넣기
        audio.clip = sound.clip;

        // 볼륨 및 피치 초기화
        audio.volume = sound.volume;
        audio.pitch = sound.pitch * globalPitch;

        // 2D 로 초기화
        if (spatialBlend == 0)
            audio.spatialBlend = 0f;
        // 3D 로 초기화
        else
        {
            // 위치값이 들어왔으므로 3D 오디오 소스로 초기화
            audio.spatialBlend = 1f;
            audio.rolloffMode = AudioRolloffMode.Custom;
            audio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve_3D);
            audio.maxDistance = 30f;
        }

        // 재생하고 끝나면 디스폰
        StartCoroutine(Play(sound, soundPool, audio, true, fadeIn, delay, loopNum, scaledTime));

        return audio;
    }

    public Sound GetSound(string soundName)
    {
        Sound sound = Origin_SoundList.Find(x => x.name == soundName);
        return sound;
    }

    // 사운드 매니저에서 전역 사운드 재생
    public AudioSource PlaySound(string soundName, float fadeIn = 0, float delay = 0, int loopNum = 1, bool scaledTime = true)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = Origin_SoundList.Find(x => x.name == soundName);

        // 없으면 리턴
        if (sound == null || sound.source == null)
        {
            Debug.Log("Sound Not Found");
            return null;
        }

        // 사운드풀 오브젝트 참조
        Transform soundPool = GetSoundPool(sound);

        // 빈 오디오소스 프리팹을 자식으로 스폰
        GameObject audioObj = LeanPool.Spawn(emptyAudio, Vector2.zero, Quaternion.identity, soundPool);

        // 오디오 초기화 후 플레이
        AudioSource audio = InitAudio(audioObj, soundPool, sound, 0, fadeIn, delay, loopNum, scaledTime);

        return audio;
    }

    // 특정 위치에 사운드 재생
    public AudioSource PlaySound(string soundName, Vector3 position, float fadeIn = 0, float delay = 0, int loopNum = 1, bool scaledTime = false)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = Origin_SoundList.Find(x => x.name == soundName);

        if (sound == null || sound.source == null)
        {
            print("Sound Not Found");
            return null;
        }

        // 사운드풀 오브젝트 참조
        Transform soundPool = GetSoundPool(sound);

        // 빈 오디오소스 프리팹을 자식으로 스폰
        GameObject audioObj = LeanPool.Spawn(emptyAudio, position, Quaternion.identity, soundPool);

        // 오디오 초기화 후 플레이
        AudioSource audio = InitAudio(audioObj, soundPool, sound, 1, fadeIn, delay, loopNum, scaledTime);

        return audio;
    }

    // 특정 오브젝트에 오디오 소스 붙여주기
    public AudioSource PlaySound(string soundName, Transform attachor, float fadeIn = 0, float delay = 0, int loopNum = 1, bool scaledTime = false)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = Origin_SoundList.Find(x => x.name == soundName);

        if (sound == null || sound.source == null)
            return null;

        // 사운드풀 오브젝트 참조
        Transform soundPool = GetSoundPool(sound);

        // attachor에 붙은 모든 오디오 소스 찾기
        List<AudioSource> audioList = attachor.GetComponentsInChildren<AudioSource>().ToList();
        foreach (AudioSource audioSource in audioList)
        {
            // 해당 오브젝트에 이미 같은 오디오 소스가 있으면
            if (audioSource.clip == sound.clip
            && audioSource.volume == sound.volume
            && audioSource.pitch == sound.pitch * globalPitch)
            {
                // 재생하고 끝나면 디스폰
                StartCoroutine(Play(sound, soundPool, audioSource, true, fadeIn, delay, loopNum, scaledTime));

                return audioSource;
            }
        }

        // 빈 오디오소스 프리팹을 자식으로 스폰
        GameObject audioObj = LeanPool.Spawn(emptyAudio, attachor.position, Quaternion.identity, attachor);

        // 오디오 초기화 후 플레이
        AudioSource audio = InitAudio(audioObj, soundPool, sound, 1, fadeIn, delay, loopNum, scaledTime);

        return audio;
    }

    IEnumerator Play(Sound sound, Transform soundPool, AudioSource audio, bool autoDespawn, float fadeinTime, float delay, int loopNum, bool scaledTime)
    {
        // 재생중인 오디오를 리스트에 저장
        SaveSoundList(soundPool, audio);

        // 사운드 매니저 초기화 대기
        yield return new WaitUntil(() => initFinish);

        WaitForSeconds waitScaled_Delay = new WaitForSeconds(delay);
        WaitForSecondsRealtime waitUnScaled_Delay = new WaitForSecondsRealtime(delay);

        // 페이드인 시간이 0 이상이면
        if (fadeinTime > 0)
        {
            // 볼륨 0으로 초기화
            audio.volume = 0;

            // 서서히 원래 볼륨까지 올리기
            DOTween.To(() => audio.volume, x => audio.volume = x, sound.volume, fadeinTime)
            .SetUpdate(!scaledTime);
        }

        // 무한 반복일때
        if (loopNum == -1)
        {
            // 사운드 있으면 재생
            if (audio != null)
            {
                // 루프로 변경
                audio.loop = true;

                // 처음부터 재생
                audio.time = 0;
                audio.Play();
            }
        }
        // 무한 반복 아닐때
        else
            for (int i = 0; i < loopNum; i++)
            {
                // 스케일타임에 따라 딜레이 동안 대기
                if (scaledTime)
                    yield return waitScaled_Delay;
                else
                    yield return waitUnScaled_Delay;

                // 사운드 있으면 재생
                if (audio != null)
                {
                    // 루프 없음
                    audio.loop = false;

                    // 처음부터 재생
                    audio.time = 0;
                    audio.Play();
                }
            }

        WaitForSeconds waitScaled_Delta = new WaitForSeconds(Time.deltaTime);
        WaitForSecondsRealtime waitUnScaled_Delta = new WaitForSecondsRealtime(Time.unscaledDeltaTime);

        // 자동 디스폰일때
        if (autoDespawn)
        {
            // 오디오 오브젝트가 꺼지거나, 사운드 끝날때까지 대기
            yield return new WaitUntil(() => audio == null || !audio.gameObject.activeInHierarchy || !audio.isPlaying);

            // 오디오 살아있으면
            if (audio != null)
            {
                // 오디오 클립 비우기
                audio.clip = null;

                // 해당 오디오 오브젝트 제거
                if (audio.gameObject)
                    LeanPool.Despawn(audio.gameObject);

                // 오디오 리스트에서삭제
                Playing_SFXList.Remove(audio);
            }
        }
    }

    // public void StopSound(string soundName, float fadeoutTime, float delay = 0, bool scaledTime = false)
    // {
    //     // 해당 이름으로 전역 사운드 찾기
    //     Sound sound = all_Sounds.Find(x => x.name == soundName);

    //     // 사운드 있으면 멈추기
    //     if (sound.source != null)
    //         StartCoroutine(Stop(sound.source, false, fadeoutTime, delay, scaledTime));
    // }

    public void StopSound(AudioSource audio, float fadeoutTime = 0, float delay = 0, bool scaledTime = false)
    {
        // 오디오 오브젝트 디스폰
        StartCoroutine(Stop(audio, true, fadeoutTime, delay, scaledTime));
    }

    IEnumerator Stop(AudioSource audio, bool isDespawn, float fadeoutTime, float delay, bool scaledTime)
    {
        // 딜레이 동안 대기
        if (scaledTime)
            yield return new WaitForSeconds(delay);
        else
            yield return new WaitForSecondsRealtime(delay);

        // 페이드 아웃 시간이 있을때
        if (fadeoutTime > 0)
        {
            // 서서히 볼륨 제로까지 내리기
            if (scaledTime)
                DOTween.To(() => audio.volume, x => audio.volume = x, 0, fadeoutTime);
            else
                DOTween.To(() => audio.volume, x => audio.volume = x, 0, fadeoutTime)
                .SetUpdate(true);

            // 페이드 아웃 시간동안 대기
            if (scaledTime)
                yield return new WaitForSeconds(fadeoutTime);
            else
                yield return new WaitForSecondsRealtime(fadeoutTime);
        }

        if (audio != null)
        {
            // 오디오 정지
            audio.Stop();

            // 디스폰일때
            if (isDespawn)
            {
                // 오디오 클립 비우기
                audio.clip = null;

                // 오디오 오브젝트 디스폰
                if (audio.gameObject.activeSelf)
                    LeanPool.Despawn(audio.gameObject);
            }
        }
    }

    public void SoundTimeScale(float scale, float fadeTime, bool scaledTime = false)
    {
        StartCoroutine(ChangeAll_Pitch(scale, fadeTime, scaledTime));
    }

    // 재생중인 오디오들의 피치값 조정
    public IEnumerator ChangeAll_Pitch(float scale, float fadeTime, bool scaledTime)
    {
        // 사운드 매니저 초기화 대기
        yield return new WaitUntil(() => initFinish);
        // // 오브젝트풀 없으면 리턴
        // if (ObjectPool.Instance == null)
        //     yield break;

        // 글로벌 재생중인 원본 오디오들의 피치값 조정
        foreach (Sound sound in Origin_SoundList)
        {
            if (sound.source != null)
                DOTween.To(() => sound.source.pitch, x => sound.source.pitch = x, sound.pitch * scale * globalPitch, fadeTime)
                .SetUpdate(!scaledTime);
        }

        // 효과음들의 피치값 조정
        for (int i = 0; i < Playing_SFXList.Count; i++)
        {
            AudioSource audio = Playing_SFXList[i];

            if (audio != null)
            {
                // 오브젝트 이름으로 원본 사운드 찾기
                Sound sound = Origin_SoundList.Find(x => x.name == audio.name);

                DOTween.To(() => audio.pitch, x => audio.pitch = x, sound.pitch * scale * globalPitch, fadeTime)
                .SetUpdate(!scaledTime);
            }
        }
    }

    public void Set_MasterVolume(float setVolume)
    {
        // 마스터 볼륨값 저장
        PlayerPrefs.SetFloat(SaveManager.MASTER_VOLUME_KEY, setVolume);

        // 믹서에 볼륨값 갱신
        audiomixer.SetFloat(masterMixerGroup.name + "Volume", Mathf.Log10(setVolume) * 20);
    }
    public void Set_MusicVolume(float setVolume)
    {
        // 배경음 볼륨값 저장
        PlayerPrefs.SetFloat(SaveManager.MUSIC_VOLUME_KEY, setVolume);

        // 믹서에 볼륨값 갱신
        audiomixer.SetFloat(musicMixerGroup.name + "Volume", Mathf.Log10(setVolume) * 20);
    }
    public void Set_SFXVolume(float setVolume)
    {
        // 효과음 볼륨값 저장
        PlayerPrefs.SetFloat(SaveManager.SFX_VOLUME_KEY, setVolume);

        float volume = Mathf.Log10(setVolume) * 20;
        // 메인화면이면 볼륨 0으로 적용
        if (SystemManager.Instance.GetSceneName() != SceneName.InGameScene.ToString())
            volume = -80f;

        // 믹서에 볼륨값 갱신
        audiomixer.SetFloat(sfxMixerGroup.name + "Volume", volume);
    }

    public void Set_UIVolume(float setVolume)
    {
        // UI 볼륨값 저장
        PlayerPrefs.SetFloat(SaveManager.UI_VOLUME_KEY, setVolume);

        // 믹서에 볼륨값 갱신
        audiomixer.SetFloat(uiMixerGroup.name + "Volume", Mathf.Log10(setVolume) * 20);
    }

    public int PlaySoundPool(List<string> soundPool, Vector2 playPos = default, int lastPlayed_Index = -1)
    {
        // lastPlayed_Index = 마지막으로 재생된 사운드의 인덱스
        // 해당 인덱스는 풀에서 제거
        if (lastPlayed_Index != -1)
            soundPool.RemoveAt(lastPlayed_Index);

        // 풀에서 뽑힌 인덱스 저장
        lastPlayed_Index = UnityEngine.Random.Range(0, soundPool.Count);

        // 뽑은 사운드 이름
        string soundName = soundPool[lastPlayed_Index];

        if (playPos == default)
            // 사운드 이름으로 찾아 재생
            SoundManager.Instance.PlaySound(soundName);
        else
            // 사운드 이름으로 찾아 해당 위치에 재생
            SoundManager.Instance.PlaySound(soundName, playPos);

        // 선택된 인덱스 리턴
        return lastPlayed_Index;
    }

    public float GetVolume(string soundName)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = Origin_SoundList.Find(x => x.name == soundName);

        if (sound == null || sound.source == null)
            print("Sound Not Found");

        return sound.source.volume;
    }

    public float GetVolume(string soundName, Transform attachor = null)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = Origin_SoundList.Find(x => x.name == soundName);

        if (sound == null || sound.source == null)
            print("Sound Not Found");

        // attachor 입력 없으면 글로벌 사운드의 볼륨 리턴
        if (attachor == null)
            return sound.source.volume;
        else
        {
            // attachor에 붙은 모든 오디오 소스 찾기
            List<AudioSource> audioList = attachor.GetComponentsInChildren<AudioSource>().ToList();
            foreach (AudioSource audioSource in audioList)
            {
                // 해당 오브젝트에 이미 같은 오디오 소스가 있으면
                if (audioSource.clip == sound.clip
                && audioSource.pitch == sound.pitch * globalPitch)
                    // attachor에 붙은 오디오 볼륨 리턴
                    return audioSource.volume;
            }

            return -1;
        }
    }

    public void VolumeChange(string soundName, float volumeMultiple, float changeTime = 0)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = Origin_SoundList.Find(x => x.name == soundName);

        if (sound == null || sound.source == null)
            print("Sound Not Found");

        // 볼륨 교체 시간이 있으면
        if (changeTime > 0)
        {
            // 부드럽게 체인지
            DOTween.To(() => sound.source.volume, x => sound.source.volume = x, sound.volume * volumeMultiple, changeTime);
        }
        else
            // 볼륨 및 피치 초기화
            sound.source.volume = sound.volume * volumeMultiple;
    }

    public void VolumeChange(string soundName, Transform attachor, float volumeMultiple, float changeTime = 0)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = Origin_SoundList.Find(x => x.name == soundName);

        if (sound == null || sound.source == null)
            print("Sound Not Found");

        // attachor에 붙은 모든 오디오 소스 찾기
        List<AudioSource> audioList = attachor.GetComponentsInChildren<AudioSource>().ToList();
        foreach (AudioSource audioSource in audioList)
        {
            // 해당 오브젝트에 같은 오디오 소스가 있으면 볼륨 변경 후 리턴
            if (audioSource.clip == sound.clip
            && audioSource.pitch == sound.pitch * globalPitch)
            {
                // 볼륨 교체 시간이 있으면
                if (changeTime > 0)
                {
                    // 부드럽게 체인지
                    DOTween.To(() => audioSource.volume, x => audioSource.volume = x, sound.volume * volumeMultiple, changeTime);
                }
                else
                    // 볼륨 및 피치 초기화
                    audioSource.volume = sound.volume * volumeMultiple;

                return;
            }
        }
    }

    public void DestoryAllSound()
    {
        // 플레이중인 모든 오디오 정지
        foreach (AudioSource audio in Playing_SFXList)
        {
            // 오디오 살아있으면
            if (audio != null)
            {
                // 오디오 클립 비우기
                audio.clip = null;

                // 해당 오디오 오브젝트 제거
                if (audio.gameObject)
                    LeanPool.Despawn(audio.gameObject);
            }
        }

        // 재생 리스트 비우기
        Playing_SFXList.Clear();
    }
}
