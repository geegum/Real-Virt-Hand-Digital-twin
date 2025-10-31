using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playCamOnSphere : MonoBehaviour
{
    public GameObject ProjectionSphere;
    public int cameraNumber = 1;

    private WebCamTexture webCamTexture;
    private WebCamDevice webCamDevice;

    void Start()
    {
        webCamDevice = WebCamTexture.devices[cameraNumber]; //use cam number = 1 when using laptop
        webCamTexture = new WebCamTexture(webCamDevice.name, 2880, 1440);
        ProjectionSphere.GetComponent<Renderer>().material.mainTexture = webCamTexture;
        webCamTexture.Play();
    }
}
