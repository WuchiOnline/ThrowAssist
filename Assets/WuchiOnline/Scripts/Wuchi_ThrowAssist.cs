using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// A smoothing algorithm that assists the accuracy and trajectory of an interactable object when thrown at a given target.
/// Originally built and implemented as the core interaction for Hooplord, a PC VR game centered around basketball shooting mechanics.
/// </summary>

public class Wuchi_ThrowAssist : XRGrabInteractable // XR Interaction Toolkit uses Hungarian notation, which has been matched here for consistency.
{
    // All member field default values were determined by extensive playtesting for best feel.
    // These can be experimented with and fine-tuned to achieve a wide variety of smoothing effects.

    // If you discover a new smoothing effect and want to save the values as presets that initialize on Start():
    // Create a new Throw Smoothing Presets ScriptableObject instance by right-clicking in your Project window > Create > Throw Smoothing Presets.
    public ThrowSmoothingPresets throwSmoothingPreset;

    // The optimal amount of velocities to poll and utilize when assisting velocity transformations.
    public int m_OptimalPolledVelocityCount = 4;

    // The minimum amount of magnitude a throw must have in either forward and upward directions to be eligible for the smoothing algorithm.
    public float m_ThrowStrengthThreshold = 0.45f;
    // The maximum amount of horizontal inaccuracy a throw must not exceed to be eligible for the smoothing algorithm.
    public float m_NormalizedHorizontalInaccuracyThreshold = 0.2f;
    // The minimum local velocity threshold a throw's magnitude must exceed to be assisted.
    public float m_MinLocalAssistThreshold = 0.5f;
    // The maximum local velocity threshold a throw's magnitude must not exceed to be assisted.
    public float m_MaxLocalAssistThreshold = 5.5f;
    // The minimum amount of horizontal inaccuracy (relative to target object) a throw must have to warrant proportionately adjusting with assisted forward velocity.
    public float m_HorizontalAdjustThreshold = 0.75f;

    // The upward velocity modifier applied when a throw is below the minimum local velocity assist threshold.
    public float m_MinUpwardThrowModifier = 15.0f;
    // The upward velocity modifier applied when a throw is above the maximum local velocity assist threshold.
    public float m_MaxUpwardThrowModifier = 1.54f;
    // The forward velocity modifier applied when a throw is below the minimum local velocity assist threshold.
    public float m_MinForwardThrowModifier = 13.0f;
    // The forward velocity modifier applied when a throw is above the maximum local velocity assist threshold.
    public float m_MaxForwardThrowModifier = 1.36f;

    // The average release height of a throw, used to modify the smoothed velocity for users of all heights and wingspans.
    public float m_AverageReleaseHeight = 2.5f;
    public float m_AboveAverageReleaseHeightModifier = 0.05f;
    public float m_BelowAverageReleaseHeightModifier = 0.06f;

    // Optional strength modifier for unassisted throws.
    public float m_UnassistedThrowVelocityModifier = 1.0f;

    // Retains Interactor's Attach Transform reference for Detach() in LateUpdate().
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
        InitializeThrowSmoothingPresets();
        m_PolledVelocities = new Queue<Vector3>();
        rigToTarget = GameObject.FindWithTag("RigToTarget").transform;
    }

    void InitializeThrowSmoothingPresets()
    {
        if (throwSmoothingPreset != null)
        {
            m_OptimalPolledVelocityCount = throwSmoothingPreset.optimalPolledVelocityCount;
            m_ThrowStrengthThreshold = throwSmoothingPreset.throwStrengthThreshold;
            m_NormalizedHorizontalInaccuracyThreshold = throwSmoothingPreset.normalizedHorizontalInaccuracyThreshold;
            m_MinLocalAssistThreshold = throwSmoothingPreset.minLocalAssistThreshold;
            m_MaxLocalAssistThreshold = throwSmoothingPreset.maxLocalAssistThreshold;
            m_HorizontalAdjustThreshold = throwSmoothingPreset.horizontalAdjustThreshold;
            m_MinUpwardThrowModifier = throwSmoothingPreset.minUpwardThrowModifier;
            m_MaxUpwardThrowModifier = throwSmoothingPreset.maxUpwardThrowModifier;
            m_MinForwardThrowModifier = throwSmoothingPreset.minForwardThrowModifier;
            m_MaxForwardThrowModifier = throwSmoothingPreset.maxForwardThrowModifier;
            m_AverageReleaseHeight = throwSmoothingPreset.averageReleaseHeight;
            m_AboveAverageReleaseHeightModifier = throwSmoothingPreset.aboveAverageReleaseHeightModifier;
            m_BelowAverageReleaseHeightModifier = throwSmoothingPreset.belowAverageReleaseHeightModifier;
            m_UnassistedThrowVelocityModifier = throwSmoothingPreset.unassistedThrowVelocityModifier;
        }
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

            if(m_PolledVelocities.Count >= m_OptimalPolledVelocityCount)
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
            if (m_DetachVelocity.y < m_ThrowStrengthThreshold || // Throw does not meet the minimum vertical throw strength to warrant smoothing.
                rigToTarget.InverseTransformVector(m_DetachVelocity).z < m_ThrowStrengthThreshold || // Throw does not meet the minimum forward throw strength to warrant smoothing. 
                Mathf.Abs(rigToTarget.InverseTransformVector(m_DetachVelocity).normalized.x) > m_NormalizedHorizontalInaccuracyThreshold || // Throw is not horizontally accurate enough to warrant smoothing.
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
        else if (localVelocity.y < m_MinLocalAssistThreshold)
        {
            return localVelocity.y * m_MinUpwardThrowModifier;
        }
        else if (localVelocity.y < m_MaxLocalAssistThreshold)
        {
            return AssistLocalUpwardMagnitude(localVelocity);
        }
        else
        {
            return localVelocity.y * m_MaxUpwardThrowModifier;
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
        else if (localVelocity.z < m_MinLocalAssistThreshold)
        {
            return localVelocity.z * m_MinForwardThrowModifier;
        }
        else if (localVelocity.z < m_MaxLocalAssistThreshold)
        {
            return AssistLocalForwardMagnitude(localVelocity);
        }
        else
        {
            return localVelocity.z * m_MaxForwardThrowModifier;
        }
    }

    float AssistLocalForwardMagnitude(Vector3 localVelocity)
    {
        // This function was determined through extensive playtesting for best feel, please see: https://www.desmos.com/calculator/m07laliezy)
        return (localVelocity.z / 6.0f) + 6.5f;
    }

    float AdjustLocalHorizontalVelocity(Vector3 localVelocity, float assistedForwardVelocity)
    {
        if (localVelocity.x > m_HorizontalAdjustThreshold || localVelocity.x < -1 * m_HorizontalAdjustThreshold)
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
        if (m_CurrentInteractorAttach.position.y > m_AverageReleaseHeight) // Above-Average Release Height 
        {
            releaseHeightThrowModifier = 1.0f + ((m_AverageReleaseHeight - m_CurrentInteractorAttach.position.y) * m_AboveAverageReleaseHeightModifier);
        }
        else // Below-Average Release Height
        {
            releaseHeightThrowModifier = 1.0f + ((m_AverageReleaseHeight - m_CurrentInteractorAttach.position.y) * m_BelowAverageReleaseHeightModifier);
        }

        return assistedVelocity * releaseHeightThrowModifier;
    }

}
