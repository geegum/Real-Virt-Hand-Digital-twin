using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Re_Oscill : MonoBehaviour
{
    public Transform target; // 추적할 대상
    public float positionThreshold = 0.05f; // 위치 오차 한계값
    public float rotationThreshold = 5.0f; // 회전 오차 한계값 (도 단위)
    public float moveSpeed = 5.0f; // 이동 속도
    public float rotateSpeed = 5.0f; // 회전 속도

    void Update()
    {
        // 위치 오차 계산
        if (Vector3.Distance(transform.position, target.position) > positionThreshold)
        {
            // 부드러운 위치 이동
            transform.position = Vector3.Lerp(transform.position, target.position, moveSpeed * Time.deltaTime);
        }

        // 회전 오차 계산
        if (Quaternion.Angle(transform.rotation, target.rotation) > rotationThreshold)
        {
            // 부드러운 회전
            transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, rotateSpeed * Time.deltaTime);
        }
    }
}