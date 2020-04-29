using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Wuchi_ThrowAssist : XRGrabInteractable
{
    public int pollsToProcess = 3;
    public float ThrowStrengthAssistThreshold = 0.45f;

    bool isInteractorVelocityPollingActive;

    public Queue<Vector3> polledVelocities;

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

            if(polledVelocities.Count >= pollsToProcess)
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
                // m_RigidBody.velocity = DetermineAssistedThrowVelocity();
                m_RigidBody.velocity = m_DetachVelocity; // temporary until you finish refactoring DetermineAssistedThrowVelocity();
                m_RigidBody.angularVelocity = m_DetachAngularVelocity;
            }

        }

        Debug.Log("Detached.");
    }

    private Vector3 DetermineHighestUpwardVelocityFromPolledList()
    {

        Vector3 highestUpwardVelocity = polledVelocities.OrderBy(velocity => velocity.y).Last();

        Debug.Log("Highest Upward Velocity From Polled List is: " + highestUpwardVelocity);

        return highestUpwardVelocity;

    }
}