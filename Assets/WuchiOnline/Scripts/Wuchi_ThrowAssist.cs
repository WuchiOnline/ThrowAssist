using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// A smoothing algorithm that assists the accuracy and trajectory of an interactable object when thrown at a given target.
/// Originally built and implemented as the core interaction for Hooplord, a PC VR game centered around basketball shooting mechanics.
/// </summary>

public class Wuchi_ThrowAssist : XRGrabInteractable
{
    // The Magic Numbers: All constants were determined by extensive playtesting for best feel.
    // These can be abstracted and fine-tuned to achieve a wide variety of smoothing results.

    // The optimal amount of velocities to poll and utilize when assisting velocity transformations.
    const int k_OptimalPolledVelocityCount = 4;

    // The minimum amount of magnitude a throw must have in either forward and upward directions to be eligible for the smoothing algorithm.
    const float k_ThrowStrengthThreshold = 0.45f;
    // The maximum amount of horizontal inaccuracy a throw must not exceed to be eligible for the smoothing algorithm.
    const float k_NormalizedHorizontalInaccuracyThreshold = 0.2f;
    // The minimum local velocity threshold a throw's magnitude must exceed to be assisted.
    const float k_MinLocalAssistThreshold = 0.5f;
    // The maximum local velocity threshold a throw's magnitude must not exceed to be assisted.
    const float k_MaxLocalAssistThreshold = 5.5f;
    // The minimum amount of horizontal inaccuracy (relative to target object) a throw must have to warrant proportionately adjusting with assisted forward velocity.
    const float k_HorizontalAdjustThreshold = 0.75f;

    // The upward velocity modifier applied when a throw is below the minimum local velocity assist threshold.
    const float k_MinUpwardThrowModifier = 15.0f;
    // The upward velocity modifier applied when a throw is above the maximum local velocity assist threshold.
    const float k_MaxUpwardThrowModifier = 1.54f;
    // The forward velocity modifier applied when a throw is below the minimum local velocity assist threshold.
    const float k_MinForwardThrowModifier = 13.0f;
    // The forward velocity modifier applied when a throw is above the maximum local velocity assist threshold.
    const float k_MaxForwardThrowModifier = 1.36f;

    // The average release height of a throw, used to modify the smoothed velocity for users of all heights and wingspans.
    const float k_AverageReleaseHeight = 2.5f;
    const float k_AboveAverageReleaseHeightModifier = 0.05f;
    const float k_BelowAverageReleaseHeightModifier = 0.06f;

    // Optional strength modifier for unassisted throws.
    [SerializeField]
    float m_UnassistedThrowVelocityModifier = 1.0f;

    // Retains Interactor's Attach Transform reference for Detach() in LateUpdate.
    Transform m_CurrentInteractorAttach;

    // Store interactor velocities while selecting object to utilize in smoothing algorithm.
    bool m_ShouldPollVelocity;
    Queue<Vector3> m_PolledVelocities;

    // Target to assist the user in throwing the object towards.
    public Transform target;

    // Transform of an empty GameObject childed to XR Rig, which can be rotated to face the target on only the Y-axis. Used for transformations.
    public Transform rigToTarget;

    void Start()
    {
        m_PolledVelocities = new Queue<Vector3>();
        rigToTarget = GameObject.FindWithTag("RigToTarget").transform;
    }

    protected override void OnSelectEnter(XRBaseInteractor interactor)
    {
        if (!interactor)
            return;
        base.OnSelectEnter(interactor);
        m_ShouldPollVelocity = true;
    }

    protected override void OnSelectExit(XRBaseInteractor interactor)
    {
        m_CurrentInteractorAttach = interactor.attachTransform;
        base.OnSelectExit(interactor);
        m_ShouldPollVelocity = false;
    }

    void Update()
    {
        if (m_ShouldPollVelocity)
        {
            Vector3 smoothedVelocity = getSmoothedVelocityValue(m_ThrowSmoothingVelocityFrames);
            Vector3 velocityToPoll = smoothedVelocity * m_ThrowVelocityScale;

            if(m_PolledVelocities.Count >= k_OptimalPolledVelocityCount)
            {
                m_PolledVelocities.Dequeue();
            }

            m_PolledVelocities.Enqueue(velocityToPoll);
        }
    }

    protected override void Detach()
    {
        if (m_ThrowOnDetach)
        {
            UpdateRigToTargetRotation();

            // Evaluate if throw is both strong and accurate enough to warrant applying the smoothing algorithm.
            if (m_DetachVelocity.y < k_ThrowStrengthThreshold || // Throw does not meet the minimum vertical throw strength to warrant smoothing.
                rigToTarget.InverseTransformVector(m_DetachVelocity).z < k_ThrowStrengthThreshold || // Throw does not meet the minimum forward throw strength to warrant smoothing. 
                Mathf.Abs(rigToTarget.InverseTransformVector(m_DetachVelocity).normalized.x) > k_NormalizedHorizontalInaccuracyThreshold || // Throw is not horizontally accurate enough to warrant smoothing.
                m_PolledVelocities.Count < 1) // Object's velocity was not polled for at least a single frame before release.
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

        m_PolledVelocities.Clear();

        return finalAssistedThrowVelocity;
    }

    Vector3 DetermineHighestUpwardVelocityFromPolledList()
    {
        Vector3 highestUpwardVelocity = m_PolledVelocities.OrderBy(velocity => velocity.y).Last();

        return highestUpwardVelocity;
    }

    float AssistLocalUpwardVelocity(Vector3 localVelocity)
    {
        if (localVelocity.y <= 0f)
        {
            return localVelocity.y * m_UnassistedThrowVelocityModifier;
        }
        else if (localVelocity.y < k_MinLocalAssistThreshold)
        {
            return localVelocity.y * k_MinUpwardThrowModifier;
        }
        else if (localVelocity.y < k_MaxLocalAssistThreshold)
        {
            return AssistLocalUpwardMagnitude(localVelocity);
        }
        else
        {
            return localVelocity.y * k_MaxUpwardThrowModifier;
        }
    }

    float AssistLocalUpwardMagnitude(Vector3 localVelocity)
    {
        // This function was determined through extensive playtesting for best feel
        return (localVelocity.y / 6.0f) + 7.5f;
    }

    float AssistLocalForwardVelocity(Vector3 localVelocity)
    {
        if (localVelocity.z <= 0f)
        {
            return localVelocity.z * m_UnassistedThrowVelocityModifier;
        }
        else if (localVelocity.z < k_MinLocalAssistThreshold)
        {
            return localVelocity.z * k_MinForwardThrowModifier;
        }
        else if (localVelocity.z < k_MaxLocalAssistThreshold)
        {
            return AssistLocalForwardMagnitude(localVelocity);
        }
        else
        {
            return localVelocity.z * k_MaxForwardThrowModifier;
        }
    }

    float AssistLocalForwardMagnitude(Vector3 localVelocity)
    {
        // This function was determined through extensive playtesting for best feel, please see: https://www.desmos.com/calculator/m07laliezy)
        return (localVelocity.z / 6.0f) + 6.5f;
    }

    float AdjustLocalHorizontalVelocity(Vector3 localVelocity, float assistedForwardVelocity)
    {
        if (localVelocity.x > k_HorizontalAdjustThreshold || localVelocity.x < -1 * k_HorizontalAdjustThreshold)
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
        if (m_CurrentInteractorAttach.position.y > k_AverageReleaseHeight) // Above-Average Release Height 
        {
            releaseHeightThrowModifier = 1.0f + ((k_AverageReleaseHeight - m_CurrentInteractorAttach.position.y) * k_AboveAverageReleaseHeightModifier);
        }
        else // Below-Average Release Height
        {
            releaseHeightThrowModifier = 1.0f + ((k_AverageReleaseHeight - m_CurrentInteractorAttach.position.y) * k_BelowAverageReleaseHeightModifier);
        }

        return assistedVelocity * releaseHeightThrowModifier;
    }

}