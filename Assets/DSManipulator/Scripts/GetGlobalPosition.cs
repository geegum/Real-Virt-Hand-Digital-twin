using UnityEngine;

public class GetGlobalPosition : MonoBehaviour
{
    // 글로벌 좌표값을 받아올 객체
    public GameObject targetObject;

    void Update()
    {
        if (targetObject != null)
        {
            // targetObject의 글로벌 좌표값을 받아오기
            Vector3 globalPosition = targetObject.transform.position;

            // 글로벌 좌표값 출력
            Debug.Log("Global Position: " + globalPosition);
        }
        else
        {
            Debug.LogWarning("Target Object가 설정되지 않았습니다.");
        }
    }
}
