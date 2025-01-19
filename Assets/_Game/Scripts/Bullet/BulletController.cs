using _Game.Scripts.Managers;
using UnityEngine;

namespace _Game.Scripts.Bullet
{
    [RequireComponent(typeof(Rigidbody))]
    public class BulletController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Forward speed in world -X direction.")]
        public float forwardSpeed = 20f;

        [Tooltip("Side movement speed in world Z.")]
        public float lateralSpeed = 1.0f;

        [Tooltip("Mouse X sensitivity for side movement.")]
        public float mouseSensitivity = 2.0f;

        [Tooltip("Gravity multiplier (lower = slower fall).")]
        public float gravityMultiplier = 0.02f;

        [Header("Collision Boosts")]
        [Tooltip("Extra forward speed gained on enemy collision.")]
        public float collisionSpeedBoost = 0.3f;

        [Tooltip("Additional upward force on collision.")]
        public float collisionUpwardBoost = 0.3f;

        private Rigidbody _rb;

        // store the bullet's upward velocity (gravity), 
        // plus a separate "side" velocity. forward is kept constant by default.
        private float _verticalVelocity;
        private float _sideVelocity;   // movement along the world Z-axis

        private bool _isBulletFrozen;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            
            _sideVelocity = 0f;
            _verticalVelocity = 0f;
        }

        private void Update()
        {
            if (_isBulletFrozen) return;

            // 1) keyboard input (A/D or Left/Right arrows)
            float horizontalInput = Input.GetAxis("Horizontal");

            // 2) mouse X input
            float mouseInput = Input.GetAxis("Mouse X") * mouseSensitivity;

            // combined lateral input
            float totalLateralInput = horizontalInput + mouseInput;

            // accumulate side velocity
            // maybe diirect mapping? do: _sideVelocity = totalLateralInput * lateralSpeed;
            _sideVelocity += (totalLateralInput * lateralSpeed) * Time.deltaTime;
            
            _verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }

        private void FixedUpdate()
        {
            if (_isBulletFrozen) return;

            // Forward along negative X in WORLD space
            Vector3 forwardVelocity = new Vector3(-forwardSpeed, 0f, 0f);

            // Side movement along the WORLD Z axis
            Vector3 sideVelocity = new Vector3(0f, 0f, _sideVelocity);

            // Vertical velocity in the WORLD Y axis
            Vector3 verticalVelocity = new Vector3(0f, _verticalVelocity, 0f);

            // Sum
            Vector3 totalVelocity = forwardVelocity + sideVelocity + verticalVelocity;
            _rb.linearVelocity = totalVelocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isBulletFrozen) return;
            
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                if (collision.gameObject.CompareTag("Boss"))
                {
                    GameManager.instance.OnBossHit();
                }
                else
                {
                    // Boost forward speed
                    forwardSpeed += collisionSpeedBoost;

                    // Lift bullet upward
                    _verticalVelocity += collisionUpwardBoost;

                    // Damage the enemy
                    var enemy = collision.gameObject.GetComponent<_Game.Scripts.Enemies.Enemy>();
                    if (enemy != null) enemy.TakeDamage(1);

                    GameManager.instance.IncrementScore();
                }
            }
            else
            {
                // Run ends
                GameManager.instance.OnBulletDestroyed();
                FreezeBullet();
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
