using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Wuchi_ThrowAssist : XRGrabInteractable
{
    protected override void Detach()
    {
        base.Detach();
        Debug.Log("Tester.");
    }
}