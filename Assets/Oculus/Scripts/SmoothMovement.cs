using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothMovement : MonoBehaviour
{
    public Transform target;
    public float positionSmoothTime = 0.3f;
    public float rotationSmoothTime = 0.3f;

    private Vector3 velocity = Vector3.zero;
    private Quaternion rotationVelocity;

    void Update()
    {
        // 위치 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, target.position, positionSmoothTime * Time.deltaTime);

        // 회전 부드럽게 변경
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, rotationSmoothTime * Time.deltaTime);
    }
}

