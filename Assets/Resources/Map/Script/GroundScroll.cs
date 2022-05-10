using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GroundScroll : MonoBehaviour
{
    public Transform[] background = null;
    public Transform player;

    float sizeX;
    float sizeY;

    float rightX;
    float leftX;
    float upY;
    float downY;

    private void Start()
    {
        sizeX = background[0].GetComponent<SpriteRenderer>().sprite.bounds.size.x;
        sizeY = background[0].GetComponent<SpriteRenderer>().sprite.bounds.size.y;

        rightX = sizeX / 2;
        leftX = -sizeX / 2;
        upY = sizeY / 2;
        downY = -sizeY / 2;
        // print(rightX + " : " + leftX + " : " + upY + " : " + downY);
    }

    void Update()
    {
        // 플레이어가 경계 끝에 왔을때 배경 9개 전부 위치 변경
        if (player.position.x >= rightX)
        {
            for (int i = 0; i < background.Length; i++)
            {
                Vector3 pos = background[i].position;
                pos.x += sizeX;
                background[i].position = pos;
            }
            rightX += sizeX;
            leftX += sizeX;
        }

        if (player.position.x <= leftX)
        {
            for (int i = 0; i < background.Length; i++)
            {
                Vector3 pos = background[i].position;
                pos.x -= sizeX;
                background[i].position = pos;
            }
            rightX -= sizeX;
            leftX -= sizeX;
        }

        if (player.position.y >= upY)
        {
            for (int i = 0; i < background.Length; i++)
            {
                Vector3 pos = background[i].position;
                pos.y += sizeY;
                background[i].position = pos;
            }
            upY += sizeY;
            downY += sizeY;
        }

        if (player.position.y <= downY)
        {
            for (int i = 0; i < background.Length; i++)
            {
                Vector3 pos = background[i].position;
                pos.y -= sizeY;
                background[i].position = pos;
            }
            upY -= sizeY;
            downY -= sizeY;
        }
    }
}
