using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LowPassFilterMovement : MonoBehaviour
{
    public Transform target;
    private float positionAlpha = 0.05f;
    private float rotationAlpha = 0.05f;

    void Update()
    {
        // Low-pass filter 적용하여 위치 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, target.position, positionAlpha);

        // Low-pass filter 적용하여 회전 부드럽게 변경
        transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, rotationAlpha);
    }
}
