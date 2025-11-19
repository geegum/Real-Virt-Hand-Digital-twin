using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

public class Impedance_Control : MonoBehaviour
{
    private float tablepositiony = -0.4f;

    public Combine_Hand_R combineHandR;

    public GameObject imp_target;
    public GameObject ee_Target; // End effector target
    public GameObject target;    // Target object
    public GameObject Axis_45;   // Object prone to collisions
    public GameObject Right_hand;
    public Text debugText;       // Debug text UI


    public GameObject workspaceCenterGO;       // Debug text UI
    

    private GameObject[] joint = new GameObject[6];
    private float[] angle = new float[6];
    private float[] angle_ = new float[6];
    private Vector3[] point = new Vector3[7];
    private Vector3[] axis = new Vector3[6];
    private Quaternion[] rotation = new Quaternion[6];
    private Quaternion[] wRotation = new Quaternion[6];

    // Angle limits (degrees)
    private float?[] minAngle = new float?[6];
    private float?[] maxAngle = new float?[6];

    // Angle velocity limits (degrees/s)
    private float[] minAngleVelocity = new float[6];
    private float[] maxAngleVelocity = new float[6];

    private Vector3[] vangle = new Vector3[6]; // Axis of each joint

    private Vector3 pos;
    private Vector3 rot;

    private float d_tolerance = 0.01f; // Distance tolerance to satisfy target reached condition
    private float r_tolerance = 0.5f; // Rotation tolerance to satisfy target reached condition
    private int maxIterations = 10;  // Maximum number of iterations

    private float[] previousAngles = new float[6]; // To store previous angles for error calculation

    // To follow target rotation
    private Vector3 ee_target_vangle = new Vector3(); // Axis of ee_target
    private Vector3 target_vangle = new Vector3(); // Axis of target

    // Gaussian distribution parameters
    private float sigma = 0.5f; // Adjustable parameter, standard deviation of Gaussian distribution
    private float mu = 0f; // Mean (center)

    // Workspace parameters
    private Vector3 workspaceCenter; // Center of inner workspace
    private float workspaceRadius = 0.95f; // Radius of workspace (1800mm diameter / 2 + 100mm (gripper) / 2 but proper value)
    private float innerRadius = 0.35f; // Radius of inner workspace (280mm diameter / 2 + 100mm (gripper) but proper value)
    private Vector3 cubeCenter = new Vector3(0, -0.3f, 0); // Mobile cube center
    private float cubeSide = 0.315f; // Cube side length (630mm / 2)
    private float bufferZone = 0.22f; // Additional buffer space

    // For Debugging
    private float[] debug_angle = new float[6];

    // Impedance Control parameters
    public float Kp = 50.0f; // Spring constant
    public float Kd = 10.0f;  // Damper constant
    public float mass = 1.0f; // Mass

    private Vector3 eeVelocity;
    private Vector3 targetVelocity;

    public Vector3 impTargetForce; // imp_target�� ���� ��
    private Vector3 axis45Force; // Axis_45�� ���� ��


    void Start()
    {
        combineHandR = GameObject.FindObjectOfType<Combine_Hand_R>();

        for (int i = 0; i < joint.Length; i++)
        {
            joint[i] = GameObject.Find("Axis_" + i.ToString());
        }

        // Initial angles
        angle[0] = 0f;
        angle[1] = 0f;
        angle[2] = 90f;
        angle[3] = 0f;
        angle[4] = 0f;
        angle[5] = 0f;

        for (int i = 0; i < angle_.Length; i++)
        {
            angle_[i] = 0f;
        }

        // Angle limits for each joint
        minAngle[0] = -340f;
        maxAngle[0] = 340f;
        minAngle[1] = -80f;
        maxAngle[1] = 80f;
        minAngle[2] = -110f;
        maxAngle[2] = 110f;
        minAngle[3] = -340f;
        maxAngle[3] = 340f;
        minAngle[4] = -120f;
        maxAngle[4] = 120f;
        minAngle[5] = -340f;
        maxAngle[5] = 340f;

        // Angle velocity limits for each joint 
        minAngleVelocity[0] = -3.1f;
        maxAngleVelocity[0] =  3.1f;
        minAngleVelocity[1] = -3.1f;
        maxAngleVelocity[1] =  3.1f;
        minAngleVelocity[2] = -3.1f;
        maxAngleVelocity[2] =  3.1f;
        minAngleVelocity[3] = -6.1f;
        maxAngleVelocity[3] =  6.1f;
        minAngleVelocity[4] = -6.1f;
        maxAngleVelocity[4] =  6.1f;
        minAngleVelocity[5] = -6.1f;
        maxAngleVelocity[5] =  6.1f;
        // minAngleVelocity[0] = -350f;
        // minAngleVelocity[1] = -350f;
        // minAngleVelocity[2] = -350f;
        // minAngleVelocity[3] = -350f;
        // minAngleVelocity[4] = -350f;
        // minAngleVelocity[5] = -350f;
        // maxAngleVelocity[0] = 350f;
        // maxAngleVelocity[1] = 350f;
        // maxAngleVelocity[2] = 350f;
        // maxAngleVelocity[3] = 350f;
        // maxAngleVelocity[4] = 350f;
        // maxAngleVelocity[5] = 350f;
        // Joint axis directions
        vangle[0] = new Vector3(0f, -1f, 0f);
        vangle[1] = new Vector3(0f, 0f, -1f);
        vangle[2] = new Vector3(0f, 0f, -1f);
        vangle[3] = new Vector3(0f, -1f, 0f);
        vangle[4] = new Vector3(0f, 0f, -1f);
        vangle[5] = new Vector3(0f, -1f, 0f);

        ee_target_vangle = new Vector3(0f, 1f, 0f);
        target_vangle = new Vector3(0f, 1f, 0f);

        // Set initial joint rotations and store previous angles
        for (int i = 0; i < joint.Length; i++)
        {
            joint[i].transform.localRotation = Quaternion.Euler(vangle[i] * angle[i]);
            previousAngles[i] = angle[i];
        }

        eeVelocity = Vector3.zero;
        targetVelocity = Vector3.zero;

        debugText.text = "";
    }

    void Update()
    {
        // Check if the end effector or target is within the workspace, apply impedance control if not

        if (!IsWithinWorkspace(imp_target.transform.position, bufferZone))
        {
            ApplyImpedanceControl(imp_target);
        }

        if (!IsWithinWorkspace(Axis_45.transform.position, 0.3f))
        {
            ApplyImpedanceControl(Axis_45); // joint 6
        }

        // Check if Combine_Hand_R.Able is true and distance between target and Right_hand is greater than 0.02
        if (combineHandR != null && combineHandR.Able && Vector3.Distance(imp_target.transform.position, Right_hand.transform.position) > 0.02f)
        {
            // Gradually move target towards Right_hand's position
            float lerpSpeed = 1.0f; // Adjust this value to control the speed of movement
            imp_target.transform.position = Vector3.Lerp(imp_target.transform.position, Right_hand.transform.position, lerpSpeed * Time.deltaTime);
        }

        SolveIK();
    }

    void SolveIK()
    {
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            bool reachedTarget = true;

            Matrix<double> jacobian = CalculateJacobian();
            Vector<double> positionError = CalculatePositionError();
            Vector<double> rotationError = CalculateRotationError();

            // Ư���� ���� ����
            if (IsSingularity(jacobian))
            {
                // SingularityDataLogger�� ã�Ƽ� �����͸� ���
                SingularityDataLogger logger = GameObject.FindObjectOfType<SingularityDataLogger>();
                if (logger != null)
                {
                    logger.LogData(angle, jacobian);
                }
            }

            // GDLS�� ����Ͽ� ���� �ӵ� ���
            Vector<double> jointVelocities = GDLSWithSVD(jacobian, positionError, rotationError);
            
            // ���� ���� ������Ʈ
            for (int i = 0; i < joint.Length; i++)
            {
                float angleChange = (float)jointVelocities[i];
                angleChange = Mathf.Clamp(angleChange, minAngleVelocity[i] * Time.deltaTime, maxAngleVelocity[i] * Time.deltaTime);

                float newAngle = previousAngles[i] + angleChange;
                newAngle = Mathf.Clamp(newAngle, minAngle[i].Value, maxAngle[i].Value);

                //joint speed

                // 실제 각도 변화량 계산
                float actualAngleChange = newAngle - previousAngles[i];

                if (Socket_Jointspeed.Instance != null)
                {
                    if (Time.deltaTime > 0)
                    {
                        float angularVelocity = (newAngle - previousAngles[i]) / Time.deltaTime;

                        Socket_Jointspeed.Instance.targetJointPosition[i] = newAngle;
                        Socket_Jointspeed.Instance.targetJointVelocities[i] = angleChange;

                    }
                }

                joint[i].transform.localRotation = Quaternion.Slerp(joint[i].transform.localRotation, Quaternion.AngleAxis(newAngle, vangle[i]), Mathf.Clamp01(Time.deltaTime * 2.5f));

                previousAngles[i] = newAngle;
                angle[i] = newAngle; // ���⿡�� angle �迭�� ������Ʈ

                // ���⿡�� Socket_Joint�� newAngles �迭�� ������Ʈ�մϴ�.
                if (Socket_Joint.Instance != null)
                {
                    Socket_Joint.Instance.newAngles[i] = newAngle;
                }



                if (Vector3.Distance(ee_Target.transform.position, target.transform.position) >= d_tolerance || Quaternion.Angle(ee_Target.transform.rotation, target.transform.rotation) >= r_tolerance)
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
        
        //if (Socket_Jointspeed.Instance != null)
        //{
        //    // String.Format을 사용하여 소수점 4자리까지 깔끔하게 출력합니다.
        //    Debug.Log(string.Format("Joint Velocities: [{0:F4}, {1:F4}, {2:F4}, {3:F4}, {4:F4}, {5:F4}]",
        //        Socket_Jointspeed.Instance.targetJointVelocities[0],
        //        Socket_Jointspeed.Instance.targetJointVelocities[1],
        //        Socket_Jointspeed.Instance.targetJointVelocities[2],
        //        Socket_Jointspeed.Instance.targetJointVelocities[3],
        //        Socket_Jointspeed.Instance.targetJointVelocities[4],
        //        Socket_Jointspeed.Instance.targetJointVelocities[5]
        //    ));
        //}
        DisplayDebugInfo("IK solving...");
    }


    bool IsSingularity(Matrix<double> jacobian)
    {
        //var svd = jacobian.Svd();

        // �� (�ñ׸�) ����� �ּ� Ư�̰��� ������
        //double minSingularValue = svd.S.Minimum();

        // �Ӱ谪�� ����, 0.02f���� ����
        //double singularityThreshold = 0.02f;

        // �ּ� Ư�̰��� �Ӱ谪 �����̸� Ư�������� ����
        //return minSingularValue < singularityThreshold;
        return true;
    }


    Vector<double> GDLSWithSVD(Matrix<double> jacobian, Vector<double> positionError, Vector<double> rotationError)
    {
        var svd = jacobian.Svd();

        // SVD�κ��� U, S, V ��� ����
        Matrix<double> U = svd.U;
        Vector<double> S = svd.S;
        Matrix<double> V = svd.VT.Transpose();

        // ���� singular value�� ���� ���� ����
        double dampingFactor = CalculateGaussianDampingFactor(positionError);

        // S�� �� ��ҿ� ���� ���� ���ڸ� �����Ͽ� ���ο� S�� ���
        for (int i = 0; i < S.Count; i++)
        {
            S[i] = S[i] / (S[i] * S[i] + dampingFactor * dampingFactor);
        }

        // S�� �밢 ��ķ� ��ȯ, ��� ���� ũ�⸦ ����
        Matrix<double> dampedS = DiagonalMatrix.OfDiagonal(S.Count, S.Count, S);

        // Damped Jacobian�� �̿��Ͽ� �ӵ� ���� ���
        Matrix<double> dampedJacobian = V * dampedS * U.Transpose();
        Vector<double> dampedVelocity = dampedJacobian * (positionError + rotationError);

        return dampedVelocity;
    }



    double CalculateGaussianDampingFactor(Vector<double> positionError)
    {
        double errorMagnitude = positionError.SubVector(0, 3).L2Norm(); // ��ġ ������ ũ�� ���
        double sigma = this.sigma; // Gaussian ������ ǥ�� ����
        double mu = this.mu;       // Gaussian ������ ��� (�߽�)

        // Gaussian ������ ����Ͽ� ���� ���ڸ� ���
        double dampingFactor = Mathf.Exp(-(float)(errorMagnitude - mu) * (float)(errorMagnitude - mu) / (2.0f * (float)sigma * (float)sigma));
        return dampingFactor;
    }

    void ApplyImpedanceControl(GameObject obj)
    {
        if (obj.CompareTag("golden_baby"))
        {
            // Apply different impedance control parameters for "golden_baby"
            Vector3 closestPoint = GetClosestPointOnWorkspace(obj.transform.position);
            Vector3 positionError = closestPoint - obj.transform.position;
            Vector3 velocityError = -GetObjectVelocity(obj);

            Vector3 force = 5 * Kp * positionError + 2 * Kd * velocityError;
            Vector3 acceleration = force / mass;
            Vector3 velocity = GetObjectVelocity(obj) + acceleration * Time.deltaTime;
            imp_target.transform.position += velocity * Time.deltaTime;

            SetObjectVelocity(obj, velocity);

            axis45Force = force;

            DisplayDebugInfo("Applying impedance control...");
        }
        else
        {
            // Apply standard impedance control
            Vector3 closestPoint = GetClosestPointOnWorkspace(obj.transform.position);
            Vector3 positionError = closestPoint - obj.transform.position;
            Vector3 velocityError = -GetObjectVelocity(obj);

            Vector3 force = 3 * Kp * positionError + Kd * velocityError;
            Vector3 acceleration = force / mass;
            Vector3 velocity = GetObjectVelocity(obj) + acceleration * Time.deltaTime;
            obj.transform.position += velocity * Time.deltaTime;

            SetObjectVelocity(obj, velocity);

            impTargetForce = force;

            DisplayDebugInfo("Applying impedance control...");
        }
    }

    Vector3 GetClosestPointOnWorkspace(Vector3 position) //To decide force vector by using Impedence Control
    {
        Vector3 closestPoint = position;
        workspaceCenter = workspaceCenterGO.transform.position;

        float distance = Vector3.Distance(workspaceCenter, position);
        if (distance > workspaceRadius)
        {
            closestPoint = workspaceCenter + (position - workspaceCenter).normalized * workspaceRadius;
        }
        else if (distance < innerRadius)
        {
            closestPoint = workspaceCenter + (position - workspaceCenter).normalized * innerRadius;
        }

        Vector3 localPos = position - cubeCenter;
        bool insideCube = Mathf.Abs(localPos.x) < cubeSide + bufferZone && Mathf.Abs(localPos.y) < cubeSide + bufferZone && Mathf.Abs(localPos.z) < cubeSide + bufferZone;

        if (insideCube)
        {
            float xDist = cubeSide + bufferZone - Mathf.Abs(localPos.x);
            float yDist = cubeSide + bufferZone - Mathf.Abs(localPos.y);
            float zDist = cubeSide + bufferZone - Mathf.Abs(localPos.z);

            float absXDist = Mathf.Abs(xDist);
            float absYDist = Mathf.Abs(yDist);
            float absZDist = Mathf.Abs(zDist);

            if (absXDist < absYDist && absXDist < absZDist)
            {
                closestPoint = cubeCenter + new Vector3(
                    localPos.x > 0 ? cubeSide + bufferZone : -cubeSide - bufferZone,
                    localPos.y,
                    localPos.z
                );
            }
            else if (absYDist < absXDist && absYDist < absZDist)
            {
                closestPoint = cubeCenter + new Vector3(
                    localPos.x,
                    localPos.y > 0 ? cubeSide + bufferZone : -cubeSide - bufferZone,
                    localPos.z
                );
            }
            else
            {
                closestPoint = cubeCenter + new Vector3(
                    localPos.x,
                    localPos.y,
                    localPos.z > 0 ? cubeSide + bufferZone : -cubeSide - bufferZone
                );
            }

            // Slightly push outwards to avoid collision
            closestPoint += new Vector3(
                localPos.x > 0 ? 0.01f : -0.01f,
                localPos.y > 0 ? 0.01f : -0.01f,
                localPos.z > 0 ? 0.01f : -0.01f
            );
        }

        // Handle y-coordinate being less than -0.4
        if (position.y < tablepositiony)
        {
            closestPoint.y = tablepositiony;
        }

        return closestPoint;
    }

    Vector3 GetObjectVelocity(GameObject obj)
    {
        if (obj == ee_Target)
            return eeVelocity;
        else if (obj == target)
            return targetVelocity;
        else
            return Vector3.zero;
    }

    void SetObjectVelocity(GameObject obj, Vector3 velocity)
    {
        if (obj == ee_Target)
            eeVelocity = velocity;
        else if (obj == target)
            targetVelocity = velocity;
    }

    Matrix<double> CalculateJacobian() //To use DLS
    {
        Matrix<double> jacobian = DenseMatrix.OfArray(new double[6, 6]);

        for (int i = 0; i <= joint.Length - 1; i++)
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

    Vector<double> CalculateRotationError()
    {
        Quaternion targetRotation = target.transform.rotation;
        Quaternion currentRotation = ee_Target.transform.rotation;
        Quaternion rotationDiff = targetRotation * Quaternion.Inverse(currentRotation);
        Vector3 rotationError = rotationDiff.eulerAngles;

        rotationError.x = WrapAngle(rotationError.x);
        rotationError.y = WrapAngle(rotationError.y);
        rotationError.z = WrapAngle(rotationError.z);

        return DenseVector.OfArray(new double[] { 0, 0, 0, rotationError.x, rotationError.y, rotationError.z });
    }

    bool IsWithinWorkspace(Vector3 position, float buffer) //Check that object is in workspace
    {
        float distance = Vector3.Distance(workspaceCenter, position);
        if (distance > workspaceRadius - buffer || distance < innerRadius + buffer)
        {
            return false;
        }

        Vector3 localPos = position - cubeCenter;
        if (Mathf.Abs(localPos.x) < cubeSide + buffer && Mathf.Abs(localPos.y) < cubeSide + buffer && Mathf.Abs(localPos.z) < cubeSide + buffer)
        {
            return false;
        }

        // Handle y-coordinate being less than -0.4
        if (position.y < tablepositiony)
        {
            return false;
        }

        return true;
    }

    float WrapAngle(float angle) //Make sure angle is continuous at 180 or -180
    {
        angle = angle % 360;
        if (angle > 180)
            angle -= 360;
        else if (angle < -180)
            angle += 360;
        return angle;
    }

    void DisplayDebugInfo(string message) //Debug each joint angles using Text UI
    {
        string debugInfo = $"{message}\nJoint Angles:\n";
        for (int i = 0; i < joint.Length; i++)
        {
            debugInfo += $"Joint {i + 1} (Unity): {debug_angle[i]}\n";
        }
        debugText.text = debugInfo;
    }

    public Vector3 GetImpedanceForce(GameObject obj)
    {
        if (obj == imp_target)
        {
            return impTargetForce;
        }
        else if (obj == Axis_45)
        {
            return axis45Force;
        }
        return Vector3.zero;
    }

    public float[] GetJointAngles()
    {
        return angle; // �� ������ ���� ������ ��ȯ�մϴ�.
    }

}
