using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    [SerializeField] bool noDuplicate = true; // 중복 방지
    int lastIndex = -1;
    [SerializeField] List<string> soundPool = new List<string>();

    void SoundPlayGlobal(string soundName)
    {
        SoundManager.Instance.PlaySound(soundName);
    }

    void SoundPlayHere(string soundName)
    {
        SoundManager.Instance.PlaySound(soundName, transform.position);
    }

    public void PlaySoundPool()
    {
        string soundName = "";

        // 리스트 크기만큼 인덱스 풀 만들기
        List<int> indexPool = new List<int>();
        for (int i = 0; i < soundPool.Count; i++)
            indexPool.Add(i);

        // 마지막 인덱스 삭제
        if (lastIndex != -1)
            indexPool.Remove(lastIndex);

        // 새로운 인덱스 뽑기
        lastIndex = indexPool[Random.Range(0, indexPool.Count)];

        // 사운드 풀에서 랜덤 사운드 이름 뽑기
        soundName = soundPool[lastIndex];

        SoundManager.Instance.PlaySound(soundName);
    }
}
