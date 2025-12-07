using UnityEngine;

public class FullScreenSwitchor : MonoBehaviour
{
    public Transform targetQuad;   // 拖你的 Quad 进来
    public float smallScale = 1f;  // 原始大小
    public float largeScale = 2f;  // 放大大小
    private bool isLarge = false;  // 当前是否放大

    public void ToggleFullScreen()
    {
        if (targetQuad == null)
        {
            Debug.LogWarning("FullScreenSwitcher: targetQuad 未绑定！");
            return;
        }

        if (isLarge)
        {
            // 缩回原始尺寸
            targetQuad.localScale = new Vector3(smallScale, smallScale, 1f);
            isLarge = false;
        }
        else
        {
            // 放大到大窗口 / 全屏效果
            targetQuad.localScale = new Vector3(largeScale, largeScale, 1f);
            isLarge = true;
        }
    }
}
