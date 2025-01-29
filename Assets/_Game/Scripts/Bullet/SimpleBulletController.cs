using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using Polyperfect.Common;       // If you want to call wander.Die() for enemies
using _Game.Scripts.Managers;  // For GameManager calls
using Unity.Cinemachine;

namespace _Game.Scripts.Bullet
{
    /// <summary>
    /// A bullet controller that:
    ///  - Has a kinematic Rigidbody + trigger collider (isTrigger=true).
    ///  - Moves using Transform in Update() for forward/verticalVelocity.
    ///  - Collisions are handled in OnTriggerEnter (with enemies or environment).
    ///  - Ricochet logic is in TryRicochet(). After a ricochet, we enter bullet time again.
    ///  - If out of ricochets -> Game Over.
    ///  - Also has flight-time limit, distance limit, bullet time toggles, etc.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class SimpleBulletController : MonoBehaviour, IInputAxisOwner
    {
        [Header("Layer Settings")]
        private int enemyLayer;
        private int environmentLayer;

        [Header("Speed Settings")]
        public float normalSpeed = 40f;
        public float bulletTimeSpeed = 2f;

        [Header("Gravity & Distance")]
        public float gravity = 0.4f;
        public bool reduceGravityInBulletTime = true;
        public float maxDistance = 150f;

        [Header("Ricochet")]
        [Tooltip("How many times bullet can bounce off environment before 'GameOver'.")]
        public int ricochetCount = 1;

        [Header("Flight Time Limit")]
        [Tooltip("If bullet travels longer than this in real time, we end the bullet. <= 0 => no limit.")]
        public float maxFlightSeconds = 20f;

        [Header("Events")]
        public UnityEvent onBulletFired;
        public UnityEvent onBulletEnd;

        // If you want bullet-time re-aim or hooking with an aim controller
        public Action PreUpdate;
        public Action<Vector3, float> PostUpdate;

        // Internal flight state
        private bool fired;
        private bool isBulletTime;
        private Vector3 lastPosition;
        private float distanceTraveled;
        private float verticalVelocity;
        private float flightTimer;

        // Expose "am I fired" to external code if needed
        public bool IsFired => fired;

        private void Awake()
        {
            // Resolve layer indices
            enemyLayer = LayerMask.NameToLayer("Enemy");
            environmentLayer = LayerMask.NameToLayer("Environment");

            // Ensure we have a kinematic RB for trigger usage
            var rb = GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
                rb.useGravity = false; // We'll handle "gravity" ourselves via transform in Update()
            }

            // Ensure collider is trigger
            var coll = GetComponent<Collider>();
            if (coll && !coll.isTrigger)
            {
                coll.isTrigger = true;
                Debug.LogWarning("SimpleBulletController: Setting collider isTrigger=true at runtime!");
            }
        }

        private void OnEnable()
        {
            fired = false;
            isBulletTime = false;
            lastPosition = transform.position;
            distanceTraveled = 0f;
            verticalVelocity = 0f;
            flightTimer = 0f;
        }

        private void Update()
        {
            // Let an aim controller or other code run first
            PreUpdate?.Invoke();

            if (!fired)
            {
                // If bullet not fired, we might still call PostUpdate with zero velocity
                PostUpdate?.Invoke(Vector3.zero, 1f);
                return;
            }

            // Track real-time flight
            flightTimer += Time.deltaTime;
            if (maxFlightSeconds > 0 && flightTimer >= maxFlightSeconds)
            {
                EndBullet("Flight time exceeded");
                return;
            }

            // Decide forward speed
            float speed = isBulletTime ? bulletTimeSpeed : normalSpeed;

            // Gravity factor
            float currentGravity = (reduceGravityInBulletTime && isBulletTime) ? 0f : gravity;

            // Accumulate vertical velocity
            verticalVelocity -= currentGravity * Time.deltaTime;

            // Move the bullet
            Vector3 moveFrame = (transform.forward * speed + Vector3.up * verticalVelocity) * Time.deltaTime;
            transform.position += moveFrame;

            // Track distance traveled
            float distanceThisFrame = (transform.position - lastPosition).magnitude;
            distanceTraveled += distanceThisFrame;
            lastPosition = transform.position;

            // If maxDistance > 0, check
            if (maxDistance > 0 && distanceTraveled >= maxDistance)
            {
                EndBullet("Distance limit reached");
                return;
            }

            // Optional post update for aim logic
            Vector3 localVel = Quaternion.Inverse(transform.rotation) * (transform.forward * speed + Vector3.up * verticalVelocity);
            PostUpdate?.Invoke(localVel, 1f);
        }

        /// <summary>
        /// Fire the bullet once, setting all the counters to zero and marking "fired=true".
        /// </summary>
        public void FireBullet()
        {
            if (fired) return;
            fired = true;
            distanceTraveled = 0f;
            lastPosition = transform.position;
            verticalVelocity = 0f;
            flightTimer = 0f;

            onBulletFired?.Invoke();
        }

        /// <summary>
        /// Enter bullet-time mode (slowed speed).
        /// </summary>
        public void EnterBulletTime() => isBulletTime = true;

        /// <summary>
        /// Exit bullet-time mode (return to normal speed).
        /// </summary>
        public void ExitBulletTime() => isBulletTime = false;

        /// <summary>
        /// End bullet flight and disable the object.
        /// </summary>
        private void EndBullet(string reason)
        {
            Debug.Log($"Bullet ended: {reason}");
            onBulletEnd?.Invoke();

            fired = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Re-aim bullet direction to "Player Aiming Core" transform, and reset vertical velocity.
        /// Good for re-firing in bullet time or normal time after user aims.
        /// </summary>
        public void ReFireInCurrentAimDirection()
        {
            verticalVelocity = 0f;
            Transform aimCore = transform.Find("Player Aiming Core");
            if (aimCore)
                transform.rotation = aimCore.rotation;
        }

        /// <summary>
        /// Try to ricochet off given surface normal. 
        /// If we have any ricochet left, reflect forward direction and return true.
        /// If no ricochet left, return false.
        /// </summary>
        private bool TryRicochet(Vector3 hitNormal)
        {
            if (ricochetCount <= 0)
                return false;

            ricochetCount--;

            Vector3 reflect = Vector3.Reflect(transform.forward, hitNormal);
            transform.forward = reflect.normalized;
            verticalVelocity = 0f;

            Debug.Log($"Ricochet! Count left: {ricochetCount}");
            return true;
        }

        /// <summary>
        /// We do OnTriggerEnter for both enemies and environment. 
        /// If environment => attempt ricochet. If successful => bullet time again; else game over.
        /// If enemy => kill it, bullet time again.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!fired) return; // ignore collisions if not in flight

            int otherLayer = other.gameObject.layer;

            if (otherLayer == enemyLayer)
            {
                Debug.Log("Bullet hit enemy");
                var wander = other.GetComponent<Common_WanderScript>();
                if (wander) wander.Die();

                // Re-enter bullet time
                GameManager.instance.EnterBulletTimeAfterEnemyHit();
            }
            else if (otherLayer == environmentLayer)
            {
                Debug.Log("Bullet hit environment");

                // Attempt to find surface normal by raycasting from lastPosition
                Vector3 normal = Vector3.up;
                Vector3 dir = (transform.position - lastPosition).normalized;
                float dist = Vector3.Distance(lastPosition, transform.position);

                if (Physics.Raycast(lastPosition, dir, out RaycastHit hitInfo, dist + 0.2f,
                                    1 << otherLayer, QueryTriggerInteraction.Ignore))
                {
                    normal = hitInfo.normal;
                }

                bool success = TryRicochet(normal);
                if (success)
                {
                    // After each ricochet, also re-enter bullet time
                    GameManager.instance.EnterBulletTimeAfterEnemyHit();
                }
                else
                {
                    // If no ricochet left => game over
                    GameManager.instance.GameOver("No ricochets left!");
                }
            }
        }

        // Cinemachine Input Axis (not used if no direct input needed)
        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            // no direct input needed
        }
    }
}
