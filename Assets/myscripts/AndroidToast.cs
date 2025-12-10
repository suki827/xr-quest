using UnityEngine;

public static class AndroidToast
{
    public static void ShowToast(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // 1. 拿到当前 Activity
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                if (activity == null)
                    return;

                // 2. 在 UI 线程里调用 Android 的 Toast
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    using (AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast"))
                    {
                        AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

                        AndroidJavaObject toast = toastClass.CallStatic<AndroidJavaObject>(
                            "makeText",
                            context,
                            message,
                            toastClass.GetStatic<int>("LENGTH_SHORT")
                        );

                        toast.Call("show");
                    }
                }));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Toast] Exception: " + e.Message);
        }
#else
        // 在编辑器里就当成普通 log
        Debug.Log("[Toast] " + message);
#endif
    }
}
