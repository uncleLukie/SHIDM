using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;

namespace _Game.Scripts.Bullet
{
    /// <summary>
    /// Parent bullet script on the BulletPlayer root:
    /// - Moves forward once fired.
    /// - Applies a slight gravity drop (e.g. Gravity=1.5).
    /// - Has max distance limit.
    /// - Provides public EndBullet() method so child collision script can end it.
    /// - No collision code here, because the mesh child "PistolBulletCollisionProxy" handles that.
    /// </summary>
    public class SimpleBulletController : MonoBehaviour, IInputAxisOwner
    {
        [Header("Speed Settings")]
        public float NormalSpeed = 40f;
        public float BulletTimeSpeed = 2f;

        [Header("Gravity & Distance")]
        [Tooltip("Set this to a small value (e.g. 1 or 1.5) for slight bullet drop.")]
        public float Gravity = 0.4f;

        [Tooltip("If true, gravity is zero (or partial) in bullet time.")]
        public bool ReduceGravityInBulletTime = true;

        [Tooltip("How far the bullet can travel before disabling. <= 0 => no limit.")]
        public float MaxDistance = 150f;

        [Header("Events")]
        public UnityEvent OnBulletFired;
        public UnityEvent OnBulletEnd;

        // If you want bullet time re-aim or hooking with an aim controller
        public Action PreUpdate;
        public Action<Vector3, float> PostUpdate;

        // Internal
        private bool _fired;
        private bool _isBulletTime;
        private Vector3 _lastPosition;
        private float _distanceTraveled;
        private float _verticalVelocity;

        /// <summary>
        /// Is the bullet currently in flight?
        /// </summary>
        public bool IsFired => _fired;

        private void OnEnable()
        {
            // Reset
            _fired = false;
            _isBulletTime = false;
            _lastPosition = transform.position;
            _distanceTraveled = 0f;
            _verticalVelocity = 0f;
        }

        private void Update()
        {
            PreUpdate?.Invoke();

            if (!_fired)
            {
                PostUpdate?.Invoke(Vector3.zero, 1f);
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

            // Accumulate vertical velocity (slight drop)
            _verticalVelocity -= currentGravity * Time.deltaTime;

            // Move
            Vector3 moveFrame = (transform.forward * speed + Vector3.up * _verticalVelocity) * Time.deltaTime;
            transform.position += moveFrame;

            // Distance limit
            _distanceTraveled += (transform.position - _lastPosition).magnitude;
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

            OnBulletFired?.Invoke();
        }

        public void EnterBulletTime() => _isBulletTime = true;
        public void ExitBulletTime()  => _isBulletTime = false;

        // Called from the child collision script if environment is hit,
        // or from anywhere if you want to forcibly end the bullet.
        public void EndBullet(string reason)
        {
            Debug.Log($"Bullet ended: {reason}");
            OnBulletEnd?.Invoke();

            // Option A: disable
            _fired = false;
            gameObject.SetActive(false);

            // Option B: or Destroy(gameObject);
        }

        public void ReFireInCurrentAimDirection()
        {
            // If you want to reset vertical velocity
            _verticalVelocity = 0f;

            // Snap to "Player Aiming Core" if desired
            Transform aimCore = transform.Find("Player Aiming Core");
            if (aimCore)
                transform.rotation = aimCore.rotation;
        }

        // Implementation for Cinemachine Input Axis Owner 
        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {  /* no input needed if we only do forward bullet. */ }
    }
}
