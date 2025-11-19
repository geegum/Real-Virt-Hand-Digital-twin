using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

public class Impedance_Control3 : MonoBehaviour
{
    public Combine_Hand_R combineHandR;

    public GameObject imp_target;
    public GameObject ee_Target; // End effector target
    public GameObject target;    // Target object
    public GameObject Axis_45;   // Object prone to collisions
    public GameObject Right_hand;
    public Text debugText;       // Debug text UI

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

    // Gaussian distribution parameters (not used in this version)
    private float sigma = 1.2f;
    private float mu = 0.1f;

    // Workspace parameters
    private Vector3 workspaceCenter = new Vector3(0, 0.1555f, 0);
    private float workspaceRadius = 0.95f;
    private float innerRadius = 0.35f;
    private Vector3 cubeCenter = new Vector3(0, -0.3f, 0);
    private float cubeSide = 0.315f;
    private float bufferZone = 0.22f;

    // For Debugging
    private float[] debug_angle = new float[6];

    // Impedance Control parameters
    public float Kp = 50.0f;
    public float Kd = 10.0f;
    public float mass = 1.0f;

    private Vector3 eeVelocity;
    private Vector3 targetVelocity;

    private Vector3 impTargetForce;
    private Vector3 axis45Force;

    void Start()
    {
        combineHandR = GameObject.FindObjectOfType<Combine_Hand_R>();

        for (int i = 0; i < joint.Length; i++)
        {
            joint[i] = GameObject.Find("Axis_" + i.ToString());
        }

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

        minAngleVelocity[0] = -3f;
        maxAngleVelocity[0] = 3f;
        minAngleVelocity[1] = -3f;
        maxAngleVelocity[1] = 3f;
        minAngleVelocity[2] = -3f;
        maxAngleVelocity[2] = 3f;
        minAngleVelocity[3] = -5f;
        maxAngleVelocity[3] = 5f;
        minAngleVelocity[4] = -5f;
        maxAngleVelocity[4] = 5f;
        minAngleVelocity[5] = -5f;
        maxAngleVelocity[5] = 5f;

        vangle[0] = new Vector3(0f, -1f, 0f);
        vangle[1] = new Vector3(0f, 0f, -1f);
        vangle[2] = new Vector3(0f, 0f, -1f);
        vangle[3] = new Vector3(0f, -1f, 0f);
        vangle[4] = new Vector3(0f, 0f, -1f);
        vangle[5] = new Vector3(0f, -1f, 0f);

        ee_target_vangle = new Vector3(0f, 1f, 0f);
        target_vangle = new Vector3(0f, 1f, 0f);

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
        if (!IsWithinWorkspace(imp_target.transform.position, bufferZone))
        {
            ApplyImpedanceControl(imp_target);
        }

        if (!IsWithinWorkspace(Axis_45.transform.position, 0.3f))
        {
            ApplyImpedanceControl(Axis_45);
        }

        if (combineHandR != null && combineHandR.Able && Vector3.Distance(imp_target.transform.position, Right_hand.transform.position) > 0.02f)
        {
            float lerpSpeed = 1.0f;
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

            if (IsSingularity(jacobian))
            {
                SingularityDataLogger logger = GameObject.FindObjectOfType<SingularityDataLogger>();
                if (logger != null)
                {
                    logger.LogData(angle, jacobian);
                }
            }

            Vector<double> jointVelocities = jacobian.PseudoInverse() * (positionError + rotationError);

            for (int i = 0; i < joint.Length; i++)
            {
                float angleChange = (float)jointVelocities[i];
                angleChange = Mathf.Clamp(angleChange, minAngleVelocity[i] * Time.deltaTime, maxAngleVelocity[i] * Time.deltaTime);

                float newAngle = previousAngles[i] + angleChange;
                newAngle = Mathf.Clamp(newAngle, minAngle[i].Value, maxAngle[i].Value);

                joint[i].transform.localRotation = Quaternion.Slerp(joint[i].transform.localRotation, Quaternion.AngleAxis(newAngle, vangle[i]), Mathf.Clamp01(Time.deltaTime * 2.5f));

                previousAngles[i] = newAngle;
                angle[i] = newAngle;

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
        DisplayDebugInfo("IK solving...");
    }

    bool IsSingularity(Matrix<double> jacobian)
    {
        //var svd = jacobian.Svd();
        //double minSingularValue = svd.S.Minimum();
        //double singularityThreshold = 0.02f;
        //return minSingularValue < singularityThreshold;
        return true;
    }

    void ApplyImpedanceControl(GameObject obj)
    {
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

    Vector3 GetClosestPointOnWorkspace(Vector3 position)
    {
        Vector3 closestPoint = position;

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

            if (Mathf.Abs(xDist) < Mathf.Abs(yDist) && Mathf.Abs(xDist) < Mathf.Abs(zDist))
            {
                closestPoint = cubeCenter + new Vector3(localPos.x > 0 ? cubeSide + bufferZone : -cubeSide - bufferZone, localPos.y, localPos.z);
            }
            else if (Mathf.Abs(yDist) < Mathf.Abs(xDist) && Mathf.Abs(yDist) < Mathf.Abs(zDist))
            {
                closestPoint = cubeCenter + new Vector3(localPos.x, localPos.y > 0 ? cubeSide + bufferZone : -cubeSide - bufferZone, localPos.z);
            }
            else
            {
                closestPoint = cubeCenter + new Vector3(localPos.x, localPos.y, localPos.z > 0 ? cubeSide + bufferZone : -cubeSide - bufferZone);
            }

            closestPoint += new Vector3(localPos.x > 0 ? 0.01f : -0.01f, localPos.y > 0 ? 0.01f : -0.01f, localPos.z > 0 ? 0.01f : -0.01f);
        }

        if (position.y < -0.4f)
        {
            closestPoint.y = -0.4f;
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

    Matrix<double> CalculateJacobian()
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

    bool IsWithinWorkspace(Vector3 position, float buffer)
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

        if (position.y < -0.4f)
        {
            return false;
        }

        return true;
    }

    float WrapAngle(float angle)
    {
        angle = angle % 360;
        if (angle > 180)
            angle -= 360;
        else if (angle < -180)
            angle += 360;
        return angle;
    }

    void DisplayDebugInfo(string message)
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
        return angle; // 각 관절의 현재 각도를 반환합니다.
    }
}
