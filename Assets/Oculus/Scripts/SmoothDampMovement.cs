using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothDampMovement : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.3f;

    private Vector3 velocity = Vector3.zero;

    void Update()
    {
        // 위치 부드럽게 이동
        transform.position = Vector3.SmoothDamp(transform.position, target.position, ref velocity, smoothTime);
    }
}
