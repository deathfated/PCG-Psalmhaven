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
        public bool canMove = true;

        [Header("Rotation")]
        public float rotationSpeed = 10f;

        [Header("Ground Check")]
        public float groundCheckDistance = 0.3f;
        public LayerMask groundLayer;
        public float maxSlopeAngle = 45f;
        public bool isGrounded;
        private Vector3 groundNormal = Vector3.up;

        [Header("Camera")]
        public Transform camTarget;
        public bool isOnCamOffset = true;
        public Vector3 camOffset = new Vector3(10f, 10f, -10f);
        public Vector3 camOffsetCombat = new Vector3(0, 0, 0);
        public Vector3 camOffsetMain = new Vector3(0, 0, 0);
        public float camFollowSmoothTime = -0.05f;
        public float rotationXOffset = -10f;
        public float positionYOffset = -1f;
        private Transform cameraTransform;
        private Vector3 velocity;

        private Rigidbody rb;
        private Vector2 moveInput;
        private Animator _animator;

        void Start()
        {
            cameraTransform = Camera.main.transform;
            camOffsetMain = cameraTransform.position;
            if (isOnCamOffset) camOffset = camOffsetMain;

            _animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
        }

        #region CameraLogic

        public void SwitchToCombatCam()
        {
            //camOffsetCombat = cameraTransform.position + new Vector3(-13, -16, -13);
            camOffset = camOffsetCombat;
        }

        public void SwitchToMainCam()
        {
            camOffset = camOffsetMain;
        }

        #endregion

        public void DisableMovement()
        {
            canMove = false;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        void FixedUpdate()
        {
            CheckGround();
            MovePlayer();

            if (!camTarget) return;

            Vector3 desiredPosition = camTarget.position + camOffset;
            cameraTransform.transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                camFollowSmoothTime
            );
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
            if (!canMove) moveInput = new Vector2(0,0);

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

            if (isGrounded)
            {
                // Set animation bool
                bool isRunning = moveInput.magnitude >= 0.1f;
                _animator.SetBool("IsRunning", isRunning);

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

                Vector3 slopeMove = Vector3.ProjectOnPlane(desiredMove, groundNormal).normalized;
                Vector3 targetVelocity = slopeMove * moveSpeed;

                Vector3 velocityChange = targetVelocity - rb.linearVelocity;
                    velocityChange.y = 0f;

                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
            else
            {
                rb.AddForce(desiredMove * moveSpeed * airControlMultiplier, ForceMode.Acceleration);
                _animator.SetBool("IsRunning", false);
            }
        }

        public void FaceObject(Transform target, float turnSpeed = 10f)
        {
            if (target == null)
                return;

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * turnSpeed
        );
        }
    }
}