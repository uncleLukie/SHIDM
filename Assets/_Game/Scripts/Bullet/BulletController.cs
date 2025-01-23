using _Game.Scripts.Managers;
using UnityEngine;

namespace _Game.Scripts.Bullet
{
    [RequireComponent(typeof(Rigidbody))]
    public class BulletController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float forwardSpeed = 20f;
        public float maxForwardSpeed = 25f;
        public float inputInfluence = 2f;
        public float inputSensitivity = 2f;
        public float deadzone = 0.2f;
        public float gravityMultiplier = 0.02f;
        public float forwardSlowdown = 0.5f;

        [Header("Rotation Settings")]
        public float maxRotationAngle = 45f;

        [Header("Collision Boosts")]
        public float collisionSpeedBoost = 5f;
        public float upwardBoostDuration = 0.2f;
        public float collisionUpwardBoost = 4f;

        [Header("Effects")]
        public GameObject bloodFXPrefab;

        [Header("Anchor System")]
        [Tooltip("Tag used to find the Cinemachine camera anchor.")]
        public string cameraAnchorTag = "CamAnchor";
        public float anchorSmoothSpeed = 5f; // How fast the anchor catches up

        private Rigidbody _rb;
        private Transform cameraAnchor; // Camera anchor found via tag
        private Vector2 _inputDirection; // Input direction from mouse/controller
        private Vector3 _trajectory;    // Bullet trajectory after release
        private float _verticalVelocity;
        private float _upwardBoostTimer;
        private bool _isBulletFrozen;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false; // Manual gravity
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            _trajectory = Vector3.left * forwardSpeed;

            // Find the camera anchor using the specified tag
            GameObject anchorObject = GameObject.FindGameObjectWithTag(cameraAnchorTag);
            if (anchorObject != null)
            {
                cameraAnchor = anchorObject.transform;
            }
            else
            {
                Debug.LogWarning($"Camera anchor with tag '{cameraAnchorTag}' not found.");
            }
        }

        private void Update()
        {
            if (_isBulletFrozen) return;

            // Input handling
            Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            Vector2 controllerInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            Vector2 combinedInput = mouseInput + controllerInput;

            // Apply deadzone
            _inputDirection = combinedInput.magnitude < deadzone ? Vector2.zero : combinedInput.normalized;

            // Adjust vertical velocity for gravity
            if (_upwardBoostTimer > 0)
            {
                _upwardBoostTimer -= Time.deltaTime;
            }
            else
            {
                _verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            }

            // Gradual forward slowdown
            forwardSpeed = Mathf.Max(forwardSpeed - forwardSlowdown * Time.deltaTime, 0f);

            // Update the camera anchor position
            UpdateCameraAnchor();
        }

        private void FixedUpdate()
        {
            if (_isBulletFrozen) return;

            // Forward velocity along -X
            Vector3 forwardVelocity = Vector3.left * forwardSpeed;

            // Apply input influence to trajectory
            Vector3 inputVelocity = new Vector3(
                _inputDirection.x * inputInfluence,
                _inputDirection.y * inputInfluence + _verticalVelocity,
                0f
            );

            // Combine forward and input velocities
            _trajectory = forwardVelocity + inputVelocity;

            // Update Rigidbody velocity
            _rb.linearVelocity = _trajectory;

            // Adjust rotation based on trajectory
            AdjustBulletRotation();
        }

        private void AdjustBulletRotation()
        {
            // Calculate target rotation from trajectory
            float targetPitch = Mathf.Clamp(_trajectory.y * maxRotationAngle / inputInfluence, -maxRotationAngle, maxRotationAngle); // Up/Down
            float targetYaw = Mathf.Clamp(_trajectory.z * maxRotationAngle / inputInfluence, -maxRotationAngle, maxRotationAngle);  // Left/Right

            // Calculate and apply target rotation
            Quaternion targetRotation = Quaternion.Euler(targetPitch, 0f, 90f + targetYaw);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }

        private void UpdateCameraAnchor()
        {
            if (cameraAnchor == null) return;

            // Smoothly move the camera anchor forward
            Vector3 targetPosition = transform.position;
            targetPosition.z = transform.position.z + _trajectory.z * 0.5f;

            cameraAnchor.position = Vector3.Lerp(cameraAnchor.position, targetPosition, anchorSmoothSpeed * Time.deltaTime);
        }
    }
}
