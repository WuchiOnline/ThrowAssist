using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Presets", menuName = "Throw Smoothing Presets")]
public class ThrowSmoothingPresets : ScriptableObject
{
    // The optimal amount of velocities to poll and utilize when assisting velocity transformations.
    public int optimalPolledVelocityCount;

    // The minimum amount of magnitude a throw must have in either forward and upward directions to be eligible for the smoothing algorithm.
    public float throwStrengthThreshold;
    // The maximum amount of horizontal inaccuracy a throw must not exceed to be eligible for the smoothing algorithm.
    public float normalizedHorizontalInaccuracyThreshold;
    // The minimum local velocity threshold a throw's magnitude must exceed to be assisted.
    public float minLocalAssistThreshold;
    // The maximum local velocity threshold a throw's magnitude must not exceed to be assisted.
    public float maxLocalAssistThreshold;
    // The minimum amount of horizontal inaccuracy (relative to target object) a throw must have to warrant proportionately adjusting with assisted forward velocity.
    public float horizontalAdjustThreshold;

    // The upward velocity modifier applied when a throw is below the minimum local velocity assist threshold.
    public float minUpwardThrowModifier;
    // The upward velocity modifier applied when a throw is above the maximum local velocity assist threshold.
    public float maxUpwardThrowModifier;
    // The forward velocity modifier applied when a throw is below the minimum local velocity assist threshold.
    public float minForwardThrowModifier;
    // The forward velocity modifier applied when a throw is above the maximum local velocity assist threshold.
    public float maxForwardThrowModifier;

    // The average release height of a throw, used to modify the smoothed velocity for users of all heights and wingspans.
    public float averageReleaseHeight;
    public float aboveAverageReleaseHeightModifier;
    public float belowAverageReleaseHeightModifier;

    // Optional strength modifier for unassisted throws.
    public float unassistedThrowVelocityModifier;
}
