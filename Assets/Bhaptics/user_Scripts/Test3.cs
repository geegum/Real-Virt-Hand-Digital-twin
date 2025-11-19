using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bhaptics.SDK2;

public class Test3 : MonoBehaviour
{
    private float timeSinceLastDynamicCall = 0f; // Dynamic �� ���� �ð� ���
    private float dynamicCallInterval = 0.0105f; // Dynamic �� ȣ�� ����

    private bool resetDynamic = true;

    private float[] xValues;
    private float[] yValues;
    private int[] intensityValues;

    private Impedance_Control impedanceControl; // Impedance_Control Ŭ���� ����

    void Start()
    {
        // x�� y ��ǥ �迭 �ʱ�ȭ (0f���� 1f���� 0.2f ������ 6x6 �׸��� ����)
        int steps = 6;
        int totalPoints = steps * steps; // �� 36���� ��

        xValues = new float[totalPoints];
        yValues = new float[totalPoints];
        intensityValues = new int[totalPoints];

        for (int i = 0; i < steps; i++)
        {
            for (int j = 0; j < steps; j++)
            {
                int index = i * steps + j;
                xValues[index] = i * 0.2f;
                yValues[index] = j * 0.2f;
            }
        }

        impedanceControl = GameObject.FindObjectOfType<Impedance_Control>(); // Impedance_Control �ν��Ͻ� ��������
    }

    void Update()
    {
        // Dynamic �� ����
        timeSinceLastDynamicCall += Time.deltaTime;
        if (resetDynamic)
        {
            // imp_target�� ���� ���� ������
            Vector3 impTargetForce = impedanceControl.GetImpedanceForce(impedanceControl.imp_target);
            ApplyDynamicHapticForce(impTargetForce); // imp_target�� ���� ���� �����Ͽ� ��ƽ �ǵ�� ����

            resetDynamic = false;
        }

        if (timeSinceLastDynamicCall >= dynamicCallInterval && !resetDynamic)
        {
            timeSinceLastDynamicCall = 0f;
            resetDynamic = true;
        }
    }

    public void ApplyDynamicHapticForce(Vector3 force)
    {
        // imp_target�� ���� ��ǥ�迡�� ���� ������ �������� ȸ����Ŵ
        Quaternion rotation = impedanceControl.imp_target.transform.rotation;

        // ȸ�� ���� �����Ͽ� ȸ�� ����
        Quaternion inverseRotation = Quaternion.Inverse(rotation);
        Vector3 rotatedForce = inverseRotation * force;

        // ��ƽ �尩�� x, y ��ǥ�� �°� ��ȯ
        Vector3 mappedForce = new Vector3(rotatedForce.x, rotatedForce.y, rotatedForce.z);

        bool shouldVibrate = false; // ������ �߻���ų�� ����

        // �߽����� (0f, 0f)�� ����
        Vector3 center = new Vector3(0f, 0.0f, 0f);

        for (int i = 0; i < xValues.Length; i++)
        {
            // ��ƽ ��� ���� ��ġ (ȸ�� �������� ����)
            Vector3 localPosition = new Vector3(yValues[i] - 0.5f, 0f, xValues[i] - 0.5f);

            // ���� ���� ���
            Vector3 direction = localPosition - center;
            direction.Normalize();

            // dotProduct�� ���, ������ ��� 0���� ó��
            float dotProduct = Vector3.Dot(direction, mappedForce);
            if (dotProduct < 0)
            {
                dotProduct = 0f;
            }

            // ���� ����
            float mappedIntensity = LinearMap(dotProduct, 0f, 1f, 0f, 1f);
            int intensity = Mathf.Clamp((int)(mappedIntensity), 0, 30);

            if (intensity >= 20f)
            {
                intensityValues[i] = Mathf.RoundToInt(LinearMap(intensity, 20f, 30f, 25f, 30f));
                shouldVibrate = true;
            }
            else if (intensity >= 5f)
            {
                intensityValues[i] = Mathf.RoundToInt(LinearMap(intensity, 5f, 20f, 0f, 25f));
                shouldVibrate = true;
            }
            else
            {
                intensityValues[i] = 0;
            }

            // ����� ���
            /*
            Debug.Log($"Step: {i}, LocalPosition: {localPosition}, RotatedForce: {rotatedForce}, " +
                      $"DotProduct: {dotProduct}, " +
                      $"MappedIntensity: {mappedIntensity}, Intensity: {intensity}");
                      */


        }

        if (shouldVibrate)
        {
            int duration = 10;

            BhapticsLibrary.PlayPath(
                (int)PositionType.GloveR,
                xValues,
                yValues,
                intensityValues,
                duration
            );

            //Debug.Log($"Vibration triggered. Force: {force}, Max Intensity: {intensityValues[0]}");
        }
        else
        {
            //Debug.Log($"No vibration. Force: {force}, Max Intensity: {intensityValues[0]}");
        }
    }


    // ���� ���� �Լ�
    private float LinearMap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
    }
}
