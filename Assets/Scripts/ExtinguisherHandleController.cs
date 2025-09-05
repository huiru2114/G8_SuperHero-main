using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
using System.Collections.Generic;

public class ExtinguisherHandleController : MonoBehaviour
{
    [Header("Handle References")]
    public Transform lowerHandle; // The lower handle that gets squeezed
    
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
    private XRDirectInteractor currentInteractor;
    private InputDevice leftHandDevice;
    private InputDevice rightHandDevice;
    
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
        audioSource.loop = true; // Loop the discharge sound while active
        
        // Set up XR grab events if extinguisher is grabbable
        if (extinguisherGrabInteractable != null)
        {
            extinguisherGrabInteractable.selectEntered.AddListener(OnExtinguisherGrabbed);
            extinguisherGrabInteractable.selectExited.AddListener(OnExtinguisherReleased);
        }
        
        // Initialize VR devices
        InitializeVRDevices();
    }
    
    void InitializeVRDevices()
    {
        var leftDevices = new List<InputDevice>();
        var rightDevices = new List<InputDevice>();
        
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightDevices);
        
        if (leftDevices.Count > 0)
            leftHandDevice = leftDevices[0];
            
        if (rightDevices.Count > 0)
            rightHandDevice = rightDevices[0];
    }
    
    void Update()
    {
        // Reinitialize devices if they become invalid
        if (!leftHandDevice.isValid || !rightHandDevice.isValid)
            InitializeVRDevices();
            
        HandleKeyboardInput();
        HandleVRInput();
        UpdateHandleRotation();
        UpdateParticlePosition();
        CheckDischarge();
    }
    
    void HandleKeyboardInput()
    {
        // Only handle keyboard input if enabled
        if (!enableKeyboardInput) return;
        
        // Check both keyboard triggers
        bool keyboardPressed = Input.GetKey(keyboardTrigger) || Input.GetKey(leftHandTrigger);
        
        if (keyboardPressed)
        {
            PressHandle();
        }
        else
        {
            ReleaseHandle();
        }
    }
    
    void HandleVRInput()
    {
        // Only process VR input if someone is holding the extinguisher
        if (currentInteractor != null)
        {
            bool triggerPressed = false;
            
            // Try to get trigger input from the current interactor's controller
            XRController controller = currentInteractor.GetComponent<XRController>();
            if (controller != null)
            {
                if (controller.inputDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
                {
                    triggerPressed = triggerValue > 0.5f;
                }
            }
            
            // Also check for grip button on left hand (for two-handed operation)
            bool leftGripPressed = false;
            if (leftHandDevice.isValid)
            {
                leftHandDevice.TryGetFeatureValue(CommonUsages.gripButton, out leftGripPressed);
            }
            
            // Also check for grip button on right hand
            bool rightGripPressed = false;
            if (rightHandDevice.isValid)
            {
                rightHandDevice.TryGetFeatureValue(CommonUsages.gripButton, out rightGripPressed);
            }
            
            // Handle is considered pressed if trigger OR either grip is pressed
            if (triggerPressed || leftGripPressed || rightGripPressed)
            {
                PressHandle();
            }
            else
            {
                ReleaseHandle();
            }
        }
    }
    
    void UpdateParticlePosition()
    {
        // Always keep particles at the nozzle tip
        if (co2Particles != null && nozzleTip != null)
        {
            // Update particle system position to match nozzle tip
            co2Particles.transform.position = nozzleTip.position;
            // co2Particles.transform.rotation = nozzleTip.rotation;
        }
    }
    
    void PressHandle()
    {
        isPressed = true;
        // Increase squeeze percentage up to maximum
        currentSqueezePercentage = Mathf.Min(currentSqueezePercentage + Time.deltaTime * returnSpeed, 1f);
    }
    
    void ReleaseHandle()
    {
        isPressed = false;
        // Decrease squeeze percentage back to 0
        currentSqueezePercentage = Mathf.Max(currentSqueezePercentage - Time.deltaTime * returnSpeed, 0f);
    }
    
    void UpdateHandleRotation()
    {
        if (lowerHandle != null)
        {
            // Interpolate between original and squeezed rotation based on squeeze percentage
            Quaternion targetRotation = Quaternion.Lerp(originalLowerHandleRotation, squeezedRotation, currentSqueezePercentage);
            lowerHandle.localRotation = Quaternion.Lerp(lowerHandle.localRotation, targetRotation, Time.deltaTime * returnSpeed);
        }
    }
    
    void CheckDischarge()
    {
        // Check if handle is squeezed enough to trigger discharge
        bool shouldDischarge = currentSqueezePercentage >= pressThreshold;
        
        // Start or stop particle discharge
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
    
    // Called when extinguisher is grabbed in VR
    void OnExtinguisherGrabbed(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject as XRDirectInteractor;
        Debug.Log("Extinguisher grabbed - handle controls activated");
    }
    
    // Called when extinguisher is released in VR
    void OnExtinguisherReleased(SelectExitEventArgs args)
    {
        currentInteractor = null;
        ReleaseHandle(); // Auto-release handle when extinguisher is dropped
        Debug.Log("Extinguisher released - handle controls deactivated");
    }
    
    // Public methods for external scripts to control
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
    
    public float GetPressPercentage()
    {
        return currentSqueezePercentage; // Alias for backwards compatibility
    }
    
    // Method to manually trigger discharge (for testing or other scripts)
    public void ManualDischarge(bool discharge)
    {
        if (discharge)
        {
            StartDischarge();
        }
        else
        {
            StopDischarge();
        }
    }
    
    void OnDestroy()
    {
        // Clean up event listeners
        if (extinguisherGrabInteractable != null)
        {
            extinguisherGrabInteractable.selectEntered.RemoveListener(OnExtinguisherGrabbed);
            extinguisherGrabInteractable.selectExited.RemoveListener(OnExtinguisherReleased);
        }
    }
}