using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class LowerHandleSqueeze : MonoBehaviour
{
    public Transform lowerHand;
    public float squeezeAngle = 35f;
    public float speed = 5f;

    private Quaternion initialRot;
    private Quaternion squeezedRot;

    private InputDevice leftHandDevice;

    void Start()
    {
        initialRot = lowerHand.localRotation;
        squeezedRot = initialRot * Quaternion.Euler(0, 0, squeezeAngle);
        InitDevices();
    }

    void InitDevices()
    {
        var leftDevices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftDevices);
        if (leftDevices.Count > 0)
            leftHandDevice = leftDevices[0];
    }

    void Update()
    {
        if (!leftHandDevice.isValid)
            InitDevices();

        bool isSqueezing = false;

        // Check VR left hand grip
        if (leftHandDevice.TryGetFeatureValue(CommonUsages.gripButton, out bool leftGripPressed) && leftGripPressed)
        {
            isSqueezing = true;
        }

        // Check keyboard fallback
        if (Input.GetKey(KeyCode.LeftShift)) // or try KeyCode.Q
        {
            isSqueezing = true;
        }

        // Apply rotation
        Quaternion targetRot = isSqueezing ? squeezedRot : initialRot;

        lowerHand.localRotation = Quaternion.Lerp(
            lowerHand.localRotation,
            targetRot,
            Time.deltaTime * speed
        );
    }
}