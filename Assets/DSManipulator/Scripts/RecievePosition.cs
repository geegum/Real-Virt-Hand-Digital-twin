using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading.Tasks;

public class RecievePosition : MonoBehaviour
{

    public GameObject Mobile_Box; // ������ ������ ������Ʈ

    private TcpClient client;
    private NetworkStream stream;
    private string serverIP = "127.0.0.1"; // Server IP address
    private int serverPort = 11111; // Server port
    private float receivedX = 0.0f;
    private float receivedY = 0.0f;
    private float receivedT = 0.0f;

    void Start()
    {
        try
        {
            client = new TcpClient(serverIP, serverPort);
            stream = client.GetStream();
            Debug.Log("Connected to server.");
            Task.Run(() => ListenForData());
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to connect to server: " + e.Message);
        }
    }

    void ListenForData()
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
                    ParseData(response);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error receiving data: " + e.Message);
            }
        }
    }

    void ParseData(string data)
    {
        try
        {
            if (data.StartsWith("x") && data.Contains("y") && data.Contains("t"))
            {
                int xStart = data.IndexOf('x') + 1;
                int yStart = data.IndexOf('y') + 1;
                int tStart = data.IndexOf('t') + 1;
                
                string xValue = data.Substring(xStart, yStart - xStart - 1);
                string yValue = data.Substring(yStart, tStart - yStart - 1);
                string tValue = data.Substring(tStart);

                float x = float.Parse(xValue);
                float y = float.Parse(yValue);
                float t = float.Parse(tValue);

                receivedX = x / 100;
                receivedY = y / 100;
                receivedT = t;

                Debug.Log($"Parsed data - X: {x}, Y: {y}, T: {t}");


            MoveRobotToPosition(receivedX, receivedY, receivedT);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing data: " + e.Message);
        }
    }
    void MoveRobotToPosition(float x, float y, float t)
{
    if (Mobile_Box != null)
    {
        // Transform x and y into Unity's 3D space (XZ plane)
        Vector3 newPosition = new Vector3(x, 0, y); // y becomes z in Unity

        // Set the new position
        Mobile_Box.transform.position = newPosition;

        // Set the new rotation (assuming t is in radians, convert to degrees)
        float rotationAngle = t * Mathf.Rad2Deg; // Convert from radians to degrees
        Quaternion newRotation = Quaternion.Euler(0, rotationAngle, 0); // Rotation around the Y-axis
        Mobile_Box.transform.rotation = newRotation;

        Debug.Log($"Robot moved to position: {newPosition}, with rotation: {rotationAngle} degrees");
    }
    else
    {
        Debug.LogError("Mobile_Box is not assigned.");
    }
}
}
