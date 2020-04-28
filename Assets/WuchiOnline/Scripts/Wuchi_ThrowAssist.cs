using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Wuchi_ThrowAssist : XRGrabInteractable
{

    List<Vector3> pollingList;

    void Start()
    {
        pollingList = new List<Vector3>();
    }

    void Update()
    {
        
    }

    protected override void Detach()
    {
        if (m_ThrowOnDetach)
        {
            m_RigidBody.velocity = m_DetachVelocity;
            m_RigidBody.angularVelocity = m_DetachAngularVelocity;
        }

        Debug.Log("Test");
    }
}