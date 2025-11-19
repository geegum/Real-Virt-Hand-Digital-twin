using UnityEngine;

public class MidpointCalculator : MonoBehaviour
{
    public Transform transformA;
    public Transform transformB;
    public Transform resultTransform;
    public Transform rotationReferenceTransform; // 회전을 참조할 Transform

    void Update()
    {
        if (transformA != null && transformB != null && resultTransform != null && rotationReferenceTransform != null)
        {
            // 두 좌표의 중점 계산 (글로벌 좌표계)
            Vector3 midpoint = (transformA.position + transformB.position) / 2.0f;
            resultTransform.position = midpoint - new Vector3(0f,0.03f,0f);

            // rotationReferenceTransform의 회전을 resultTransform에 할당
            //resultTransform.rotation = rotationReferenceTransform.rotation;
        }
    }
}
