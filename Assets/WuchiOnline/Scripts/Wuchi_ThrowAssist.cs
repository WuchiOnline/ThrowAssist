using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Wuchi_ThrowAssist : XRGrabInteractable
{

    // Backlog 5-01-20:
    // 1. Do an overall clean up refactor
    // 2. Improve comments
    // 3. Put together Github readme.

    // Magic Numbers: all constants were determined by extensive playtesting for best feel.
    // These can be adjusted and abstracted to achieve different throwing trajectories.

    // The optimal amount of velocities to poll and utilize when assisting velocity transformations.
    const int OptimalPolledVelocityCount = 4;
    // The minimum amount of strength a throw must have in both forward and upward velocities to be eligable for the assist algorithm.
    const float ThrowStrengthAssistThreshold = 0.45f;
    // The maximum amount of horizontal inaccuracy a throw must not exceed to be eligable for the assist algorithm.
    const float NormalizedHorizontalInaccuracyThreshold = 0.2f;
    // The minimum local velocity threshold a throw's magnitude must exceed to be eligable for the assist algorithm.
    const float MinLocalAssistThreshold = 0.5f;
    // The maximum local velocity threshold a throw's magnitude must not exceed to be eligable for the assist algorithm.
    const float MaxLocalAssistThreshold = 5.5f;
    // The minimum amount of horizontal accuracy (relative to target object) an assisted throw must have before scaling forward velocity to be proportionate to an assisted horizontal velocity.
    const float HorizontalAssistThreshold = 0.75f;
    // The upward velocity modifier applied when a throw is below the minimum local velocity assist threshold.
    const float MinUpwardThrowModifier = 15.0f;
    // The upward velocity modifier applied when a throw is above the maximum local velocity assist threshold.
    const float MaxUpwardThrowModifier = 1.54f;
    // The forward velocity modifier applied when a throw is below the minimum local velocity assist threshold.
    const float MinForwardThrowModifier = 13.0f;
    // The forward velocity modifier applied when a throw is above the maximum local velocity assist threshold.
    const float MaxForwardThrowModifier = 1.36f;
    // The average release height of a throw, which affects the release height modifier calculations.
    const float AverageReleaseHeight = 2.5f;


    // Target to assist the user in throwing the object towards.
    public Transform target;
    // Transform of an empty GameObject childed to XR Rig, which can be rotated to face the target on only the Y-axis. Used for local transformations.
    public Transform rigToTarget;
    // Modifier for velocity magnitudes that are unassisted.
    public float unassistedThrowVelocityModifier = 2.0f;

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

            // Debug.Log("Velocity has been added to polledVelocities: " + velocityToPoll);
        }

    }

    protected override void Detach() // Throw velocity upon detach (ungrab).
    {
        if (m_ThrowOnDetach)
        {
            UpdateRigToTargetRotation();

            // Evaluate if throw is strong and accurate enough to warrant applying the throw assist algorithm.
            if (m_DetachVelocity.y < ThrowStrengthAssistThreshold || // Throw does not meet the minimum vertical throw strength to trigger assisted throw.
                rigToTarget.InverseTransformVector(m_DetachVelocity).z < ThrowStrengthAssistThreshold || // Throw does not meet the minimum forward throw strength to trigger assisted throw. 
                Mathf.Abs(rigToTarget.InverseTransformVector(m_DetachVelocity).normalized.x) > NormalizedHorizontalInaccuracyThreshold || // Throw is not within horizontal accuracy tolerance to warrant assisting velocity.
                polledVelocities.Count < 1) // Object's velocity was not polled for at least a single frame.
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

        Debug.Log("Detached.");
    }

    void UpdateRigToTargetRotation()
    {
        Vector3 direction = (target.position - rigToTarget.position).normalized;

        // create the rotation we need to be in to look at the target
        Quaternion lookAtRotation = Quaternion.LookRotation(direction);

        Quaternion lookAtRotation_onlyY = Quaternion.Euler(rigToTarget.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, rigToTarget.rotation.eulerAngles.z);

        rigToTarget.rotation = lookAtRotation_onlyY;
    }

    Vector3 DetermineAssistedThrowVelocity()
    {
        Vector3 baseThrowVelocity = DetermineHighestUpwardVelocityFromPolledList();

        // we need to convert the velocity from world space to local before adjusting.
        Vector3 localizedThrowVelocity = rigToTarget.InverseTransformVector(baseThrowVelocity);

        float assistedUpwardVelocity = AssistLocalUpwardVelocity(localizedThrowVelocity);
        float assistedForwardVelocity = AssistLocalForwardVelocity(localizedThrowVelocity);
        float adjustedHorizontalVelocity = AdjustLocalHorizontalVelocity(localizedThrowVelocity, assistedForwardVelocity);

        Vector3 localAssistedThrowVelocity = new Vector3(adjustedHorizontalVelocity, assistedUpwardVelocity, assistedForwardVelocity);

        // convert back to world space.
        Vector3 worldAssistedThrowVelocity = rigToTarget.TransformVector(localAssistedThrowVelocity);

        Vector3 finalAssistedThrowVelocity = ApplyReleaseHeightModifier(worldAssistedThrowVelocity);

        polledVelocities.Clear();

        return finalAssistedThrowVelocity;
    }

    Vector3 ApplyReleaseHeightModifier(Vector3 assistedVelocity)
    {
        float releaseHeightThrowModifier;

        if (currentInteractorAttach.position.y > AverageReleaseHeight) // Above-Average Release Height 
        {
            releaseHeightThrowModifier = 1.0f + ((AverageReleaseHeight - currentInteractorAttach.position.y) * 0.05f); // This function was determined through extensive playtesting for best feel 
        }
        else // Below-Average Release Height
        {
            releaseHeightThrowModifier = 1.0f + ((AverageReleaseHeight - currentInteractorAttach.position.y) * 0.06f); // This function was determined through extensive playtesting for best feel
        }

        return assistedVelocity * releaseHeightThrowModifier;
    }

    Vector3 DetermineHighestUpwardVelocityFromPolledList()
    {

        Vector3 highestUpwardVelocity = polledVelocities.OrderBy(velocity => velocity.y).Last();

        Debug.Log("Highest Upward Velocity From Polled List is: " + highestUpwardVelocity);

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
        // This function was determined through extensive playtesting for best feel, please see: https://www.desmos.com/calculator/clrzceq5ch
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
        // If throw is not within horizontal threshold of target object, then adjust accordingly to make throw more accurate.
        if (localVelocity.x > HorizontalAssistThreshold || localVelocity.x < -1 * HorizontalAssistThreshold)
        {
            return (localVelocity.x * assistedForwardVelocity) / localVelocity.z;
        }
        else
        {
            return localVelocity.x;
        }

    }

}