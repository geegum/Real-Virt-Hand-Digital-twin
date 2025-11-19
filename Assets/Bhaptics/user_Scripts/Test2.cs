using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bhaptics.SDK2;

public class Test2 : MonoBehaviour
{
    private float timeSinceLastDynamicCall = 0f; // Dynamic 힘 적용 시간 기록
    private float dynamicCallInterval = 0.011f; // Dynamic 힘 호출 간격

    private bool resetDynamic = true;

    private float dynamicForceValue = 0f;
    private float forceChangeSpeed = 0.1f; // force 크기 변화 속도

    private float[] xValues;
    private float[] yValues;
    private int[] intensityValues;

    void Start()
    {
        // x와 y 좌표 배열 초기화 (0f부터 1f까지 0.2f 단위로 6개)
        int steps = 6;
        xValues = new float[steps];
        yValues = new float[steps];
        intensityValues = new int[steps];

        for (int i = 0; i < steps; i++)
        {
            xValues[i] = i * 0.2f;
            yValues[i] = i * 0.2f;
        }
    }

    void Update()
    {
        // Dynamic 힘 적용
        timeSinceLastDynamicCall += Time.deltaTime;
        dynamicForceValue += forceChangeSpeed * Time.deltaTime;
        if (resetDynamic)
        {
            
            if (dynamicForceValue > 1f) dynamicForceValue = 0f; // force 크기가 1을 넘으면 0으로 초기화

            ApplyDynamicHapticForce(new Vector3(-dynamicForceValue, 0f, 0f));
            resetDynamic = false;
        }

        if (timeSinceLastDynamicCall >= dynamicCallInterval && !resetDynamic)
        {
            timeSinceLastDynamicCall = 0f;
            resetDynamic = true;
            Debug.Log("reset");
        }
    }

    public void ApplyDynamicHapticForce(Vector3 force)
    {
        // 강체의 응력 분산처럼 강도 계산
        for (int i = 0; i < xValues.Length; i++)
        {
            // 각 점에서 힘의 방향과 크기를 고려한 거리 및 반발력 계산
            float distance = Vector2.Distance(new Vector2(xValues[i], yValues[i]), new Vector2(0.5f, 0.5f)); // 중심을 (0.5, 0) 기준으로 거리 계산
            Vector2 direction = new Vector2(xValues[i], yValues[i]) - new Vector2(0.5f, 0.5f); // 중심에서 각 점으로의 방향 벡터
            direction.Normalize();
            float dotProduct = Vector2.Dot(direction, new Vector2(force.x, force.y)); // 방향 벡터와 힘의 내적

            // 반발력 계산: 힘의 크기와 방향에 따라 비례하는 강도
            float mappedIntensity = LinearMap(dotProduct, -1f, 1f, 0f, 50f); // dotProduct를 0에서 100 사이로 매핑
            float distanceFactor = LinearMap(distance, 0f, 1.118f, 1f, 0f); // distance를 1에서 0 사이로 매핑
            intensityValues[i] = Mathf.Clamp((int)(mappedIntensity * distanceFactor), 0, 50); // 거리와 dotProduct 기반 강도 계산
        }

        int duration = 10; // 100ms 동안 피드백 제공

        BhapticsLibrary.PlayPath(
            (int)PositionType.GloveR,
            xValues,
            yValues,
            intensityValues,
            duration
        );

        Debug.Log($"Force: {force}, Max Intensity: {intensityValues[0]}");
    }

    // 선형 매핑 함수
    private float LinearMap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
    }
}
