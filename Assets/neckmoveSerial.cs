using UnityEngine;
using System.IO.Ports; // 시리얼 통신을 위해 꼭 추가해야 합니다.

public class neckmoveSerial : MonoBehaviour
{
    // 인스펙터 창에서 CenterEyeAnchor 오브젝트를 연결할 변수
    public Transform centerEyeAnchor;

    // 시리얼 포트 객체
    private SerialPort serialPort;

    // 포트 이름과 보드레이트를 인스펙터에서 설정할 수 있도록 public으로 선언
    public string portName = "COM4";
    public int baudRate = 9600;

    void Start()
    {
        // CenterEyeAnchor가 할당되었는지 확인
        if (centerEyeAnchor == null)
        {
            Debug.LogError("CenterEyeAnchor가 할당되지 않았습니다. 인스펙터에서 오브젝트를 연결해주세요.");
            return; // 할당되지 않았으면 실행을 중지
        }

        // 시리얼 포트 설정 및 열기
        serialPort = new SerialPort(portName, baudRate);
        try
        {
            serialPort.Open();
            // 읽기 타임아웃 설정 (선택 사항)
            serialPort.ReadTimeout = 100;
            Debug.Log($"시리얼 포트 {portName} 열기 성공!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"시리얼 포트 열기 실패: {e.Message}");
        }
    }

    void Update()
    {
        // CenterEyeAnchor가 있고, 시리얼 포트가 열려있는지 확인
        if (centerEyeAnchor != null && serialPort != null && serialPort.IsOpen)
        {
            // CenterEyeAnchor의 월드 회전 값을 오일러 각으로 가져옵니다. (0 ~ 360 범위)
            Vector3 eulerAngles = centerEyeAnchor.eulerAngles;

            // 0~360 범위의 각도를 -180~180 범위로 변환합니다.
            // 180도보다 큰 각도는 360을 빼서 음수 값으로 만듭니다. (예: 359 -> -1)
            float signedRotX = eulerAngles.y > 180 ? eulerAngles.y - 360 : eulerAngles.y;
            float signedRotZ = eulerAngles.x > 180 ? eulerAngles.x - 360 : eulerAngles.x;

            // 변환된 회전 값을 정수로 변환합니다.
            int finalRotX = (int)signedRotX;
            int finalRotZ = (int)signedRotZ;


            // "x값z값\n" 형태의 문자열로 만듭니다.
            string message = $"x{finalRotX}z{finalRotZ}\n";

            try
            {
                // 시리얼 포트로 문자열을 보냅니다.
                serialPort.Write(message);
                
                // 디버깅용: 어떤 값이 보내지는지 콘솔에 출력합니다.
                //Debug.Log($"Sending: {message}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"데이터 전송 실패: {e.Message}");
            }
        }
    }

    // 애플리케이션이 종료될 때 시리얼 포트를 닫아줍니다.
    void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log("시리얼 포트를 닫았습니다.");
        }
    }
}

