using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading;

public class AxisMover : MonoBehaviour
{
    public ForceColorChanger ForceColorChanger;
    public Vector3 force1;

    public GameObject Axis_0; // �Ÿ� ����� ���� ���� ������Ʈ
    public GameObject Mobile_Box; // ������ ������ ������Ʈ
    public GameObject target; // �Ÿ� ����� ���� Ÿ�� ������Ʈ
    public float mobilemoveSpeed = 1f; // ������Ʈ�� ������ �ӵ�
    public float distanceThresholdMObile; // Ÿ�ٰ��� �Ÿ� ����

    private TcpClient client;
    private NetworkStream stream;
    private string serverIP = "127.0.0.1"; // ���� IP �ּ�
    private int serverPort = 11111; // ���� ��Ʈ

    private Thread receiveThread;

    private float receivedX = 0.0f;
    private float receivedY = 0.0f;
    private float receivedT = 0.0f;

    void Start()
    {
        distanceThresholdMObile = 0.93f;
        // TCP Ŭ���̾�Ʈ ���� ����
        try
        {
            client = new TcpClient(serverIP, serverPort);
            stream = client.GetStream();
            Debug.Log("Connected to server.");

            // Start a thread to continuously receive data
            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to connect to server: " + e.Message);
        }
    }
    void Update()
    {

        Vector3 newPosition = new Vector3(receivedX, 0 , receivedY); // y becomes z in Unity

        // Set the new position
        Mobile_Box.transform.position = newPosition;

        // Set the new rotation (assuming t is in radians, convert to degrees)
        float rotationAngle = receivedT;//* Mathf.Deg2Rad; // Convert from radians to degrees
        Quaternion newRotation = Quaternion.Euler(0, rotationAngle, 0); // Rotation around the Y-axis
        Mobile_Box.transform.rotation = newRotation;


        force1=ForceColorChanger.force;
        if (Axis_0 != null && Mobile_Box != null && target != null)
        {
            // Axis_0�� target ���� �Ÿ� ���
            float base2targetDist = Vector3.Distance(Axis_0.transform.position, target.transform.position);
            
            if (ForceColorChanger.forceMagnitude >20f)
            {
                // Ÿ�� ���� ���
                Vector3 directionToTarget = target.transform.position - Mobile_Box.transform.position;
                Debug.Log($"directionToTarget - X: {directionToTarget.x}, Y: {directionToTarget.y}, Z: {directionToTarget.z}");

                // ��鿡���� �̵��ϵ��� ���� ������ y ���� 0���� ����
                directionToTarget.y = 0;
                directionToTarget.Normalize(); // ���� ���͸� ����ȭ�Ͽ� �̵� �ӵ��� �����ϰ� ����

                // Mobile_Box�� �̵�
                //Mobile_Box.transform.Translate(directionToTarget * moveSpeed * Time.deltaTime, Space.World);

                // target�� ��� ��ǥ ���ϱ� (Mobile_Box ����)
                Vector3 relativePosToTarget = Mobile_Box.transform.InverseTransformPoint(target.transform.position);

                // Mobile_Box�� ���� X�� ����
                Vector3 mobileBoxXAxis = Mobile_Box.transform.right;

                // Signed angle ���
                float signedAngleBetweenXAxes = Vector3.SignedAngle(mobileBoxXAxis, directionToTarget, Vector3.up);
                Debug.Log("Signed angle between Mobile_Box X-axis and target direction: " + signedAngleBetweenXAxes);

                // ���� ���� �˻� �� ȸ�� ����
                if (Mathf.Abs(signedAngleBetweenXAxes) > 5f)
                {
                    // ȸ�� ����
                    float rotationSpeed = 10f; // Rotation speed in degrees per second
                    float rotationStep = rotationSpeed * Time.deltaTime;

                    // Calculate the new angle
                    float currentYAngle = Mobile_Box.transform.eulerAngles.y;
                    float targetAngle = currentYAngle + signedAngleBetweenXAxes;

                    // Smoothly rotate towards the target angle
                    float newAngle = Mathf.MoveTowardsAngle(currentYAngle, targetAngle, rotationStep);
                    Mobile_Box.transform.rotation = Quaternion.Euler(0, newAngle, 0);
                    if(signedAngleBetweenXAxes > 5f){SendRight();}
                    else if(signedAngleBetweenXAxes < -5f){SendLeft();}
                }
                else
                {
                    SendFront();
                    // Move forward when aligned
                    Mobile_Box.transform.Translate(directionToTarget * mobilemoveSpeed * Time.deltaTime, Space.World);
                }

                // TCP�� �κ��� �̵� ������ ����
                SendMovementData(Mobile_Box.transform.position, directionToTarget, relativePosToTarget, signedAngleBetweenXAxes);

                float movedx = Mobile_Box.transform.position.x;
                float movedy = Mobile_Box.transform.position.y;
                float movedt = Mobile_Box.transform.rotation.eulerAngles.y;

                receivedX = movedx;
                receivedY = movedy;
                receivedT = movedt;


            }
        }
        else
        {
            if (Axis_0 == null)
            {
                Debug.LogWarning("Axis_0 is not assigned.");
            }

            if (Mobile_Box == null)
            {
                Debug.LogWarning("Mobile_Box is not assigned.");
            }

            if (target == null)
            {
                Debug.LogWarning("Target is not assigned.");
            }
        }
    }

void ReceiveData()
{
    byte[] buffer = new byte[1024];
    while (client.Connected)
    {
        try
        {
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead > 0)
            {
                string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Debug.Log("Received from server: " + response);

                // Parse the received data (e.g., "x0.00y0.00t0.00")
                ParseData(response);
            }
        }
        catch (Exception e)
        {
            //Debug.LogError("Error receiving data: " + e.Message);
        }
    }
}

void ParseData(string data)
{
    try
    {
        // Example: "x0.00y0.00t0.00"
        if (data.StartsWith("x") && data.Contains("y") && data.Contains("t"))
        {
            // Split the string based on 'x', 'y', and 't'
            int xStart = data.IndexOf('x') + 1;
            int yStart = data.IndexOf('y') + 1;
            int tStart = data.IndexOf('t') + 1;

            // Extract the substrings for x, y, and t
            string xValue = data.Substring(xStart, yStart - xStart - 1);
            string yValue = data.Substring(yStart, tStart - yStart - 1);
            string tValue = data.Substring(tStart);

            // Convert the string values to floats
            float x = float.Parse(xValue);
            float y = float.Parse(yValue);
            float t = float.Parse(tValue);

            float receivedX = x / 10000;
            float receivedY = y / 10000;
            float receivedT = t;



            // Log the parsed values
            Debug.Log($"Parsed data - X: {receivedX}, Y: {receivedY}, T: {receivedT}");
        }
    }
    catch (Exception e)
    {
        //Debug.LogError("Error parsing data: " + e.Message);
    }
}


    // �κ��� ��ġ, ����, ��� ��ġ, X�� ������ TCP�� ����
    void SendMovementData(Vector3 position, Vector3 direction, Vector3 relativePosToTarget, float signedAngleBetweenXAxes)
    {
        if (stream != null)
        {
            try
            {
                // ������ �����͸� ������
                string data = $"Position: {position}, Direction: {direction}, relativePosToTarget: {relativePosToTarget}, Signed Angle: {signedAngleBetweenXAxes}";
                byte[] bytes = Encoding.ASCII.GetBytes(data);

                // �����͸� TCP�� ����
                //stream.Write(bytes, 0, bytes.Length);
                Debug.Log("Sent movement data: " + data);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to send data: " + e.Message);
            }
        }
    }

        void SendStop()
    {
        if (stream != null)
        {
            try
            {
                // ������ �����͸� ������
                string data = $"s";
                byte[] bytes = Encoding.ASCII.GetBytes(data);

                // �����͸� TCP�� ����
                stream.Write(bytes, 0, bytes.Length);
                Debug.Log("Sent movement data: " + data);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to send data: " + e.Message);
            }
        }
    }


    // �κ��� ��ġ, ����, ��� ��ġ, X�� ������ TCP�� ����
    void SendRight()
    {
        if (stream != null)
        {
            try
            {
                // ������ �����͸� ������
                string data = $"r";
                byte[] bytes = Encoding.ASCII.GetBytes(data);

                // �����͸� TCP�� ����
                stream.Write(bytes, 0, bytes.Length);
                Debug.Log("Sent movement data: " + data);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to send data: " + e.Message);
            }
        }
    }
        void SendLeft()
    {
        if (stream != null)
        {
            try
            {
                // ������ �����͸� ������
                string data = $"l";
                byte[] bytes = Encoding.ASCII.GetBytes(data);

                // �����͸� TCP�� ����
                stream.Write(bytes, 0, bytes.Length);
                Debug.Log("Sent movement data: " + data);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to send data: " + e.Message);
            }
        }
    }
        void SendFront()
    {
        if (stream != null)
        {
            try
            {
                // ������ �����͸� ������
                string data = $"f";
                byte[] bytes = Encoding.ASCII.GetBytes(data);

                // �����͸� TCP�� ����
                stream.Write(bytes, 0, bytes.Length);
                Debug.Log("Sent movement data: " + data);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to send data: " + e.Message);
            }
        }
    }
    void OnApplicationQuit()
    {
        if (stream != null)
        {
            stream.Close();
        }
        if (client != null)
        {
            client.Close();
        }

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
    }
}
