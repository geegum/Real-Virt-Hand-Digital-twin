using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bhaptics.SDK2;

public class haptic_for_box : MonoBehaviour
{
    [Header("햅틱을 발생시킬 두 오브젝트")]
    public GameObject haptic_object1; // 이 스크립트가 붙어있는 오브젝트 (vr_box2)
    public GameObject haptic_object2; // 충돌 대상 오브젝트 (target)

    [Header("햅틱 설정")]
    [Tooltip("진동 신호를 보내는 시간 간격 (초)")]
    public float dynamicCallInterval = 0.05f;

    // 내부 변수
    private float timeSinceLastDynamicCall = 0f;
    public bool boxisColliding = false;

    // [수정] 햅틱 좌표만 필요하고 강도는 항상 최대로 설정할 것이므로 intensityValues는 ApplyDynamicHapticForce에서 생성
    private float[] xValues;
    private float[] yValues;
    
    void Start()
    {
        // 햅틱 그리드 좌표 초기화
        int steps = 6;
        int totalPoints = steps * steps;
        xValues = new float[totalPoints];
        yValues = new float[totalPoints];

        for (int i = 0; i < steps; i++)
        {
            for (int j = 0; j < steps; j++)
            {
                int index = i * steps + j;
                xValues[index] = i * 0.2f;
                yValues[index] = j * 0.2f;
            }
        }
    }

    void Update()
    {
        // isColliding이 false이면 실행하지 않음
        if (!boxisColliding) return;

        timeSinceLastDynamicCall += Time.deltaTime;

        if (timeSinceLastDynamicCall >= dynamicCallInterval)
        {
            timeSinceLastDynamicCall = 0f;

            // [핵심 수정] 방향 계산 없이 바로 햅틱 함수를 호출합니다.
            ApplyMaxHapticForce();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == haptic_object2)
        {
            boxisColliding = true;
            Debug.Log("충돌 시작. 최대 강도 햅틱을 재생합니다.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == haptic_object2)
        {
            boxisColliding = false;
            Debug.Log("충돌 종료. 햅틱을 중지합니다.");
        }
    }

    // [핵심 수정] 방향 계산을 모두 제거하고 무조건 최대 강도로 진동을 주는 함수로 변경
    public void ApplyMaxHapticForce()
    {
        // 모든 모터의 강도를 50으로 설정
        int[] intensityValues = new int[xValues.Length];
        for (int i = 0; i < intensityValues.Length; i++)
        {
            intensityValues[i] = 50;
        }

        // 지속 시간을 짧게 하여 '타격감'을 줍니다.
        int duration = 50;

        BhapticsLibrary.PlayPath(
            (int)PositionType.GloveR,
            xValues,
            yValues,
            intensityValues,
            duration
        );
    }
}

