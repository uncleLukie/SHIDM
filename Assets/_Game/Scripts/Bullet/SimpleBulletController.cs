using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;

namespace _Game.Scripts.Bullet
{
    public class SimpleBulletController : MonoBehaviour, IInputAxisOwner
    {
        [Header("Speed Settings")]
        public float NormalSpeed = 40f;
        public float BulletTimeSpeed = 2f;

        [Header("Gravity & Distance")]
        public float Gravity = 0.4f;
        public bool ReduceGravityInBulletTime = true;
        public float MaxDistance = 150f;

        [Header("Ricochet")]
        [Tooltip("How many times bullet can bounce off environment before 'GameOver.'")]
        public int RicochetCount = 1;

        [Header("Flight Time Limit")]
        [Tooltip("If bullet travels longer than this in real time, we end the bullet. <= 0 => no limit.")]
        public float MaxFlightSeconds = 20f;

        [Header("Events")]
        public UnityEvent OnBulletFired;
        public UnityEvent OnBulletEnd;

        // If you want bullet time re-aim or hooking with an aim controller
        public Action PreUpdate;
        public Action<Vector3, float> PostUpdate;

        private bool _fired;
        private bool _isBulletTime;
        private Vector3 _lastPosition;
        private float _distanceTraveled;
        private float _verticalVelocity;

        // Additional
        private float _flightTimer; // how many seconds since fired

        public bool IsFired => _fired;

        private void OnEnable()
        {
            _fired = false;
            _isBulletTime = false;
            _lastPosition = transform.position;
            _distanceTraveled = 0f;
            _verticalVelocity = 0f;
            _flightTimer = 0f;
        }

        private void Update()
        {
            PreUpdate?.Invoke();

            if (!_fired)
            {
                PostUpdate?.Invoke(Vector3.zero, 1f);
                return;
            }

            // Flight time check
            _flightTimer += Time.deltaTime;
            if (MaxFlightSeconds > 0 && _flightTimer >= MaxFlightSeconds)
            {
                // If bullet is in flight too long => game over
                EndBullet("Flight time exceeded");
                // or call game manager => game over
                return;
            }

            // Decide forward speed
            float speed = _isBulletTime ? BulletTimeSpeed : NormalSpeed;

            // Gravity factor
            float currentGravity = Gravity;
            if (ReduceGravityInBulletTime && _isBulletTime)
            {
                currentGravity = 0f;
            }

            // Accumulate vertical velocity
            _verticalVelocity -= currentGravity * Time.deltaTime;

            // Move
            Vector3 moveFrame = (transform.forward * speed + Vector3.up * _verticalVelocity) * Time.deltaTime;
            transform.position += moveFrame;

            // Distance limit
            float distanceThisFrame = (transform.position - _lastPosition).magnitude;
            _distanceTraveled += distanceThisFrame;
            _lastPosition = transform.position;

            if (MaxDistance > 0 && _distanceTraveled >= MaxDistance)
            {
                EndBullet("Distance limit reached");
                return;
            }

            // Post update
            Vector3 localVel = Quaternion.Inverse(transform.rotation)
                * (transform.forward * speed + Vector3.up * _verticalVelocity);
            PostUpdate?.Invoke(localVel, 1f);
        }

        public void FireBullet()
        {
            if (_fired) return;
            _fired = true;
            _distanceTraveled = 0f;
            _lastPosition = transform.position;
            _verticalVelocity = 0f;
            _flightTimer = 0f;

            OnBulletFired?.Invoke();
        }

        public void EnterBulletTime() => _isBulletTime = true;
        public void ExitBulletTime()  => _isBulletTime = false;

        public void EndBullet(string reason)
        {
            Debug.Log($"Bullet ended: {reason}");
            OnBulletEnd?.Invoke();

            _fired = false;
            gameObject.SetActive(false);
        }

        public void ReFireInCurrentAimDirection()
        {
            _verticalVelocity = 0f;
            Transform aimCore = transform.Find("Player Aiming Core");
            if (aimCore)
                transform.rotation = aimCore.rotation;
        }

        /// <summary>
        /// Attempt a ricochet off the given hitNormal. 
        /// If we have ricochets left, reflect the bullet's forward direction across the normal,
        /// reduce RicochetCount by 1, reset vertical velocity if desired. Return true.
        /// If no ricochets left, return false.
        /// </summary>
        public bool TryRicochet(Vector3 hitNormal)
        {
            if (RicochetCount <= 0)
            {
                // can't bounce
                return false;
            }

            // Decrement ricochet
            RicochetCount--;

            // Reflect bullet forward vector about hitNormal
            // reflect(incident, normal) = incident - 2*(incident dot normal)*normal
            Vector3 incident = transform.forward;
            Vector3 reflect = Vector3.Reflect(incident, hitNormal);

            // We'll maintain same orientation, just new forward
            // Possibly keep bullet "up" the same? For simplicity we do:
            transform.forward = reflect.normalized;

            // Optionally reset vertical velocity
            // so that the bullet doesn't keep diving
            _verticalVelocity = 0f;

            Debug.Log($"Ricochet! Count left: {RicochetCount}");
            return true;
        }

        // Implementation for Cinemachine Input Axis Owner
        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes) { }
    }
}
