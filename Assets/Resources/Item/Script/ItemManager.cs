using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lean.Pool;
using DG.Tweening;

public class ItemManager : MonoBehaviour
{

    [Header("Refer")]
    public ItemInfo itemInfo; // 해당 아이템 정보
    public MagicInfo magicInfo; // 해당 아이템이 갖고 있는 마법 정보
    public SpriteRenderer sprite;
    public GameObject despawnEffect; //사라질때 이펙트
    public Collider2D coll;
    private Rigidbody2D rigid;
    private Vector3 velocity;
    [SerializeField] GameObject gadgetPrefab;

    [Header("State")]
    [SerializeField] ItemState itemState = 0;
    [SerializeField] enum ItemState { Idle, Ready, Follow, Get };
    [SerializeField, ReadOnly] public bool isBundle; //합쳐진 아이템인지
    [SerializeField, ReadOnly] public int gemTypeIndex = -1;
    [SerializeField, ReadOnly] private string itemName;
    public float autoDespawnTime = 0; //자동 디스폰 시간

    private void Awake()
    {
        sprite = sprite == null ? GetComponent<SpriteRenderer>() : sprite;
        rigid = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        velocity = rigid.velocity;

        //아이템 정보 없을때 충돌 끄기
        coll.enabled = false;
    }

    private void OnEnable()
    {
        //아이템 스폰할때마다 초기화
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 초기화 전까지 콜라이더 끄기
        coll.enabled = false;

        //아이템DB 로드 완료까지 대기
        yield return new WaitUntil(() => ItemDB.Instance.loadDone);

        // item 정보 불러올때까지 대기
        // yield return new WaitUntil(() => item != null);

        // item 정보 없으면 프리팹 이름으로 아이템 정보 찾아 넣기
        if (itemInfo == null)
        {
            // 아이템 정보 찾기
            itemInfo = ItemDB.Instance.GetItemByName(transform.name.Split('_')[0]);

            // 정보 찾았으면 새 인스턴스 만들어서 갱신
            if (itemInfo != null)
                itemInfo = new ItemInfo(itemInfo);
        }

        if (itemInfo != null)
        {
            itemName = itemInfo.name;

            //지불 원소젬 이름을 인덱스로 반환
            gemTypeIndex = System.Array.FindIndex(MagicDB.Instance.ElementNames, x => x == itemInfo.priceType);
        }

        // 아이템, 마법 정보 들어올때까지 대기
        yield return new WaitUntil(() => itemInfo != null || magicInfo != null);

        if (itemInfo != null)
        {
            itemName = itemInfo.name;

            // 아이템 개수에 따라 사이즈 초기화 (개당 10프로)
            transform.localScale = Vector2.one * (1 + (itemInfo.amount - 1) * 0.1f);
        }

        if (magicInfo != null)
            itemName = magicInfo.name;

        //아이템 번들 여부 초기화
        isBundle = false;

        // 아이템 상태 초기화
        itemState = ItemState.Idle;

        // 콜라이더 초기화
        coll.enabled = true;
        coll.isTrigger = false;

        // 자동 디스폰 실행
        if (autoDespawnTime > 0)
            AutoDespawn();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // 정보 없으면 리턴
        if (itemInfo == null && magicInfo == null)
            return;

        // 원소젬일때
        if (itemInfo != null && itemInfo.itemType == ItemDB.ItemType.Gem.ToString())
        {
            // 자석 레이저와 충돌 했을때
            if (other.gameObject.CompareTag(TagNameList.Item.ToString()))
            {
                // idle 상태일때
                if (itemState == ItemState.Idle)
                    PlayerCollision();
            }

            // 리스폰 콜라이더 안에 들어왔을때
            if (other.gameObject.CompareTag("Respawn") && !isBundle)
            {
                //해당 타입 원소젬 개수 -1
                if (WorldSpawner.Instance.outGemNum[gemTypeIndex] > 0)
                    WorldSpawner.Instance.outGemNum[gemTypeIndex]--;

                //원소젬 리스트에서 빼기
                WorldSpawner.Instance.outGem.Remove(gameObject);
            }
        }

        // 플레이어와 충돌 했을때
        if (other.gameObject.CompareTag(TagNameList.Player.ToString()))
        {
            // idle 상태일때
            if (itemState == ItemState.Idle)
                PlayerCollision();

            // 플레이어 쫓아가는 상태일때
            if (itemState == ItemState.Follow)
                GetItem();
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // 플레이어와 충돌 했을때
        if (other.gameObject.CompareTag(TagNameList.Player.ToString()))
        {
            // idle 상태일때
            if (itemState == ItemState.Idle)
                PlayerCollision();

            // 플레이어 쫓아가는 상태일때
            if (itemState == ItemState.Follow)
                GetItem();
        }
    }

    void PlayerCollision()
    {
        // 이중 충돌 방지
        coll.isTrigger = true;

        // 인벤토리에 들어가는 아이템일때 (마법이거나 샤드 아이템)
        if (magicInfo != null
        || (itemInfo != null && (itemInfo.itemType == ItemDB.ItemType.Shard.ToString())))
        {
            // 인벤토리 빈칸 있을때
            if (PhoneMenu.Instance.GetEmptySlot() != -1)
            {
                // 자동 디스폰 중지
                sprite.DOKill();

                // 플레이어에게 날아가기
                StartCoroutine(GetMove(PlayerManager.Instance.transform));
            }
        }
        // 인벤토리에 들어가지 않는 아이템일때
        else
        {
            // 샤드일때는 인벤토리에 빈칸 있을때, 다른 아이템이면 그냥 획득
            if (itemInfo.itemType == ItemDB.ItemType.Gem.ToString()
            || itemInfo.itemType == ItemDB.ItemType.Heal.ToString()
            || itemInfo.itemType == ItemDB.ItemType.Artifact.ToString()
            || itemInfo.itemType == ItemDB.ItemType.Gadget.ToString())
            {
                //이중 충돌 방지
                // coll.enabled = false;

                // 자동 디스폰 중지
                sprite.DOKill();

                // 플레이어에게 날아가기
                StartCoroutine(GetMove(PlayerManager.Instance.transform));
            }
            // 인벤토리에 빈칸 없을때
            else
            {
                // 화면의 핸드폰 아이콘 빨간불 점멸
                UIManager.Instance.phoneNoticeIcon.DOColor(new Color(1, 20f / 255f, 20f / 255f, 1f), 0.2f)
                .SetLoops(4, LoopType.Yoyo)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    // 흰색으로 초기화
                    UIManager.Instance.phoneNoticeIcon.color = Color.white;
                });
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // 스폰 콜라이더 밖으로 나갔을때
        if (other.CompareTag("Respawn"))
        {
            // 원소젬이고, 합쳐지지 않았을때
            if (itemInfo != null && itemInfo.itemType == ItemDB.ItemType.Gem.ToString() && !isBundle)
            {
                // 같은 타입 원소젬 개수가 본인포함 10개 이상일때
                if (WorldSpawner.Instance.outGemNum[gemTypeIndex] >= 9)
                {
                    print(itemInfo.name + " : " + WorldSpawner.Instance.outGemNum[gemTypeIndex]);

                    // 해당 원소젬 사이즈 키우고
                    // transform.localScale = Vector2.one * 2;
                    // 개수 늘리기
                    itemInfo.amount = 5;
                    isBundle = true;

                    //해당 타입 원소젬 개수 초기화
                    WorldSpawner.Instance.outGemNum[gemTypeIndex] = 0;

                    // 이름 같은 원소젬 찾아서 다 디스폰 시키기
                    List<GameObject> despawnList = WorldSpawner.Instance.outGem.FindAll(x => x.name == transform.name);
                    print(despawnList.Count);
                    foreach (var gem in despawnList)
                    {
                        WorldSpawner.Instance.outGem.Remove(gem);
                        LeanPool.Despawn(gem);
                    }
                }
                else
                {
                    //해당 타입 원소젬 개수 +1
                    WorldSpawner.Instance.outGemNum[gemTypeIndex]++;
                    //카메라 밖으로 나간 원소젬 리스트에 넣기
                    WorldSpawner.Instance.outGem.Add(gameObject);
                }
            }
        }
    }

    public IEnumerator GetMove(Transform Getter)
    {
        // 준비 상태로 변경
        itemState = ItemState.Ready;

        // 아이템 위치부터 플레이어 쪽으로 방향 벡터
        Vector2 dir = Getter.position - transform.position;

        // 플레이어 반대 방향으로 날아가기
        rigid.DOMove((Vector2)transform.position - dir.normalized * 3f, 0.2f);

        yield return new WaitForSeconds(0.2f);

        // 따라가기 상태로 변경
        itemState = ItemState.Follow;

        // 플레이어 이동 속도 계수
        float playerSpeed = 10f;
        // 가속도값
        float accelSpeed = 1.5f;

        // 플레이어 방향으로 날아가기, 아이템 사라질때까지 방향 갱신하며 반복
        while (itemState == ItemState.Follow && gameObject.activeSelf)
        {
            accelSpeed += 0.05f;

            //방향 벡터 갱신
            dir = Getter.position - transform.position;

            // // 벡터 거리가 getRange 이하일때 획득
            // if (dir.magnitude <= getRange)
            // {
            //     GetItem();
            //     break;
            // }

            // 플레이어 속도 및 가속도 반영
            dir = dir.normalized * playerSpeed * accelSpeed;

            //해당 방향으로 날아가기
            rigid.velocity = dir;

            // x방향으로 회전 시키기
            rigid.angularVelocity = -dir.x * 10f * Random.Range(1f, 2f);

            yield return new WaitForSeconds(0.05f);
        }
    }

    void GetItem()
    {
        // 아이템 획득 상태로 변경
        itemState = ItemState.Get;

        // 마법일때
        if (magicInfo != null)
        {
            // 인벤토리 빈칸 없으면 리턴
            if (PhoneMenu.Instance.GetEmptySlot() == -1)
                coll.enabled = true;
            // 인벤토리 빈칸 있으면
            else
            {
                // 마법 획득
                PhoneMenu.Instance.GetMagic(magicInfo, true);
            }
        }
        // 아이템일때
        else
        {
            // 샤드일때는 인벤토리에 빈칸 없을때 리턴
            if ((itemInfo.itemType == ItemDB.ItemType.Shard.ToString() && PhoneMenu.Instance.GetEmptySlot() == -1))
            {
                coll.enabled = true;

                return;
            }

            // 아이템이 젬 타입일때
            if (itemInfo.itemType == ItemDB.ItemType.Gem.ToString())
            {
                //플레이어 소지 젬 갯수 올리기
                PlayerManager.Instance.AddGem(itemInfo, itemInfo.amount);
                // print(item.itemName + amount);

                // 젬 획득 사운드 재생
                SoundManager.Instance.PlaySound("GetGem");
            }
            // 아이템이 힐 타입일때
            else if (itemInfo.itemType == ItemDB.ItemType.Heal.ToString())
            {
                PlayerManager.Instance.hitBox.Damage(-itemInfo.amount, false);
            }
            // 아이템이 아티팩트 타입일때
            else if (itemInfo.itemType == ItemDB.ItemType.Artifact.ToString())
            {
                //todo 인벤토리가 아닌 hasItem으로 넣기
            }
            // 아이템이 샤드 타입일때
            else if (itemInfo.itemType == ItemDB.ItemType.Shard.ToString())
            {
                //아이템 획득
                PhoneMenu.Instance.GetItem(itemInfo);

                // 샤드 획득 사운드 재생
                SoundManager.Instance.PlaySound("GetShard");
            }
            // 아이템이 가젯 타입일때
            else if (itemInfo.itemType == ItemDB.ItemType.Gadget.ToString())
            {
                // 가젯에 대응하는 오브젝트 소환
                LeanPool.Spawn(gadgetPrefab, PlayerManager.Instance.transform.position, Quaternion.identity, PlayerManager.Instance.transform);
            }
        }

        // 이름 초기화
        itemName = "";

        //아이템 속도 초기화
        if (gameObject.activeSelf && rigid)
            rigid.DOKill();
        rigid.velocity = Vector2.zero;

        //디스폰 이펙트 있으면 생성
        if (despawnEffect)
            LeanPool.Spawn(despawnEffect, transform.position, transform.rotation, ObjectPool.Instance.effectPool);

        //아이템 비활성화
        LeanPool.Despawn(transform);
    }

    void AutoDespawn()
    {
        // 점점 검은색으로 변하기
        sprite.DOColor(Color.black, autoDespawnTime)
        .OnStart(() =>
        {
            sprite.color = Color.white;
        })
        .OnComplete(() =>
        {
            //디스폰 이펙트 있으면 생성
            if (despawnEffect)
                LeanPool.Spawn(despawnEffect, transform.position, Quaternion.identity, ObjectPool.Instance.effectPool);

            // 아이템 디스폰
            LeanPool.Despawn(transform);
        })
        .OnKill(() =>
        {
            sprite.color = Color.white;
        });
    }
}
