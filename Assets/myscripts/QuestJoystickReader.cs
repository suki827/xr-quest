using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

using MQTTnet;
using MQTTnet.Client;

public class QuestDualJoystickReader : MonoBehaviour
{
    [Header("Input Actions（在 Inspector 里拖进来）")]
    public InputActionReference leftStickAction;
    public InputActionReference rightStickAction;

    [Header("摇杆参数")]
    [Tooltip("摇杆多小的幅度视为松手")]
    public float deadZone = 0.3f;

    [Tooltip("判定为一个明确方向的阈值")]
    public float dirThreshold = 0.6f;

    [Header("MQTT 设置")]
    [Tooltip("Broker 列表，按顺序尝试连接")]
    public string[] brokers = { "192.168.0.101" };   // 树莓派 IP 等
    public int brokerPort = 1883;

    [Tooltip("机器人订阅的 Topic")]
    public string topic = "tony_one/cmd";

    private IMqttClient _mqttClient;
    private bool _mqttConnected = false;

    private enum StickSide { Left, Right }
    private enum MoveDir { None, Forward, Backward, Left, Right }

    private MoveDir _currentLeftDir = MoveDir.None;
    private MoveDir _currentRightDir = MoveDir.None;

    // ==================== Unity 生命周期 ====================

    private async void Start()
    {
        Application.runInBackground = true;
        await ConnectMqttAsync();
    }

    private void OnEnable()
    {
        if (leftStickAction != null) leftStickAction.action.Enable();
        if (rightStickAction != null) rightStickAction.action.Enable();
    }

    private void OnDisable()
    {
        if (leftStickAction != null) leftStickAction.action.Disable();
        if (rightStickAction != null) rightStickAction.action.Disable();

        _ = DisconnectMqttAsync();
    }

    private void Update()
    {
        if (leftStickAction != null)
        {
            Vector2 left = leftStickAction.action.ReadValue<Vector2>();
            HandleStick(left, StickSide.Left);
        }

        if (rightStickAction != null)
        {
            Vector2 right = rightStickAction.action.ReadValue<Vector2>();
            HandleStick(right, StickSide.Right);
        }
    }

    // ==================== 摇杆处理 ====================

    private void HandleStick(Vector2 stick, StickSide side)
    {
        MoveDir desired = GetDirection(stick);

        MoveDir current =
            (side == StickSide.Left) ? _currentLeftDir : _currentRightDir;

        // 方向没变化，不发命令
        if (desired == current)
            return;

        // 更新当前方向
        if (side == StickSide.Left)
            _currentLeftDir = desired;
        else
            _currentRightDir = desired;

        Debug.Log($"[{side} Stick] Dir = {desired}");

        // 发 MQTT
        _ = PublishJoystickCommandAsync(side, desired);
    }

    private MoveDir GetDirection(Vector2 s)
    {
        // 松手：发 None，外面会转成 stop
        if (s.magnitude < deadZone)
            return MoveDir.None;

        if (s.y > dirThreshold) return MoveDir.Forward;
        if (s.y < -dirThreshold) return MoveDir.Backward;
        if (s.x > dirThreshold) return MoveDir.Right;
        if (s.x < -dirThreshold) return MoveDir.Left;

        // 模糊区域当作松手
        return MoveDir.None;
    }

    // ==================== MQTT 连接 ====================

    private async Task ConnectMqttAsync()
    {
        try
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            await TryConnectToBrokersAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError("[MQTT] Connect error: " + ex.Message);
        }
    }

    private async Task TryConnectToBrokersAsync()
    {
        if (_mqttClient == null) return;

        foreach (var host in brokers)
        {
            if (string.IsNullOrWhiteSpace(host))
                continue;

            var options = new MqttClientOptionsBuilder()
                .WithClientId("quest3_" + Guid.NewGuid().ToString("N").Substring(0, 8))
                .WithTcpServer(host, brokerPort)
                .WithCleanSession()
                .Build();

            try
            {
                Debug.Log("[MQTT] Try connect " + host + ":" + brokerPort);

                // 旧版 API：只有一个参数
                await _mqttClient.ConnectAsync(options);

                if (_mqttClient.IsConnected)
                {
                    Debug.Log("[MQTT] Connected to " + host);
                    _mqttConnected = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MQTT] Failed connect " + host + " : " + ex.Message);
            }
        }

        Debug.LogError("[MQTT] All brokers connect failed.");
    }

    private async Task DisconnectMqttAsync()
    {
        if (_mqttClient != null && _mqttClient.IsConnected)
        {
            try
            {
                await _mqttClient.DisconnectAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MQTT] Disconnect error: " + ex.Message);
            }
        }
    }

    // ==================== MQTT 发送 ====================

    private async Task PublishJoystickCommandAsync(StickSide side, MoveDir dir)
    {
        if (_mqttClient == null || !_mqttClient.IsConnected || !_mqttConnected)
        {
            Debug.LogWarning("[MQTT] Not connected, skip publish.");
            return;
        }

        string data;

        switch (dir)
        {
            case MoveDir.Forward: data = "forward"; break;
            case MoveDir.Backward: data = "backward"; break;
            case MoveDir.Left: data = "left"; break;
            case MoveDir.Right: data = "right"; break;
            case MoveDir.None: data = "stop"; break;
            default: data = "stop"; break;
        }

        string stickStr = (side == StickSide.Left) ? "left" : "right";

        // JSON 格式：
        // {"type":"joystick","stick":"left/right","data":"forward/backward/left/right/stop"}
        string jsonPayload =
            $"{{\"type\":\"joystick\",\"stick\":\"{stickStr}\",\"data\":\"{data}\"}}";

        try
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(jsonPayload)
                .WithAtMostOnceQoS()
                .Build();

            // 旧版 API：PublishAsync(message) 只有一个参数
            await _mqttClient.PublishAsync(message);

            Debug.Log($"[MQTT] Publish: {topic} -> {jsonPayload}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[MQTT] Publish error: " + ex.Message);
        }
    }
}
