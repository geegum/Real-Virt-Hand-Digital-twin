using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//AzureKinect SDK
using Microsoft.Azure.Kinect.Sensor;
//비동기 처리
using System.Threading.Tasks;

using TMPro; // TextMeshPro를 사용하기 위해 추가
public class KinectPointCloudCtrl : MonoBehaviour
{
    //Kinect 변수
    Device kinect;

    public float leftSum = 0f;
    public float rightSum = 0f;
    public float allsum = 0f;

    public TextMeshProUGUI distanceText;

    //PointCloud의 수
    int num;

    Mesh mesh;
    //PointCloud의 각 점의 좌표의 배열
    Vector3[] vertices;
    //PointCloud의 각 점에 대응하는 색의 배열
    Color32[] colors;
    //PointCloud 배열 번호를 기록
    int[] indices;
    //좌표 변환
    Transformation transformation;
    int width;
    int height;

    void Start()
    {
        //Kinect 초기화
        InitKinect();

        //PointCloud 준비
        InitMesh();

        //Kinect 데이터 가져오기
        Task t = KinectLoop();
        if (distanceText == null)

        {

            Debug.LogError("Distance Text UI가 할당되지 않았습니다. Inspector에서 연결해주세요.");

        }
    }

    //Kinect 초기화
    private void InitKinect()
    {
        // 0번째 Kinect와 연결
        kinect = Device.Open(0);

        //Kinect 모드 설정
        kinect.StartCameras(new DeviceConfiguration
        {
            ColorFormat = ImageFormat.ColorBGRA32,
            ColorResolution = ColorResolution.R720p,
            // DepthMode = DepthMode.NFOV_2x2Binned, // 기존 (320x288 해상도)
            DepthMode = DepthMode.NFOV_Unbinned, // 변경 (640x576 해상도)
            SynchronizedImagesOnly = true,
            CameraFPS = FPS.FPS30
        });

        //좌표 변환(Color <=> Depth 대응이나 Depth -> xyz에 대한 정보를 생성)
        transformation = kinect.GetCalibration().CreateTransformation();
    }

    //PointCloud 준비
    private void InitMesh()
    {
        //뎁스 이미지의 가로 폭(width)과 세로 폭(height)을 취득
        width = kinect.GetCalibration().DepthCameraCalibration.ResolutionWidth;
        height = kinect.GetCalibration().DepthCameraCalibration.ResolutionHeight;
        num = width * height;

        //mesh를 인스턴스화
        mesh = new Mesh();
        //65535점 이상을 표현하기 위해 설정
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        //Depth 이미지의 총 픽셀 수만큼의 저장 공간을 확보
        vertices = new Vector3[num];
        colors = new Color32[num];
        indices = new int[num];

        //PointCloud 배열 번호를 기록
        for (int i = 0; i < num; i++)
        {
            indices[i] = i;
        }
        //점의 좌표와 색상을 mesh에 전달
        mesh.vertices = vertices;
        mesh.colors32 = colors;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        //메쉬를 MeshFilter에 적용
        gameObject.GetComponent<MeshFilter>().mesh = mesh;
    }

    //Kinect 데이터 가져오기
    private async Task KinectLoop()
    {
        //while문에서 kinect에서 데이터를 계속 취득
        while (true)
        {
            //GetCapture에서 Kinect의 데이터를 검색
            using (Capture capture = await Task.Run(() => kinect.GetCapture()).ConfigureAwait(true))
            {
                //Depth 이미지를 얻음
                Image colorImage = transformation.ColorImageToDepthCamera(capture);
                //색상 정보를 배열로 가져옴
                BGRA[] colorArray = colorImage.GetPixels<BGRA>().ToArray();

                //Depth 이미지를 xyz로 변환
                Image xyzImage = transformation.DepthImageToPointCloud(capture.Depth);
                //변화된 데이터에서 점의 좌표를 배열로 가져옴
                Short3[] xyzArray = xyzImage.GetPixels<Short3>().ToArray();

                // 좌우 거리 합산 변수 초기화
                leftSum = 0f;
                rightSum = 0f;

                // Kinect의 중심 가로, 세로 위치 계산
                int centerX = width / 2;
                int centerY = height / 2;


                // 중심 픽셀의 인덱스 계산
                int centerIndex = centerY * width + centerX;
                int centerrow = centerY * width;

                // 중심 픽셀의 거리 변수
                float centerDistance = 0f;


                //Kinect에서 취득한 모든 점의 좌표와 색상을 대입
                for (int i = 0; i < num; i++)
                {
                    //정점 좌표를 대입
                    vertices[i].x = xyzArray[i].X * 0.001f;
                    vertices[i].y = xyzArray[i].Y * -0.001f; //상하 반전
                    vertices[i].z = xyzArray[i].Z * 0.001f;
                    //색상 할당
                    colors[i].b = colorArray[i].B;
                    colors[i].g = colorArray[i].G;
                    colors[i].r = colorArray[i].R;
                    colors[i].a = 255;

                    // ★★★ 수정된 부분 ★★★
                    // i가 중심 픽셀의 인덱스와 같으면, z 값(거리)을 centerDistance에 저장
                    if (i == centerIndex)
                    {
                        centerDistance = vertices[i].z;
                        centerDistance = centerDistance * 100f; // cm 단위로 변환
                        distanceText.text = $"dis: {centerDistance} cm";
                    }





                }
                //mesh에 좌표와 색상을 전달
                mesh.vertices = vertices;
                mesh.colors32 = colors;
                mesh.RecalculateBounds();



            }
        }
    }
    private void OnDestroy()
    {
        //Kinect 정지
        kinect.StopCameras();
    }
}

