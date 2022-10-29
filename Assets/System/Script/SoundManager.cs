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
    public float volume = 1f;
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

    public Sound[] sounds;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // 입력된 오디오 클립을 오디오 소스로 생성
        foreach (Sound sound in sounds)
        {
            // 오디오 소스 컴포넌트 생성
            sound.source = gameObject.AddComponent<AudioSource>();
            // 오디오 클립 넣기
            sound.source.clip = sound.clip;

            // 볼륨 및 피치 동기화
            sound.source.volume = sound.volume;
            sound.source.pitch = sound.pitch;
        }
    }

    public void Play(string name)
    {
        // 해당 이름으로 사운드 찾기
        Sound sound = Array.Find(sounds, sound => sound.name == name);

        // 사운드 있으면 재생
        if (sound != null)
            sound.source.Play();
    }
}
