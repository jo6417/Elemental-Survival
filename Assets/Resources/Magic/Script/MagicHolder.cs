using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicHolder : Attack
{
    [Header("Refer")]
    public System.Action hitAction;
    public System.Action despawnAction;
    public MagicCastCallback magicCastCallback; // 패시브를 액티브 사용 했을때 콜백
    public delegate void MagicCastCallback();
    public MagicInfo magic; //보유한 마법 데이터

    [Header("Status")]
    public float fixCoolTime = 0; // 수동 쿨타임 입력
    public bool isQuickCast = false; //수동으로 시전한 마법인지 여부
    [ReadOnly] public GameObject targetObj = null; //목표 오브젝트
    public string magicName; //마법 이름 확인
    public Vector3 targetPos = default(Vector3); //목표 위치

    private float addDuration; // 추가 유지 시간
    public float AddDuration // 추가 유지 시간
    {
        get { return Mathf.Clamp(addDuration, 0f, 100f); }
        set { addDuration = value; }
    }
    private float multipleSpeed; // 추가 스피드
    public float MultipleSpeed // 추가 스피드
    {
        get { return Mathf.Clamp(multipleSpeed, 1f, 100f); }
        set { multipleSpeed = value; }
    }
    [ReadOnly] public bool initDone = false; //초기화 완료 여부

    [Header("Stat")]
    public float power = 1; //데미지
    public float speed = 1; //투사체 속도 및 쿨타임
    public float range = 1; //시전 범위
    public float scale = 1; //마법 스케일
    public float duration = 1; //지속시간
    public float criticalRate = 1f; //크리티컬 확률
    public float criticalPower = 1f; //크리티컬 데미지 증가율
    public int pierce = 0; //관통 횟수 및 넉백 계수
    public int atkNum = 0; //투사체 수
    public float coolTime = 0; //쿨타임

    private void Awake()
    {
        // 변수 없으면 찾기
        atkColl = atkColl == null ? GetComponentInChildren<Collider2D>() : atkColl;
    }

    private void OnEnable()
    {
        //초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 초기화 완료 안됨
        initDone = false;

        // 마법 정보 알기 전까지 콜라이더 끄기
        // if (atkColl != null)
        //     atkColl.enabled = false;

        yield return new WaitUntil(() => MagicDB.Instance.loadDone);

        //프리팹 이름으로 마법 정보 찾아 넣기
        if (magic == null)
            magic = MagicDB.Instance.GetMagicByName(transform.name.Split('_')[0]);

        // magic 정보 들어올때까지 대기
        yield return new WaitUntil(() => magic != null);

        // 모든 스탯 초기화
        power = MagicDB.Instance.MagicPower(magic);
        // speed = MagicDB.Instance.MagicSpeed(magic);
        range = MagicDB.Instance.MagicRange(magic);
        scale = MagicDB.Instance.MagicScale(magic);
        duration = MagicDB.Instance.MagicDuration(magic);
        criticalRate = MagicDB.Instance.MagicCriticalRate(magic);
        criticalPower = MagicDB.Instance.MagicCriticalPower(magic);
        pierce = MagicDB.Instance.MagicPierce(magic);
        atkNum = MagicDB.Instance.MagicAtkNum(magic);
        coolTime = MagicDB.Instance.MagicCoolTime(magic);

        //타겟 임의 지정되면 마법 정보에 반영
        if (targetType != TargetType.None)
            SetTarget(targetType);

        // 마법 정보 찾은 뒤 콜라이더 활성화
        // if (atkColl != null)
        //     atkColl.enabled = true;

        //! 마법 이름 확인
        magicName = magic.name;

        // 초기화 완료
        initDone = true;
    }

    private void OnDisable()
    {
        //변수 초기화
        addDuration = 0;
        multipleSpeed = 1;
        power = 0f;

        // 마법정보 초기화
        // magic = null;
    }

    public TargetType GetTarget()
    {
        return targetType;
    }

    void GlobalSoundPlay(string soundName)
    {
        SoundManager.Instance.PlaySound(soundName);
    }

    void SoundPlay(string soundName)
    {
        SoundManager.Instance.PlaySound(soundName, transform.position);
    }

    // public void SetTarget(TargetType changeTarget)
    // {
    //     //입력된 타겟에 따라 오브젝트 태그 및 레이어 변경
    //     switch (changeTarget)
    //     {
    //         case TargetType.Enemy:
    //             gameObject.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
    //             break;

    //         case TargetType.Player:
    //             gameObject.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
    //             break;

    //         case TargetType.Both:
    //             gameObject.layer = SystemManager.Instance.layerList.AllAttack_Layer;
    //             break;
    //     }

    //     //해당 마법의 타겟 변경
    //     targetType = changeTarget;

    //     // StartCoroutine(MagicTarget(changeTarget));
    // }

    // IEnumerator MagicTarget(Target changeTarget)
    // {
    //     // 마법 정보 들어올때까지 대기
    //     yield return new WaitUntil(() => magic != null);

    //     //해당 마법의 타겟 변경
    //     targetType = changeTarget;
    // }
}
