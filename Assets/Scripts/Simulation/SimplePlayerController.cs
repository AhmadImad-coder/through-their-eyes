using UnityEngine;

namespace OCDSimulation
{
    [RequireComponent(typeof(CharacterController))]
    public class SimplePlayerController : MonoBehaviour
    {
        public float moveSpeed = 3.5f;
        public float lookSpeed = 2f;
        public float gravity = -9.81f;

        private CharacterController controller;
        private Camera cam;
        private float pitch = 0f;
        private bool movementLocked = false;
        private bool inputEnabled = true;
        private float verticalVelocity = 0f;

        public Camera Camera => cam;
        public CharacterController Controller => controller;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            UnlockCursor();
        }

        public void BindCamera(Camera camera)
        {
            cam = camera;
        }

        public void LockMovement(bool locked)
        {
            movementLocked = locked;
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
            {
                UnlockCursor();
            }
        }

        public void ForceLockCursor()
        {
            if (!inputEnabled) return;
            LockCursor();
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.position = position;
            transform.rotation = rotation;
            controller.enabled = wasEnabled;
        }

        private void Update()
        {
            if (cam == null) return;

            if (inputEnabled && Input.GetMouseButtonDown(0))
            {
                LockCursor();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                UnlockCursor();
            }

            if (inputEnabled && Cursor.lockState == CursorLockMode.Locked)
            {
                float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
                pitch = Mathf.Clamp(pitch - mouseY, -80f, 80f);
                cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                transform.Rotate(Vector3.up * mouseX);
            }

            if (movementLocked || !inputEnabled)
            {
                controller.Move(Vector3.zero);
                return;
            }

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 move = (transform.right * h + transform.forward * v) * moveSpeed;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
