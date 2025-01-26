using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;

namespace _Game.Scripts.Bullet
{
    /// <summary>
    /// A simplified bullet controller that does NOT read WASD or user inputs
    /// for steering. Instead, it travels forward once fired, and can optionally
    /// be re-aimed at bullet time or by other means.
    /// 
    /// It still supports:
    ///  - Normal speed vs. bullet-time speed
    ///  - Hooking into SimpleBulletAimController (PreUpdate/PostUpdate)
    ///  - A "ReFireInCurrentAimDirection()" method that re-aims the bullet at the
    ///    "Player Aiming Core" transform, then sets velocity to NormalSpeed
    /// </summary>
    public class SimpleBulletController : MonoBehaviour, IInputAxisOwner
    {
        [Header("Speed Settings")]
        [Tooltip("Speed when in normal mode.")]
        public float NormalSpeed = 20f;

        [Tooltip("Speed when in bullet-time.")]
        public float BulletTimeSpeed = 5f;

        [Header("Other Settings")]
        [Tooltip("Event fired when bullet is first launched.")]
        public UnityEvent OnBulletFired;

        // For hooking into an aim controller if desired
        public Action PreUpdate;
        public Action<Vector3, float> PostUpdate;

        private bool _fired;          // True once bullet is launched
        private bool _isBulletTime;   // True if bullet-time is active
        private Vector3 _currentVelocity; 

        /// <summary>
        /// We keep this so the aim controller can know if the bullet is "moving."
        /// (Used in CoupledWhenMoving mode, for instance.)
        /// </summary>
        public bool IsMoving => _currentVelocity.sqrMagnitude > 0.01f;

        private void OnEnable()
        {
            _fired = false;
            _isBulletTime = false;
            _currentVelocity = Vector3.zero;
        }

        private void Update()
        {
            // Let any aim controller do pre-update logic
            PreUpdate?.Invoke();

            if (!_fired)
            {
                // Bullet hasn't been fired yet
                PostUpdate?.Invoke(Vector3.zero, 1f);
                return;
            }

            // If bullet is in bullet time, it uses BulletTimeSpeed, else NormalSpeed
            float speed = _isBulletTime ? BulletTimeSpeed : NormalSpeed;

            // Move the bullet forward by current velocity
            // But if we want the bullet to keep the same direction, we can
            // either forcibly keep the bullet oriented in that same transform.forward,
            // or let velocity define our direction. 
            // For simplicity, we'll treat velocity as "transform.forward * speed"
            // every frame. That means if you want to do physics-like flight, you'd expand logic. 
            _currentVelocity = transform.forward * speed;

            // Move 
            transform.position += _currentVelocity * Time.deltaTime;

            // Post-update: we pass local velocity so the aim controller can see it if needed
            var localVel = Quaternion.Inverse(transform.rotation) * _currentVelocity;
            PostUpdate?.Invoke(localVel, 1f);
        }

        /// <summary>
        /// Launch the bullet the first time, setting _fired = true and 
        /// velocity in transform.forward direction at NormalSpeed
        /// </summary>
        public void FireBullet()
        {
            if (_fired) return;  // don't refire if already launched
            _fired = true;
            // Set initial forward velocity
            transform.rotation = transform.rotation;  // optional sanity
            _currentVelocity = transform.forward * NormalSpeed;

            OnBulletFired?.Invoke();
        }

        /// <summary>
        /// Switch to bullet-time mode (slowed speed). 
        /// </summary>
        public void EnterBulletTime() 
            => _isBulletTime = true;

        /// <summary>
        /// Switch out of bullet-time mode (normal speed). 
        /// </summary>
        public void ExitBulletTime()  
            => _isBulletTime = false;

        /// <summary>
        /// Called after the user has re-aimed the bullet while in bullet time, 
        /// and we want to snap the bullet orientation + velocity to that new direction,
        /// then go at NormalSpeed (or bullet time speed, if you prefer).
        /// </summary>
        public void ReFireInCurrentAimDirection()
        {
            // 1) Find the child "Player Aiming Core"
            var aimingCore = transform.Find("Player Aiming Core");
            if (!aimingCore)
            {
                Debug.LogWarning("ReFireInCurrentAimDirection(): 'Player Aiming Core' not found under bullet!");
                return;
            }

            // 2) Snap the bullet's rotation to match the aiming core
            transform.rotation = aimingCore.rotation;

            // 3) Decide which speed you want after user re-aims. If you want normal speed:
            _currentVelocity = transform.forward * NormalSpeed;

            // If you want it to remain bullet-time speed until time is returned to normal,
            // do: 
            // _currentVelocity = transform.forward * (_isBulletTime ? BulletTimeSpeed : NormalSpeed);
        }

        /// <summary>
        /// We do not actually use MoveX/MoveZ for steering now,
        /// but we keep these fields in case SimpleBulletAimController or 
        /// Cinemachine input expects them.  If you don't need them at all,
        /// remove them and remove the IInputAxisOwner interface.
        /// </summary>
        public InputAxis MoveX = InputAxis.DefaultMomentary;
        public InputAxis MoveZ = InputAxis.DefaultMomentary;

        public void GetInputAxes(List<IInputAxisOwner.AxisDescriptor> axes)
        {
            // If you truly have no intention of reading user input at all,
            // you can remove this entire method. 
            // For now, we keep it to maintain Cinemachine sample structure. 
        }
    }
}
