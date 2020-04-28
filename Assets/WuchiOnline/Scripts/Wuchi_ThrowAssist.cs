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

    protected override void Detach()
    {
        base.Detach();
        Debug.Log("Tester.");
    }
}