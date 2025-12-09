using UnityEngine;
using UnityEngine.InputSystem;


public class Explorer : MonoBehaviour
{
    private Vector3 moveAmount;
    private Vector2 lookAmount;
    private Vector2 currentRotation;

    private Camera cam;

    public float moveSpeed = 2f;
    public float speedBoost = 4f;
    private float lookSpeed = 4f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        currentRotation.x = this.gameObject.transform.localRotation.x;
        currentRotation.y = this.gameObject.transform.localRotation.y;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveAmount = context.ReadValue<Vector3>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookAmount = context.ReadValue<Vector2>();
    }

    public void OnFastForward(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            moveSpeed += speedBoost;
        }

        if(context.canceled)
        {
            moveSpeed -= speedBoost;
        }
    }   

    private void Update()
    {
        Vector3 cameraForward = cam.transform.forward;
        Vector3 cameraRight = cam.transform.right;
        Vector3 cameraUp = cam.transform.up;

        Vector3 moveDirection = ((cameraForward * moveAmount.z) + (cameraRight * moveAmount.x) + (cameraUp * moveAmount.y));

        this.transform.position += moveDirection * Time.deltaTime * moveSpeed;

        transform.localRotation = Quaternion.Euler((currentRotation.x -= lookAmount.y * Time.deltaTime * lookSpeed), (currentRotation.y += lookAmount.x * Time.deltaTime * lookSpeed), 0f);
    }

}
