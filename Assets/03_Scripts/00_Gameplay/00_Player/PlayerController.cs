using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

namespace Psalmhaven
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float airControlMultiplier = 0.4f;

        [Header("Rotation")]
        public float rotationSpeed = 10f;

        [Header("Ground Check")]
        public float groundCheckDistance = 0.3f;
        public LayerMask groundLayer;
        public float maxSlopeAngle = 45f;

        [Header("Camera")]
        public Transform camTarget;
        public Vector3 camOffset = new Vector3(8.7f, 13f, -9f);
        public float camFollowSmoothTime = -0.05f;
        private Transform cameraTransform;

        private Vector3 velocity;

        private Rigidbody rb;
        private Vector2 moveInput;
        private Animator _animator;
        private bool isGrounded = true;
        private Vector3 groundNormal = Vector3.up;

        void Awake()
        {
            cameraTransform = Camera.main.transform;
            _animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
        }

        #region CameraLogic

        void LateUpdate()
        {
            if (!camTarget) return;

            Vector3 desiredPosition = camTarget.position + camOffset;
            cameraTransform.transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                camFollowSmoothTime
            );
        }


        #endregion

        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        void FixedUpdate()
        {
            //CheckGround();
            MovePlayer();
        }

        void CheckGround()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
            {
                groundNormal = hit.normal;
                float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
                isGrounded = slopeAngle <= maxSlopeAngle;
            }
            else
            {
                isGrounded = false;
                groundNormal = Vector3.up;
            }
        }

        void MovePlayer()
        {
            // Set animation bool
            bool isRunning = moveInput.magnitude >= 0.1f;
            _animator.SetBool("IsRunning", isRunning);

            // Normalize input to prevent diagonal speed boost
            Vector2 normalizedInput = Vector2.ClampMagnitude(moveInput, 1f);


            // Camera forward & right (flattened)
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            //Movement direction based on camera POV
            Vector3 desiredMove =
                camForward * normalizedInput.y +
                camRight * normalizedInput.x;

            //Rotate character on move
            if (desiredMove.sqrMagnitude > 0.001f)
            {
                Vector3 rotateMove = new Vector3(-desiredMove.z, desiredMove.y, desiredMove.x);
                Quaternion targetRotation = Quaternion.LookRotation(rotateMove);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            if (isGrounded)
            {
                Vector3 slopeMove = Vector3.ProjectOnPlane(desiredMove, groundNormal).normalized;
                Vector3 targetVelocity = slopeMove * moveSpeed;

                Vector3 velocityChange = targetVelocity - rb.linearVelocity;
                velocityChange.y = 0f;

                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
            else
            {
                rb.AddForce(desiredMove * moveSpeed * airControlMultiplier, ForceMode.Acceleration);
            }
        }
    }
}