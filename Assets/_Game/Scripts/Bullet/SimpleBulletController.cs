using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using _Game.Scripts.Managers;
using Polyperfect.Common;
using Unity.Cinemachine;
using UnityEngine.Serialization;

namespace _Game.Scripts.Bullet
{
    [RequireComponent(typeof(Collider))]
    public class SimpleBulletController : MonoBehaviour, IInputAxisOwner
    {
        [Header("Layer Settings")]
        private int enemyLayer;
        private int environmentLayer;

        [FormerlySerializedAs("NormalSpeed")] [Header("Speed Settings")]
        public float normalSpeed = 40f;
        [FormerlySerializedAs("BulletTimeSpeed")] public float bulletTimeSpeed = 2f;

        [FormerlySerializedAs("Gravity")] [Header("Gravity & Distance")]
        public float gravity = 0.4f;
        [FormerlySerializedAs("ReduceGravityInBulletTime")] public bool reduceGravityInBulletTime = true;
        [FormerlySerializedAs("MaxDistance")] public float maxDistance = 150f;

        [FormerlySerializedAs("RicochetCount")] [Header("Ricochet")]
        public int ricochetCount = 1;

        [FormerlySerializedAs("MaxFlightSeconds")] [Header("Flight Time Limit")]
        public float maxFlightSeconds = 20f;

        [FormerlySerializedAs("OnBulletFired")] [Header("Events")]
        public UnityEvent onBulletFired;
        [FormerlySerializedAs("OnBulletEnd")] public UnityEvent onBulletEnd;

        public Action PreUpdate;
        public Action<Vector3, float> PostUpdate;

        private bool fired;
        private bool isBulletTime;
        private Vector3 lastPosition;
        private float distanceTraveled;
        private float verticalVelocity;
        private float flightTimer;

        public bool IsFired => fired;

        private void Awake()
        {
            // Assign layer numbers
            enemyLayer = LayerMask.NameToLayer("Enemy");
            environmentLayer = LayerMask.NameToLayer("Environment");

            var coll = GetComponent<Collider>();
            if (coll && !coll.isTrigger)
            {
                coll.isTrigger = true;
                Debug.LogWarning("SimpleBulletController: Collider set to isTrigger=true at runtime!");
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
            float currentGravity = (reduceGravityInBulletTime && isBulletTime) ? 0f : gravity;

            verticalVelocity -= currentGravity * Time.deltaTime;

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

            Vector3 localVel = Quaternion.Inverse(transform.rotation) * (transform.forward * speed + Vector3.up * verticalVelocity);
            PostUpdate?.Invoke(localVel, 1f);
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

        private void EndBullet(string reason)
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
            if (aimCore)
                transform.rotation = aimCore.rotation;
        }

        private bool TryRicochet(Vector3 hitNormal)
        {
            if (ricochetCount <= 0)
            {
                return false;
            }

            ricochetCount--;

            Vector3 reflect = Vector3.Reflect(transform.forward, hitNormal);
            transform.forward = reflect.normalized;
            verticalVelocity = 0f;

            Debug.Log($"Ricochet! Count left: {ricochetCount}");
            return true;
        }

        private void OnTriggerEnter(Collider other)
        {
            int otherLayer = other.gameObject.layer;

            if (otherLayer == enemyLayer)
            {
                Debug.Log("Bullet hit enemy");

                var wander = other.GetComponent<Common_WanderScript>();
                wander?.Die();

                GameManager.instance.EnterBulletTimeAfterEnemyHit();
            }
            else if (otherLayer == environmentLayer)
            {
                Debug.Log("Bullet hit environment");

                Vector3 normal = Vector3.up;
                Vector3 dir = (transform.position - lastPosition).normalized;
                float dist = Vector3.Distance(lastPosition, transform.position);

                if (Physics.Raycast(lastPosition, dir, out RaycastHit hitInfo, dist + 0.2f,
                                    1 << otherLayer, QueryTriggerInteraction.Ignore))
                {
                    normal = hitInfo.normal;
                }

                if (!TryRicochet(normal))
                {
                    GameManager.instance.GameOver("No ricochets left!");
                }
            }
        }

        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes) { }
    }
}
