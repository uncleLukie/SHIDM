using _Game.Scripts.Managers;
using UnityEngine;

namespace _Game.Scripts.Bullet
{
    [RequireComponent(typeof(Rigidbody))]
    public class BulletController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Starting forward speed. Keep very low for dramatic slow-motion.")]
        public float initialForwardSpeed = 0.3f;

        [Tooltip("Horizontal movement speed for steering left/right.")]
        public float lateralSpeed = 0.3f;

        [Tooltip("Scales gravity pulling the bullet down. Lower = slower fall.")]
        public float gravityMultiplier = 0.02f;

        [Tooltip("Minimum forward speed so bullet doesn't stall.")]
        public float minForwardSpeed = 0.2f;

        [Header("Collision Boosts")]
        [Tooltip("Extra forward speed gained on enemy collision.")]
        public float collisionSpeedBoost = 0.3f;

        [Tooltip("Additional upward force on collision with enemy.")]
        public float collisionUpwardBoost = 0.3f;

        private Rigidbody _rb;
        private Vector3 _velocity;
        private bool _isBulletFrozen;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate; // smoother movement

            // We assume your bullet is oriented so that "forward" is negative X in world space.
            // So we push it along negative X:
            _velocity = Vector3.left * initialForwardSpeed;
        }

        private void Update()
        {
            // If frozen, don't update logic
            if (_isBulletFrozen) return;

            // Basic left/right steering via horizontal input
            float horizontalInput = Input.GetAxis("Horizontal");

            // Add side-to-side lateral velocity
            Vector3 lateral = transform.right * (horizontalInput * lateralSpeed);
            _velocity += lateral * Time.deltaTime;

            // Enforce a minimum forward speed
            float currentSpeed = _velocity.magnitude;
            if (currentSpeed < minForwardSpeed)
            {
                _velocity = _velocity.normalized * minForwardSpeed;
            }

            // Apply manual gravity
            _velocity += Physics.gravity * (gravityMultiplier * Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_isBulletFrozen) return;
            
            _rb.linearVelocity = _velocity;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isBulletFrozen) return;
            
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                // Distinguish between boss or normal enemy
                if (collision.gameObject.CompareTag("Boss"))
                {
                    GameManager.instance.OnBossHit();
                }
                else
                {
                    _velocity += _velocity.normalized * collisionSpeedBoost;
                    
                    _velocity += Vector3.up * collisionUpwardBoost;
                    
                    var enemy = collision.gameObject.GetComponent<_Game.Scripts.Enemies.Enemy>();
                    if (enemy != null) enemy.TakeDamage(1);

                    GameManager.instance.IncrementScore();
                }
            }
            else
            {
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
