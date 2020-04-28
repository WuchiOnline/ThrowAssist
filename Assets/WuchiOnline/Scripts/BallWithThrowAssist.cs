namespace Hooplord
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    // using VRTK;
    // using VRTK.GrabAttachMechanics;

    public class BallWithThrowAssist : MonoBehaviour // VRTK_BaseGrabAttach
    {
        //public PlayerPositionTracker playerPositionTrackerScript;

        //public float unassistedThrowVelocityModifier;

        //private List<Vector3> pollingList;
        //private bool isControllerVelocityPollingActive;

        //[Header("Valid Teleport Positions")]
        //// The five valid teleport positions on the court, rotated towards the basketball hoop.
        //public Transform playerPositionA;
        //public Transform playerPositionB;
        //public Transform playerPositionC;
        //public Transform playerPositionD;
        //public Transform playerPositionE;
        //private Transform currentPlayerTransform;

        //private Vector3 controllerPositionAtRelease;

        //private Vector3 baseThrowVelocity;

        //// All constants were determined by extensive playtesting for best feel.
        //private const float HorizontalAssistThreshold = 0.75f;
        //private const float MinUpwardThrowModifier = 15.0f;
        //private const float MaxUpwardThrowModifier = 1.54f;
        //private const float MinForwardThrowModifier = 13.0f;
        //private const float MaxForwardThrowModifier = 1.36f;
        //private const float MinBaseAssistRange = 0.5f;
        //private const float MaxBaseAssistRange = 5.5f;
        //private const int OptimalPolledVelocityCount = 3; // Three is the sweet spot, although four and five produce decent results as well.
        //private const float AverageReleaseHeight = 2.5f;
        //private const float ThrowStrengthAssistThreshold = 0.45f;

        //public void Start()
        //{
        //    pollingList = new List<Vector3>();
        //}

        //public override bool StartGrab(GameObject grabbingObject, GameObject givenGrabbedObject, Rigidbody givenControllerAttachPoint)
        //{
        //    isControllerVelocityPollingActive = true;
        //    return base.StartGrab(grabbingObject, givenGrabbedObject, givenControllerAttachPoint);
        //}

        //public override void StopGrab(bool applyGrabbingObjectVelocity)
        //{
        //    isControllerVelocityPollingActive = false;
        //    base.StopGrab(applyGrabbingObjectVelocity);
        //}

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

        //public override void ThrowReleasedObject(Rigidbody objectRigidbody)
        //{
        //    if (grabbedObjectScript != null)
        //    {
        //        VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(grabbedObjectScript.GetGrabbingObject());
        //        if (VRTK_ControllerReference.IsValid(controllerReference) && controllerReference.scriptAlias != null)
        //        {
        //            VRTK_InteractGrab grabbingObjectScript = controllerReference.scriptAlias.GetComponentInChildren<VRTK_InteractGrab>();

        //            Transform origin = VRTK_DeviceFinder.GetControllerOrigin(controllerReference);
        //            Vector3 velocity = VRTK_DeviceFinder.GetControllerVelocity(controllerReference);
        //            Vector3 angularVelocity = VRTK_DeviceFinder.GetControllerAngularVelocity(controllerReference);

        //            controllerPositionAtRelease = VRTK_DeviceFinder.GetScriptAliasController(controllerReference.scriptAlias).transform.position;

        //            DetermineCurrentPlayerPositionAnchor();

        //            objectRigidbody.angularVelocity = origin.TransformDirection(angularVelocity);

        //            if (velocity.y < ThrowStrengthAssistThreshold || velocity.z < ThrowStrengthAssistThreshold) // Does not meet the minimum throw strength to trigger assisted throw.
        //            {
        //                objectRigidbody.velocity = currentPlayerTransform.InverseTransformVector(velocity) * unassistedThrowVelocityModifier;
        //            }
        //            else
        //            {
        //                objectRigidbody.velocity = DetermineAssistedThrowVelocity();
        //            }
        //        }
        //    }
        //}

        //private Vector3 DetermineAssistedThrowVelocity()
        //{
        //    baseThrowVelocity = DetermineHighestUpwardVelocityFromPolledList();

        //    float assistedUpwardVelocity = AssistBaseUpwardVelocity();
        //    float assistedForwardVelocity = AssistBaseForwardVelocity();
        //    float adjustedHorizontalVelocity = AdjustBaseHorizontalVelocity(assistedForwardVelocity);

        //    Vector3 newAssistedThrowVelocity = new Vector3(adjustedHorizontalVelocity, assistedUpwardVelocity, assistedForwardVelocity);
        //    Vector3 transformedAssistedThrowVelocity = currentPlayerTransform.InverseTransformVector(newAssistedThrowVelocity);

        //    float releaseHeightThrowModifier;

        //    if (controllerPositionAtRelease.y > AverageReleaseHeight) // Above-Average Release Height 
        //    {
        //        releaseHeightThrowModifier = 1.0f + ((AverageReleaseHeight - controllerPositionAtRelease.y) * 0.05f); // This function was determined through extensive playtesting for best feel 
        //    }
        //    else // Below-Average Release Height
        //    {
        //        releaseHeightThrowModifier = 1.0f + ((AverageReleaseHeight - controllerPositionAtRelease.y) * 0.06f); // This function was determined through extensive playtesting for best feel
        //    }

        //    Vector3 finalAssistedThrowVelocity = transformedAssistedThrowVelocity * releaseHeightThrowModifier;

        //    pollingList.Clear();

        //    return finalAssistedThrowVelocity;
        //}

        //private Vector3 DetermineHighestUpwardVelocityFromPolledList()
        //{

        //    int polledVelocitiesToUse = Mathf.Min(OptimalPolledVelocityCount, pollingList.Count);

        //    if (polledVelocitiesToUse == 0)
        //    {
        //        return Vector3.zero;
        //    }

        //    Vector3 highestUpwardVelocity = pollingList[pollingList.Count - 1];

        //    for (int i = 1; i < polledVelocitiesToUse; i++)
        //    {
        //        int index = pollingList.Count - (i + 1);

        //        if (pollingList[index].y > highestUpwardVelocity.y)
        //        {
        //            highestUpwardVelocity = pollingList[index];
        //        }
        //    }

        //    return highestUpwardVelocity;

        //}

        //private void DetermineCurrentPlayerPositionAnchor()
        //{

        //    switch (playerPositionTrackerScript.currentPlayerPosition)
        //    {
        //        case 1:
        //            currentPlayerTransform = playerPositionA;
        //            break;
        //        case 2:
        //            currentPlayerTransform = playerPositionB;
        //            break;
        //        case 3:
        //            currentPlayerTransform = playerPositionC;
        //            break;
        //        case 4:
        //            currentPlayerTransform = playerPositionD;
        //            break;
        //        case 5:
        //            currentPlayerTransform = playerPositionE;
        //            break;
        //    }
        //}

        //private float AssistBaseUpwardVelocity()
        //{
        //    if (baseThrowVelocity.y <= 0f)
        //    {
        //        return baseThrowVelocity.y * unassistedThrowVelocityModifier;
        //    }
        //    else if (baseThrowVelocity.y < MinBaseAssistRange)
        //    {
        //        return baseThrowVelocity.y * MinUpwardThrowModifier; // This constant was determined through extensive playtesting for best feel
        //    }
        //    else if (baseThrowVelocity.y < MaxBaseAssistRange)
        //    {
        //        return AssistedBaseUpwardMagnitude(); 
        //    }
        //    else
        //    {
        //        return baseThrowVelocity.y * MaxUpwardThrowModifier; // This constant was determined through extensive playtesting for best feel
        //    }
        //}

        //private float AssistedBaseUpwardMagnitude()
        //{
        //    // This function was determined through extensive playtesting for best feel, please see: https://www.desmos.com/calculator/clrzceq5ch
        //    return (baseThrowVelocity.y / 6.0f) + 7.5f;
        //}

        //private float AssistBaseForwardVelocity()
        //{
        //    if (baseThrowVelocity.z <= 0f)
        //    {
        //        return baseThrowVelocity.z * unassistedThrowVelocityModifier;
        //    }
        //    else if (baseThrowVelocity.z < MinBaseAssistRange) // This constant was determined through extensive playtesting for best feel
        //    {
        //        return baseThrowVelocity.z * MinForwardThrowModifier;
        //    }
        //    else if (baseThrowVelocity.z < MaxBaseAssistRange)
        //    {
        //        return AssistedBaseForwardMagnitude();
        //    }
        //    else
        //    {
        //        return baseThrowVelocity.z * MaxForwardThrowModifier; // This constant was determined through extensive playtesting for best feel
        //    }
        //}

        //private float AssistedBaseForwardMagnitude()
        //{
        //    // This function was determined through extensive playtesting for best feel, please see: https://www.desmos.com/calculator/m07laliezy)
        //    return (baseThrowVelocity.z / 6.0f) + 6.5f; 
        //}

        //private float AdjustBaseHorizontalVelocity(float assistedForwardVelocity)
        //{
        //    // If throw is not within horizontal threshold of target (basketball hoop), then adjust accordingly to make throw more accurate.
        //    if (baseThrowVelocity.x > HorizontalAssistThreshold || baseThrowVelocity.x < -1 * HorizontalAssistThreshold) 
        //    {
        //        return (baseThrowVelocity.x * assistedForwardVelocity) / baseThrowVelocity.z;
        //    }
        //    else
        //    {
        //        return baseThrowVelocity.x;
        //    }

        //}

    }
}