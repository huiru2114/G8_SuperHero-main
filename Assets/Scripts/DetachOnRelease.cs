using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DetachOnRelease : MonoBehaviour
{
    private XRGrabInteractable grab;
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip pullSound; // Assign this in the Unity Inspector

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        audioSource = GetComponent<AudioSource>();

        // Add AudioSource if not already present
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrab); // Listen for when the pin is grabbed
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrab); // Clean up grab listener
        grab.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Play the pull sound when the pin is grabbed (pulled)
        if (pullSound != null)
        {
            audioSource.PlayOneShot(pullSound);
        }

        // Detach from parent so it can be held freely
        transform.parent = null;

        // Make sure gravity is off while held
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // Keep kinematic while held to prevent falling
        rb.useGravity = false;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // Make sure gravity is on when released
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}