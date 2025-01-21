using _Game.Scripts.Managers;
using UnityEngine;

namespace _Game.Scripts.Bullet
{
    [RequireComponent(typeof(Rigidbody))]
    public class BulletController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float forwardSpeed = 20f;
        public float lateralSpeed = 1.0f;
        public float mouseSensitivity = 2.0f;
        public float gravityMultiplier = 0.02f;

        [Header("Collision Boosts")]
        public float collisionSpeedBoost = 0.3f;
        public float collisionUpwardBoost = 0.3f;

        private Rigidbody _rb;
        private float _verticalVelocity;
        private float _sideVelocity;
        private bool _isBulletFrozen;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void Update()
        {
            if (_isBulletFrozen) return;

            float horizontalInput = Input.GetAxis("Horizontal");
            float mouseInput = Input.GetAxis("Mouse X") * mouseSensitivity;
            float totalLateralInput = horizontalInput + mouseInput;

            _sideVelocity += (totalLateralInput * lateralSpeed) * Time.deltaTime;
            _verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }

        private void FixedUpdate()
        {
            if (_isBulletFrozen) return;

            Vector3 forwardVelocity = new Vector3(-forwardSpeed, 0f, 0f);
            Vector3 sideVelocity = new Vector3(0f, 0f, _sideVelocity);
            Vector3 verticalVelocity = new Vector3(0f, _verticalVelocity, 0f);

            Vector3 totalVelocity = forwardVelocity + sideVelocity + verticalVelocity;
            _rb.linearVelocity = totalVelocity;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isBulletFrozen) return;

            if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                // Boss logic
                if (other.CompareTag("Boss"))
                {
                    GameManager.instance.OnBossHit();
                }
                else
                {
                    forwardSpeed += collisionSpeedBoost;
                    _verticalVelocity += collisionUpwardBoost;

                    // Check if it's a People_WanderScript from Polyperfect
                    var wanderNpc = other.GetComponent<Polyperfect.People.People_WanderScript>();
                    if (wanderNpc != null)
                    {
                        // Kill the NPC via their built-in flow
                        wanderNpc.Die();
                        GameManager.instance.IncrementScore();
                    }
                }
            }
            else
            {
                // End the run
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
