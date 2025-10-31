using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveJointAngles : MonoBehaviour
{
    public Impedance_Control3 impedanceControl;
    public string fileName = "JointAngles.csv";
    private float elapsedTime = 0f;
    private StreamWriter writer;

    void Start()
    {
        // Impedance_Control 스크립트를 자동으로 찾기
        if (impedanceControl == null)
        {
            impedanceControl = FindObjectOfType<Impedance_Control3>();
        }

        // CSV 파일 열기 및 헤더 작성
        writer = new StreamWriter(fileName);
        writer.WriteLine("Time,Joint1,Joint2,Joint3,Joint4,Joint5,Joint6");
    }

    void Update()
    {
        // 5초 동안 데이터 저장
        if (elapsedTime < 5f)
        {
            // 각도값 가져오기
            float[] angles = impedanceControl.GetJointAngles(); // 이 메서드는 Impedance_Control에서 각도값을 가져오는 메서드라고 가정합니다.

            // 현재 시간과 각도를 CSV에 저장
            writer.Write(elapsedTime.ToString("F3")); // 시간 기록
            for (int i = 0; i < angles.Length; i++)
            {
                writer.Write("," + angles[i].ToString("F3")); // 각도 기록
            }
            writer.WriteLine();

            // 경과 시간 업데이트
            elapsedTime += Time.deltaTime;
        }
        else
        {
            // 5초가 지나면 파일 닫기
            if (writer != null)
            {
                writer.Close();
                writer = null;
                Debug.Log("Joint angles saved to " + fileName);
            }
        }
    }

    void OnDestroy()
    {
        // 씬이 종료되거나 게임이 중지될 때 파일이 열려 있으면 닫기
        if (writer != null)
        {
            writer.Close();
        }
    }
}
