using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Wuchi_ThrowAssist : XRGrabInteractable
{

    // Backlog 4-30-20:
    // 1. Need to refactor code so that localizedVelocity is accounted for, instead of everything using base velocity.
    // 2. Need to set threshold so that ball does not spike to the side on certain throws.
    // 3. Do an overall clean up refactor
    // 4. Clarify that constants can be set to public fields to abstract for any application.

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


    public Transform rigToTarget;
    public Transform target;

    Transform currentInteractorAttach;

    public float unassistedThrowVelocityModifier;

    bool isInteractorVelocityPollingActive;

    public Queue<Vector3> polledVelocities;

    private Vector3 baseThrowVelocity;

    public void Start()
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
        Debug.Log("Grabbed.");

    }

    protected override void OnSelectExit(XRBaseInteractor interactor)
    {
        currentInteractorAttach = interactor.attachTransform;
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
            UpdateRigToTargetRotation();

            if (m_DetachVelocity.y < ThrowStrengthAssistThreshold || rigToTarget.InverseTransformVector(m_DetachVelocity).z < ThrowStrengthAssistThreshold || polledVelocities.Count < 1) // Does not meet the minimum throw strength to trigger assisted throw.
            {
                m_RigidBody.velocity = m_DetachVelocity;
                m_RigidBody.angularVelocity = m_DetachAngularVelocity;
            }

            else
            {
                m_RigidBody.velocity = DetermineAssistedThrowVelocity();
                // m_RigidBody.velocity = m_DetachVelocity; // temporary until you finish refactoring DetermineAssistedThrowVelocity();
                m_RigidBody.angularVelocity = m_DetachAngularVelocity;
            }

        }

        Debug.Log("Detached.");
    }


    private Vector3 DetermineAssistedThrowVelocity()
    {
        baseThrowVelocity = DetermineHighestUpwardVelocityFromPolledList();

        // we need to convert the velocity from world space to local before adjusting.
        baseThrowVelocity = rigToTarget.InverseTransformVector(baseThrowVelocity);

        float assistedUpwardVelocity = AssistBaseUpwardVelocity();
        float assistedForwardVelocity = AssistBaseForwardVelocity();
        float adjustedHorizontalVelocity = AdjustBaseHorizontalVelocity(assistedForwardVelocity);

        Vector3 newAssistedThrowVelocity = new Vector3(adjustedHorizontalVelocity, assistedUpwardVelocity, assistedForwardVelocity);

        // convert back to world space.
        Vector3 transformedAssistedThrowVelocity = rigToTarget.TransformVector(newAssistedThrowVelocity);

        float releaseHeightThrowModifier;

        if (currentInteractorAttach.position.y > AverageReleaseHeight) // Above-Average Release Height 
        {
            releaseHeightThrowModifier = 1.0f + ((AverageReleaseHeight - currentInteractorAttach.position.y) * 0.05f); // This function was determined through extensive playtesting for best feel 
        }
        else // Below-Average Release Height
        {
            releaseHeightThrowModifier = 1.0f + ((AverageReleaseHeight - currentInteractorAttach.position.y) * 0.06f); // This function was determined through extensive playtesting for best feel
        }

        Vector3 finalAssistedThrowVelocity = transformedAssistedThrowVelocity * releaseHeightThrowModifier;

        polledVelocities.Clear();

        return finalAssistedThrowVelocity;
    }

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

    void UpdateRigToTargetRotation()
    {
        Vector3 direction = (target.position - rigToTarget.position).normalized;

        // create the rotation we need to be in to look at the target
        Quaternion lookAtRotation = Quaternion.LookRotation(direction);

        Quaternion lookAtRotation_onlyY = Quaternion.Euler(rigToTarget.rotation.eulerAngles.x, lookAtRotation.eulerAngles.y, rigToTarget.rotation.eulerAngles.z);

        rigToTarget.rotation = lookAtRotation_onlyY;
    }
}