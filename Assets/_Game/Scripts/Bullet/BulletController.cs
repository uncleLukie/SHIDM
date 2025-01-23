using _Game.Scripts.Managers;
using UnityEngine;
using System.Collections;

namespace _Game.Scripts.Bullet
{
    [RequireComponent(typeof(Rigidbody))]
    public class BulletController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Forward speed once bullet is fully traveling.")]
        public float forwardSpeed = 20f;

        [Tooltip("Gravity multiplier for downward arc (optional).")]
        public float gravityMultiplier = 0.02f;

        [Tooltip("How quickly the bullet drifts forward in bullet time (slow). " +
                 "If 0, bullet won't move forward at all during bullet time.")]
        public float bulletTimeForwardSpeed = 1f;

        [Header("Rotation Settings")]
        [Tooltip("Maximum rotation angle from center (for mouse steering).")]
        public float maxRotationAngle = 45f;

        [Tooltip("How fast the bullet rotates (degrees per second).")]
        public float rotationSpeed = 180f;

        [Tooltip("Deadzone radius in screen pixels (mouse movements smaller than this won't rotate).")]
        public float mouseDeadzone = 20f;

        [Header("Effects")]
        public GameObject bloodFXPrefab;

        [Header("Anchor System")]
        [Tooltip("Tag used to find the Cinemachine camera anchor.")]
        public string cameraAnchorTag = "CamAnchor";

        [Tooltip("How quickly the camera anchor follows the bullet.")]
        public float anchorSmoothSpeed = 5f;

        private Rigidbody _rb;
        private Transform _cameraAnchor;

        // The bullet's velocity outside bullet time
        private Vector3 _trajectory;
        // Are we in bullet time?
        private bool _isInBulletTime;
        // Are we allowed to steer the bullet?
        private bool _canSteer;
        // Are we done shooting? (Once we start the game, bullet is fired.)
        private bool _fired;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Default straight trajectory along the bullet's UP axis
            // Adjust if your bullet mesh is oriented differently
            _trajectory = transform.up * forwardSpeed;

            // Find camera anchor by tag
            GameObject anchorObject = GameObject.FindGameObjectWithTag(cameraAnchorTag);
            if (anchorObject != null)
                _cameraAnchor = anchorObject.transform;
            else
                Debug.LogWarning($"Camera anchor with tag '{cameraAnchorTag}' not found.");

            // Ensure initial rotation is something sensible
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private void Update()
        {
            // If bullet hasn't fired, we let it just travel forward in normal time (set in FixedUpdate).

            // If in bullet time AND we can steer, handle input
            if (_isInBulletTime && _canSteer)
            {
                HandleBulletTimeSteering();
            }

            // Exiting bullet time with Left Click or Space Bar:
            if (_isInBulletTime && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
            {
                ResumeNormalTime();
            }

            UpdateCameraAnchor();
        }

        private void FixedUpdate()
        {
            if (!_fired)
            {
                // Not fired yet => proceed on initial trajectory
                _rb.linearVelocity = _trajectory;
                return;
            }

            if (!_isInBulletTime)
            {
                // Normal flight outside bullet time
                Vector3 gravityVec = Physics.gravity * (gravityMultiplier * Time.fixedDeltaTime);
                _rb.linearVelocity = _trajectory + gravityVec;
            }
            else
            {
                // In bullet time => slow forward speed + gravity
                Vector3 slowForward = transform.up * bulletTimeForwardSpeed;
                Vector3 gravityVec = Physics.gravity * (gravityMultiplier * Time.fixedDeltaTime);
                _rb.linearVelocity = slowForward + gravityVec;
            }
        }

        private void HandleBulletTimeSteering()
        {
            // Mouse position relative to screen center
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 mousePos = (Vector2)Input.mousePosition;
            Vector2 offset = mousePos - screenCenter;
            float dist = offset.magnitude;

            // If inside deadzone, no rotation
            if (dist < mouseDeadzone) return;

            // Compute pitch (vertical) & yaw (horizontal)
            Vector2 normalizedOffset = offset / dist;  
            float pitchAngle = -normalizedOffset.y * maxRotationAngle; 
            float yawAngle = normalizedOffset.x * maxRotationAngle;

            // Rotate around local X for pitch, local Z for yaw
            Quaternion targetRotation = Quaternion.Euler(pitchAngle, 0f, 90f + yawAngle);

            // Smoothly rotate toward this in bullet time
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.unscaledDeltaTime
            );
        }

        private void UpdateCameraAnchor()
        {
            if (_cameraAnchor == null) return;

            Vector3 targetPosition = transform.position;
            _cameraAnchor.position = Vector3.Lerp(
                _cameraAnchor.position,
                targetPosition,
                anchorSmoothSpeed * Time.unscaledDeltaTime
            );
        }

        private void OnTriggerEnter(Collider other)
        {
            // If we hit an enemy, we spawn FX, then re-enter bullet time (unless it's a freeze or kill?)
            if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                SpawnBloodFX(other);

                if (other.CompareTag("Boss"))
                {
                    // Possibly do boss logic
                }

                // Re-enter bullet time again after passing through
                // For a short delay to ensure we pass the collider fully.
                StartCoroutine(ReEnterBulletTimeAfterDelay(0.2f));
            }
            else
            {
                // Freeze bullet on non-enemy collisions
                FreezeBullet();
            }
        }

        private IEnumerator ReEnterBulletTimeAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            // We'll call the game manager to start the slow-mo ramp
            GameManager.instance.StartGradualBulletTime();
        }

        private void SpawnBloodFX(Collider other)
        {
            if (bloodFXPrefab)
            {
                Vector3 collisionPoint = other.ClosestPoint(transform.position);
                Instantiate(bloodFXPrefab, collisionPoint, Quaternion.identity);
            }
        }

        private void FreezeBullet()
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
            _rb.detectCollisions = false;
            enabled = false;
        }

        // Called by GameManager once bullet is spawned & we want it to start flight
        public void FireBullet()
        {
            _fired = true; // bullet is now live & traveling
            _rb.linearVelocity = _trajectory;
        }

        // Called by the GameManager's GraduallyDecreaseTimeScale => once we reach bulletTime
        public void EnterBulletTime()
        {
            _isInBulletTime = true;
            _canSteer = true;
        }

        // Called when user hits left click or space in bullet time => back to normal time
        public void ResumeNormalTime()
        {
            StartCoroutine(GameManager.instance.GraduallyIncreaseTimeScale());

            _isInBulletTime = false;
            _canSteer = false;

            // Recalc forward trajectory based on bullet's current orientation
            _trajectory = transform.up * forwardSpeed;
        }
    }
}
