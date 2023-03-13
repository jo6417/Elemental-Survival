using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomMethod
{
    // 오브젝트의 하이어라키상 경로 얻기
    public static string GetFullPath(this Transform transform)
    {
        string path = "/" + transform.name;
        while (transform.transform.parent != null)
        {
            transform = transform.parent;
            path = "/" + transform.name + path;
        }
        return path;
    }

    // 스프라이트의 실제 크기을 계산해서 리턴
    public static Vector2 AntualSpriteScale(SpriteRenderer spriteRenderer)
    {
        // SpriteRenderer에서 Sprite 가져오기
        Sprite sprite = spriteRenderer.sprite;

        // Sprite의 텍스처 크기 가져오기
        Texture2D texture = sprite.texture;
        int textureWidth = texture.width;
        int textureHeight = texture.height;

        // SpriteRenderer의 Sprite가 차지하는 영역 크기 가져오기
        Bounds spriteBounds = sprite.bounds;
        float spriteWidth = spriteBounds.size.x;
        float spriteHeight = spriteBounds.size.y;

        // SpriteRenderer의 Transform 스케일 값 가져오기
        Vector3 localScale = spriteRenderer.transform.localScale;
        float scaleX = localScale.x;
        float scaleY = localScale.y;

        // 실제 크기 계산하기
        float actualWidth = textureWidth * spriteWidth * scaleX;
        float actualHeight = textureHeight * spriteHeight * scaleY;

        // 계산된 스케일값 리턴
        return new Vector3(actualWidth / textureWidth, actualHeight / textureHeight, 1);
    }

    // Hex 값 Color를 RGB 값으로 변환
    public static Color HexToRGBA(string hex, float alpha = 1)
    {
        Color color;
        ColorUtility.TryParseHtmlString("#" + hex, out color);

        if (alpha != 1)
        {
            color.a = alpha;
        }

        return color;
    }

    public static float GetVector2Dir(Vector2 to, Vector2 from)
    {
        // 타겟 방향
        Vector2 targetDir = to - from;

        // 플레이어 방향 2D 각도
        float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

        // 각도를 리턴
        return angle;
    }

    public static float GetVector2Dir(Vector2 targetDir)
    {
        // 플레이어 방향 2D 각도
        float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

        // 각도를 리턴
        return angle;
    }
}
