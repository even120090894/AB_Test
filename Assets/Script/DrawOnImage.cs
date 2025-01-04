using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawOnImage : MonoBehaviour
{
    public int width = 5;

    private RenderTexture saveRT;

    void Update () {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * 15f, Color.red);
        if (Physics.Raycast(ray,out hit, 15f))
        {
            if (Input.GetMouseButton(0))
            {

                GameObject target = hit.collider.gameObject;
                RawImage rawImage = target.GetComponent<RawImage>();
                RenderTexture targetT = rawImage.texture as RenderTexture;

                saveRT = targetT;

                // 将 localPosition 转换为以 GameObject 左下角为原点的数值
                Vector2 texturePos = LocalPos2TexturePos(target);

                // 修改纹理在指定坐标的颜色
                ModifyPixelInRenderTexture(targetT, texturePos, Color.red, width);

            }
        }
	}
    void OnDestroy()
    {
        SaveRenderTextureToFile(saveRT, "SavedRenderTexture.png");
    }
    Vector2 LocalPos2TexturePos(GameObject target){
        RawImage rawImage = target.GetComponent<RawImage>();
        RenderTexture targetT = rawImage.texture as RenderTexture;

        // 获取 Canvas 下的坐标
        RectTransform canvasRect = rawImage.canvas.GetComponent<RectTransform>();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, Camera.main, out localPoint);

        RectTransform targetRect = target.GetComponent<RectTransform>();
        // 获取 GameObject 的宽度和高度
        float width = targetRect.rect.width;
        float height = targetRect.rect.height;

        // 将 localPosition 转换为以 GameObject 左下角为原点的数值
        Vector2 texturePos = new Vector2(localPoint.x + width / 2 - targetRect.anchoredPosition.x, localPoint.y + height / 2 - targetRect.anchoredPosition.y);
        texturePos *= targetT.width / width; // raw image 可能本身有缩放
        return texturePos;
    }
    void ModifyPixelInRenderTexture(RenderTexture renderTexture, Vector2 screenPosition, Color color, int width)
    {
        // 创建一个新的 Texture2D
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

        // 将 RenderTexture 的内容拷贝到 Texture2D
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;

        // 根据画笔大小修改 Texture2D 的像素
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < width; j++)
            {
                texture2D.SetPixel((int)screenPosition.x + i, (int)screenPosition.y + j, color);
            }
        }
        texture2D.Apply();

        // 将修改后的 Texture2D 内容拷贝回 RenderTexture
        RenderTexture.active = renderTexture;
        Graphics.Blit(texture2D, renderTexture);
        RenderTexture.active = null;

        // 清理临时创建的 Texture2D
        Destroy(texture2D);
    }
    void SaveRenderTextureToFile(RenderTexture rt, string fileName)
    {
        if (rt == null)
        {
            return;
        }
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D texture2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture2D.Apply();

        byte[] bytes = texture2D.EncodeToPNG();
        System.IO.File.WriteAllBytes($"Assets/Script/Lenia/TestImg/"+fileName, bytes);

        RenderTexture.active = currentRT;
    }
}
