using UnityEngine;

public class FollowRotation : MonoBehaviour
{
    // 회전값을 가져올 대상 오브젝트를 인스펙터에서 지정합니다.
    public GameObject targetObject;

    void Update()
    {
        // targetObject가 할당되었는지 확인하여 오류를 방지합니다.
        if (targetObject != null)
        {
            // 1. 대상의 현재 회전값을 오일러 각(Euler Angles)으로 가져옵니다.
            Vector3 targetRotation = targetObject.transform.eulerAngles;

            // 2. 이 스크립트가 적용된 오브젝트의 현재 회전값을 가져옵니다.
            Vector3 currentRotation = transform.eulerAngles;

            // 3. 새로운 회전값을 만듭니다.
            // X축은 자신의 값을 유지하고, Y축과 Z축은 대상의 값을 사용합니다.
            Vector3 newRotation = new Vector3(
                currentRotation.x, 
                targetRotation.y, 
                targetRotation.z
            );

            // 4. 계산된 새로운 회전값을 적용합니다.
            transform.eulerAngles = newRotation;
        }
        else
        {
            // 대상이 지정되지 않았을 경우를 대비한 경고 메시지입니다.
            Debug.LogWarning("회전 대상을 지정해주세요 (Target Object).");
        }
    }
}