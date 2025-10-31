using UnityEngine;
using System.Collections;

public class ShapeDrawing : MonoBehaviour
{
    public float radius = 0.3f; // 원운동의 반지름
    public float speed = 1.0f;  // 원운동의 속도 (라디안/초)
    public float heartSpeed = 1.0f; // 하트 모양 궤적의 속도
    private float elapsedTime;
    private Vector3 initialPosition; // 물체의 초기 위치
    private bool isMoving = false; // 모양 그리기 실행 여부
    private bool isReturning = false; // 초기 위치로 돌아가는 중인지 여부
    private bool isStopped = false; // 멈춤 여부

    public bool drawCircle = false; // 원을 그릴지 여부
    public bool drawHeart = false; // 하트 모양을 그릴지 여부
    private float scaleHeart = 0.2f; // 하트 크기

    void Start()
    {
        // 물체의 초기 위치를 저장합니다.
        initialPosition = transform.position;
        elapsedTime = 0.0f; // 시작 시간을 0으로 설정
    }

    void Update()
    {
        // 둘 다 true 또는 둘 다 false일 때 그 자리에서 멈추고 절대로 안 움직이게 함
        if ((drawCircle && drawHeart) || (!drawCircle && !drawHeart))
        {
            isMoving = false;
            isStopped = true;
            return;
        }

        // 스페이스바 입력을 감지하여 원운동을 시작하거나 멈추고, 하트 모양을 그립니다.
        if (Input.GetKeyDown(KeyCode.Space) && !isStopped)
        {
            if (isMoving)
            {
                // 모양 그리기를 멈추고 초기 위치로 돌아가는 코루틴을 시작합니다.
                isMoving = false;
                StartCoroutine(ReturnToInitialPosition());
            }
            else
            {
                // 원운동 또는 하트 그리기를 1초 후에 시작합니다.
                if (drawHeart)
                {
                    StartCoroutine(StartHeartDrawing());
                }
                else if (drawCircle)
                {
                    StartCoroutine(StartCircularMotion());
                }
            }
        }

        // 모양 그리기가 실행 중일 때만 위치를 업데이트합니다.
        if (isMoving && !isReturning && !isStopped)
        {
            // 프레임 간 경과 시간을 누적합니다.
            elapsedTime += Time.deltaTime;

            if (drawHeart)
            {
                DrawHeart();
            }
            else if (drawCircle)
            {
                DrawCircle();
            }
        }
    }

    IEnumerator StartCircularMotion()
    {
        // 원운동 시작 위치로 부드럽게 이동
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = initialPosition + new Vector3(radius+0.4f, 0.2f, 0); // 원의 시작점

        float moveDuration = 1.0f; // 이동 시간
        float moveElapsed = 0.0f;

        while (moveElapsed < moveDuration)
        {
            moveElapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPosition, targetPosition, moveElapsed / moveDuration);
            yield return null;
        }

        // 1초 대기
        yield return new WaitForSeconds(1.0f);

        // 원운동을 시작합니다.
        isMoving = true;
        isReturning = false;
        elapsedTime = 0.0f; // 원운동을 시작할 때 경과 시간을 초기화
    }

    IEnumerator StartHeartDrawing()
    {
        // 하트 시작 위치로 부드럽게 이동
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = initialPosition + new Vector3(scaleHeart * 5.0f / 13.0f + 0.4f, 0.2f, 0f); // 하트의 시작점을 0.5f 위로 올림

        float moveDuration = 1.0f; // 이동 시간
        float moveElapsed = 0.0f;

        while (moveElapsed < moveDuration)
        {
            moveElapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPosition, targetPosition, moveElapsed / moveDuration);
            yield return null;
        }

        // 1초 대기
        yield return new WaitForSeconds(1.0f);

        // 하트 그리기를 시작합니다.
        isMoving = true;
        isReturning = false;
        elapsedTime = 0.0f; // 하트를 그리기 시작할 때 경과 시간을 초기화
    }

    IEnumerator ReturnToInitialPosition()
    {
        // 1초 대기합니다.
        yield return new WaitForSeconds(1.0f);

        // 초기 위치로 천천히 돌아갑니다.
        Vector3 currentPosition = transform.position;
        float returnSpeed = 0.5f; // 초기 위치로 돌아가는 속도
        float journey = 0f;

        isReturning = true;

        while (journey < 1f)
        {
            journey += Time.deltaTime * returnSpeed;
            transform.position = Vector3.Lerp(currentPosition, initialPosition, journey);
            yield return null;
        }

        // 초기 위치로 돌아간 후 상태를 리셋합니다.
        transform.position = initialPosition;
        isReturning = false;
    }

    void DrawCircle()
    {
        // 물체의 새로운 위치를 계산합니다.
        float angle = elapsedTime * speed; // 라디안 단위의 각도
        float x = Mathf.Cos(angle) * radius+0.4f;
        float z = Mathf.Sin(angle) * radius;

        // 물체의 위치를 초기 위치를 기준으로 업데이트합니다.
        transform.position = initialPosition + new Vector3(x, 0.2f, z);
    }

    void DrawHeart()
    {
        // 물체의 새로운 위치를 계산합니다.
        float t = elapsedTime * heartSpeed; // 하트 모양을 그리는 시간
        float z = scaleHeart * (16.0f * Mathf.Pow(Mathf.Sin(t), 3)) / 16.0f;
        float x = scaleHeart * (13.0f * Mathf.Cos(t) - 5.0f * Mathf.Cos(2.0f * t) - 2.0f * Mathf.Cos(3.0f * t) - Mathf.Cos(4.0f * t)) / 13.0f;

        // 물체의 위치를 초기 위치를 기준으로 업데이트합니다.
        transform.position = initialPosition + new Vector3(x+0.4f, 0.2f, z); // y 값을 0.5f만큼 올림
    }

    void OnValidate()
    {
        // 둘 다 true 또는 둘 다 false일 때 멈추게 함
        if ((drawCircle && drawHeart) || (!drawCircle && !drawHeart))
        {
            isMoving = false;
            isStopped = true;
        }
        else
        {
            isStopped = false;
        }
    }
}

