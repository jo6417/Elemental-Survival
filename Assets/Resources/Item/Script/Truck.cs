using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Truck : MonoBehaviour
{
    [SerializeField] Collider2D coll; // 몬스터 충격 콜라이더
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] List<Sprite> truckSprites = new List<Sprite>();
    [SerializeField] GameObject arriveEffect; // 도착 후 이펙트(배기구 매연, 착지 먼지 등)
    [SerializeField] GameObject shopGlass;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        // 도착 후 이펙트 끄기
        arriveEffect.SetActive(false);
        // 자판기 부모 오브젝트 끄기
        shopGlass.SetActive(false);

        // 플레이어가 오른쪽 보고있을때
        if (PlayerManager.Instance.lastDir.x > 0)
        {
            // 좌측에서 등장
            transform.position = new Vector2(WorldSpawner.Instance.spawnColl.bounds.min.x + 6f, PlayerManager.Instance.transform.position.y + 2f);

            // 진행 방향으로 트럭 방향 회전
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            // 우측에서 등장
            transform.position = new Vector2(WorldSpawner.Instance.spawnColl.bounds.max.x - 6f, PlayerManager.Instance.transform.position.y + 2f);

            // 진행 방향으로 트럭 방향 회전
            transform.rotation = Quaternion.Euler(0, 180f, 0);
        }

        // 문 닫힌 스프라이트로 초기화
        sprite.sprite = truckSprites[0];
        // 공격 콜라이더 켜기
        coll.enabled = true;

        // 화면 가운데 혹은 입력된 위치로 이동
        Vector2 targetPos = new Vector2(PlayerManager.Instance.transform.position.x, transform.position.y);
        transform.DOMove(targetPos, 1f);
        yield return new WaitForSeconds(0.5f);

        // 앞으로 기울어지는 애니메이션
        sprite.transform.DOLocalRotate(new Vector3(0f, 0f, -20f), 0.2f)
        .SetEase(Ease.OutQuint);

        yield return new WaitForSeconds(0.5f);

        // 다시 기울기 복구하는 애니메이션
        sprite.transform.DOLocalRotate(new Vector3(0f, 0f, 0f), 0.2f)
        .SetEase(Ease.OutBounce)
        .OnComplete(() =>
        {
            // 도착 후 이펙트 켜기
            arriveEffect.SetActive(true);

            // 공격 콜라이더 끄기
            coll.enabled = false;
        });

        yield return new WaitForSeconds(0.5f);

        // 가판대 열린 스프라이트로 교체
        sprite.sprite = truckSprites[1];

        //todo 내부 자판기 종류 2개 정하기
        //todo 정해진대로 위치 이동

        // 자판기 각도 초기화
        for (int i = 0; i < shopGlass.transform.childCount; i++)
        {
            shopGlass.transform.GetChild(i).rotation = Quaternion.Euler(Vector3.zero);
        }

        // 자판기 부모 켜기
        shopGlass.SetActive(true);
    }
}
