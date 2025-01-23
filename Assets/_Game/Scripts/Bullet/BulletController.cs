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
        public float maxInfluence = 2f;
        public float inputSensitivity = 2f;
        public float deadzone = 0.2f;
        public float gravityMultiplier = 0.02f;
        public float forwardSlowdown = 0.5f;

        [Header("Collision Boosts")]
        public float collisionSpeedBoost = 5f;
        public float upwardBoostDuration = 0.2f;
        public float collisionUpwardBoost = 4f;

        [Header("Effects")]
        public GameObject bloodFXPrefab;

        [Header("Anchor System")]
        [Tooltip("Anchor for the Cinemachine camera.")]
        public Transform cameraAnchor;
        public float anchorSmoothSpeed = 5f; // How fast the anchor catches up

        private Rigidbody _rb;
        private bool _isBulletFrozen;

        // Movement variables
        private Vector2 _inputDirection;
        private Vector3 _trajectoryOffset;
        private float _verticalVelocity;
        private float _upwardBoostTimer;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void Update()
        {
            if (_isBulletFrozen) return;

            // Input handling
            Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            Vector2 controllerInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            Vector2 combinedInput = mouseInput + controllerInput;

            // Apply deadzone
            if (combinedInput.magnitude < deadzone)
            {
                _inputDirection = Vector2.zero;
            }
            else
            {
                _inputDirection = combinedInput.normalized * Mathf.Min((combinedInput.magnitude - deadzone) * inputSensitivity, 1f);
            }

            // Adjust trajectory offset
            _trajectoryOffset = new Vector3(
                _inputDirection.x * maxInfluence,
                _inputDirection.y * maxInfluence,
                0f
            );

            // Gravity and upward force logic
            if (_upwardBoostTimer > 0)
            {
                _upwardBoostTimer -= Time.deltaTime;
            }
            else
            {
                _verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            }

            // Natural forward slowdown
            forwardSpeed = Mathf.Max(forwardSpeed - forwardSlowdown * Time.deltaTime, 0f);

            // Update the camera anchor's position
            UpdateCameraAnchor();
        }

        private void FixedUpdate()
        {
            if (_isBulletFrozen) return;

            // Forward velocity along -X
            Vector3 forwardVelocity = new Vector3(-Mathf.Min(forwardSpeed, maxForwardSpeed), 0f, 0f);

            // Combine trajectory offset and gravity
            Vector3 adjustedVelocity = forwardVelocity + _trajectoryOffset + new Vector3(0f, _verticalVelocity, 0f);

            // Set bullet velocity
            _rb.linearVelocity = adjustedVelocity;

            // Adjust bullet orientation
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(adjustedVelocity.normalized, Vector3.up),
                Time.deltaTime * 5f
            );
        }

        private void UpdateCameraAnchor()
        {
            if (cameraAnchor == null) return;

            // Smoothly move the camera anchor forward
            Vector3 targetPosition = transform.position;
            targetPosition.z = transform.position.z + _trajectoryOffset.z * 0.5f; // Slightly ahead of the bullet

            cameraAnchor.position = Vector3.Lerp(cameraAnchor.position, targetPosition, anchorSmoothSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isBulletFrozen) return;

            if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                SpawnBloodFX(other);

                if (other.CompareTag("Boss"))
                {
                    GameManager.instance.OnBossHit();
                }
                else
                {
                    forwardSpeed = Mathf.Min(forwardSpeed + collisionSpeedBoost, maxForwardSpeed);
                    _verticalVelocity += collisionUpwardBoost;
                    _upwardBoostTimer = upwardBoostDuration;

                    var wanderNpc = other.GetComponent<Polyperfect.People.People_WanderScript>();
                    if (wanderNpc != null)
                    {
                        wanderNpc.Die();
                        GameManager.instance.IncrementScore();
                    }
                }
            }
            else
            {
                GameManager.instance.OnBulletDestroyed();
                FreezeBullet();
            }
        }

        private void SpawnBloodFX(Collider other)
        {
            if (bloodFXPrefab)
            {
                Vector3 collisionPoint = other.ClosestPoint(transform.position);
                Vector3 bulletDirection = _rb.linearVelocity.normalized;
                Quaternion bloodRotation = Quaternion.LookRotation(bulletDirection);

                Instantiate(bloodFXPrefab, collisionPoint, bloodRotation);
            }
            else
            {
                Debug.LogWarning("BloodFXPrefab not assigned in BulletController!");
            }
        }

        private void FreezeBullet()
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            _rb.detectCollisions = false;

            _isBulletFrozen = true;
            enabled = false;
        }
    }
}
