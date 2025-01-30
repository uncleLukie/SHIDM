using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using Polyperfect.Common;
using _Game.Scripts.Managers;
using Unity.Cinemachine;

namespace _Game.Scripts.Bullet
{
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

        public Action PreUpdate;
        public Action<Vector3, float> PostUpdate;

        private bool fired;
        private bool isBulletTime;
        private Vector3 lastPosition;
        private float distanceTraveled;
        private float verticalVelocity;
        private float flightTimer;

        public bool IsFired => fired;

        // store whether we've already bounced this frame to prevent repeated triggers if we remain inside the environment collider.
        private bool didBounceThisFrame;

        void Awake()
        {
            enemyLayer = LayerMask.NameToLayer("Enemy");
            environmentLayer = LayerMask.NameToLayer("Environment");

            var rb = GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = true;
                rb.useGravity = false; 
            }

            var coll = GetComponent<Collider>();
            if (coll && !coll.isTrigger)
            {
                coll.isTrigger = true;
                Debug.LogWarning("SimpleBulletController: Setting collider isTrigger=true at runtime!");
            }
        }

        void OnEnable()
        {
            fired = false;
            isBulletTime = false;
            lastPosition = transform.position;
            distanceTraveled = 0f;
            verticalVelocity = 0f;
            flightTimer = 0f;
            didBounceThisFrame = false;
        }

        void Update()
        {
            // Let an aim controller run first
            PreUpdate?.Invoke();

            if (!fired)
            {
                PostUpdate?.Invoke(Vector3.zero, 1f);
                return;
            }

            flightTimer += Time.deltaTime;
            if (maxFlightSeconds > 0 && flightTimer >= maxFlightSeconds)
            {
                EndBullet("Flight time exceeded");
                return;
            }

            float speed = isBulletTime ? bulletTimeSpeed : normalSpeed;
            float g = (reduceGravityInBulletTime && isBulletTime) ? 0f : gravity;
            verticalVelocity -= g * Time.deltaTime;

            // Move the bullet
            Vector3 moveFrame = (transform.forward * speed + Vector3.up * verticalVelocity) * Time.deltaTime;
            transform.position += moveFrame;

            float distanceThisFrame = (transform.position - lastPosition).magnitude;
            distanceTraveled += distanceThisFrame;
            lastPosition = transform.position;

            if (maxDistance > 0 && distanceTraveled >= maxDistance)
            {
                EndBullet("Distance limit reached");
                return;
            }

            Vector3 localVel = Quaternion.Inverse(transform.rotation)
                               * (transform.forward * speed + Vector3.up * verticalVelocity);
            PostUpdate?.Invoke(localVel, 1f);

            // Reset didBounceThisFrame each frame so we can bounce again on a *future* collision
            // but never multiple times in the same frame
            didBounceThisFrame = false;
        }

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

        public void EnterBulletTime() => isBulletTime = true;
        public void ExitBulletTime() => isBulletTime = false;

        void EndBullet(string reason)
        {
            Debug.Log($"Bullet ended: {reason}");
            onBulletEnd?.Invoke();
            fired = false;
            gameObject.SetActive(false);
        }

        public void ReFireInCurrentAimDirection()
        {
            verticalVelocity = 0f;
            Transform aimCore = transform.Find("Player Aiming Core");
            if (aimCore) transform.rotation = aimCore.rotation;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!fired) return;
            if (didBounceThisFrame) return; // already bounced once this frame, ignore further triggers

            int layer = other.gameObject.layer;
            if (layer == enemyLayer)
            {
                var wander = other.GetComponent<Common_WanderScript>();
                if (wander) wander.Die();
                GameManager.instance.EnterBulletTimeAfterEnemyHit();
            }
            else if (layer == environmentLayer)
            {
                // attempt reflection
                Vector3 hitNormal = FindSurfaceNormalOfEnvironment(other);
                bool success = TryRicochet(hitNormal);
                didBounceThisFrame = true;

                if (success)
                {
                    // After each ricochet, re-enter bullet time
                    GameManager.instance.EnterBulletTimeAfterEnemyHit();
                }
                else
                {
                    GameManager.instance.GameOver("No ricochets left!");
                }
            }
        }

        Vector3 FindSurfaceNormalOfEnvironment(Collider envCollider)
        {
            // Raycast from lastPosition to current to find hit normal
            Vector3 dir = (transform.position - lastPosition).normalized;
            float dist = Vector3.Distance(lastPosition, transform.position);
            Vector3 normal = Vector3.up;
            if (Physics.Raycast(lastPosition, dir, out RaycastHit hitInfo, dist + 0.2f,
                                1 << environmentLayer, QueryTriggerInteraction.Ignore))
            {
                normal = hitInfo.normal;
                // Move bullet slightly outside the collision plane so we don't stay inside the collider
                transform.position = hitInfo.point + hitInfo.normal * 0.01f;
            }
            return normal;
        }

        bool TryRicochet(Vector3 hitNormal)
        {
            if (ricochetCount <= 0) return false;
            ricochetCount--;

            Vector3 reflect = Vector3.Reflect(transform.forward, hitNormal);
            transform.forward = reflect.normalized;
            verticalVelocity = 0f;

            Debug.Log($"Ricochet! Count left: {ricochetCount}");
            return true;
        }

        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
        }
    }
}
