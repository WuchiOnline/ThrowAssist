# Wuchi's Throw Assist

A smoothing algorithm that assists the accuracy and trajectory of an interactable object when thrown at a given target.

Originally built and implemented as the core interaction for [Hooplord](https://wuchi.online/hooplord), a virtual reality experience centered around basketball shooting mechanics.

To dive straight into the code, please see: [Assets/WuchiOnline/Scripts/Wuchi_ThrowAssist.cs](https://github.com/WuchiOnline/ThrowAssist/blob/master/Assets/WuchiOnline/Scripts/Wuchi_ThrowAssist.cs)

![Hooplord Demo](GIF/HooplordThrow.gif)

## Features

- Designed to be fun-to-learn, yet hard-to-master.

	- Significantly lowers required accuracy by redirecting throws within a proximity threshold of the target.
	
	- Maintains high accuracy ceiling that rewards precision and consistency.

- Notably decreased user fatigue and frustration when compared to standard throwing implementations.

- Adjusts for players of all heights and wingspans with a modifier based on release height.

- Multi-factor detection system that guesses if a throw is intended towards the target before applying velocity transformations.

- Refactored to extend the Unity XR Interaction Toolkit, but can be easily adapted for other interaction frameworks.

	- Can be further abstracted and configured for a wide range of desired results.

# Example Project

## Built With

The example project showcases a barebones implementation using primitives, which utilizes:

* Unity 2019.3.2f1
* Oculus XR Plugin 1.3.3
* Unity XR Interaction Toolkit 0.9.4

Oculus XR Plugin and Unity XR Interaction Toolkit are pre-imported into the project.

## How do I open the example project?

1. Clone or download the repo: ```git clone https://github.com/WuchiOnline/ThrowAssist```
2. Open your Unity Hub.
3. Press Add and select the repo's folder location.
4. Open up the example scene, which is located in the WuchiOnline/Scenes folder.
5. This project natively supports all PC-compatible Oculus headsets. To use other PC-compatible headsets with this demo:

	- Open the Package Manager
	- Install the Unity XR Management package
	- Open the Project Settings
	- Under the XR Plugin Management section, install the appropriate XR Plugin and Loader.
	
6. Press Play.

# Author

**Eric Wu** [@WuchiOnline] - [Twitter](https://twitter.com/WuchiOnline) - [GitHub](https://github.com/WuchiOnline) - [LinkedIn](https://www.linkedin.com/in/ericwu90/)

# License

Wuchi's Throw Assist is licensed under the MIT License.
