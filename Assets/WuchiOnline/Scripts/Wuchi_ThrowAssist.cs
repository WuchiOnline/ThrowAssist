using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Wuchi_ThrowAssist : XRGrabInteractable
{

    // Magic Numbers: all constants were determined by extensive playtesting for best feel.
    private const float HorizontalAssistThreshold = 0.75f;
    private const float MinUpwardThrowModifier = 15.0f;
    private const float MaxUpwardThrowModifier = 1.54f;
    private const float MinForwardThrowModifier = 13.0f;
    private const float MaxForwardThrowModifier = 1.36f;
    private const float MinBaseAssistRange = 0.5f;
    private const float MaxBaseAssistRange = 5.5f;
    private const int OptimalPolledVelocityCount = 3; // Three is the sweet spot, although four and five produce decent results as well.
    private const float AverageReleaseHeight = 2.5f;
    private const float ThrowStrengthAssistThreshold = 0.45f;

    public Transform target;

    public float unassistedThrowVelocityModifier;

    bool isInteractorVelocityPollingActive;

    public Queue<Vector3> polledVelocities;

    private Vector3 baseThrowVelocity;

    public void Start()
    {
        polledVelocities = new Queue<Vector3>();
    }

    protected override void OnSelectEnter(XRBaseInteractor interactor)
    {
        if (!interactor)
            return;
        base.OnSelectEnter(interactor);

        isInteractorVelocityPollingActive = true;
        Debug.Log("Grabbed.");

    }

    protected override void OnSelectExit(XRBaseInteractor interactor)
    {
        base.OnSelectExit(interactor);

        isInteractorVelocityPollingActive = false;
        Debug.Log("Ungrabbed.");

    }

    private void Update()
    {
        if (isInteractorVelocityPollingActive)
        {
            Vector3 smoothedVelocity = getSmoothedVelocityValue(m_ThrowSmoothingVelocityFrames);
            Vector3 velocityToPoll = smoothedVelocity * m_ThrowVelocityScale;

            if(polledVelocities.Count >= OptimalPolledVelocityCount)
            {
                polledVelocities.Dequeue();
            }

            polledVelocities.Enqueue(velocityToPoll);

            Debug.Log("Velocity has been added to polledVelocities: " + velocityToPoll);
        }

    }

    protected override void Detach() // Throw velocity upon detach (ungrab).
    {
        if (m_ThrowOnDetach)
        {
            // RESUME HERE:
            // 4-28-20 You need to figure out how to abstract this logic so that it works from any position/rotation in the scene,
            // not just predetermined transforms with preset rotations towards the target object.

            // DetermineCurrentPlayerPositionAnchor();

            if (m_DetachVelocity.y < ThrowStrengthAssistThreshold || m_DetachVelocity.z < ThrowStrengthAssistThreshold || polledVelocities.Count < 1) // Does not meet the minimum throw strength to trigger assisted throw.
            {
                m_RigidBody.velocity = m_DetachVelocity;
                m_RigidBody.angularVelocity = m_DetachAngularVelocity;
            }

            else
            {
                // m_RigidBody.velocity = DetermineAssistedThrowVelocity(); // NEXT UP
                m_RigidBody.velocity = m_DetachVelocity; // temporary until you finish refactoring DetermineAssistedThrowVelocity();
                m_RigidBody.angularVelocity = m_DetachAngularVelocity;
            }

        }

        Debug.Log("Detached.");
    }

    private void DetermineCurrentPlayerPositionAnchor()
    {
        // This logic needs to be abstracted.
        // This may be that you create a new currentPlayerTransform that has everything at 0 except for the rotation of the headset.
        // logic abstraction attempt 1:

        //Vector3 direction = (target.position - transform.position).normalized;

        //// create the rotation we need to be in to look at the target
        //Quaternion lookAtRotation = Quaternion.LookRotation(direction);

        //Quaternion lookAtRotation_onlyY = Quaternion.Euler(transform.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        //transform.rotation = lookAtRotation_onlyY;


        //switch (playerPositionTrackerScript.currentPlayerPosition)
        //{
        //    case 1:
        //        currentPlayerTransform = playerPositionA;
        //        break;
        //    case 2:
        //        currentPlayerTransform = playerPositionB;
        //        break;
        //    case 3:
        //        currentPlayerTransform = playerPositionC;
        //        break;
        //    case 4:
        //        currentPlayerTransform = playerPositionD;
        //        break;
        //    case 5:
        //        currentPlayerTransform = playerPositionE;
        //        break;
        //}
    }

    //public void ConstructPlayerToTargetTransformAnchor()
    //{
    //    Vector3 direction = (target.position - transform.position).normalized;

    //    //create the rotation we need to be in to look at the target
    //    Quaternion lookAtRotation = Quaternion.LookRotation(direction);

    //    Quaternion lookAtRotation_onlyY = Quaternion.Euler(transform.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, transform.rotation.eulerAngles.z);

    //    transform.rotation = lookAtRotation_onlyY;
    //}

    //private Vector3 DetermineAssistedThrowVelocity()
    //{
    //    baseThrowVelocity = DetermineHighestUpwardVelocityFromPolledList();

    //    float assistedUpwardVelocity = AssistBaseUpwardVelocity();
    //    float assistedForwardVelocity = AssistBaseForwardVelocity();
    //    float adjustedHorizontalVelocity = AdjustBaseHorizontalVelocity(assistedForwardVelocity);

    //    Vector3 newAssistedThrowVelocity = new Vector3(adjustedHorizontalVelocity, assistedUpwardVelocity, assistedForwardVelocity);

    //    // resume here. 4-30-20 12:48 PM
    //    // transform world space velocity so that it is localized to a transform that is rotated (looking at) target object.

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

    private Vector3 DetermineHighestUpwardVelocityFromPolledList()
    {

        Vector3 highestUpwardVelocity = polledVelocities.OrderBy(velocity => velocity.y).Last();

        Debug.Log("Highest Upward Velocity From Polled List is: " + highestUpwardVelocity);

        return highestUpwardVelocity;

    }

    private float AssistBaseUpwardVelocity()
    {
        if (baseThrowVelocity.y <= 0f)
        {
            return baseThrowVelocity.y * unassistedThrowVelocityModifier;
        }
        else if (baseThrowVelocity.y < MinBaseAssistRange)
        {
            return baseThrowVelocity.y * MinUpwardThrowModifier; // This constant was determined through extensive playtesting for best feel
        }
        else if (baseThrowVelocity.y < MaxBaseAssistRange)
        {
            return AssistedBaseUpwardMagnitude();
        }
        else
        {
            return baseThrowVelocity.y * MaxUpwardThrowModifier; // This constant was determined through extensive playtesting for best feel
        }
    }

    private float AssistedBaseUpwardMagnitude()
    {
        // This function was determined through extensive playtesting for best feel, please see: https://www.desmos.com/calculator/clrzceq5ch
        return (baseThrowVelocity.y / 6.0f) + 7.5f;
    }

    private float AssistBaseForwardVelocity()
    {
        if (baseThrowVelocity.z <= 0f)
        {
            return baseThrowVelocity.z * unassistedThrowVelocityModifier;
        }
        else if (baseThrowVelocity.z < MinBaseAssistRange) // This constant was determined through extensive playtesting for best feel
        {
            return baseThrowVelocity.z * MinForwardThrowModifier;
        }
        else if (baseThrowVelocity.z < MaxBaseAssistRange)
        {
            return AssistedBaseForwardMagnitude();
        }
        else
        {
            return baseThrowVelocity.z * MaxForwardThrowModifier; // This constant was determined through extensive playtesting for best feel
        }
    }

    private float AssistedBaseForwardMagnitude()
    {
        // This function was determined through extensive playtesting for best feel, please see: https://www.desmos.com/calculator/m07laliezy)
        return (baseThrowVelocity.z / 6.0f) + 6.5f;
    }

    private float AdjustBaseHorizontalVelocity(float assistedForwardVelocity)
    {
        // If throw is not within horizontal threshold of target object, then adjust accordingly to make throw more accurate.
        if (baseThrowVelocity.x > HorizontalAssistThreshold || baseThrowVelocity.x < -1 * HorizontalAssistThreshold)
        {
            return (baseThrowVelocity.x * assistedForwardVelocity) / baseThrowVelocity.z;
        }
        else
        {
            return baseThrowVelocity.x;
        }

    }
}