using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class VarManager : MonoBehaviour
{
    #region Singleton
    private static VarManager instance;
    public static VarManager Instance
    {
        get
        {
            if (instance == null)
            {
                var obj = FindObjectOfType<VarManager>();
                if (obj != null)
                {
                    instance = obj;
                }
                else
                {
                    var newObj = new GameObject().AddComponent<VarManager>();
                    instance = newObj;
                }
            }
            return instance;
        }
    }
    #endregion

    public float playerTimeScale = 1f; //플레이어만 사용하는 타임스케일
    public float timeScale = 1f; //전역으로 사용하는 타임스케일

    public void AllTimeScale(float scale)
    {
        playerTimeScale = scale;
        timeScale = scale;
    }

    public void TimeStopToggle(bool isStop)
    {
        if (isStop)
        {
            AllTimeScale(0);
            DOTween.PauseAll();
        }
        else
        {
            AllTimeScale(1);
            DOTween.PlayAll();
        }

        #region StopEnemy
        //존재하는 모든 적 찾기
        List<GameObject> enemyObjs = GameObject.FindGameObjectsWithTag("Enemy").ToList();

        if (enemyObjs.Count != 0)
        {
            //플레이어와 몬스터 레이어 충돌 무시, 몬스터 스폰 멈춤, 
            if (isStop)
            {
                Physics2D.IgnoreLayerCollision(PlayerManager.Instance.gameObject.layer, enemyObjs[0].layer, true);
                EnemySpawn.Instance.spawnSwitch = false;
            }
            else
            {
                Physics2D.IgnoreLayerCollision(PlayerManager.Instance.gameObject.layer, enemyObjs[0].layer, false);
                EnemySpawn.Instance.spawnSwitch = true;
            }

            foreach (var enemy in enemyObjs)
            {
                //애니메이터 멈추기 토글
                List<Animator> anims = new List<Animator>();
                anims = enemy.GetComponentsInChildren<Animator>().ToList();
                foreach (var anim in anims)
                {
                    anim.speed = timeScale;
                }

                //파티클 멈추기 토글
                List<ParticleSystem> particles = new List<ParticleSystem>();
                particles = enemy.GetComponentsInChildren<ParticleSystem>().ToList();
                foreach (var particle in particles)
                {
                    if (isStop)
                        particle.Pause();
                    else
                        particle.Play();
                }
            }
        }
        #endregion

        #region StopMagic
        //존재하는 모든 적 찾기
        List<GameObject> Magics = GameObject.FindGameObjectsWithTag("Magic").ToList();

        if (Magics.Count != 0)
        {
            foreach (var magic in Magics)
            {
                //애니메이터 멈추기 토글
                List<Animator> anims = new List<Animator>();
                anims = magic.GetComponentsInChildren<Animator>().ToList();
                foreach (var anim in anims)
                {
                    anim.speed = playerTimeScale;
                }

                //파티클 멈추기 토글
                List<ParticleSystem> particles = new List<ParticleSystem>();
                particles = magic.GetComponentsInChildren<ParticleSystem>().ToList();
                foreach (var particle in particles)
                {
                    if (isStop)
                        particle.Pause();
                    else
                        particle.Play();
                }
            }
        }
        #endregion

        #region StopItem
        //존재하는 모든 아이템 찾기
        // List<GameObject> Items = GameObject.FindGameObjectsWithTag("Item").ToList();

        // foreach (var item in Items)
        // {
        //     if (isStop)
        //         item.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        //     else
        //         item.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Dynamic;
        // }
        #endregion
    }
}
