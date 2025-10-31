using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceColorChanger : MonoBehaviour
{
    public Vector3 force; // �޾ƿ� force ��
    public float forceMagnitude;
    private Impedance_Control impedanceControl; // Impedance_Control Ŭ���� ����
    private Combine_Hand_R Combine_Hand_R;
    public GameObject color_target;

    private Renderer targetRenderer;

    void Start()
    {
        // imp_target�� Renderer ������Ʈ�� ������
        Combine_Hand_R = GameObject.FindObjectOfType<Combine_Hand_R>();
        impedanceControl = GameObject.FindObjectOfType<Impedance_Control>(); // Impedance_Control �ν��Ͻ� ��������

        targetRenderer = color_target.GetComponent<Renderer>();
        force = impedanceControl.GetImpedanceForce(impedanceControl.imp_target);
    }

    void Update()
    {
        // force�� ũ�⸦ ���
        force = impedanceControl.GetImpedanceForce(impedanceControl.imp_target);
        forceMagnitude = force.magnitude;

        // force�� x, y, z ���� �α׷� ���
        //Debug.Log($"Force - X: {force.x}, Y: {force.y}, Z: {force.z}");

        // ���ǿ� ���� ���� ����
        if (forceMagnitude > 30f)
        {
            targetRenderer.material.color = Color.red; // ������
        }
        else if (forceMagnitude > 20f)
        {
            targetRenderer.material.color = new Color(1.0f, 0.5f, 0.0f); // ��Ȳ��
        }
        else if (forceMagnitude > 0f)
        {
            targetRenderer.material.color = Color.yellow; // �����
        }
        else
        {
            if (Combine_Hand_R.Able)
            {
                targetRenderer.material.color = Color.green; // �ʷϻ�
            }
            else
            {
                targetRenderer.material.color = Color.white; // ���
            }
        }
    }
}
