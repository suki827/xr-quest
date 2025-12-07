using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class MjpegToRawImage : MonoBehaviour
{
    [Header("MJPEG 流地址 (multipart/x-mixed-replace)")]
    public string Url = "http://192.168.0.100:8000/stream/usb_cam";

    [Header("边界名（需与服务端一致，不带前缀 -- ）")]
    public string Boundary = "frameboundary";   // 服务端若是 --frame，这里只填 "frame"

    [Header("网络与重连")]
    public int ConnectTimeoutMs = 5000;
    public int RWTimeoutMs = 15000;
    public float ReconnectDelaySec = 0.5f;

    private RawImage _rawImage;
    private Texture2D _tex;
    private Thread _thread;
    private volatile bool _running;
    private readonly Queue<Action> _queue = new();
    private readonly object _queueLock = new();
    private readonly byte[] _lineBuf = new byte[8192];

    void Start()
    {
        _rawImage = GetComponent<RawImage>();

        // 先给一个占位纹理
        _tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        SetTexture(_tex);

        _running = true;
        _thread = new Thread(StreamWorker)
        {
            IsBackground = true,
            Name = "MJPEGWorker"
        };
        _thread.Start();
    }

    void OnDestroy()
    {
        _running = false;
        try { _thread?.Join(500); } catch { }
    }

    void Update()
    {
        // 主线程执行排队的纹理更新
        while (true)
        {
            Action a = null;
            lock (_queueLock)
            {
                if (_queue.Count > 0)
                    a = _queue.Dequeue();
            }

            if (a == null) break;

            try { a(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    private void StreamWorker()
    {
        string boundaryLine = "--" + Boundary; // 流里的分隔行是 --frame 这种

        while (_running)
        {
            HttpWebRequest req = null;
            Stream stream = null;
            try
            {
                req = (HttpWebRequest)WebRequest.Create(Url);
                req.Timeout = ConnectTimeoutMs;
                req.ReadWriteTimeout = RWTimeoutMs;
                req.KeepAlive = true;

                using (var resp = (HttpWebResponse)req.GetResponse())
                {
                    stream = resp.GetResponseStream();
                    stream.ReadTimeout = RWTimeoutMs;

                    while (_running)
                    {
                        // 1) 找到分界线
                        if (!ReadLine(stream, out var line)) break;
                        if (!line.StartsWith(boundaryLine, StringComparison.Ordinal))
                            continue;

                        // 2) 解析头
                        int contentLength = -1;
                        while (ReadLine(stream, out line) && line.Length > 0)
                        {
                            if (line.StartsWith("Content-Length",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                var idx = line.IndexOf(':');
                                if (idx >= 0 && int.TryParse(
                                        line[(idx + 1)..].Trim(), out var len))
                                    contentLength = len;
                            }
                        }

                        if (contentLength <= 0) continue;

                        // 3) 读取 JPEG 数据
                        var buf = new byte[contentLength];
                        int read = 0;
                        while (read < contentLength)
                        {
                            int n = stream.Read(buf, read, contentLength - read);
                            if (n <= 0) throw new EndOfStreamException();
                            read += n;
                        }

                        // 4) 在主线程更新纹理
                        EnqueueOnMainThread(() =>
                        {
                            if (_tex == null) return;

                            _tex.LoadImage(buf, markNonReadable: false);
                            SetTexture(_tex);
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MJPEG] reconnect: {e.GetType().Name} {e.Message}");
                Thread.Sleep((int)(ReconnectDelaySec * 1000));
            }
            finally
            {
                try { stream?.Dispose(); } catch { }
                try { req?.Abort(); } catch { }
            }
        }
    }

    private bool ReadLine(Stream s, out string line)
    {
        int pos = 0;
        while (true)
        {
            int b = s.ReadByte();
            if (b == -1) { line = null; return false; }
            if (b == '\n') break;
            if (b != '\r')
            {
                if (pos < _lineBuf.Length) _lineBuf[pos++] = (byte)b;
            }
        }
        line = Encoding.ASCII.GetString(_lineBuf, 0, pos);
        return true;
    }

    private void EnqueueOnMainThread(Action a)
    {
        lock (_queueLock) _queue.Enqueue(a);
    }

    private void SetTexture(Texture2D tex)
    {
        if (_rawImage == null) return;
        _rawImage.texture = tex;

        // 如果想让 RawImage 根据纹理原始大小自动调节，可以顺带：
        // _rawImage.SetNativeSize();
    }
}
