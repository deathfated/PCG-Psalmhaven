using UnityEngine;
using UnityEngine.InputSystem;

namespace Psalmhaven
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 6f;
        public float airControlMultiplier = 0.4f;

        [Header("Ground Check")]
        public float groundCheckDistance = 0.3f;
        public LayerMask groundLayer;
        public float maxSlopeAngle = 45f;

        private Rigidbody rb;
        private Vector2 moveInput;
        private bool isGrounded = true;
        private Vector3 groundNormal = Vector3.up;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
        }

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
            // Normalize input to prevent diagonal speed boost
            Vector2 normalizedInput = moveInput.magnitude > 1f
                ? moveInput.normalized
                : moveInput;

            Vector3 desiredMove = new Vector3(normalizedInput.x, 0f, normalizedInput.y);
            desiredMove = transform.TransformDirection(desiredMove);

            if (isGrounded)
            {
                // Project movement onto slope
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