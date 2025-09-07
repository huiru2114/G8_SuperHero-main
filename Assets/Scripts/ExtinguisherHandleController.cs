using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
using System.Collections.Generic;

public class ExtinguisherHandleController : MonoBehaviour
{
    [Header("Handle References")]
    public Transform lowerHandle; // The lower handle that gets squeezed
    public XRGrabInteractable handleGrabInteractable; // Grab interactable for the handle
   
    [Header("Particle System")]
    public ParticleSystem co2Particles; // CO2 particle system
    public Transform nozzleTip; // Empty GameObject positioned at hose nozzle tip
   
    [Header("Handle Settings")]
    public float squeezeAngle = 35f; // Maximum rotation angle for the handle squeeze
    public float pressThreshold = 0.8f; // Percentage of max squeeze to trigger discharge (0.8 = 80%)
    public float returnSpeed = 5f; // Speed at which handle returns to original position
   
    [Header("Input Settings")]
    public KeyCode keyboardTrigger = KeyCode.Space; // Keyboard key for testing
    public KeyCode leftHandTrigger = KeyCode.LeftShift; // Keyboard key for left hand simulation
    public bool enableKeyboardInput = true; // Toggle to disable keyboard input in VR mode
   
    [Header("XR Settings")]
    public XRGrabInteractable extinguisherGrabInteractable; // Reference to the main extinguisher grab
   
    [Header("Audio Settings")]
    [SerializeField]
    private AudioClip dischargeSound; // Audio clip for CO2 discharge sound
    private AudioSource audioSource; // Audio source for playing discharge sound
   
    // Private variables
    private Quaternion originalLowerHandleRotation;
    private Quaternion squeezedRotation;
    private float currentSqueezePercentage = 0f;
    private bool isPressed = false;
    private bool isDischarging = false;
    private bool handleGrabbed = false;
    private XRDirectInteractor handleInteractor;
   
    void Start()
    {
        // Store the original rotation of the lower handle
        if (lowerHandle != null)
        {
            originalLowerHandleRotation = lowerHandle.localRotation;
            squeezedRotation = originalLowerHandleRotation * Quaternion.Euler(0, 0, squeezeAngle);
        }
       
        // Make sure particles are stopped initially
        if (co2Particles != null)
        {
            co2Particles.Stop();
        }
       
        // Set up audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true;
       
        // Set up handle grab events
        if (handleGrabInteractable != null)
        {
            handleGrabInteractable.selectEntered.AddListener(OnHandleGrabbed);
            handleGrabInteractable.selectExited.AddListener(OnHandleReleased);
        }
    }
   
    void Update()
    {
        HandleInput();
        UpdateHandleRotation();
        UpdateParticlePosition();
        CheckDischarge();
    }
   
    void HandleInput()
    {
        bool shouldPress = false;
       
        // Keyboard input (always works for testing)
        if (enableKeyboardInput)
        {
            if (Input.GetKey(keyboardTrigger) || Input.GetKey(leftHandTrigger))
            {
                shouldPress = true;
            }
        }
       
        // VR input - check right-hand controller buttons at any time
        InputDevice rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHandDevice.isValid)
        {
            bool aButtonPressed = false;
            bool bButtonPressed = false;
            rightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out aButtonPressed); // A button
            rightHandDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bButtonPressed); // B button
            if (aButtonPressed || bButtonPressed)
            {
                shouldPress = true;
            }
        }
       
        // Update handle state
        if (shouldPress)
        {
            PressHandle();
        }
        else
        {
            ReleaseHandle();
        }
    }
   
    void UpdateParticlePosition()
    {
        if (co2Particles != null && nozzleTip != null)
        {
            co2Particles.transform.position = nozzleTip.position;
        }
    }
   
    void PressHandle()
    {
        isPressed = true;
        currentSqueezePercentage = Mathf.Min(currentSqueezePercentage + Time.deltaTime * returnSpeed, 1f);
    }
   
    void ReleaseHandle()
    {
        isPressed = false;
        currentSqueezePercentage = Mathf.Max(currentSqueezePercentage - Time.deltaTime * returnSpeed, 0f);
    }
   
    void UpdateHandleRotation()
    {
        if (lowerHandle != null)
        {
            Quaternion targetRotation = Quaternion.Lerp(originalLowerHandleRotation, squeezedRotation, currentSqueezePercentage);
            lowerHandle.localRotation = Quaternion.Lerp(lowerHandle.localRotation, targetRotation, Time.deltaTime * returnSpeed);
        }
    }
   
    void CheckDischarge()
    {
        bool shouldDischarge = currentSqueezePercentage >= pressThreshold;
       
        if (shouldDischarge && !isDischarging)
        {
            StartDischarge();
        }
        else if (!shouldDischarge && isDischarging)
        {
            StopDischarge();
        }
    }
   
    void StartDischarge()
    {
        if (co2Particles != null)
        {
            isDischarging = true;
            co2Particles.Play();
            if (dischargeSound != null && audioSource != null)
            {
                audioSource.clip = dischargeSound;
                audioSource.Play();
            }
            Debug.Log("CO2 discharge started!");
        }
    }
   
    void StopDischarge()
    {
        if (co2Particles != null)
        {
            isDischarging = false;
            co2Particles.Stop();
            if (audioSource != null)
            {
                audioSource.Stop();
            }
            Debug.Log("CO2 discharge stopped!");
        }
    }
   
    void OnHandleGrabbed(SelectEnterEventArgs args)
    {
        handleInteractor = args.interactorObject as XRDirectInteractor;
        handleGrabbed = true;
        Debug.Log("Handle grabbed - press A or B button on right controller to squeeze!");
    }
   
    void OnHandleReleased(SelectExitEventArgs args)
    {
        handleInteractor = null;
        handleGrabbed = false;
        Debug.Log("Handle released");
    }
   
    // Public methods
    public bool IsHandlePressed()
    {
        return isPressed;
    }
   
    public bool IsDischarging()
    {
        return isDischarging;
    }
   
    public float GetSqueezePercentage()
    {
        return currentSqueezePercentage;
    }
   
    public bool IsHandleGrabbed()
    {
        return handleGrabbed;
    }
   
    void OnDestroy()
    {
        if (handleGrabInteractable != null)
        {
            handleGrabInteractable.selectEntered.RemoveListener(OnHandleGrabbed);
            handleGrabInteractable.selectExited.RemoveListener(OnHandleReleased);
        }
    }
}