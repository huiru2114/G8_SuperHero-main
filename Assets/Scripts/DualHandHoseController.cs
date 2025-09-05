using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DualHandHoseController : MonoBehaviour
{
    [Header("References")]
    public XRGrabInteractable fireExtinguisherGrab;
    public XRGrabInteractable hoseGrab;
    public Transform hoseTransform;
    
    [Header("Hose Settings")]
    public float bendSpeed = 5f;
    public float targetBendAngle = 180f; // 180 degrees when both grabbed
    public float restAngle = 0f; // Straight when not dual-grabbed
    
    private ConfigurableJoint hoseJoint;
    private Rigidbody hoseRigidbody;
    private bool isFireExtinguisherGrabbed = false;
    private bool isHoseGrabbed = false;
    private bool isDualGrabbed = false;
    
    void Start()
    {
        // Get components
        hoseJoint = hoseTransform.GetComponent<ConfigurableJoint>();
        hoseRigidbody = hoseTransform.GetComponent<Rigidbody>();
        
        // Set up event listeners
        if (fireExtinguisherGrab != null)
        {
            fireExtinguisherGrab.selectEntered.AddListener(OnFireExtinguisherGrabbed);
            fireExtinguisherGrab.selectExited.AddListener(OnFireExtinguisherReleased);
        }
        
        if (hoseGrab != null)
        {
            hoseGrab.selectEntered.AddListener(OnHoseGrabbed);
            hoseGrab.selectExited.AddListener(OnHoseReleased);
        }
        
        // Initialize joint settings
        SetupFlexibleJoint();
    }
    
    void SetupFlexibleJoint()
    {
        if (hoseJoint == null) return;
        
        // Make joint unbreakable
        hoseJoint.breakForce = Mathf.Infinity;
        hoseJoint.breakTorque = Mathf.Infinity;
        
        // Allow rotation on X-axis for bending
        hoseJoint.xMotion = ConfigurableJointMotion.Locked;
        hoseJoint.yMotion = ConfigurableJointMotion.Locked;
        hoseJoint.zMotion = ConfigurableJointMotion.Locked;
        hoseJoint.angularXMotion = ConfigurableJointMotion.Free; // Allow full rotation
        hoseJoint.angularYMotion = ConfigurableJointMotion.Locked;
        hoseJoint.angularZMotion = ConfigurableJointMotion.Locked;
        
        // Set up angular drive for smooth movement
        JointDrive angularDrive = new JointDrive();
        angularDrive.positionSpring = 2000f;
        angularDrive.positionDamper = 100f;
        angularDrive.maximumForce = Mathf.Infinity;
        hoseJoint.angularXDrive = angularDrive;
        
        // Optimize rigidbody
        if (hoseRigidbody != null)
        {
            hoseRigidbody.useGravity = false;
            hoseRigidbody.mass = 0.1f;
            hoseRigidbody.drag = 1f;
            hoseRigidbody.angularDrag = 5f;
        }
    }
    
    void OnFireExtinguisherGrabbed(SelectEnterEventArgs args)
    {
        isFireExtinguisherGrabbed = true;
        CheckDualGrab();
    }
    
    void OnFireExtinguisherReleased(SelectExitEventArgs args)
    {
        isFireExtinguisherGrabbed = false;
        CheckDualGrab();
    }
    
    void OnHoseGrabbed(SelectEnterEventArgs args)
    {
        isHoseGrabbed = true;
        CheckDualGrab();
    }
    
    void OnHoseReleased(SelectExitEventArgs args)
    {
        isHoseGrabbed = false;
        CheckDualGrab();
    }
    
    void CheckDualGrab()
    {
        bool wasDualGrabbed = isDualGrabbed;
        isDualGrabbed = isFireExtinguisherGrabbed && isHoseGrabbed;
        
        if (isDualGrabbed && !wasDualGrabbed)
        {
            // Just started dual grab - bend hose to 180 degrees
            StartAutoBend();
        }
        else if (!isDualGrabbed && wasDualGrabbed)
        {
            // Just stopped dual grab - return to rest position
            StartAutoStraighten();
        }
    }
    
    void StartAutoBend()
    {
        if (hoseJoint == null) return;
        
        // Set target rotation to 180 degrees
        hoseJoint.targetRotation = Quaternion.Euler(targetBendAngle, 0, 0);
        
        // Increase spring strength for quick movement
        JointDrive drive = hoseJoint.angularXDrive;
        drive.positionSpring = 5000f;
        drive.positionDamper = 200f;
        hoseJoint.angularXDrive = drive;
        
        Debug.Log("Hose bending to 180 degrees");
    }
    
    void StartAutoStraighten()
    {
        if (hoseJoint == null) return;
        
        // Set target rotation back to straight
        hoseJoint.targetRotation = Quaternion.Euler(restAngle, 0, 0);
        
        // Use gentler spring for return movement
        JointDrive drive = hoseJoint.angularXDrive;
        drive.positionSpring = 2000f;
        drive.positionDamper = 150f;
        hoseJoint.angularXDrive = drive;
        
        Debug.Log("Hose returning to rest position");
    }
    
    void Update()
    {
        // Optional: Add visual feedback or additional logic here
        if (isDualGrabbed)
        {
            // You could add particle effects, sounds, etc.
        }
    }
    
    // Public method to manually trigger bending (for testing)
    public void ManualBend()
    {
        StartAutoBend();
    }
    
    // Public method to manually straighten (for testing)
    public void ManualStraighten()
    {
        StartAutoStraighten();
    }
}