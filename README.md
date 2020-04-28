# Wuchi-ThrowAssist
 Wuchi's Throw Assist Algorithm
 
 Objective: Abstract the custom throwing assist algorithm used in Hooplord, then adapt for Unity's XR Interaction Toolkit.

1. Make sure Oculus XR Plugin and Loader is installed in your project.


Notes:

XRGrabInteractable class (XR Interaction Toolkit preview version 0.9.4) keeps the following members as private:

- m_RigidBody
- m_DetachVelocity
- m_DetachAngularVelocity
- Detach()

These have been set to protected internal so that they can be accessed in the Wuchi-ThrowAssist subclass that derives from XRGrabInteractable.
