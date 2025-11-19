using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Combine_Hand_R : MonoBehaviour
{
    public bool Able;
    public bool isHandFree;
    private Transform originalParent; 

    void Start()
    {
        Able = false;
        isHandFree = false;
        originalParent = transform.parent;
    }

    void OnTriggerEnter(Collider other)
    {

        if (isHandFree) return; 

        if (other.CompareTag("Real_R") && !isHandFree)
        {

            Able = true;

            transform.SetParent(other.transform);
            Debug.Log("Right On");
        }
    }


    public void RestoreParent()
    {
        Debug.Log("Reset");
        Able = false;
        transform.SetParent(originalParent);

    }
}