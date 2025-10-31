using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

public class SingularityDataLogger : MonoBehaviour
{
    private string filePath;
    private float elapsedTime; // 누적된 시간

    void Start()
    {
        filePath = Application.dataPath + "SingularityData.csv";
        elapsedTime = 0f; // 초기화

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "ElapsedTime,JointAngles,Jacobian\n");
        }
    }

    void Update()
    {
        elapsedTime += Time.deltaTime; // 매 프레임 경과 시간 누적
    }

    public void LogData(float[] jointAngles, Matrix<double> jacobian)
    {
        string elapsedTimeString = elapsedTime.ToString("F3"); // 누적된 시간을 문자열로 변환, 소수점 3자리까지
        string jointAngleString = string.Join(",", jointAngles);
        string jacobianString = MatrixToString(jacobian);

        string logEntry = $"{elapsedTimeString},{jointAngleString},{jacobianString}\n";
        File.AppendAllText(filePath, logEntry);
    }

    private string MatrixToString(Matrix<double> matrix)
    {
        List<string> elements = new List<string>();

        for (int i = 0; i < matrix.RowCount; i++)
        {
            for (int j = 0; j < matrix.ColumnCount; j++)
            {
                elements.Add(matrix[i, j].ToString());
            }
        }

        return string.Join(",", elements);
    }
}
