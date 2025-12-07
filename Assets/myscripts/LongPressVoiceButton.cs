using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class LongPressVoiceButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    
    public string serverUrl = "http://192.168.0.100:8000/asr";

    [Header("rate")]
    public int sampleRate = 16000;

    private AudioClip clip;
    private bool isRecording = false;



    // pressdown trigger
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("the button be pressDown: " + gameObject.name);
        StartRecording();
    }

    // pressup trigger
    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("the button be pressUp: " + gameObject.name);
        StopRecording();
    }

    // 开始录音
    void StartRecording()
    {
        if (isRecording) return;

        clip = Microphone.Start(null, false, 10, sampleRate);
        isRecording = true;

        Debug.Log("start recording…");
    }

    // 停止录音并发送
    void StopRecording()
    {
        if (!isRecording) return;

        Microphone.End(null);
        isRecording = false;

        Debug.Log("stop recording …");

        StartCoroutine(SendAudio());
    }

    // AudioClip → WAV → 上传
    IEnumerator SendAudio()
    {
        byte[] wavData = AudioClipToWav(clip);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "voice.wav", "audio/wav");

        UnityWebRequest req = UnityWebRequest.Post(serverUrl, form);

        yield return req.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
        {
            Debug.LogError("voice upload fail: " + req.error);
        }
        else
        {
            Debug.Log("voice upload success: " + req.downloadHandler.text);
        }
    }

    // WAV 编码
    private byte[] AudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        int sampleCount = samples.Length;
        int byteCount = sampleCount * 2;

        // WAV Header
        writer.Write("RIFF".ToCharArray());
        writer.Write(36 + byteCount);
        writer.Write("WAVE".ToCharArray());
        writer.Write("fmt ".ToCharArray());
        writer.Write(16);
        writer.Write((short)1);       // PCM
        writer.Write((short)1);       // 单声道
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2); // byte rate
        writer.Write((short)2);       // block align
        writer.Write((short)16);      // bits per sample
        writer.Write("data".ToCharArray());
        writer.Write(byteCount);

        // PCM 数据 float → int16
        foreach (float f in samples)
        {
            short v = (short)(Mathf.Clamp(f, -1f, 1f) * 32767);
            writer.Write(v);
        }

        return stream.ToArray();
    }
}
