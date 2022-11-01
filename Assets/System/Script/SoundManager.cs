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
    [Range(0.1f, 3f)]
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

    private List<Sound> all_Sounds = new List<Sound>(); // 미리 준비된 사운드 소스 (같은 사운드 동시 재생 불가)
    public Sound[] UI_Sounds;

    public Sound[] music_Sounds;
    public Sound[] player_Sounds;

    public Sound[] magic_Sounds;
    public Sound[] item_Sounds;
    public Sound[] enemy_Sounds;

    [SerializeField] GameObject emptyAudio;

    bool init = false;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // 거리에 상관없이 똑같이 재생
        AudioMake(player_Sounds);
        AudioMake(UI_Sounds);
        AudioMake(music_Sounds);

        // 거리에 따라 음량 변하는 사운드
        AudioMake(magic_Sounds);
        AudioMake(item_Sounds);
        AudioMake(enemy_Sounds);

        // 초기화 완료
        init = true;
    }

    void AudioMake(Sound[] sounds)
    {
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
            sound.source = gameObject.AddComponent<AudioSource>();
            // 오디오 클립 넣기
            sound.source.clip = sound.clip;

            // 볼륨 및 피치 동기화
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;

            // 사운드 리스트에 종합
            all_Sounds.Add(sound);
        }
    }

    public AudioSource SoundPlay(string soundName, Transform soundPoint, float delay = 0, int loopNum = 1, bool scaledTime = false)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = all_Sounds.Find(x => x.name == soundName);

        if (sound == null)
            return null;

        // 빈 오디오소스 프리팹을 자식으로 스폰
        GameObject audioObj = LeanPool.Spawn(emptyAudio, soundPoint.position, Quaternion.identity, transform);

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

    public void SoundPlay(string soundName, float delay = 0, int loopNum = 1, bool scaledTime = false)
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

    IEnumerator Play(bool autoDespawn, AudioSource audio, float delay, int loopNum, bool scaledTime)
    {
        WaitForSeconds waitScaled_Delay = new WaitForSeconds(delay);
        WaitForSecondsRealtime waitUnScaled_Delay = new WaitForSecondsRealtime(delay);

        for (int i = 0; i < loopNum; i++)
        {
            // 스케일타임에 따라 딜레이 동안 대기
            if (scaledTime)
                yield return waitScaled_Delay;
            else
                yield return waitUnScaled_Delay;

            // 사운드 있으면 재생
            if (audio != null)
                audio.Play();
        }

        WaitForSeconds waitScaled_Delta = new WaitForSeconds(Time.deltaTime);
        WaitForSecondsRealtime waitUnScaled_Delta = new WaitForSecondsRealtime(Time.unscaledDeltaTime);

        // 자동 디스폰일때
        if (autoDespawn)
        {
            // 재생중이면 반복
            while (audio.isPlaying)
            {
                // 디스폰 됬으면 그만
                if (audio == null)
                    break;

                // 스케일타임에 따라 딜레이 동안 대기
                if (scaledTime)
                    yield return waitScaled_Delta;
                else
                    yield return waitUnScaled_Delta;
            }

            // 오디오 살아있으면
            if (audio != null)
                // 해당 오디오 오브젝트 제거
                LeanPool.Despawn(audio.gameObject);
        }
    }

    public void SoundStop(string soundName, float delay = 0, bool scaledTime = false)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = all_Sounds.Find(x => x.name == soundName);

        // 사운드 있으면 멈추기
        if (sound.source != null)
            StartCoroutine(Stop(sound.source, delay, scaledTime));
    }

    public void SoundStop(AudioSource audio, float delay = 0, bool scaledTime = false)
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
        if (!init)
            yield return new WaitUntil(() => init);

        // 해당 사운드 리스트 소스의 pitch 디폴트 값에 타임스케일 곱하기
        foreach (Sound sound in magic_Sounds)
            sound.source.pitch = sound.pitch * scale;
        // 해당 사운드 리스트 소스의 pitch 디폴트 값에 타임스케일 곱하기
        foreach (Sound sound in player_Sounds)
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
