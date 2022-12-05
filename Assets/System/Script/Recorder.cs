using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;

public class Recorder : MonoBehaviour
{
    [SerializeField] float recordDelay;
    [SerializeField, ReadOnly] float lastRecordTime;
    [SerializeField] float startTime = 0f;
    [SerializeField] float endTime = 10f;
    [SerializeField] AnimationClip clip;
    private GameObjectRecorder m_Recorder;

    void Start()
    {
        // Create recorder and record the script GameObject.
        m_Recorder = new GameObjectRecorder(gameObject);

        // Bind all the Transforms on the GameObject and all its children.
        m_Recorder.BindComponentsOfType<Transform>(gameObject, true);

        // 딜레이 없으면 초기화
        if (recordDelay == 0)
            recordDelay = Time.deltaTime;
    }

    void LateUpdate()
    {
        if (clip == null)
            return;

        // 끝나는 시간이 있을때만
        if (endTime > 0)
        {
            // 현재 시간이 시작 시간 이전이면 리턴
            if (startTime > Time.time)
                return;
            // 현재 시간이 끝나는 시간 이후이면 리턴
            if (endTime < Time.time)
                return;
        }

        // 딜레이마다 녹화
        if (Time.time >= lastRecordTime + recordDelay)
        {
            // Take a snapshot and record all the bindings values for this frame.
            m_Recorder.TakeSnapshot(recordDelay);

            // 마지막 녹화시간 갱신
            lastRecordTime = Time.time;
        }
    }

    void OnDisable()
    {
        if (clip == null)
            return;

        if (m_Recorder.isRecording)
        {
            // Save the recorded session to the clip.
            m_Recorder.SaveToClip(clip);
        }
    }
}