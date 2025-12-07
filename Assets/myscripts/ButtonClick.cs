using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
public class ButtonClick : MonoBehaviour
   {
    public string command = "default";

    public string serverUrl = "http://192.168.0.100:8000/cmd";
    void Start()
    {

        // get Button component
        Button btn = GetComponent<Button>();

        if (btn != null)
        {
            // add listener event
            btn.onClick.AddListener(OnButtonClicked);
        }
        else
        {
            Debug.LogError("ButtonClickTest：not find Button ！");
        }
    }

    void OnButtonClicked()
    {
        Debug.Log("the button be clicked: " + command);
        // 点击时发送命令到 PC
        StartCoroutine(SendCommandToServer(command));
    }

    IEnumerator SendCommandToServer(string cmd)
    {
        // 这里用 JSON 方式发送：{"cmd":"forward"}
        string json = "{\"cmd\":\"" + cmd + "\"}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(serverUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"[ButtonClick] sending cmd = {cmd} to {serverUrl}");

            yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError("[ButtonClick] HTTP Error: " + req.error);
            }
            else
            {
                Debug.Log("[ButtonClick] Server response: " + req.downloadHandler.text);
            }
        }
    }


}