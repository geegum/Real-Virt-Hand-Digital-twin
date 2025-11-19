using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Socket_Joint : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    public static Socket_Joint Instance { get; private set; }
    public float[] newAngles = new float[6];
    public float rotationAngle; // 추가된 rotationAngle 변수
    private string clientId = "unity"; // 고유한 클라이언트 식별자 설정
    private bool isConnected = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        ConnectToServer();
    }

    private async void ConnectToServer()
    {
        try
        {
            client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", 5000); // 서버 IP 주소와 포트 번호
            stream = client.GetStream();
            await SendClientId();
            Debug.Log("Connected to server.");
            isConnected = true;
            StartSendingData(); // 서버에 연결되면 주기적으로 데이터 전송 시작
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not connect to server: " + e.Message);
        }
    }

    private async Task SendClientId()
    {
        try
        {
            if (stream != null)
            {
                byte[] idData = Encoding.UTF8.GetBytes(clientId);
                await stream.WriteAsync(idData, 0, idData.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending client ID: " + e.Message);
        }
    }

    private async void StartSendingData()
    {
        while (isConnected)
        {
            SendData(newAngles, rotationAngle);
            await Task.Delay(10); // 10ms 대기 (100Hz 주기)
        }
    }

    public async void SendData(float[] angles, float rotationAngle)
    {
        if (!isConnected || client == null || !client.Connected)
        {
            Debug.LogError($"Not connected to server.");
            return;
        }

        try
        {
            if (stream != null)
            {
                byte[] data = new byte[(angles.Length + 1) * sizeof(float)]; // 각도 배열과 rotationAngle 포함
                Buffer.BlockCopy(angles, 0, data, 0, angles.Length * sizeof(float));
                Buffer.BlockCopy(BitConverter.GetBytes(rotationAngle), 0, data, angles.Length * sizeof(float), sizeof(float));
                await stream.WriteAsync(data, 0, data.Length);
                Debug.Log("Data sent to server.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending data: " + e.Message);
        }
    }

    private void OnDestroy()
    {
        isConnected = false;
        if (stream != null)
            stream.Close();
        if (client != null)
            client.Close();
    }
}
