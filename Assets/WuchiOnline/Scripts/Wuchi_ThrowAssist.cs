using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// A smoothing algorithm that assists the accuracy and trajectory of an interactable object when thrown at a given target.
/// Originally built and implemented as Hooplord's throwing interaction, I've refactored it so that it extends Unity's XR Interaction Toolkit.
/// </summary>

public class Wuchi_ThrowAssist : XRGrabInteractable
{
    // The Magic Numbers: All constants were determined by extensive playtesting for best feel.
    // These can be abstracted and fine-tuned to achieve a wide variety of smoothing results.

    // The optimal amount of velocities to poll and utilize when assisting velocity transformations.
    const int OptimalPolledVelocityCount = 4;

    // The minimum amount of magnitude a throw must have in either forward and upward directions to be eligible for the smoothing algorithm.
    const float ThrowStrengthThreshold = 0.45f;
    // The maximum amount of horizontal inaccuracy a throw must not exceed to be eligible for the smoothing algorithm.
    const float NormalizedHorizontalInaccuracyThreshold = 0.2f;
    // The minimum local velocity threshold a throw's magnitude must exceed to be assisted.
    const float MinLocalAssistThreshold = 0.5f;
    // The maximum local velocity threshold a throw's magnitude must not exceed to be assisted.
    const float MaxLocalAssistThreshold = 5.5f;
    // The minimum amount of horizontal inaccuracy (relative to target object) a throw must have to warrant proportionately adjusting with assisted forward velocity.
    const float HorizontalAdjustThreshold = 0.75f;

    // The upward velocity modifier applied when a throw is below the minimum local velocity assist threshold.
    const float MinUpwardThrowModifier = 15.0f;
    // The upward velocity modifier applied when a throw is above the maximum local velocity assist threshold.
    const float MaxUpwardThrowModifier = 1.54f;
    // The forward velocity modifier applied when a throw is below the minimum local velocity assist threshold.
    const float MinForwardThrowModifier = 13.0f;
    // The forward velocity modifier applied when a throw is above the maximum local velocity assist threshold.
    const float MaxForwardThrowModifier = 1.36f;

    // The average release height of a throw, used to modify the smoothed velocity for users of all heights and wingspans.
    const float AverageReleaseHeight = 2.5f;
    const float AboveAverageReleaseHeightModifier = 0.05f;
    const float BelowAverageReleaseHeightModifier = 0.06f;


    // Target to assist the user in throwing the object towards.
    public Transform target;

    // Transform of an empty GameObject childed to XR Rig, which can be rotated to face the target on only the Y-axis. Used for transformations.
    public Transform rigToTarget;

    // Optional strength modifier for unassisted throws.
    public float unassistedThrowVelocityModifier = 1.0f;

    Transform currentInteractorAttach;
    bool isInteractorVelocityPollingActive;
    Queue<Vector3> polledVelocities;

    void Start()
    {
        polledVelocities = new Queue<Vector3>();
        rigToTarget = GameObject.FindWithTag("RigToTarget").transform;
    }

    protected override void OnSelectEnter(XRBaseInteractor interactor)
    {
        if (!interactor)
            return;
        base.OnSelectEnter(interactor);
        isInteractorVelocityPollingActive = true;
    }

    protected override void OnSelectExit(XRBaseInteractor interactor)
    {
        currentInteractorAttach = interactor.attachTransform;
        base.OnSelectExit(interactor);
        isInteractorVelocityPollingActive = false;
    }

    void Update()
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
        }
    }

    protected override void Detach()
    {
        if (m_ThrowOnDetach)
        {
            UpdateRigToTargetRotation();

            // Evaluate if throw is both strong and accurate enough to warrant applying the smoothing algorithm.
            if (m_DetachVelocity.y < ThrowStrengthThreshold || // Throw does not meet the minimum vertical throw strength to warrant smoothing.
                rigToTarget.InverseTransformVector(m_DetachVelocity).z < ThrowStrengthThreshold || // Throw does not meet the minimum forward throw strength to warrant smoothing. 
                Mathf.Abs(rigToTarget.InverseTransformVector(m_DetachVelocity).normalized.x) > NormalizedHorizontalInaccuracyThreshold || // Throw is not horizontally accurate enough to warrant smoothing.
                polledVelocities.Count < 1) // Object's velocity was not polled for at least a single frame before release.
            {
                m_RigidBody.velocity = m_DetachVelocity;
                m_RigidBody.angularVelocity = m_DetachAngularVelocity;
            }
            else
            {
                m_RigidBody.velocity = DetermineAssistedThrowVelocity();
                m_RigidBody.angularVelocity = m_DetachAngularVelocity;
            }
        }
    }

    void UpdateRigToTargetRotation()
    {
        Vector3 direction = (target.position - rigToTarget.position).normalized;
        Quaternion lookAtRotation = Quaternion.LookRotation(direction);
        Quaternion lookAtRotation_onlyY = Quaternion.Euler(rigToTarget.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, rigToTarget.rotation.eulerAngles.z);

        rigToTarget.rotation = lookAtRotation_onlyY;
    }

    Vector3 DetermineAssistedThrowVelocity()
    {
        Vector3 baseThrowVelocity = DetermineHighestUpwardVelocityFromPolledList();

        Vector3 localizedThrowVelocity = rigToTarget.InverseTransformVector(baseThrowVelocity);

        float assistedUpwardVelocity = AssistLocalUpwardVelocity(localizedThrowVelocity);
        float assistedForwardVelocity = AssistLocalForwardVelocity(localizedThrowVelocity);
        float adjustedHorizontalVelocity = AdjustLocalHorizontalVelocity(localizedThrowVelocity, assistedForwardVelocity);

        Vector3 localAssistedThrowVelocity = new Vector3(adjustedHorizontalVelocity, assistedUpwardVelocity, assistedForwardVelocity);

        Vector3 worldAssistedThrowVelocity = rigToTarget.TransformVector(localAssistedThrowVelocity);

        Vector3 finalAssistedThrowVelocity = ApplyReleaseHeightModifier(worldAssistedThrowVelocity);

        polledVelocities.Clear();

        return finalAssistedThrowVelocity;
    }

    Vector3 DetermineHighestUpwardVelocityFromPolledList()
    {
        Vector3 highestUpwardVelocity = polledVelocities.OrderBy(velocity => velocity.y).Last();

        return highestUpwardVelocity;
    }

    float AssistLocalUpwardVelocity(Vector3 localVelocity)
    {
        if (localVelocity.y <= 0f)
        {
            return localVelocity.y * unassistedThrowVelocityModifier;
        }
        else if (localVelocity.y < MinLocalAssistThreshold)
        {
            return localVelocity.y * MinUpwardThrowModifier;
        }
        else if (localVelocity.y < MaxLocalAssistThreshold)
        {
            return AssistedLocalUpwardMagnitude(localVelocity);
        }
        else
        {
            return localVelocity.y * MaxUpwardThrowModifier;
        }
    }

    float AssistedLocalUpwardMagnitude(Vector3 localVelocity)
    {
        // This function was determined through extensive playtesting for best feel
        return (localVelocity.y / 6.0f) + 7.5f;
    }

    float AssistLocalForwardVelocity(Vector3 localVelocity)
    {
        if (localVelocity.z <= 0f)
        {
            return localVelocity.z * unassistedThrowVelocityModifier;
        }
        else if (localVelocity.z < MinLocalAssistThreshold)
        {
            return localVelocity.z * MinForwardThrowModifier;
        }
        else if (localVelocity.z < MaxLocalAssistThreshold)
        {
            return AssistedLocalForwardMagnitude(localVelocity);
        }
        else
        {
            return localVelocity.z * MaxForwardThrowModifier;
        }
    }

    float AssistedLocalForwardMagnitude(Vector3 localVelocity)
    {
        // This function was determined through extensive playtesting for best feel, please see: https://www.desmos.com/calculator/m07laliezy)
        return (localVelocity.z / 6.0f) + 6.5f;
    }

    float AdjustLocalHorizontalVelocity(Vector3 localVelocity, float assistedForwardVelocity)
    {
        if (localVelocity.x > HorizontalAdjustThreshold || localVelocity.x < -1 * HorizontalAdjustThreshold)
        {
            return (localVelocity.x * assistedForwardVelocity) / localVelocity.z;
        }
        else
        {
            return localVelocity.x;
        }

    }

    Vector3 ApplyReleaseHeightModifier(Vector3 assistedVelocity)
    {
        float releaseHeightThrowModifier;
        if (currentInteractorAttach.position.y > AverageReleaseHeight) // Above-Average Release Height 
        {
            releaseHeightThrowModifier = 1.0f + ((AverageReleaseHeight - currentInteractorAttach.position.y) * AboveAverageReleaseHeightModifier);
        }
        else // Below-Average Release Height
        {
            releaseHeightThrowModifier = 1.0f + ((AverageReleaseHeight - currentInteractorAttach.position.y) * BelowAverageReleaseHeightModifier);
        }

        return assistedVelocity * releaseHeightThrowModifier;
    }

}