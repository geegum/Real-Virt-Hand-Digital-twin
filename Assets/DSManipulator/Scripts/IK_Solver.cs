using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IK_Solver : MonoBehaviour
{
    public GameObject ee_Target; // 엔드 이펙터 타겟
    public GameObject target;    // 목표 타겟 오브젝트
    public Text debugText;       // 디버그 텍스트 UI

    // DH Parameters
    private float[] d = { 0.1555f, 0f, 0.409f, 0.367f, 0f, 0.124f };
    private float[] a = { 0f, 0f, 0f, 0f, 0f, 0f };

    // 로봇 관절들
    private GameObject[] joint = new GameObject[6];
    private float[] angle = new float[6];
    private float[] angle_ = new float[6];
    private Vector3[] point = new Vector3[7];           // 관절 끝의 월드 포지션
    private Vector3[] axis = new Vector3[6];            // 각 축의 로컬 방향
    private Quaternion[] rotation = new Quaternion[6];  // 부모에 상대적인 각 관절의 로컬 회전(쿼터니언)
    private Quaternion[] wRotation = new Quaternion[6]; // 각 관절의 월드 회전(쿼터니언)
    private float[] minAngle = new float[6];            // 관절 회전의 제한
    private float[] maxAngle = new float[6];
    private float[] minAngleVelocity = new float[6];    // 관절 회전속도의 제한
    private float[] maxAngleVelocity = new float[6];

    private Vector3[] vangle = new Vector3[6]; // Unity상에서 회전방향

    private Vector3 pos;      // 참조(목표) 위치
    private Vector3 rot;      // 참조(목표) 자세

    private float tolerance = 0.01f; // 목표에 도달했다고 간주할 거리
    private int maxIterations = 10;  // 최대 반복 횟수
    private float[] previousAngles = new float[6]; // 이전 프레임의 관절 각도
    private bool singularityDetected = false; // 특이점 여부
    private List<Vector3> path; // 경로
    private int currentPathIndex; // 현재 경로 인덱스
    private float interpolationStep = 0.15f; // 보간 단계 크기
    private Vector3 lastTargetPosition; // 마지막 타겟 위치

    // Workspace boundaries (in meters)
    private float workspaceRadius = 1.8f / 2f; // 1552mm 지름을 반지름으로 변환

    void Start()
    {
        // 로봇 관절 초기화
        for (int i = 0; i < joint.Length; i++)
        {
            joint[i] = GameObject.Find("Axis_" + i.ToString());
        }

        // 초기 각도 설정
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

        // 각도 제한 설정
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

        // 각속도 제한 설정 (degree/s 단위)
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

        // Unity상에서의 회전방향
        vangle[0] = new Vector3(0f, -1f, 0f);
        vangle[1] = new Vector3(0f, 0f, -1f);
        vangle[2] = new Vector3(0f, 0f, -1f);
        vangle[3] = new Vector3(0f, -1f, 0f);
        vangle[4] = new Vector3(0f, 0f, -1f);
        vangle[5] = new Vector3(0f, 1f, 0f);

        for (int i = 0; i < joint.Length; i++)
        {
            joint[i].transform.localRotation = Quaternion.Euler(vangle[i] * angle[i]);
            previousAngles[i] = angle[i]; // 초기 각도를 이전 각도에 저장
        }

        debugText.text = ""; // 초기화
        path = new List<Vector3>();
        currentPathIndex = 0;
        lastTargetPosition = target.transform.position;
        GeneratePath();
    }

    void Update()
    {
        if (!singularityDetected)
        {
            // 타겟이 워크스페이스를 벗어났는지 확인
            if (!IsWithinWorkspace(target.transform.position))
            {
                singularityDetected = true;
                DisplayDebugInfo("Target out of workspace. Stopping movement.");
                return;
            }

            // 타겟이 움직였는지 확인하고 경로를 재생성
            if (Vector3.Distance(lastTargetPosition, target.transform.position) > tolerance)
            {
                GeneratePath();
                currentPathIndex = 0;
                lastTargetPosition = target.transform.position;
            }

            FollowPath();
        }
        else
        {
            CheckIfSingularityResolved();
        }
    }

    void GeneratePath()
    {
        path.Clear(); // 기존 경로 초기화
        Vector3 startPosition = ee_Target.transform.position;
        Vector3 endPosition = target.transform.position;
        float distance = Vector3.Distance(startPosition, endPosition);
        int steps = Mathf.CeilToInt(distance / interpolationStep);

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 intermediatePosition = Vector3.Lerp(startPosition, endPosition, t);
            path.Add(intermediatePosition);
        }
    }

    void FollowPath()
    {
        if (currentPathIndex >= path.Count)
        {
            DisplayDebugInfo("Path completed.");
            return;
        }

        Vector3 targetPosition = path[currentPathIndex];

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            bool reachedTarget = true;

            for (int i = joint.Length - 1; i >= 0; i--)
            {
                Vector3 jointPosition = joint[i].transform.position;
                Vector3 toEndEffector = ee_Target.transform.position - jointPosition;
                Vector3 toTarget = targetPosition - jointPosition;

                float angleChange = Vector3.SignedAngle(toEndEffector, toTarget, vangle[i]);

                // 각속도 제한 적용 (degree/s 단위)
                float maxAngleChange = maxAngleVelocity[i] * Time.deltaTime;
                float minAngleChange = minAngleVelocity[i] * Time.deltaTime;
                angleChange = Mathf.Clamp(angleChange, minAngleChange, maxAngleChange);

                float newAngle = previousAngles[i] + angleChange;
                newAngle = Mathf.Clamp(newAngle, minAngle[i], maxAngle[i]);

                // 회전 값의 연속성을 유지하기 위해 Quaternion 사용
                Quaternion targetRotation = Quaternion.AngleAxis(newAngle, vangle[i]);

                // 곡률 조절을 위해 보간 비율을 줄임
                float slerpT = Mathf.Clamp01(Time.deltaTime * 2.0f); // 곡률을 줄이기 위해 2.0f로 곱하여 보간 비율을 조절
                joint[i].transform.localRotation = Quaternion.Slerp(joint[i].transform.localRotation, targetRotation, slerpT);

                previousAngles[i] = newAngle;

                if (Vector3.Distance(ee_Target.transform.position, targetPosition) >= tolerance)
                {
                    reachedTarget = false;
                }

                if (IsSingularity(i))
                {
                    ResolveSingularity(i, targetPosition); // 특이점 해결을 시도
                }
            }

            if (reachedTarget)
            {
                currentPathIndex++;
                if (currentPathIndex >= path.Count)
                {
                    DisplayDebugInfo("Target reached.");
                }
                return; // 목표에 도달하면 종료
            }
        }
        DisplayDebugInfo("IK solving...");
    }

    void ResolveSingularity(int jointIndex, Vector3 targetPosition)
    {
        // 가능한 최소 각도 오차를 찾기 위해 jointIndex의 각도를 서서히 조정
        float currentAngle = previousAngles[jointIndex];
        float optimalAngle = currentAngle;
        float minError = float.MaxValue;

        // 현재 각도를 기준으로 작은 범위 내에서 각도를 변경
        float searchRange = 1f; // 각도 조정 범위 (degrees)
        float stepSize = 0.1f;  // 각도 조정 단계 (degrees)

        for (float angle = currentAngle - searchRange; angle <= currentAngle + searchRange; angle += stepSize)
        {
            Quaternion testRotation = Quaternion.AngleAxis(angle, vangle[jointIndex]);
            joint[jointIndex].transform.localRotation = testRotation;

            float positionError = Vector3.Distance(ee_Target.transform.position, targetPosition);

            if (positionError <= 0.05f)
            {
                float currentError = CalculateTotalError();

                if (currentError < minError)
                {
                    minError = currentError;
                    optimalAngle = angle;
                }
            }
        }

        Quaternion optimalRotation = Quaternion.AngleAxis(optimalAngle, vangle[jointIndex]);

        // 각도를 서서히 변경
        float step = maxAngleVelocity[jointIndex] * Time.deltaTime;
        joint[jointIndex].transform.localRotation = Quaternion.RotateTowards(joint[jointIndex].transform.localRotation, optimalRotation, step);

        previousAngles[jointIndex] = Quaternion.Angle(joint[jointIndex].transform.localRotation, optimalRotation);
    }


    float CalculateTotalError()
    {
        float totalError = 0f;

        for (int i = 0; i < joint.Length; i++)
        {
            totalError += Mathf.Abs(joint[i].transform.localEulerAngles.z - previousAngles[i]);
        }

        return totalError;
    }

    bool IsSingularity(int jointIndex)
    {
        // 특이점을 판별하는 조건을 완화하여 구현
        return (previousAngles[jointIndex] <= minAngle[jointIndex] + 1f || previousAngles[jointIndex] >= maxAngle[jointIndex] - 1f);
    }

    void CheckIfSingularityResolved()
    {
        if (!IsSingularity(0) && IsWithinWorkspace(target.transform.position))
        {
            singularityDetected = false;
            debugText.text = ""; // 초기화
        }
    }

    bool IsWithinWorkspace(Vector3 position)
    {
        // 워크스페이스 내에 있는지 확인
        return (position.magnitude <= workspaceRadius);
    }

    void DisplayDebugInfo(string message)
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
