using System;
using System.IO;
using System.Collections;
using UnityEngine;

public class XRScreenshot : MonoBehaviour
{
    // 在 Inspector 里把你用于渲染 XR 画面的 Camera 拖进来（一般是 Main Camera）
    public Camera xrCamera;

    public void CaptureScreenshot()
    {
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        // 等这一帧画完（避免和渲染抢资源）
        yield return new WaitForEndOfFrame();

        if (xrCamera == null)
        {
            Debug.LogError("[Screenshot] xrCamera is null. Please assign it in Inspector.");
            AndroidToast.ShowToast("Screenshot failed:\nCamera not assigned");
            yield break;
        }

        int width = Screen.width;
        int height = Screen.height;

        // 1. 创建一个临时 RenderTexture
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);

        // 记住之前的 targetTexture 和 active
        RenderTexture prevTarget = xrCamera.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        try
        {
            // 2. 让 XR 相机渲染到这个 RenderTexture 上
            xrCamera.targetTexture = rt;
            xrCamera.Render();

            // 3. 从 RenderTexture 读像素到 Texture2D
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            // 4. 写入 PNG 到你之前验证过 OK 的目录
            string fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";

            // 你之前已经实测过这个路径能写成功
            string dir = "/sdcard/Pictures";
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Screenshot] CreateDirectory failed: " + e);
            }

            string path = Path.Combine(dir, fileName);

            try
            {
                byte[] png = tex.EncodeToPNG();
                File.WriteAllBytes(path, png);
                Debug.Log($"[Screenshot] Saved to: {path}");

                AndroidToast.ShowToast("Screenshot saved:\n" + fileName);
            }
            catch (Exception e)
            {
                Debug.LogError("[Screenshot] Save failed: " + e);
                AndroidToast.ShowToast("Screenshot failed:\n" + e.Message);
            }

            UnityEngine.Object.Destroy(tex);
        }
        finally
        {
            // 5. 恢复之前的状态
            xrCamera.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            if (rt != null)
                UnityEngine.Object.Destroy(rt);
        }
    }
}
