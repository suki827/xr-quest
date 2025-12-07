using UnityEngine;

public class PassthroughModeSwitcher : MonoBehaviour
{
    [Header("Underlay：PassthroughUnderlay 上的 OVRPassthroughLayer")]
    public OVRPassthroughLayer underlayLayer;

    [Header("小窗口根节点：[BuildingBlock] Passthrough Window")]
    public GameObject windowRoot;

    private bool isFullScreen = false;   // 默认先窗口模式

    private void Start()
    {
        SetWindowMode();
    }

    public void ToggleMode()   // Button OnClick 调这个
    {
        if (isFullScreen)
            SetWindowMode();
        else
            SetFullScreenMode();
    }

    // 仅小窗口
    private void SetWindowMode()
    {
        isFullScreen = false;

        if (underlayLayer != null)
        {
            underlayLayer.textureOpacity = 0f;   // 背景透明，不显示全屏现实
            underlayLayer.hidden = false;
        }

        if (windowRoot != null)
            windowRoot.SetActive(true);          // 小窗口可见
    }

    // 全屏 passthrough
    private void SetFullScreenMode()
    {
        isFullScreen = true;

        if (underlayLayer != null)
        {
            underlayLayer.textureOpacity = 1f;   // 背景全屏现实
            underlayLayer.hidden = false;
        }

        if (windowRoot != null)
            windowRoot.SetActive(false);         // 关闭小窗口
    }
}
