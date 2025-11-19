using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


//path planning삭제, Jacobian과 DLS(DampedLeastSquares)을 이용한 특이점 회피 및 최적위치추적 기법 구현
//관절마다 workspace구현하여 각 축의 각도제한에 걸리지않는 위치는 못가게까지 구현필요
//rotation문제 해결필요
public class IK_Solver_v3 : MonoBehaviour
{
    public GameObject ee_Target; // 엔드 이펙터 타겟
    public GameObject target;    // 목표 타겟 오브젝트
    public Text debugText;       // 디버그 텍스트 UI

    private GameObject[] joint = new GameObject[6];
    private float[] angle = new float[6];
    private float[] angle_ = new float[6];
    private Vector3[] point = new Vector3[7];
    private Vector3[] axis = new Vector3[6];
    private Quaternion[] rotation = new Quaternion[6];
    private Quaternion[] wRotation = new Quaternion[6];
    private float?[] minAngle = new float?[6];
    private float?[] maxAngle = new float?[6];
    private float[] minAngleVelocity = new float[6];
    private float[] maxAngleVelocity = new float[6];
    private Vector3[] vangle = new Vector3[6];
    private Vector3 pos;
    private Vector3 rot;
    private float tolerance = 0.01f;
    private int maxIterations = 10;  // 최대 반복 횟수를 늘림
    private float[] previousAngles = new float[6];

    void Start()
    {
        for (int i = 0; i < joint.Length; i++)
        {
            joint[i] = GameObject.Find("Axis_" + i.ToString());
        }

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

        minAngle[0] = -360f;
        maxAngle[0] = 360f;
        minAngle[1] = -95f;
        maxAngle[1] = 95f;
        minAngle[2] = -135f;
        maxAngle[2] = 135f;
        minAngle[3] = -360f;
        maxAngle[3] = 360f;
        minAngle[4] = -135f;
        maxAngle[4] = 135f;
        minAngle[5] = -360f;
        maxAngle[5] = 360f;

        minAngleVelocity[0] = -5f;
        maxAngleVelocity[0] = 5f;
        minAngleVelocity[1] = -5f;
        maxAngleVelocity[1] = 5f;
        minAngleVelocity[2] = -5f;
        maxAngleVelocity[2] = 5f;
        minAngleVelocity[3] = -10f;
        maxAngleVelocity[3] = 10f;
        minAngleVelocity[4] = -10f;
        maxAngleVelocity[4] = 10f;
        minAngleVelocity[5] = -10f;
        maxAngleVelocity[5] = 10f;

        vangle[0] = new Vector3(0f, -1f, 0f);
        vangle[1] = new Vector3(0f, 0f, -1f);
        vangle[2] = new Vector3(0f, 0f, -1f);
        vangle[3] = new Vector3(0f, -1f, 0f);
        vangle[4] = new Vector3(0f, 0f, -1f);
        vangle[5] = new Vector3(0f, -1f, 0f);

        for (int i = 0; i < joint.Length; i++)
        {
            joint[i].transform.localRotation = Quaternion.Euler(vangle[i] * angle[i]);
            previousAngles[i] = angle[i];
        }

        debugText.text = "";
    }

    void Update()
    {
        SolveIK();
    }

    void SolveIK()
    {
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            bool reachedTarget = true;

            Matrix<double> jacobian = CalculateJacobian();
            Vector<double> positionError = CalculatePositionError();

            Vector<double> jointVelocities = DampedLeastSquares(jacobian, positionError, 0f); // 감쇠율을 높임

            for (int i = joint.Length - 1; i >= 0; i--)
            {
                float angleChange = (float)jointVelocities[i];

                float maxAngleChange = maxAngleVelocity[i] * Time.deltaTime;
                float minAngleChange = minAngleVelocity[i] * Time.deltaTime;
                angleChange = Mathf.Clamp(angleChange, minAngleChange, maxAngleChange);

                float newAngle = WrapAngle(previousAngles[i] + angleChange);
                if (minAngle[i].HasValue && maxAngle[i].HasValue)
                {
                    newAngle = Mathf.Clamp(newAngle, minAngle[i].Value, maxAngle[i].Value);
                }

                if (Mathf.Abs(newAngle - previousAngles[i]) > 180)
                {
                    newAngle -= Mathf.Sign(newAngle - previousAngles[i]) * 360;
                }

                Quaternion targetLocalRotation = Quaternion.AngleAxis(newAngle, vangle[i]);

                float slerpT = Mathf.Clamp01(Time.deltaTime * 2.5f);
                joint[i].transform.localRotation = Quaternion.Slerp(joint[i].transform.localRotation, targetLocalRotation, slerpT);

                previousAngles[i] = newAngle;

                if (Vector3.Distance(ee_Target.transform.position, target.transform.position) >= tolerance)
                {
                    reachedTarget = false;
                }
            }

            if (reachedTarget)
            {
                DisplayDebugInfo("Target reached.");
                return;
            }
        }
        DisplayDebugInfo("IK solving...");
    }

    Matrix<double> CalculateJacobian()
    {
        Matrix<double> jacobian = DenseMatrix.OfArray(new double[6, 6]);

        for (int i = 0; i <= joint.Length-1; i++)
        {
            Vector3 jointPosition = joint[i].transform.position;
            Vector3 toEndEffector = ee_Target.transform.position - jointPosition;
            Vector3 axis = joint[i].transform.TransformDirection(vangle[i]);

            Vector3 cross = Vector3.Cross(axis, toEndEffector);

            jacobian[0, i] = cross.x;
            jacobian[1, i] = cross.y;
            jacobian[2, i] = cross.z;
            jacobian[3, i] = axis.x;
            jacobian[4, i] = axis.y;
            jacobian[5, i] = axis.z;
        }

        return jacobian;
    }

    Vector<double> CalculatePositionError()
    {
        Vector3 positionError = target.transform.position - ee_Target.transform.position;
        return DenseVector.OfArray(new double[] { positionError.x, positionError.y, positionError.z, 0, 0, 0 });
    }

    Vector<double> DampedLeastSquares(Matrix<double> jacobian, Vector<double> positionError, double dampingFactor)//최소자승법을 이용한 댐핑
    {
        Matrix<double> jacobianTranspose = jacobian.Transpose();
        Matrix<double> identity = DenseMatrix.CreateIdentity(6);
        Matrix<double> dampingMatrix = dampingFactor * dampingFactor * identity;

        Matrix<double> inverseTerm = (jacobian * jacobianTranspose + dampingMatrix).Inverse();
        return jacobianTranspose * inverseTerm * positionError;
    }

    float WrapAngle(float angle)//음수 각도에서 양수각도로 이동 할 수 있게 래핑해줌
    {
        angle = angle % 360;
        if (angle > 180)
            angle -= 360;
        else if (angle < -180)
            angle += 360;
        return angle;
    }

    void DisplayDebugInfo(string message)//Debug Text UI에 뛰워주는 함수 => Joint단으로 통신 쏴주기
    {
        string debugInfo = $"{message}\nJoint Angles:\n";
        for (int i = 0; i < joint.Length; i++)
        {
            Vector3 eulerAngles = joint[i].transform.localEulerAngles;
            debugInfo += $"Joint {i + 1} (Unity): {eulerAngles}\n";
        }
        debugText.text = debugInfo;
    }
}
