using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Wuchi_ThrowAssist : XRGrabInteractable
{

    bool isControllerVelocityPollingActive;

    List<Vector3> polledVelocities;

    public void Start()
    {
        polledVelocities = new List<Vector3>();
    }

    protected override void OnSelectEnter(XRBaseInteractor interactor)
    {
        if (!interactor)
            return;
        base.OnSelectEnter(interactor);

        isControllerVelocityPollingActive = true;
        Debug.Log("Grabbed.");

    }

    protected override void OnSelectExit(XRBaseInteractor interactor)
    {
        base.OnSelectExit(interactor);

        isControllerVelocityPollingActive = false;
        Debug.Log("Ungrabbed.");

    }

    //// VRTK's ProcessUpdate method runs in every Update on the Interactable Object while it is being grabbed.
    //public override void ProcessUpdate()
    //{
    //    if (isControllerVelocityPollingActive)
    //    {
    //        VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(grabbedObjectScript.GetGrabbingObject());
    //        Vector3 velocity = VRTK_DeviceFinder.GetControllerVelocity(controllerReference);
    //        pollingList.Add(velocity);
    //    }
    //}


    protected override void Detach() // Throw velocity upon detach (ungrab).
    {
        if (m_ThrowOnDetach)
        {
            m_RigidBody.velocity = m_DetachVelocity;
            m_RigidBody.angularVelocity = m_DetachAngularVelocity;
        }

        Debug.Log("Detached.");
    }
}