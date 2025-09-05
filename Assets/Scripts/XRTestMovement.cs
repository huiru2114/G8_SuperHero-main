using UnityEngine;

public class XRTestMovement : MonoBehaviour
{
    [Header("Development Testing")]
    public bool enableDesktopMovement = true; // Toggle this off for VR builds
    public float moveSpeed = 3f;
    private Camera mainCamera;

    void Start()
    {
        // Auto-detect if we're in VR mode
        if (UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.loadedDeviceName != "")
        {
            enableDesktopMovement = false;
            Debug.Log("VR headset detected - disabling desktop movement");
        }

        // Find the main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found! Please ensure a camera is tagged as MainCamera.");
        }
    }

    void Update()
    {
        if (!enableDesktopMovement || mainCamera == null) return;

        // WASD movement relative to camera's forward direction
        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) movement += mainCamera.transform.forward;
        if (Input.GetKey(KeyCode.S)) movement -= mainCamera.transform.forward;
        if (Input.GetKey(KeyCode.A)) movement -= mainCamera.transform.right;
        if (Input.GetKey(KeyCode.D)) movement += mainCamera.transform.right;

        if (movement != Vector3.zero)
        {
            // Remove Y component to prevent vertical movement
            movement.y = 0;
            // Normalize movement to prevent faster diagonal movement
            movement = movement.normalized;
            transform.position += movement * moveSpeed * Time.deltaTime;
        }
    }
}