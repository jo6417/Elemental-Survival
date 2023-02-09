using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;

    public AudioClip clip;

    [Range(0f, 3f)]
    public float volume = 0.5f;
    [Range(0f, 3f)]
    public float pitch = 1f;

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
    [ReadOnly] public float masterVolume = 1f; // 전체 음량
    [ReadOnly] public float musicVolume = 1f; // 배경음 음량
    [ReadOnly] public float sfxVolume = 1f; // 효과음 음량
    [ReadOnly] public float uiVolume = 1f; // UI 음량    
    [ReadOnly] public float globalPitch = 1f; // 전체 사운드 속도 계수
    [ReadOnly] public bool initFinish = false;
    [SerializeField] float bgmFadeTime = 1f; // 배경음 페이드인, 페이드아웃 시간
    public Sound nowBGM_Sound; // 현재 재생중인 배경음 정보
    public AudioSource nowBGM; // 현재 재생중인 배경음
    public bool bgmPause = false; // 배경음 일시정지
    public IEnumerator BGMCoroutine; // 배경음 코루틴

    [Header("Refer")]
    [SerializeField] string startBGM;
    [SerializeField] GameObject emptyAudio;
    // public Transform soundPool_Global;
    public Transform soundPool_Music;
    public Transform soundPool_SFX;
    public Transform soundPool_UI;
    [SerializeField] AnimationCurve curve_3D; // 3D 볼륨 커브

    [Header("Sounds")]
    private List<AudioSource> playing_Sounds = new List<AudioSource>(); // 오브젝트에 붙인 사운드
    private List<Sound> all_Sounds = new List<Sound>(); // 미리 준비된 사운드 소스 (같은 사운드 동시 재생 불가)
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
        yield return new WaitUntil(() => all_Sounds.Count == soundsNum);

        // 초기화 완료
        print("Sound Loaded!");
        initFinish = true;

        // 시스템 매니저 초기화 대기
        yield return new WaitUntil(() => SystemManager.Instance.loadDone);

        // 시작 bgm 있으면 재생
        if (startBGM != "")
        {
            // 해당 이름으로 배경음 찾기
            Sound sound = all_Sounds.Find(x => x.name == startBGM);

            // 배경음 사운드 정보 갱신
            nowBGM_Sound = sound;

            // 오디오 초기화
            nowBGM.clip = sound.clip;
            nowBGM.volume = sound.volume * masterVolume * musicVolume;
            nowBGM.pitch = sound.pitch * globalPitch;
            nowBGM.Play();
        }
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
            all_Sounds.Add(sound);

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

    float GetVolumeType(Sound findSound)
    {
        // 리턴될 타입의 볼륨 계수
        float typeVolume = 0;

        // UI, 음악, 효과음 중에 볼륨 계수 구분하기
        if (GetSoundType(findSound) == "UI_Sounds")
            typeVolume = uiVolume;
        else if (GetSoundType(findSound) == "Music_Sounds")
            typeVolume = musicVolume;
        else
            typeVolume = sfxVolume;

        // 해당 타입의 볼륨 리턴
        return typeVolume;
    }

    Transform GetSoundPool(Sound findSound)
    {
        // 리턴될 타입의 사운드풀
        Transform soundPool = null;

        // UI, 음악, 효과음 중에 볼륨 계수 구분하기
        if (GetSoundType(findSound) == "UI_Sounds")
            soundPool = soundPool_UI;
        else if (GetSoundType(findSound) == "Music_Sounds")
            soundPool = soundPool_Music;
        else
            soundPool = soundPool_SFX;

        // 해당 타입의 사운드풀 리턴
        return soundPool;
    }

    public IEnumerator BGMPlayer()
    {
        // 시스템 매니저 초기화 대기
        yield return new WaitUntil(() => SystemManager.Instance.loadDone);

        while (gameObject)
        {
            // 랜덤 배경음 이름 뽑기
            string soundName = "InGameBGM_" + UnityEngine.Random.Range(1, 4);

            // print(soundName);

            // 사운드 찾기
            Sound sound = all_Sounds.Find(x => x.name == soundName);

            // 배경음 사운드 정보 갱신
            nowBGM_Sound = sound;

            // 오디오 클립 넣기
            nowBGM.clip = sound.clip;
            // 볼륨 및 피치 초기화
            nowBGM.volume = sound.volume * masterVolume * musicVolume;
            nowBGM.pitch = sound.pitch * globalPitch;

            // 루프 없음
            nowBGM.loop = false;
            // 처음부터 재생
            nowBGM.time = 0;
            nowBGM.Play();

            // 볼륨 0으로 초기화
            nowBGM.volume = 0;
            // 서서히 원래 볼륨까지 올리기
            DOTween.To(() => nowBGM.volume, x => nowBGM.volume = x, sound.volume * masterVolume * musicVolume, bgmFadeTime)
            .SetUpdate(true);

            // 음악 끝날때까지 대기, 일시정지 아닐때
            yield return new WaitUntil(() => !bgmPause && !nowBGM.isPlaying);

            // 오디오 정지
            nowBGM.Stop();
        }
    }

    AudioSource InitAudio(GameObject audioObj, Sound sound, float spatialBlend, float fadeIn = 0, float delay = 0, int loopNum = 1, bool scaledTime = true)
    {
        // 오브젝트 이름을 사운드 이름으로 동기화
        audioObj.name = sound.name;

        // 받은 Sound 데이터를 스폰된 오브젝트에 복사
        AudioSource audio = audioObj.GetComponent<AudioSource>();

        // 오디오 클립 넣기
        audio.clip = sound.clip;

        // 볼륨 및 피치 초기화
        audio.volume = sound.volume * masterVolume * GetVolumeType(sound);
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
            audio.maxDistance = 50f;
        }

        // 재생하고 끝나면 디스폰
        StartCoroutine(Play(sound, audio, true, fadeIn, delay, loopNum, scaledTime));

        // 재생중인 오디오를 기억
        playing_Sounds.Add(audio);

        return audio;
    }

    public Sound GetSound(string soundName)
    {
        Sound sound = all_Sounds.Find(x => x.name == soundName);
        return sound;
    }

    // 사운드 매니저에서 전역 사운드 재생
    public AudioSource PlaySound(string soundName, float fadeIn = 0, float delay = 0, int loopNum = 1, bool scaledTime = true)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = all_Sounds.Find(x => x.name == soundName);

        // 없으면 리턴
        if (sound == null || sound.source == null)
        {
            Debug.Log("Sound Not Found");
            return null;
        }

        // 빈 오디오소스 프리팹을 자식으로 스폰
        GameObject audioObj = LeanPool.Spawn(emptyAudio, Vector2.zero, Quaternion.identity, GetSoundPool(sound));

        // 오디오 초기화 후 플레이
        AudioSource audio = InitAudio(audioObj, sound, 0, fadeIn, delay, loopNum, scaledTime);

        return audio;
    }

    // 특정 위치에 사운드 재생
    public AudioSource PlaySound(string soundName, Vector3 position, float fadeIn = 0, float delay = 0, int loopNum = 1, bool scaledTime = false)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = all_Sounds.Find(x => x.name == soundName);

        if (sound == null || sound.source == null)
        {
            print("Sound Not Found");
            return null;
        }

        // 빈 오디오소스 프리팹을 자식으로 스폰
        GameObject audioObj = LeanPool.Spawn(emptyAudio, position, Quaternion.identity, GetSoundPool(sound));

        // 오디오 초기화 후 플레이
        AudioSource audio = InitAudio(audioObj, sound, 1, fadeIn, delay, loopNum, scaledTime);

        return audio;
    }

    // 특정 오브젝트에 오디오 소스 붙여주기
    public AudioSource PlaySound(string soundName, Transform attachor, float fadeIn = 0, float delay = 0, int loopNum = 1, bool scaledTime = false)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = all_Sounds.Find(x => x.name == soundName);

        if (sound == null || sound.source == null)
            return null;

        // attachor에 붙은 모든 오디오 소스 찾기
        List<AudioSource> audioList = attachor.GetComponentsInChildren<AudioSource>().ToList();
        foreach (AudioSource audioSource in audioList)
        {
            // 해당 오브젝트에 이미 같은 오디오 소스가 있으면
            if (audioSource.clip == sound.clip
            && audioSource.volume == sound.volume * masterVolume * GetVolumeType(sound)
            && audioSource.pitch == sound.pitch * globalPitch)
            {
                // 재생하고 끝나면 디스폰
                StartCoroutine(Play(sound, audioSource, true, fadeIn, delay, loopNum, scaledTime));

                return audioSource;
            }
        }

        // 빈 오디오소스 프리팹을 자식으로 스폰
        GameObject audioObj = LeanPool.Spawn(emptyAudio, attachor.position, Quaternion.identity, attachor);

        // 오디오 초기화 후 플레이
        AudioSource audio = InitAudio(audioObj, sound, 1, fadeIn, delay, loopNum, scaledTime);

        return audio;
    }

    IEnumerator Play(Sound sound, AudioSource audio, bool autoDespawn, float fadeinTime, float delay, int loopNum, bool scaledTime)
    {
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
            DOTween.To(() => audio.volume, x => audio.volume = x, sound.volume * masterVolume * GetVolumeType(sound), fadeinTime)
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
                // 오디오 리스트에서삭제
                playing_Sounds.Remove(audio);

                // 오디오 클립 비우기
                audio.clip = null;

                // 해당 오디오 오브젝트 제거
                LeanPool.Despawn(audio.gameObject);
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
                LeanPool.Despawn(audio.gameObject);
            }
        }
    }

    public void SoundTimeScale(float scale, float fadeTime, bool unscaledTime = true)
    {
        StartCoroutine(ChangeAll_Pitch(scale, fadeTime, unscaledTime));
    }

    IEnumerator ChangeAll_Pitch(float scale, float fadeTime, bool unscaledTime)
    {
        // 사운드 매니저 초기화 대기
        yield return new WaitUntil(() => initFinish);
        // 오브젝트풀 없으면 리턴
        if (ObjectPool.Instance == null)
            yield break;
        // 사운드풀 불러올때까지 대기
        // yield return new WaitUntil(() => soundPool_Global != null);

        // 글로벌 피치값 수정
        // DOTween.To(() => globalPitch, x => globalPitch = x, scale, fadeTime)
        // .SetUpdate(unscaledTime);

        // 월드에서 재생중인 오디오들의 피치값 조정
        foreach (Sound sound in all_Sounds)
            if (sound.source != null)
                DOTween.To(() => sound.source.pitch, x => sound.source.pitch = x, sound.pitch * scale * globalPitch, fadeTime)
                .SetUpdate(unscaledTime);

        // 오브젝트에 붙인 오디오들의 피치값 조정
        foreach (AudioSource audio in playing_Sounds)
            if (audio != null)
            {
                // 오브젝트 이름으로 사운드 찾기
                Sound sound = all_Sounds.Find(x => x.name == audio.name);

                DOTween.To(() => audio.pitch, x => audio.pitch = x, sound.pitch * scale * globalPitch, fadeTime)
                .SetUpdate(unscaledTime);
            }

        // // 효과음 사운드풀 하위 오디오들의 피치값 조정
        // for (int i = 0; i < soundPool_SFX.childCount; i++)
        // {
        //     // 자식중에 오디오 찾기
        //     AudioSource audio = soundPool_SFX.GetChild(i).GetComponent<AudioSource>();

        //     // 오브젝트 이름으로 사운드 찾기
        //     Sound sound = all_Sounds.Find(x => x.name == audio.name);

        //     // 해당 오디오 소스의 피치값을 원본 피치값 * 타임스케일 넣기
        //     DOTween.To(() => audio.pitch, x => audio.pitch = x, sound.pitch * scale * globalPitch, fadeTime)
        //     .SetUpdate(unscaledTime);
        // }

        // // UI 사운드풀 하위 오디오들의 피치값 조정
        // for (int i = 0; i < soundPool_UI.childCount; i++)
        // {
        //     // 자식중에 오디오 찾기
        //     AudioSource audio = soundPool_UI.GetChild(i).GetComponent<AudioSource>();

        //     // 오브젝트 이름으로 사운드 찾기
        //     Sound sound = all_Sounds.Find(x => x.name == audio.name);

        //     // 해당 오디오 소스의 피치값을 원본 피치값 * 타임스케일 넣기
        //     DOTween.To(() => audio.pitch, x => audio.pitch = x, sound.pitch * scale * globalPitch, fadeTime)
        //     .SetUpdate(unscaledTime);
        // }
    }

    public void Set_MasterVolume(float setVolume)
    {
        // 마스터 볼륨값 수정
        masterVolume = setVolume;

        // 재생중인 배경음 있을때
        // if (nowBGM != null && nowBGM_Sound != null)
        // 배경음 볼륨 갱신
        nowBGM.volume = nowBGM_Sound.volume * masterVolume * musicVolume;

        StartCoroutine(SetAll_Volume());
    }
    public void Set_BGMVolume(float setVolume)
    {
        // 배경음 볼륨값 수정
        musicVolume = setVolume;

        // 재생중인 배경음 있을때
        // if (nowBGM != null && nowBGM_Sound != null)
        // 배경음 볼륨 갱신
        nowBGM.volume = nowBGM_Sound.volume * masterVolume * musicVolume;
    }
    public void Set_SFXVolume(float setVolume)
    {
        // 효과음 볼륨값 수정
        sfxVolume = setVolume;

        StartCoroutine(SetAll_Volume());
    }

    public void Set_UIVolume(float setVolume)
    {
        // UI 볼륨값 수정
        uiVolume = setVolume;

        StartCoroutine(SetAll_Volume());
    }

    IEnumerator SetAll_Volume()
    {
        // 사운드 매니저 초기화 대기
        yield return new WaitUntil(() => initFinish);
        // 오브젝트풀 없으면 리턴
        if (ObjectPool.Instance == null)
            yield break;
        // 사운드풀 불러올때까지 대기
        // yield return new WaitUntil(() => soundPool_Global != null);

        // 모든 사운드의 디폴트 volume 값에 타임스케일 곱하기
        foreach (Sound sound in all_Sounds)
            if (sound.source != null)
                sound.source.volume = sound.volume * masterVolume * GetVolumeType(sound);

        // 오브젝트에 붙인 사운드들 volume 값 조정
        foreach (AudioSource audio in playing_Sounds)
            if (audio != null)
            {
                // 오브젝트 이름으로 사운드 찾기
                Sound sound = all_Sounds.Find(x => x.name == audio.name);

                audio.volume = sound.volume * masterVolume * GetVolumeType(sound);
            }

        // 모든 자식 오디오소스 오브젝트의 volume에 타임스케일 곱하기
        for (int i = 0; i < soundPool_SFX.childCount; i++)
        {
            // 자식중에 오디오 찾기
            AudioSource audio = soundPool_SFX.GetChild(i).GetComponent<AudioSource>();

            // 오브젝트 이름으로 사운드 찾기
            Sound sound = all_Sounds.Find(x => x.name == audio.name);

            // 해당 오디오 소스의 volume값을 원본 volume값 * 타임스케일 넣기
            audio.volume = sound.volume * masterVolume * sfxVolume;
        }
        // 모든 자식 오디오소스 오브젝트의 volume에 타임스케일 곱하기
        for (int i = 0; i < soundPool_UI.childCount; i++)
        {
            // 자식중에 오디오 찾기
            AudioSource audio = soundPool_UI.GetChild(i).GetComponent<AudioSource>();

            // 오브젝트 이름으로 사운드 찾기
            Sound sound = all_Sounds.Find(x => x.name == audio.name);

            // 해당 오디오 소스의 volume값을 원본 volume값 * 타임스케일 넣기
            audio.volume = sound.volume * masterVolume * sfxVolume;
        }
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
        Sound sound = all_Sounds.Find(x => x.name == soundName);

        if (sound == null || sound.source == null)
            print("Sound Not Found");

        return sound.source.volume;
    }

    public float GetVolume(string soundName, Transform attachor = null)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = all_Sounds.Find(x => x.name == soundName);

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
        Sound sound = all_Sounds.Find(x => x.name == soundName);

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
        Sound sound = all_Sounds.Find(x => x.name == soundName);

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
}
