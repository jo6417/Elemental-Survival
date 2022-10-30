using System;
using System.Collections;
using System.Collections.Generic;
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

    private List<Sound> all_Sounds = new List<Sound>();
    public Sound[] player_Sounds;
    public Sound[] UI_Sounds;
    public Sound[] magic_Sounds;
    public Sound[] item_Sounds;
    public Sound[] enemy_Sounds;
    public Sound[] music_Sounds;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        AudioMake(player_Sounds);
        AudioMake(UI_Sounds);
        AudioMake(magic_Sounds);
        AudioMake(music_Sounds);
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

    public void Play(string soundName, float delay = 0)
    {
        StartCoroutine(PlayCoroutine(soundName, delay));
    }

    IEnumerator PlayCoroutine(string soundName, float delay)
    {
        // 딜레이 동안 대기
        yield return new WaitForSeconds(delay);

        // 해당 이름으로 사운드 찾기
        Sound sound = all_Sounds.Find(x => x.name == soundName);

        // 사운드 있으면 재생
        if (sound != null)
            sound.source.Play();
    }
}
