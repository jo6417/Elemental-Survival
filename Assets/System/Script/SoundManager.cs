using System;
using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

[System.Serializable]
public class Sound
{
    public string name;

    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 0.5f;
    [Range(0f, 3f)]
    public float pitch = 1f;

    public AudioSource source;
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
                var obj = FindObjectOfType<SoundManager>();
                if (obj != null)
                {
                    instance = obj;
                }
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

    [ReadOnly] public bool init = false;

    [Header("Refer")]
    [SerializeField] GameObject emptyAudio;
    [SerializeField] Transform soundPool;

    [Header("Sounds")]
    private List<Sound> all_Sounds = new List<Sound>(); // 미리 준비된 사운드 소스 (같은 사운드 동시 재생 불가)
    public Sound[] UI_Sounds;
    public Sound[] music_Sounds;
    public Sound[] player_Sounds;
    public Sound[] magic_Sounds;
    public Sound[] item_Sounds;
    public Sound[] enemy_Sounds;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 거리에 상관없이 똑같이 재생되는 사운드
        AudioMake(player_Sounds, nameof(player_Sounds));
        AudioMake(UI_Sounds, nameof(UI_Sounds));
        AudioMake(music_Sounds, nameof(music_Sounds));

        // 거리에 따라 음량 변하는 사운드
        AudioMake(magic_Sounds, nameof(magic_Sounds));
        AudioMake(item_Sounds, nameof(item_Sounds));
        AudioMake(enemy_Sounds, nameof(enemy_Sounds));

        // 모든 사운드 추가 될때까지 대기
        yield return new WaitUntil(() => all_Sounds.Count ==
        UI_Sounds.Length +
        music_Sounds.Length +
        player_Sounds.Length +
        magic_Sounds.Length +
        item_Sounds.Length +
        enemy_Sounds.Length);

        // 초기화 완료
        print("SoundManager Init");
        init = true;
    }

    void AudioMake(Sound[] sounds, string bundleName)
    {
        // 빈 오브젝트 만들어 자식으로 넣기
        GameObject soundsBundle = new GameObject(bundleName);
        soundsBundle.transform.SetParent(transform);

        // 입력된 오디오 클립을 오디오 소스로 생성
        foreach (Sound sound in sounds)
        {
            // 사운드 안넣었으면 넘기기
            if (sound.clip == null)
            {
                Debug.Log("Sound is Null");
                continue;
            }

            // 오디오 소스 컴포넌트 생성
            sound.source = soundsBundle.AddComponent<AudioSource>();
            // 오디오 클립 넣기
            sound.source.clip = sound.clip;

            // 볼륨 및 피치 동기화
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;

            // 사운드 리스트에 종합
            all_Sounds.Add(sound);
        }
    }

    // 사운드 매니저에서 전역 사운드 재생
    public void PlaySound(string soundName, float delay = 0, int loopNum = 1, bool scaledTime = true)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = all_Sounds.Find(x => x.name == soundName);

        // 볼륨 및 피치 초기화
        sound.source.volume = sound.volume;
        sound.source.pitch = sound.pitch;

        // 오디오 소스가 있으면 플레이
        if (sound.source != null)
            StartCoroutine(Play(false, sound.source, delay, loopNum, scaledTime));
    }

    // 특정 위치에 사운드 재생
    public AudioSource PlaySound(string soundName, Vector3 position, float delay = 0, int loopNum = 1, bool scaledTime = false)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = all_Sounds.Find(x => x.name == soundName);

        if (sound == null)
            return null;

        // 빈 오디오소스 프리팹을 자식으로 스폰
        GameObject audioObj = LeanPool.Spawn(emptyAudio, position, Quaternion.identity, soundPool);

        // 오브젝트 이름을 사운드 이름으로 동기화
        audioObj.name = soundName;

        // 받은 Sound 데이터를 스폰된 오브젝트에 복사
        AudioSource audio = audioObj.GetComponent<AudioSource>();

        // 오디오 클립 넣기
        audio.clip = sound.clip;

        // 볼륨 및 피치 초기화
        audio.volume = sound.volume;
        audio.pitch = sound.pitch;

        // 위치값이 들어왔으므로 3D 오디오 소스로 초기화
        audio.spatialBlend = 1f;
        audio.rolloffMode = AudioRolloffMode.Custom;
        audio.maxDistance = 35f;

        // 재생 끝나면 디스폰
        StartCoroutine(Play(true, audio, delay, loopNum, scaledTime));

        return audio;
    }

    // 특정 오브젝트에 오디오 소스 붙여주기
    public AudioSource PlaySound(string soundName, Transform attachor, float delay = 0, int loopNum = 1, bool scaledTime = false)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = all_Sounds.Find(x => x.name == soundName);

        if (sound == null)
            return null;

        //todo 해당 오브젝트에 이미 같은 오디오 소스가 있으면 리턴
        if (attachor.TryGetComponent(out AudioSource audioSource))
        {
            if (audioSource.clip == sound.clip
            && audioSource.volume == sound.volume
            && audioSource.pitch == sound.pitch)
                return null;
        }

        // 빈 오디오소스 프리팹을 attachor에 자식으로 붙여주기
        GameObject audioObj = LeanPool.Spawn(emptyAudio, attachor.position, Quaternion.identity, attachor);

        // 오브젝트 이름을 사운드 이름으로 동기화
        audioObj.name = soundName;

        // 받은 Sound 데이터를 스폰된 오브젝트에 복사
        AudioSource audio = audioObj.GetComponent<AudioSource>();

        // 오디오 클립 넣기
        audio.clip = sound.clip;

        // 볼륨 및 피치 초기화
        audio.volume = sound.volume;
        audio.pitch = sound.pitch;

        // 위치값이 들어왔으므로 3D 오디오 소스로 초기화
        audio.spatialBlend = 1f;
        audio.rolloffMode = AudioRolloffMode.Custom;
        audio.maxDistance = 35f;

        // 재생 끝나면 디스폰
        StartCoroutine(Play(true, audio, delay, loopNum, scaledTime));

        return audio;
    }

    IEnumerator Play(bool autoDespawn, AudioSource audio, float delay, int loopNum, bool scaledTime)
    {
        // 사운드 매니저 초기화 대기
        yield return new WaitUntil(() => init);

        WaitForSeconds waitScaled_Delay = new WaitForSeconds(delay);
        WaitForSecondsRealtime waitUnScaled_Delay = new WaitForSecondsRealtime(delay);

        // 무한 반복일때
        if (loopNum == -1)
        {
            // 사운드 있으면 재생
            if (audio != null)
            {
                // 루프로 변경
                audio.loop = true;

                audio.Play();
            }
        }
        else
            for (int i = 0; i < loopNum; i++)
            {
                // 사운드 있으면 재생
                if (audio != null)
                {
                    // 루프 없음
                    audio.loop = false;

                    audio.Play();
                }

                // 스케일타임에 따라 딜레이 동안 대기
                if (scaledTime)
                    yield return waitScaled_Delay;
                else
                    yield return waitUnScaled_Delay;
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
                // 해당 오디오 오브젝트 제거
                LeanPool.Despawn(audio.gameObject);
        }
    }

    public void StopSound(string soundName, float delay = 0, bool scaledTime = false)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = all_Sounds.Find(x => x.name == soundName);

        // 사운드 있으면 멈추기
        if (sound.source != null)
            StartCoroutine(Stop(sound.source, delay, scaledTime));
    }

    public void StopSound(AudioSource audio, float delay = 0, bool scaledTime = false)
    {
        // 오디오 오브젝트 디스폰
        if (audio != null)
            LeanPool.Despawn(audio.gameObject);
    }

    IEnumerator Stop(AudioSource audio, float delay, bool scaledTime)
    {
        if (scaledTime)
            // 딜레이 동안 대기
            yield return new WaitForSeconds(delay);
        else
            // 딜레이 동안 대기
            yield return new WaitForSecondsRealtime(delay);

        // 사운드 재생
        audio.Stop();
    }

    public void SoundTimeScale(float scale)
    {
        StartCoroutine(ChangePitch(scale));
    }

    IEnumerator ChangePitch(float scale)
    {
        // 사운드 매니저 초기화 대기
        yield return new WaitUntil(() => init);

        // 플레이어 사운드들의 pitch 디폴트 값에 타임스케일 곱하기
        foreach (Sound sound in player_Sounds)
            sound.source.pitch = sound.pitch * scale;
        // 마법 사운드들의 pitch 디폴트 값에 타임스케일 곱하기
        foreach (Sound sound in magic_Sounds)
            sound.source.pitch = sound.pitch * scale;
        // 아이템 사운드들의 pitch 디폴트 값에 타임스케일 곱하기
        foreach (Sound sound in item_Sounds)
            sound.source.pitch = sound.pitch * scale;
        // 몬스터 사운드들의 pitch 디폴트 값에 타임스케일 곱하기
        foreach (Sound sound in enemy_Sounds)
            sound.source.pitch = sound.pitch * scale;

        // 모든 자식 오디오소스 오브젝트의 피치에 타임스케일 곱하기
        for (int i = 0; i < transform.childCount; i++)
        {
            // 자식중에 오디오 찾기
            AudioSource audio = transform.GetChild(i).GetComponent<AudioSource>();

            // 오브젝트 이름으로 사운드 찾기
            Sound sound = all_Sounds.Find(x => x.name == audio.gameObject.name);

            // 해당 오디오 소스의 피치값을 원본 피치값 * 타임스케일 넣기
            audio.pitch = sound.pitch * scale;
        }
    }
}
