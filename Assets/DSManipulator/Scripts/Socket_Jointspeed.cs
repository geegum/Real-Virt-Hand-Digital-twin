using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.InteropServices;

// [수정] 데이터 배열의 크기를 18 이상으로 늘려줍니다. (여유있게 20으로 설정)
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Packet
{
    public byte packetType;
    public byte validDataCount;
    public uint timeCombined;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] // 6(pos) + 6(vel) + 6(acc) = 18개 필요
    public float[] data;

    public float checksum;

    public Packet(bool initialize)
    {
        packetType = 0;
        validDataCount = 0;
        timeCombined = 0;
        data = new float[20]; // 배열 생성 크기도 맞춰줍니다.
        checksum = 0;
    }

    public void CalculateChecksum()
    {
        checksum = 0.0f;
        checksum += packetType;
        checksum += timeCombined;

        byte actualValidCount = 0;
        if (data != null)
        {
            foreach (var val in data)
            {
                if (!float.IsNaN(val))
                {
                    checksum += val;
                    actualValidCount++;
                }
            }
        }
        validDataCount = actualValidCount;
        checksum += validDataCount;
    }
}

public class Socket_Jointspeed : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;

    public static Socket_Jointspeed Instance { get; private set; }

    public float[] targetJointPosition = new float[6];
    public float[] targetJointVelocities = new float[6];
    public float rotationAngle; // 추가된 rotationAngle 변수
    
    private float[] previousJointVelocities = new float[6];
    private float[] estimatedJointAccelerations = new float[6];
    
    private bool isConnecting = false;
    private string clientId = "unity";
    private bool isConnected = false;
    private Combine_Hand_R combineHandR;

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
    }

    private void Start()
    {
        combineHandR = FindObjectOfType<Combine_Hand_R>();
        if (combineHandR == null)
        {
            Debug.LogError("Combine_Hand_R 스크립트를 찾을 수 없습니다!");
        }
    }

    private void Update()
    {
        if (combineHandR == null) return;

        if (combineHandR.Able && !isConnected && !isConnecting)
        {
            ConnectToServer();
        }
        else if (!combineHandR.Able && isConnected)
        {
            DisconnectFromServer();
        }
    }

    public async void ConnectToServer()
    {
        if (isConnected || isConnecting) return;
        isConnecting = true;

        try
        {
            client = new TcpClient();
            Debug.Log("서버에 연결을 시도합니다...");
            await client.ConnectAsync("127.0.0.1", 5000);
            stream = client.GetStream();
            await SendClientId();
            Debug.Log("서버에 연결되었습니다.");
            isConnected = true;

            Debug.Log("1초 후 데이터 전송을 시작합니다...");
            await Task.Delay(1000);
            StartSendingData();
        }
        catch (Exception e)
        {
            Debug.LogError($"서버 연결 실패: {e.Message}");
            client?.Close();
        }
        finally
        {
            isConnecting = false;
        }
    }

    public void DisconnectFromServer()
    {
        if (!isConnected) return;
        Debug.Log("서버와의 연결을 종료합니다.");
        isConnected = false;
        stream?.Close();
        stream = null;
        client?.Close();
        client = null;
    }

    private async Task SendClientId()
    {
        try
        {
            if (stream != null && stream.CanWrite)
            {
                byte[] idData = Encoding.UTF8.GetBytes(clientId);
                await stream.WriteAsync(idData, 0, idData.Length);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending client ID: {e.Message}");
        }
    }

    private async void StartSendingData()
    {
        while (isConnected)
        {
            for (int i = 0; i < 6; i++)
            {
                float currentVelocity = targetJointVelocities[i];
                float previousVelocity = previousJointVelocities[i];
                
            }
            Array.Copy(targetJointVelocities, previousJointVelocities, 6);

            // [✅ 수정] SendData 함수에 3개의 배열(위치, 속도, 가속도)을 모두 전달합니다.
            SendData(targetJointPosition, targetJointVelocities, estimatedJointAccelerations);
            
            await Task.Delay(20);
        }
    }

    // [✅ 수정] 함수가 위치, 속도, 가속도 배열을 모두 받도록 변경합니다.
    public async void SendData(float[] jointPositions, float[] jointVelocities, float[] jointAccelerations)
    {
        if (!isConnected || client == null || !client.Connected || stream == null || !stream.CanWrite)
        {
            return;
        }

        try
        {
            Packet packetToSend = new Packet(true);
            packetToSend.packetType = 1;

            // data 배열을 NaN으로 초기화
            for (int i = 0; i < packetToSend.data.Length; i++)
            {
                packetToSend.data[i] = float.NaN;
            }

            // [✅ 수정] 0~5번: 위치, 6~11번: 속도, 12~17번: 가속도 데이터를 순서대로 채웁니다.
            for (int i = 0; i < 6; i++)
            {
                packetToSend.data[i] = jointPositions[i];         // 0-5: Position
                packetToSend.data[i + 6] = jointVelocities[i];      // 6-11: Velocity
                packetToSend.data[i + 12] = jointAccelerations[i];  // 12-17: Acceleration
                
                packetToSend.data[18] = rotationAngle;  // 12-17: Acceleration
            }

            packetToSend.CalculateChecksum();
            byte[] dataToSend = StructToBytes(packetToSend);
            await stream.WriteAsync(dataToSend, 0, dataToSend.Length);
        }
        catch (Exception e)
        {
            Debug.LogError($"데이터 전송 오류: {e.Message}");
            DisconnectFromServer();
        }
    }

    private byte[] StructToBytes<T>(T structure) where T : struct
    {
        int size = Marshal.SizeOf(structure);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(structure, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        return arr;
    }

    private void OnDestroy()
    {
        DisconnectFromServer();
    }
}