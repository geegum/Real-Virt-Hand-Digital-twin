using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tableheight : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 현재 위치를 가져옵니다.
        Vector3 currentPosition = transform.position;

        // y 값이 0.2f 보다 작은지 확인합니다.
        if (currentPosition.y < 0.2f)
        {
            // y 값을 0.2f로 고정합니다.
            currentPosition.y = 0.2f;

            // 변경된 위치를 다시 적용합니다.
            transform.position = currentPosition;
        }
    }
}