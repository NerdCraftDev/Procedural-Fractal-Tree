using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 8.0f;
    public Vector3 cameraOffset = Vector3.zero;
    public float cameraSensitivity = 1f;
    public float maxCameraAngle = 90;

    private float verticalRotation = 0;

    private void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Move the player left and right
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 forward = transform.forward;
        forward.y = 0;
        Vector3 movement = forward.normalized * verticalInput + transform.right * horizontalInput;
        transform.position += moveSpeed * Time.deltaTime * movement.normalized;

        float mouseDeltaX = Input.GetAxis("Mouse X");
        float mouseDeltaY = Input.GetAxis("Mouse Y");
        verticalRotation -= mouseDeltaY * cameraSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxCameraAngle, maxCameraAngle);
        float newRotationY = transform.localEulerAngles.y + mouseDeltaX * cameraSensitivity;

        Quaternion localRotation = Quaternion.Euler(verticalRotation, newRotationY, 0);
        transform.localRotation = localRotation;
        
        Camera.main.transform.SetPositionAndRotation(transform.position + cameraOffset, transform.rotation);
    }
}
