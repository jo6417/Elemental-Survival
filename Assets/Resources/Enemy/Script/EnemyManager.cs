using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Lean.Pool;
using UnityEngine;

public class EnemyManager : Character
{
    // [Header("Initial")]
    // public EliteClass eliteClass = EliteClass.None; // 엘리트 여부
    // public enum EliteClass { None, Power, Speed, Heal };
    // public bool lookLeft = false; //기본 스프라이트가 왼쪽을 바라보는지

    // [Header("Stat")]
    // public float powerNow;
    // public float speedNow;
    // public float rangeNow;
    // public float cooltimeNow;

    // [Header("Debug")]
    // [SerializeField] string enemyName;
    // [SerializeField] string enemyType;

    // void Awake()
    // {
    //     enemyAI = enemyAI == null ? transform.GetComponent<EnemyAI>() : enemyAI;

    //     spriteObj = spriteObj == null ? transform : spriteObj;
    //     rigid = rigid == null ? spriteObj.GetComponentInChildren<Rigidbody2D>(true) : rigid;
    //     animList = animList.Count == 0 ? GetComponentsInChildren<Animator>().ToList() : animList;

    //     // 히트 박스 모두 찾기
    //     hitBoxList = hitBoxList.Count == 0 ? GetComponentsInChildren<HitBox>().ToList() : hitBoxList;

    //     // 스프라이트 리스트에 아무것도 없으면 찾아 넣기
    //     spriteList = spriteList.Count == 0 ? GetComponentsInChildren<SpriteRenderer>().ToList() : spriteList;

    //     // 초기 스프라이트 정보 수집
    //     foreach (SpriteRenderer sprite in spriteList)
    //     {
    //         originColorList.Add(sprite.color);
    //         originMatList.Add(sprite.material);
    //         originMatColorList.Add(sprite.material.color);
    //     }

    //     // 버프 아이콘 부모 찾기
    //     buffParent = buffParent == null ? transform.Find("BuffParent") : buffParent;

    //     //초기 타겟은 플레이어
    //     TargetObj = PlayerManager.Instance.gameObject;

    //     // 공격 트리거 찾기
    //     enemyAtkTrigger = enemyAtkTrigger == null ? GetComponentInChildren<EnemyAtkTrigger>() : enemyAtkTrigger;
    //     // 공격 콜라이더 찾기
    //     enemyAtkList = enemyAtkList.Count == 0 ? GetComponentsInChildren<EnemyAttack>().ToList() : enemyAtkList;

    //     // 히트 이펙트가 없으면 기본 이펙트 가져오기
    //     if (hitEffect == null)
    //         hitEffect = EnemySpawn.Instance.hitEffect;

    //     // 초기화 시작 및 완료 변수 초기화
    //     // initialStart = false;
    //     initialFinish = false;
    // }

    // private void OnEnable()
    // {
    //     // 초기화 완료 취소
    //     initialFinish = false;

    //     StartCoroutine(Init());
    // }

    // IEnumerator Init()
    // {
    //     // 히트박스 전부 끄기
    //     for (int i = 0; i < hitBoxList.Count; i++)
    //     {
    //         hitBoxList[i].enabled = false;
    //     }

    //     // 물리 콜라이더 끄기
    //     physicsColl.enabled = false;

    //     //스케일 초기화
    //     transform.localScale = Vector3.one;

    //     // rigid 초기화
    //     rigid.velocity = Vector3.zero;
    //     rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

    //     // 초기화 스위치 켜질때까지 대기
    //     yield return new WaitUntil(() => initialStart);

    //     // 고스트 여부 초기화
    //     isGhost = changeGhost;

    //     // 다음 리스폰할때 고스트 예약 초기화
    //     changeGhost = false;

    //     //EnemyDB 로드 될때까지 대기
    //     yield return new WaitUntil(() => EnemyDB.Instance.loadDone);

    //     // 몬스터 정보 찾기
    //     enemy = EnemyDB.Instance.GetEnemyByName(transform.name.Split('_')[0]);

    //     // 몬스터 정보 인스턴싱, 몬스터 오브젝트마다 따로 EnemyInfo 갖기
    //     enemy = new EnemyInfo(enemy);

    //     // 스탯 초기화
    //     enemyName = enemy.enemyName;
    //     enemyType = enemy.enemyType;
    //     hpMax = enemy.hpMax;
    //     powerNow = enemy.power;
    //     speedNow = enemy.speed;
    //     rangeNow = enemy.range;
    //     cooltimeNow = enemy.cooltime;

    //     //엘리트 종류마다 색깔 및 능력치 적용
    //     switch ((int)eliteClass)
    //     {
    //         case 1:
    //             //체력 상승
    //             hpMax = hpMax * 2f;

    //             //힐 오브젝트 소환
    //             healRange = LeanPool.Spawn(EnemySpawn.Instance.healRange, transform.position, Quaternion.identity, transform).GetComponent<CircleCollider2D>();
    //             // 힐 오브젝트 크기 초기화
    //             healRange.transform.localScale = Vector2.one * portalSize;
    //             // enemyManager 넣어주기
    //             healRange.GetComponent<EnemyAttack>().enemyManager = this;

    //             // 초록 아웃라인 머터리얼
    //             spriteList[0].material = SystemManager.Instance.outLineMat;
    //             spriteList[0].material.color = Color.green;
    //             break;

    //         case 2:
    //             //공격력 상승
    //             powerNow = powerNow * 2f;
    //             // 빨강 아웃라인 머터리얼
    //             spriteList[0].material = SystemManager.Instance.outLineMat;
    //             spriteList[0].material.color = Color.red;
    //             break;

    //         case 3:
    //             //속도 상승
    //             speedNow = speedNow * 2f;
    //             // 쿨타임 빠르게
    //             cooltimeNow = cooltimeNow / 2f;

    //             // 하늘색 아웃라인 머터리얼
    //             spriteList[0].material = SystemManager.Instance.outLineMat;
    //             spriteList[0].material.color = Color.cyan;
    //             break;

    //         case 4:
    //             // 일정 범위 만큼 마법 차단하는 파란색 쉴드 생성
    //             // 해당 범위내 몬스터들은 무적(맞으면 Miss) 스위치 켜기, 범위 나가면 무적 끄기
    //             // 이 엘리트 몹을 잡으면 쉴드 사라짐
    //             // 콜라이더 stay 함수로 구현, 무적쉴드 충돌이면 무적 설정, 없으면 무적 해제
    //             //TODO 포스쉴드 프리팹 생성
    //             break;
    //     }

    //     //ItemDB 로드 될때까지 대기
    //     yield return new WaitUntil(() => ItemDB.Instance.loadDone);

    //     //보유 아이템 초기화
    //     nowHasItem.Clear();
    //     foreach (var itemId in defaultHasItem)
    //     {
    //         // id 할당을 위해 변수 선언
    //         int id = itemId;

    //         // -1이면 랜덤 원소젬 뽑기
    //         if (id == -1)
    //             id = Random.Range(0, 6);

    //         // item 인스턴스 생성 및 amount 초기화
    //         ItemInfo item = new ItemInfo(ItemDB.Instance.GetItemByID(id));
    //         item.amount = 1;

    //         //item 정보 넣기
    //         nowHasItem.Add(item);
    //     }

    //     // 엘리트 몬스터일때
    //     if (eliteClass != EliteClass.None)
    //     {
    //         // 랜덤 샤드 드랍 아이템에 등록
    //         ItemInfo itemInfo = null;
    //         string itemName = "";

    //         // 해당 몬스터 등급으로 뽑기 등급 산출
    //         int grade = enemy.grade;
    //         // 해당 몬스터 등급으로 랜덤 마법 뽑기
    //         MagicInfo randomMagic = MagicDB.Instance.GetRandomMagic(grade);

    //         // 뽑았는데 랜덤이면 하위 등급으로 다시 뽑기
    //         while (randomMagic == null)
    //         {
    //             if (grade > 1)
    //                 // 등급을 한단계 낮추기
    //                 grade--;
    //             // 1등급 이하면 중단
    //             else
    //                 break;

    //             // 해당 등급으로 랜덤 마법 뽑기
    //             randomMagic = MagicDB.Instance.GetRandomMagic(grade);

    //             if (randomMagic != null)
    //                 print(grade + " : " + randomMagic.name);
    //         }

    //         // 해당 등급의 마법을 뽑는데 성공했을때
    //         if (randomMagic != null)
    //         {
    //             // 아이템 이름 짓기
    //             itemName = "Magic Shard " + grade;

    //             // 몬스터 등급에 해당하는 shard 찾기
    //             itemInfo = ItemDB.Instance.GetItemByName(itemName);

    //             // 드랍 아이템 정보 넣기
    //             nowHasItem.Add(itemInfo);
    //         }
    //     }

    //     hitDelayCount = 0; //데미지 카운트 초기화
    //     stopCount = 0; //시간 정지 카운트 초기화
    //     oppositeCount = 0; //반대편 전송 카운트 초기화

    //     // 고스트일때
    //     if (IsGhost)
    //     {
    //         //체력 절반으로 초기화
    //         hpNow = hpMax / 2f;

    //         // rigid, sprite, 트윈, 애니메이션 상태 초기화
    //         for (int i = 0; i < spriteList.Count; i++)
    //         {
    //             // 고스트 여부에 따라 색깔 초기화
    //             spriteList[i].color = new Color(0, 1, 1, 0.5f);

    //             // 고스트 여부에 따라 복구 머터리얼 바꾸기
    //             spriteList[i].material = SystemManager.Instance.outLineMat;
    //         }

    //         // 그림자 더 투명하게
    //         shadow.color = new Color(0, 0, 0, 0.25f);

    //         // 자폭형 몬스터 아닐때
    //         if (!selfExplosion)
    //         {
    //             // 공격 트리거 레이어를 플레이어 공격으로 바꾸기
    //             if (enemyAtkTrigger)
    //                 enemyAtkTrigger.gameObject.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
    //             // 공격 레이어를 플레이어 공격으로 바꾸기
    //             for (int i = 0; i < enemyAtkList.Count; i++)
    //             {
    //                 enemyAtkList[i].gameObject.layer = SystemManager.Instance.layerList.PlayerAttack_Layer;
    //             }
    //         }
    //     }
    //     else
    //     {
    //         // 맥스 체력으로 초기화
    //         hpNow = hpMax;

    //         // 물리 콜라이더 켜기
    //         physicsColl.enabled = true;

    //         // 엘리트 몬스터 아닐때
    //         if (eliteClass == EliteClass.None)
    //             // rigid, sprite, 트윈, 애니메이션 상태 초기화
    //             for (int i = 0; i < spriteList.Count; i++)
    //             {
    //                 // 고스트 여부에 따라 색깔 초기화
    //                 spriteList[i].color = originColorList[i];
    //                 spriteList[i].material = originMatList[i];
    //             }

    //         // 그림자 색 초기화
    //         if (shadow)
    //             shadow.color = new Color(0, 0, 0, 0.5f);

    //         // 공격 트리거 레이어를 몬스터 공격으로 바꾸기
    //         if (enemyAtkTrigger)
    //             enemyAtkTrigger.gameObject.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
    //         // 공격 레이어를 몬스터 공격으로 바꾸기
    //         for (int i = 0; i < enemyAtkList.Count; i++)
    //         {
    //             enemyAtkList[i].gameObject.layer = SystemManager.Instance.layerList.EnemyAttack_Layer;
    //         }
    //     }

    //     //죽음 여부 초기화
    //     isDead = false;

    //     // idle 상태로 전환
    //     nowState = State.Idle;

    //     for (int i = 0; i < animList.Count; i++)
    //     {
    //         // 애니메이터 켜기
    //         animList[i].enabled = true;

    //         // 애니메이션 속도 초기화
    //         // 기본값 속도에 비례해서 현재 속도만큼 배율 넣기
    //         animList[i].speed = 1f * speedNow / EnemyDB.Instance.GetEnemyByID(enemy.id).speed;
    //     }

    //     //보스면 체력 UI 띄우기
    //     if (enemy.enemyType == EnemyDB.EnemyType.Boss.ToString())
    //     {
    //         StartCoroutine(UIManager.Instance.UpdateBossHp(this));
    //     }

    //     // 히트박스 전부 켜기
    //     for (int i = 0; i < hitBoxList.Count; i++)
    //     {
    //         hitBoxList[i].enabled = true;
    //     }

    //     // Idle로 초기화
    //     nowAction = Action.Idle;

    //     // 초기화 완료되면 초기화 스위치 끄기
    //     initialStart = false;
    //     // 초기화 완료
    //     initialFinish = true;
    // }

    // private void Update()
    // {
    //     // 초기화 안됬으면 리턴
    //     if (!initialFinish)
    //     {
    //         // 이동 멈추기
    //         rigid.velocity = Vector2.zero;

    //         return;
    //     }

    //     // 고스트일때, 타겟이 비활성화되거나 리셋타임이 되면 타겟 재설정
    //     if (IsGhost && (targetResetCount <= 0 || TargetObj == null))
    //         // 타겟 리셋하기
    //         TargetObj = null;

    //     // 타겟 리셋 카운트 차감
    //     if (targetResetCount > 0)
    //         targetResetCount -= Time.deltaTime;

    //     // 파티클 히트 딜레이 차감
    //     if (particleHitCount > 0)
    //     {
    //         // state = State.Hit;

    //         particleHitCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;
    //     }

    //     // 히트 딜레이 차감
    //     if (hitDelayCount > 0)
    //     {
    //         // state = State.Hit;

    //         hitDelayCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;
    //     }

    //     // flat 디버프 중일때 카운트 차감
    //     if (flatCount > 0)
    //         flatCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;

    //     // 멈춤 디버프 중일때 카운트 차감
    //     if (stopCount > 0)
    //         stopCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;

    //     // 반대편 보내질때 행동 멈추기
    //     if (oppositeCount > 0)
    //         oppositeCount -= Time.deltaTime * SystemManager.Instance.globalTimeScale;

    //     // 힐 오브젝트가 있을때
    //     if (healRange != null)
    //     {
    //         // 쿨타임 중일때
    //         if (healCount > 0)
    //         {
    //             // 힐 콜라이더 끄기
    //             healRange.enabled = false;

    //             // 힐 쿨타임 차감
    //             healCount -= Time.deltaTime;
    //         }
    //         // 쿨타임 끝났을때
    //         else
    //         {
    //             // 힐 콜라이더 켜기
    //             healRange.enabled = true;

    //             Transform healEffect = healRange.transform.GetChild(0);
    //             // 이펙트 오브젝트 켜기
    //             healEffect.gameObject.SetActive(true);
    //             // 사이즈 제로로 초기화
    //             healEffect.localScale = Vector2.zero;
    //             // 힐 이펙트 사이즈 키우기
    //             healEffect.DOScale(Vector2.one, cooltimeNow)
    //             .OnComplete(() =>
    //             {
    //                 // 이펙트 오브젝트 끄기
    //                 healEffect.gameObject.SetActive(false);
    //             });

    //             // 힐 쿨타임을 몬스터 쿨타임으로 갱신
    //             healCount = cooltimeNow;
    //         }
    //     }
    // }

    // public bool ManageState()
    // {
    //     // 상태이상 여부 초기화
    //     afterEffect = false;

    //     // 초기화 안됬으면 리턴
    //     if (!initialFinish)
    //         return false;

    //     // 몬스터 정보 없으면 리턴
    //     if (enemy == null)
    //         return false;

    //     // 비활성화 되었으면 리턴
    //     if (gameObject == null || !gameObject)
    //         return false;

    //     // 죽는 중일때
    //     if (isDead)
    //     {
    //         // 행동불능이므로 false 리턴
    //         return false;
    //     }

    //     //전역 타임스케일이 0 일때
    //     if (SystemManager.Instance.globalTimeScale == 0)
    //     {
    //         nowState = State.MagicStop;

    //         // 애니메이션 멈추기
    //         if (animList.Count > 0)
    //         {
    //             foreach (Animator anim in animList)
    //             {
    //                 anim.speed = 0f;
    //             }
    //         }

    //         // 이동 멈추기
    //         rigid.velocity = Vector2.zero;

    //         transform.DOPause();

    //         // 행동불능이므로 false 리턴
    //         return false;
    //     }

    //     // 멈춤 디버프일때
    //     if (stopCount > 0)
    //     {
    //         nowState = State.TimeStop;

    //         rigid.velocity = Vector2.zero; //이동 초기화
    //         rigid.constraints = RigidbodyConstraints2D.FreezeAll;
    //         // 애니메이션 멈추기
    //         if (animList.Count > 0)
    //         {
    //             foreach (Animator anim in animList)
    //             {
    //                 anim.speed = 0f;
    //             }
    //         }

    //         // 히트 딜레이중 아닐때
    //         if (hitDelayCount <= 0)
    //             //시간 멈춤 머터리얼 및 색으로 바꾸기
    //             for (int i = 0; i < spriteList.Count; i++)
    //             {
    //                 // spriteList[i].material = originMatList[i];
    //                 spriteList[i].color = SystemManager.Instance.stopColor;
    //             }

    //         transform.DOPause();

    //         // 행동불능이므로 false 리턴
    //         return false;
    //     }

    //     //스폰 콜라이더에 닿아 반대편으로 보내질때 잠시대기
    //     if (oppositeCount > 0)
    //     {
    //         rigid.velocity = Vector2.zero; //이동 초기화

    //         // 행동불능이므로 false 리턴
    //         return false;
    //     }

    //     // 피격 했을때
    //     if (hitDelayCount > 0)
    //     {
    //         return false;
    //     }

    //     // 감전 디버프일때
    //     if (shockCoroutine != null)
    //     {
    //         // 행동불능이므로 false 리턴
    //         return false;
    //     }

    //     // 슬로우 디버프일때
    //     if (slowCoroutine != null)
    //     {
    //         // 행동가능이므로 true 리턴
    //         return true;
    //     }

    //     // 포이즌 디버프일때
    //     if (poisonCoroutine != null)
    //     {
    //         // 행동가능이므로 true 리턴
    //         return true;
    //     }

    //     // 모든 문제 없으면 idle 상태로 전환
    //     // state = State.Idle;

    //     // 고스트일때
    //     if (IsGhost)
    //         // rigid, sprite, 트윈, 애니메이션 상태 초기화
    //         for (int i = 0; i < spriteList.Count; i++)
    //         {
    //             // 고스트 여부에 따라 복구 색깔 및 머터리얼 바꾸기
    //             spriteList[i].material = SystemManager.Instance.outLineMat;
    //             spriteList[i].color = new Color(0, 1, 1, 0.5f);
    //         }
    //     // 고스트 아닐때
    //     else
    //         // rigid, sprite, 트윈, 애니메이션 상태 초기화
    //         for (int i = 0; i < spriteList.Count; i++)
    //         {
    //             // 고스트 여부에 따라 복구 색깔 및 머터리얼 바꾸기
    //             spriteList[i].color = originColorList[i];

    //             // 엘리트 아닐때
    //             if (eliteClass == EliteClass.None)
    //                 spriteList[i].material = originMatList[i];
    //         }

    //     // transform.DOPlay();

    //     // 애니메이션 속도 초기화
    //     if (animList.Count > 0)
    //     {
    //         foreach (Animator anim in animList)
    //         {
    //             anim.speed = 1f * speedNow / EnemyDB.Instance.GetEnemyByID(enemy.id).speed;
    //         }
    //     }

    //     // rigid 초기화
    //     // rigid.velocity = Vector3.zero;
    //     rigid.constraints = RigidbodyConstraints2D.FreezeRotation;

    //     // 상태 이상 없음
    //     afterEffect = true;

    //     // 행동가능이므로 true 리턴
    //     return true;
    // }


}
