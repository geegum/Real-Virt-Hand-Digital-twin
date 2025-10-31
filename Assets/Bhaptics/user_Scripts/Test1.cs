using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bhaptics.SDK2;
using UnityEngine.UIElements;

public class Test1 : MonoBehaviour
{
    private float timeSinceLastCall = 0f; // 시간을 기록하는 변수
    private float callInterval = 0.011f; // 호출 간격 (1초)
    private bool reset = true;

    private float scale_x = 0f;
    private float scale_y = 0f;

    private float max_scale_x = 1000f;
    private float max_scale_y = 100f;

    private float real_scale_x = 0f;
    private float real_scale_y = 0f;

    void Update()
    {
        

        timeSinceLastCall += Time.deltaTime; // 경과 시간 누적
        if (reset)
        {
            real_scale_x = Mathf.Lerp(0f, 1f, scale_x / max_scale_x);
            real_scale_y = Mathf.Lerp(0f, 1f, scale_y / max_scale_y);
            //OnCall1();
            OnCall2();
            reset = false;
            scale_x += 1f;
            //scale_y += 1f;

            if(scale_x > max_scale_x)
            {
                scale_x = 0f;
            }

            /*if (scale_y > max_scale_y)
            {
                scale_y = 0f;
            }*/
        }

        if (timeSinceLastCall >= callInterval && !reset)
        {

            timeSinceLastCall = 0f; // 경과 시간 초기화
            reset = true;
        }
    }

    public void OnCall1()
    {
        int[] motorValues = new int[6] { 50, 80, 100, 50, 50, 0 };

        GlovePlayTime[] playTimeValues = new GlovePlayTime[6] {
            GlovePlayTime.FiveMS, GlovePlayTime.TwentyMS,
            GlovePlayTime.ThirtyMS, GlovePlayTime.FortyMS,
            GlovePlayTime.FortyMS, GlovePlayTime.FortyMS
        };

        GloveShapeValue[] shapeValues = new GloveShapeValue[6] {
            GloveShapeValue.Constant, GloveShapeValue.Decreasing,
            GloveShapeValue.Increasing, GloveShapeValue.Constant,
            GloveShapeValue.Constant, GloveShapeValue.Constant
        };

        BhapticsLibrary.PlayWaveform(
            PositionType.GloveR,    // Device Type
            motorValues,            // Intensities
            playTimeValues,         // Intervals
            shapeValues             // Intensity changing forms
        );
    }

    public void OnCall2()
    {
        BhapticsLibrary.PlayPath(
            (int)PositionType.GloveR,           // Device type
            new float[] { real_scale_x }, // X Coordinates
            new float[] { 0f}, // Y Coordinates
            new int[] { 20},         // Intensities
            10                               // Duration
        );
    }
}
