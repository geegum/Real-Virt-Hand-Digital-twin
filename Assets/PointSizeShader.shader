Shader "Unlit/PointSizeShader"

{

    Properties

    {

        // 인스펙터 창에 점 크기를 조절할 수 있는 슬라이더를 만듭니다.

        _PointSize("Point Size", Range(1, 5000)) = 500.0

    }

    SubShader

    {

        Tags { "RenderType"="Opaque" }

        LOD 100

        Pass

        {

            CGPROGRAM

            #pragma vertex vert

            #pragma fragment frag

            #include "UnityCG.cginc"



            // C# 스크립트로부터 정점 데이터를 받는 구조체

            struct appdata

            {

                float4 vertex : POSITION;

                fixed4 color : COLOR; // 정점의 색상 정보

            };



            // 픽셀 셰이더로 데이터를 넘겨주는 구조체

            struct v2f

            {

                fixed4 color : COLOR;

                float4 vertex : SV_POSITION;

                float psize : PSIZE; // 점의 크기 정보

            };



            // Properties에서 선언한 _PointSize 변수를 받습니다.

            uniform float _PointSize;



            v2f vert(appdata v)

            {

                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);

                o.color = v.color;

                // 점의 크기를 설정합니다.

                o.psize = _PointSize;

                return o;

            }



            fixed4 frag(v2f i) : SV_Target

            {

                // 정점의 색상을 그대로 출력합니다.

                return i.color;

            }

            ENDCG

        }

    }

}

