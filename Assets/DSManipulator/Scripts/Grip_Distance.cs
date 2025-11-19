using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grip_Distance : MonoBehaviour
{
    public Combine_Hand_R combineHandR;

    public GameObject GR_1;
    public GameObject GR_2;
    public GameObject GL_1;
    public GameObject GL_2;
    public GameObject index;
    public GameObject thumb;

    private float maxDistance = 0.1f;
    private float minRotation = 0f;
    private float maxRotation = 45f;
    private double rotationAngle;

    // Start is called before the first frame update
    void Start()
    {
        combineHandR = GameObject.FindObjectOfType<Combine_Hand_R>();
    }

    // Update is called once per frame
    void Update()
    {
        if (index != null)
        {
            //Debug.Log("index is not null");
        }

        if (thumb != null)
        {
            //Debug.Log("thumb is not null");
        }

        if (index != null && thumb != null && combineHandR.Able == true)
        {
            float distance = Vector3.Distance(index.transform.position, thumb.transform.position);
            distance = Mathf.Clamp(distance, 0f, maxDistance);
           // Debug.Log(distance);

            float rotationAngle = Mathf.Lerp(minRotation, maxRotation, distance / maxDistance);

            GR_1.transform.localRotation = Quaternion.Euler(90f, 0f, -rotationAngle);
            GR_2.transform.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);
            GL_1.transform.localRotation = Quaternion.Euler(90f, 0f, rotationAngle);
            GL_2.transform.localRotation = Quaternion.Euler(0f, 0f, -rotationAngle);

            // Socket_Joint ��ũ��Ʈ�� rotationAngle ���� ����
            if (Socket_Joint.Instance != null)
            {
                Socket_Joint.Instance.rotationAngle = Mathf.Lerp(0f, 1000f, rotationAngle / 45f); ;
            }

            if (Socket_Jointspeed.Instance != null)
            {
                Socket_Jointspeed.Instance.rotationAngle = Mathf.Lerp(0f, 1000f, rotationAngle / 45f); ;
            }
            
        }
    }
}
