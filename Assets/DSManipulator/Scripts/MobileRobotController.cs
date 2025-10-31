using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

public class test : MonoBehaviour
{
    public GameObject ee_Target; // 엔드 이펙터 타겟

    // 로봇 관절들
    private GameObject[] joint = new GameObject[6];
    private float[] angle = new float[6];
    private float[] angle_ = new float[6];
    private Vector3[] point = new Vector3[7];           // 관절 끝의 월드 포지션
    private Vector3[] axis = new Vector3[6];            // 각 축의 로컬 방향
    private Quaternion[] rotation = new Quaternion[6];  // 부모에 상대적인 각 관절의 로컬 회전(쿼터니언)
    private Quaternion[] wRotation = new Quaternion[6]; // 각 관절의 월드 회전(쿼터니언)
    private float[] minAngle = new float[6];            // 관절 회전의 제한
    private float[] maxAngle = new float[6];
    private Vector3[] vangle = new Vector3[6]; //Unity상에서 회전방향

    private Vector3 pos;      // 참조(목표) 위치
    private Vector3 rot;      // 참조(목표) 자세

    private float speedlimit = 10.0f;
    private float t = 0;

    void Start()
    {
        // 로봇 관절 초기화
        for (int i = 0; i < joint.Length; i++)
        {
            joint[i] = GameObject.Find("Axis_" + i.ToString());
        }

        // 초기 각도 설정
        angle[0] = 0f;
        angle[1] = 0f;
        angle[2] = 90f;
        angle[3] = 0f;
        angle[4] = 90f;
        angle[5] = 0f;

        for (int i = 0; i < angle_.Length; i++)
        {
            angle_[i] = 0f;
        }

        // 각도 제한 설정
        minAngle[0] = -175f;
        maxAngle[0] = 175f;
        minAngle[1] = -95f;
        maxAngle[1] = 95f;
        minAngle[2] = -120f;
        maxAngle[2] = 120f;
        minAngle[3] = -170f;
        maxAngle[3] = 170f; 
        minAngle[4] = -140f;
        maxAngle[4] = 140f;
        minAngle[5] = -175f;
        maxAngle[5] = 175f;

        vangle[0] = new Vector3(0f, -1f, 0f);
        vangle[1] = new Vector3(0f, 0f, -1f);
        vangle[2] = new Vector3(0f, 0f, -1f);
        vangle[3] = new Vector3(0f, -1f, 0f);
        vangle[4] = new Vector3(0f, 0f, - 1f);
        vangle[5] = new Vector3(0f, 1f, 0f);
    }



    void Update()
    {
        t += Time.deltaTime;
        for (int i = 0; i < joint.Length; i++)
        {
            if (speedlimit * t >= 90f || speedlimit * t <= -90f) break;
            joint[i].transform.localEulerAngles = speedlimit * t * vangle[i];
        }
    }








}