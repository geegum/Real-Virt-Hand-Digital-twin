using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Keyboard_Target : MonoBehaviour
{
    public float moveSpeed = 0.1f;
    public float rotateSpeed = 30f; // 회전 속도를 조절하는 변수입니다.

    public GameObject GR_1;
    public GameObject GR_2;
    public GameObject GL_1;
    public GameObject GL_2;

    private float currentGripAngle = 0f; // 현재 그리퍼의 각도

    // Update is called once per frame
    void Update()
    {
        Vector3 moveDirection = Vector3.zero;
        Vector3 rotateDirection = Vector3.zero;
        float gripperChange = 0f;

        // w, a, s, d 키에 대한 입력 처리
        if (Input.GetKey(KeyCode.W))
            moveDirection += Vector3.right;
        if (Input.GetKey(KeyCode.S))
            moveDirection += Vector3.left;
        if (Input.GetKey(KeyCode.A))
            moveDirection += Vector3.forward;
        if (Input.GetKey(KeyCode.D))
            moveDirection += Vector3.back;

        // q, e 키에 대한 입력 처리
        if (Input.GetKey(KeyCode.Q))
            moveDirection += Vector3.up;
        if (Input.GetKey(KeyCode.E))
            moveDirection += Vector3.down;

        // l, j, i, k, u, o 키에 대한 회전 입력 처리
        if (Input.GetKey(KeyCode.L))
            rotateDirection -= Vector3.right;
        if (Input.GetKey(KeyCode.J))
            rotateDirection += Vector3.right;
        if (Input.GetKey(KeyCode.I))
            rotateDirection += Vector3.forward;
        if (Input.GetKey(KeyCode.K))
            rotateDirection -= Vector3.forward;
        if (Input.GetKey(KeyCode.U))
            rotateDirection -= Vector3.up;
        if (Input.GetKey(KeyCode.O))
            rotateDirection += Vector3.up;

        // 화살표 키에 대한 그리퍼 회전 입력 처리
        if (Input.GetKey(KeyCode.UpArrow))
            gripperChange += rotateSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow))
            gripperChange -= rotateSpeed * Time.deltaTime;

        // 현재 각도에 변화값을 더하고 이를 0 ~ 45도로 제한
        currentGripAngle = Mathf.Clamp(currentGripAngle + gripperChange, 0f, 45f);

        // 제한된 값을 사용하여 gripper_grip 벡터 설정
        Vector3 gripper_grip = new Vector3(0f, 0f, currentGripAngle);

        // Cube 움직이기
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);

        // Cube 회전시키기
        transform.Rotate(rotateDirection * rotateSpeed * Time.deltaTime, Space.World);

        // gripper 열고 닫기
        GR_1.transform.localRotation = Quaternion.Euler(90f, 0f, -gripper_grip.z);
        GR_2.transform.localRotation = Quaternion.Euler(0f, 0f, gripper_grip.z);
        GL_1.transform.localRotation = Quaternion.Euler(90f, 0f, gripper_grip.z);
        GL_2.transform.localRotation = Quaternion.Euler(0f, 0f, -gripper_grip.z);
    }
}
